using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NotIrc.Core.Protocol;
using NotIrc.Core.Session;

namespace NotIrc.App.ViewModels;

/// <summary>Binds the main window to a <see cref="ChannelSession"/>. Session events arrive on any thread and are posted to the UI thread.</summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ChannelSession _session;
    private readonly Action<Action> _post;
    private string? _lastSent;

    /// <param name="session">The session to show. Subscribe before calling its <c>Start</c>.</param>
    /// <param name="serverAddress">Shown in the status bar. Must not contain the token.</param>
    /// <param name="post">Runs an action on the UI thread, in order.</param>
    public MainViewModel(ChannelSession session, string serverAddress, Action<Action> post)
    {
        _session = session;
        _post = post;
        ServerAddress = serverAddress;
        _nickname = session.Nickname;
        _status = session.Status;

        foreach (var nick in session.Presence)
        {
            Presence.Add(nick);
        }

        session.StatusChanged += HandleSessionStatus;
        session.EntryAdded += HandleSessionEntry;
        session.PresenceChanged += HandleSessionPresence;
    }

    public string ServerAddress { get; }

    /// <summary>Every message and notice, in arrival order. Never trimmed.</summary>
    public ObservableCollection<EntryViewModel> Entries { get; } = [];

    /// <summary>Who is in the channel, including the user.</summary>
    public ObservableCollection<string> Presence { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(IsStatusOk), nameof(IsStatusBusy), nameof(IsStatusBad), nameof(IsPresenceStale), nameof(CanSend), nameof(ShowChooseNickname), nameof(ErrorText))]
    private SessionStatus _status;

    [ObservableProperty]
    private string _nickname;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ByteCount), nameof(ByteCounterText), nameof(IsOverLimit), nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _inputText = "";

    public string StatusText => Status.State switch
    {
        SessionState.Connecting => "Connecting…",
        SessionState.Joined => "Joined",
        SessionState.Reconnecting => "Reconnecting…",
        SessionState.NotJoined => "Connected, not joined",
        SessionState.Stopped => Status.StopReason == StopReason.TokenRejected ? "Disconnected: token rejected" : "Disconnected",
        _ => Status.State.ToString(),
    };

    public bool IsStatusOk => Status.State == SessionState.Joined;

    public bool IsStatusBad => Status.State == SessionState.Stopped;

    public bool IsStatusBusy => !IsStatusOk && !IsStatusBad;

    /// <summary>True while the list may be out of date: the connection is lost, and it is only rebuilt on the next join.</summary>
    public bool IsPresenceStale => Status.State is SessionState.Reconnecting or SessionState.Stopped;

    /// <summary>The permanent-failure message, or null while the client is still trying.</summary>
    public string? ErrorText => Status is { State: SessionState.Stopped, StopReason: StopReason.TokenRejected }
        ? "The server rejected the token, so the client has stopped. Fix the token (--token, NOTIRC_TOKEN or notirc.json) and restart."
        : null;

    public int ByteCount => MessageLimit.ByteCount(InputText);

    public string ByteCounterText => $"{ByteCount}/{MessageLimit.MaxBytes}";

    public bool IsOverLimit => ByteCount > MessageLimit.MaxBytes;

    /// <summary>Whether the Send button is enabled: joined, with non-empty text within the limit.</summary>
    public bool CanSend => Status.State == SessionState.Joined && CanSubmit();

    public bool ShowChooseNickname => Status.State == SessionState.NotJoined;

    /// <summary>
    /// Enter key and Send button. Executable whenever there is text within the limit, even while not joined,
    /// so submitting then shows the "not connected" notice and keeps the text; the Send button itself is gated on <see cref="CanSend"/>.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SendAsync()
    {
        var text = InputText;
        _lastSent = text;
        if (await _session.SendAsync(text) == SendResult.Sent)
        {
            InputText = "";
        }
    }

    [RelayCommand]
    private Task ChooseNicknameAsync() => _session.RequestNicknameAsync();

    public void Dispose()
    {
        _session.StatusChanged -= HandleSessionStatus;
        _session.EntryAdded -= HandleSessionEntry;
        _session.PresenceChanged -= HandleSessionPresence;
    }

    private bool CanSubmit() => !IsOverLimit && !string.IsNullOrWhiteSpace(InputText);

    private void HandleSessionStatus(SessionStatus status) => _post(() =>
    {
        Status = status;
        Nickname = _session.Nickname;
    });

    private void HandleSessionPresence(IReadOnlyList<string> presence) => _post(() =>
    {
        Nickname = _session.Nickname;
        Presence.Clear();
        foreach (var nick in presence)
        {
            Presence.Add(nick);
        }
    });

    private void HandleSessionEntry(ChatEntry entry) => _post(() =>
    {
        Entries.Add(EntryViewModel.From(entry, _session.Nickname));

        // The server rejected what we just sent: put the text back, unless the user has already started something new.
        if (entry is NoticeEntry { Kind: NoticeKind.MessageRejected } && _lastSent is not null && InputText.Length == 0)
        {
            InputText = _lastSent;
        }
    });
}
