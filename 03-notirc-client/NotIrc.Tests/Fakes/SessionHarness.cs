using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using NotIrc.Core.Protocol;
using NotIrc.Core.Session;

namespace NotIrc.Tests.Fakes;

/// <summary>A fake clock that counts the timers created on it, so a test can wait until the code under test is sleeping.</summary>
internal sealed class TrackingTimeProvider : FakeTimeProvider
{
    private int _timersCreated;
    private int _timersConsumed;

    public override System.Threading.ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        // Count only once the timer is registered with the fake clock, otherwise a test that sees the
        // count can advance time before the timer exists and the timer would never fire.
        var timer = base.CreateTimer(callback, state, dueTime, period);
        Interlocked.Increment(ref _timersCreated);
        return timer;
    }

    /// <summary>Waits for the code under test to start its next delay, then advances the clock by <paramref name="amount"/>.</summary>
    public async Task AdvanceAfterNextDelayAsync(TimeSpan amount)
    {
        await Wait.UntilAsync(() => Volatile.Read(ref _timersCreated) > _timersConsumed, "a delay to start");
        _timersConsumed++;
        Advance(amount);
    }

    /// <summary>Waits for the next delay to start but does not advance, so a test can inspect the state while it sleeps.</summary>
    public async Task WaitForNextDelayAsync()
    {
        await Wait.UntilAsync(() => Volatile.Read(ref _timersCreated) > _timersConsumed, "a delay to start");
    }

    /// <summary>Advances a delay that <see cref="WaitForNextDelayAsync"/> already saw.</summary>
    public void AdvanceSeenDelay(TimeSpan amount)
    {
        _timersConsumed++;
        Advance(amount);
    }
}

internal static class Wait
{
    /// <summary>Polls in real time until <paramref name="condition"/> holds. Only used to wait for in-memory progress.</summary>
    public static async Task UntilAsync(Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Timed out waiting for {description}.");
            }

            await Task.Delay(1);
        }
    }

    public static Task ForSentAsync(ScriptedTransport transport, int count) =>
        UntilAsync(() => transport.Sent.Count >= count, $"{count} frames sent (have {transport.Sent.Count})");

    /// <summary>Gives the code under test a moment to act, for asserting that something did not happen.</summary>
    public static Task SettleAsync() => Task.Delay(50);
}

/// <summary>Server frames as the notIRC server would send them.</summary>
internal static class Frames
{
    public static string JoinOk(params string[] users) => Json(new { type = "join_ok", users });

    public static string JoinNickTaken() => Json(new { type = "join_error", reason = "nick_taken" });

    public static string UserJoined(string nick) => Json(new { type = "user_joined", nick });

    public static string UserLeft(string nick) => Json(new { type = "user_left", nick });

    public static string Message(string nick, string text) => Json(new { type = "message", nick, text });

    public static string MessageTooLong() => Json(new { type = "message_error", reason = "too_long" });

    public static string Unknown() => Json(new { type = "typing", nick = "ann" });

    /// <summary>The frame the client sends for a join.</summary>
    public static string Join(string nick) => ProtocolCodec.Encode(new JoinRequest(nick));

    /// <summary>The frame the client sends for a chat message.</summary>
    public static string Send(string text) => ProtocolCodec.Encode(new SendTextRequest(text));

    private static string Json(object value) => JsonSerializer.Serialize(value);
}

/// <summary>A session wired to fakes, recording everything it reports.</summary>
internal sealed class SessionHarness : IAsyncDisposable
{
    public static readonly Uri Url = new("ws://test.invalid/ws?token=secret");

    private readonly Lock _gate = new();
    private readonly List<SessionStatus> _statuses = [];
    private readonly List<ChatEntry> _entries = [];
    private readonly List<IReadOnlyList<string>> _presence = [];
    private readonly List<string> _prompts = [];

    public SessionHarness(string nickname = "emmz")
    {
        Time = new TrackingTimeProvider();
        Factory = new ScriptedTransportFactory(Time);
        Session = new ChannelSession(Factory, new SessionOptions(Url, nickname), PromptAsync, Time);
        Session.StatusChanged += status => { lock (_gate) { _statuses.Add(status); } };
        Session.EntryAdded += entry => { lock (_gate) { _entries.Add(entry); } };
        Session.PresenceChanged += presence => { lock (_gate) { _presence.Add(presence); } };
    }

    public TrackingTimeProvider Time { get; }

    public ScriptedTransportFactory Factory { get; }

    public ChannelSession Session { get; }

    /// <summary>What the nickname prompt answers, in order. A prompt with no answer queued fails the test.</summary>
    public Queue<string?> PromptAnswers { get; } = new();

    public IReadOnlyList<string> Prompts
    {
        get { lock (_gate) { return _prompts.ToList(); } }
    }

    public IReadOnlyList<SessionStatus> Statuses
    {
        get { lock (_gate) { return _statuses.ToList(); } }
    }

    public IReadOnlyList<ChatEntry> Entries
    {
        get { lock (_gate) { return _entries.ToList(); } }
    }

    public IReadOnlyList<IReadOnlyList<string>> PresenceUpdates
    {
        get { lock (_gate) { return _presence.ToList(); } }
    }

    public Task WaitForStateAsync(SessionState state) =>
        Wait.UntilAsync(() => Session.Status.State == state, $"state {state} (is {Session.Status.State})");

    public Task WaitForAttemptsAsync(int attempts) =>
        Wait.UntilAsync(() => Factory.Attempts >= attempts, $"{attempts} connect attempts (have {Factory.Attempts})");

    public Task WaitForEntriesAsync(int count) =>
        Wait.UntilAsync(() => Entries.Count >= count, $"{count} log entries (have {Entries.Count})");

    /// <summary>Starts the session on a fresh connection and completes the first join with the given other users.</summary>
    public async Task<ScriptedTransport> JoinAsync(params string[] otherUsers)
    {
        var transport = Factory.EnqueueConnection();
        Session.Start();
        await Wait.ForSentAsync(transport, 1);
        transport.Receive(Frames.JoinOk(otherUsers));
        await WaitForStateAsync(SessionState.Joined);
        return transport;
    }

    public async ValueTask DisposeAsync() => await Session.DisposeAsync();

    private Task<string?> PromptAsync(string rejected)
    {
        lock (_gate)
        {
            _prompts.Add(rejected);
        }

        return PromptAnswers.Count > 0
            ? Task.FromResult(PromptAnswers.Dequeue())
            : throw new InvalidOperationException($"Unexpected nickname prompt for '{rejected}'.");
    }
}
