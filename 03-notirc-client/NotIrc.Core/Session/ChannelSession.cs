using NotIrc.Core.Protocol;
using NotIrc.Core.Transport;

namespace NotIrc.Core.Session;

/// <summary>
/// Owns the connection lifecycle: connect, join, nickname retry, reconnect and presence. All of it runs
/// on one sequential loop, so state only changes in one place. Events are raised on whichever thread
/// caused them (the loop, or the caller of <see cref="SendAsync"/>), so subscribers marshal as needed.
/// </summary>
public sealed class ChannelSession : IAsyncDisposable
{
    private readonly ITransportFactory _factory;
    private readonly SessionOptions _options;
    private readonly Func<string, Task<string?>> _promptForNickname;
    private readonly TimeProvider _time;
    private readonly CancellationTokenSource _stop = new();
    private readonly Lock _gate = new();
    private readonly SemaphoreSlim _promptGate = new(1, 1);

    private SessionStatus _status = new(SessionState.Connecting);
    private string _nickname;
    private HashSet<string> _presence = [];
    private ITransport? _transport;
    private bool _hasJoined;
    private bool _joinedOnThisConnection;
    private bool _started;
    private bool _disposed;
    private Task? _run;

    /// <param name="promptForNickname">
    /// Called with the rejected nickname when the user must choose another. Returns the new nickname,
    /// or null to decline (the session then stays connected but not joined).
    /// </param>
    public ChannelSession(
        ITransportFactory factory,
        SessionOptions options,
        Func<string, Task<string?>> promptForNickname,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(promptForNickname);

        _factory = factory;
        _options = options;
        _promptForNickname = promptForNickname;
        _time = timeProvider ?? TimeProvider.System;
        _nickname = options.Nickname;
    }

    public event Action<SessionStatus>? StatusChanged;

    public event Action<ChatEntry>? EntryAdded;

    /// <summary>Raised with the full, sorted list of nicknames (including the user's own) after each change.</summary>
    public event Action<IReadOnlyList<string>>? PresenceChanged;

    public SessionStatus Status
    {
        get { lock (_gate) { return _status; } }
    }

    /// <summary>The nickname the session is joined as, or is currently trying to join as.</summary>
    public string Nickname
    {
        get { lock (_gate) { return _nickname; } }
    }

    public IReadOnlyList<string> Presence
    {
        get { lock (_gate) { return Snapshot(); } }
    }

    /// <summary>Begins connecting in the background. Call once.</summary>
    public void Start()
    {
        SessionStatus initial;
        lock (_gate)
        {
            if (_started)
            {
                throw new InvalidOperationException("The session has already been started.");
            }

            _started = true;
            initial = _status;
        }

        // Status changes only fire on a change, so announce the starting status to anyone subscribed,
        // before the loop can report anything later.
        StatusChanged?.Invoke(initial);
        lock (_gate)
        {
            _run = Task.Run(() => RunAsync(_stop.Token));
        }
    }

    /// <summary>
    /// Sends <paramref name="text"/> exactly as given. Nothing is queued: when not joined the text is refused
    /// and the caller keeps it. The message appears in the log only when the server broadcasts it back.
    /// </summary>
    public async Task<SendResult> SendAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            return SendResult.Empty;
        }

        ITransport? transport;
        lock (_gate)
        {
            transport = _status.State == SessionState.Joined ? _transport : null;
        }

        if (transport is null)
        {
            Raise(new NoticeEntry(_time.GetLocalNow(), NoticeKind.NotConnected));
            return SendResult.NotJoined;
        }

        if (!MessageLimit.IsWithinLimit(text))
        {
            return SendResult.TooLong;
        }

        try
        {
            await transport.SendAsync(ProtocolCodec.Encode(new SendTextRequest(text)), _stop.Token).ConfigureAwait(false);
            return SendResult.Sent;
        }
        catch (Exception ex) when (ex is TransportException or ObjectDisposedException or OperationCanceledException)
        {
            // The connection dropped under us, or the session is shutting down (stop token cancelled or disposed);
            // the loop will notice and reconnect.
            Raise(new NoticeEntry(_time.GetLocalNow(), NoticeKind.NotConnected));
            return SendResult.NotJoined;
        }
    }

    /// <summary>Offers the nickname prompt again. Only does anything while connected but not joined.</summary>
    public async Task RequestNicknameAsync()
    {
        ITransport? transport;
        lock (_gate)
        {
            transport = _status.State == SessionState.NotJoined ? _transport : null;
        }

        if (transport is null)
        {
            return;
        }

        try
        {
            await PromptAndJoinAsync(transport, _stop.Token, skipIfBusy: true).ConfigureAwait(false);
        }
        catch (TransportException)
        {
            // The connection dropped while sending the join; the loop sees the same drop and reconnects.
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task? run;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            run = _run;
        }

        await _stop.CancelAsync().ConfigureAwait(false);
        if (run is not null)
        {
            await run.ConfigureAwait(false);
        }

        _stop.Dispose();
    }

    private async Task RunAsync(CancellationToken ct)
    {
        var failedAttempts = 0;
        var firstAttempt = true;
        try
        {
            while (true)
            {
                SetStatus(new SessionStatus(WaitingState()));
                if (!firstAttempt)
                {
                    await Task.Delay(ReconnectPolicy.DelayBefore(failedAttempts), _time, ct).ConfigureAwait(false);
                    failedAttempts++;
                }

                firstAttempt = false;

                ITransport transport;
                try
                {
                    transport = await _factory.ConnectAsync(_options.Url, ct).ConfigureAwait(false);
                }
                catch (UnauthorizedTransportException)
                {
                    SetStatus(new SessionStatus(SessionState.Stopped, StopReason.TokenRejected));
                    return;
                }
                catch (TransportException)
                {
                    continue;
                }

                await using (transport.ConfigureAwait(false))
                {
                    lock (_gate)
                    {
                        _transport = transport;
                        _joinedOnThisConnection = false;
                    }

                    try
                    {
                        await ReadLoopAsync(transport, ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        lock (_gate)
                        {
                            _transport = null;
                        }
                    }
                }

                bool joined;
                lock (_gate)
                {
                    joined = _joinedOnThisConnection;
                }

                if (joined)
                {
                    failedAttempts = 0;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Disposed: the loop simply ends.
        }
    }

    /// <summary>Sends the join, then handles frames until the connection ends.</summary>
    private async Task ReadLoopAsync(ITransport transport, CancellationToken ct)
    {
        // A stale copy of our own nickname may linger briefly after a drop, so a rejoin quietly retries it.
        var quietRetries = _hasJoined ? ReconnectPolicy.QuietNickRetries : 0;
        try
        {
            await SendJoinAsync(transport, ct).ConfigureAwait(false);
            while (await transport.ReceiveAsync(ct).ConfigureAwait(false) is { } frame)
            {
                switch (ProtocolCodec.Decode(frame))
                {
                    case JoinOk ok:
                        OnJoined(ok);
                        break;

                    case JoinError when quietRetries > 0:
                        quietRetries--;
                        await Task.Delay(ReconnectPolicy.QuietNickRetryInterval, _time, ct).ConfigureAwait(false);
                        await SendJoinAsync(transport, ct).ConfigureAwait(false);
                        break;

                    case JoinError:
                        await PromptAndJoinAsync(transport, ct).ConfigureAwait(false);
                        break;

                    case UserJoined joined when IsJoined():
                        OnUserJoined(joined.Nick);
                        break;

                    case UserLeft left when IsJoined():
                        OnUserLeft(left.Nick);
                        break;

                    case ChannelMessage message when IsJoined():
                        Raise(new ChatLine(_time.GetLocalNow(), message.Nick, message.Text));
                        break;

                    case MessageError when IsJoined():
                        Raise(new NoticeEntry(_time.GetLocalNow(), NoticeKind.MessageRejected));
                        break;
                }
            }
        }
        catch (TransportException)
        {
            // The connection broke while sending; leave the loop so the caller reconnects.
        }
    }

    /// <summary>
    /// Asks for a nickname and joins with it. Only one prompt is open at a time: a request made while one is open
    /// is dropped when <paramref name="skipIfBusy"/> is set (a repeated click), otherwise it waits its turn.
    /// </summary>
    private async Task PromptAndJoinAsync(ITransport transport, CancellationToken ct, bool skipIfBusy = false)
    {
        if (skipIfBusy)
        {
            if (!await _promptGate.WaitAsync(0, ct).ConfigureAwait(false))
            {
                return;
            }
        }
        else
        {
            await _promptGate.WaitAsync(ct).ConfigureAwait(false);
        }

        try
        {
            var chosen = await _promptForNickname(Nickname).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(chosen))
            {
                SetStatus(new SessionStatus(SessionState.NotJoined));
                return;
            }

            lock (_gate)
            {
                _nickname = chosen;
            }

            SetStatus(new SessionStatus(WaitingState()));
            await SendJoinAsync(transport, ct).ConfigureAwait(false);
        }
        finally
        {
            _promptGate.Release();
        }
    }

    private Task SendJoinAsync(ITransport transport, CancellationToken ct) =>
        transport.SendAsync(ProtocolCodec.Encode(new JoinRequest(Nickname)), ct);

    private void OnJoined(JoinOk ok)
    {
        bool rejoin;
        IReadOnlyList<string> snapshot;
        lock (_gate)
        {
            rejoin = _hasJoined;
            _hasJoined = true;
            _joinedOnThisConnection = true;
            _presence = new HashSet<string>(ok.Users, StringComparer.Ordinal) { _nickname };
            snapshot = Snapshot();
        }

        SetStatus(new SessionStatus(SessionState.Joined));
        PresenceChanged?.Invoke(snapshot);
        if (rejoin)
        {
            Raise(new NoticeEntry(_time.GetLocalNow(), NoticeKind.MissedMessages));
        }
    }

    private void OnUserJoined(string nick)
    {
        IReadOnlyList<string> snapshot;
        lock (_gate)
        {
            _presence.Add(nick);
            snapshot = Snapshot();
        }

        PresenceChanged?.Invoke(snapshot);
        Raise(new NoticeEntry(_time.GetLocalNow(), NoticeKind.UserJoined, nick));
    }

    private void OnUserLeft(string nick)
    {
        IReadOnlyList<string> snapshot;
        lock (_gate)
        {
            _presence.Remove(nick);
            snapshot = Snapshot();
        }

        PresenceChanged?.Invoke(snapshot);
        Raise(new NoticeEntry(_time.GetLocalNow(), NoticeKind.UserLeft, nick));
    }

    private bool IsJoined()
    {
        lock (_gate)
        {
            return _status.State == SessionState.Joined;
        }
    }

    /// <summary>The status while no join is accepted: "connecting" until the first join, "reconnecting" after.</summary>
    private SessionState WaitingState()
    {
        lock (_gate)
        {
            return _hasJoined ? SessionState.Reconnecting : SessionState.Connecting;
        }
    }

    private void SetStatus(SessionStatus status)
    {
        lock (_gate)
        {
            if (_status == status)
            {
                return;
            }

            _status = status;
        }

        StatusChanged?.Invoke(status);
    }

    private void Raise(ChatEntry entry) => EntryAdded?.Invoke(entry);

    private List<string> Snapshot()
    {
        var list = _presence.ToList();
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }
}
