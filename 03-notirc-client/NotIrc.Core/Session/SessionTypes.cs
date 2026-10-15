namespace NotIrc.Core.Session;

/// <summary>Where the connection stands. Joined is the only state in which messages can be sent.</summary>
public enum SessionState
{
    /// <summary>Connecting or joining for the first time.</summary>
    Connecting,

    /// <summary>The server accepted the join.</summary>
    Joined,

    /// <summary>The connection was lost after a successful join; trying to get back in.</summary>
    Reconnecting,

    /// <summary>Connected, but no nickname has been accepted (the user dismissed the prompt).</summary>
    NotJoined,

    /// <summary>Permanently stopped; see <see cref="SessionStatus.StopReason"/>.</summary>
    Stopped,
}

public enum StopReason
{
    /// <summary>The server answered the connection attempt with HTTP 401.</summary>
    TokenRejected,
}

public sealed record SessionStatus(SessionState State, StopReason? StopReason = null);

public enum NoticeKind
{
    UserJoined,
    UserLeft,

    /// <summary>The client rejoined after a drop; the server keeps no history, so messages may be missing.</summary>
    MissedMessages,

    /// <summary>The server rejected a message the client sent (too long).</summary>
    MessageRejected,

    /// <summary>The user tried to send while not joined.</summary>
    NotConnected,
}

/// <summary>One item in the message log. Timestamps are set by the client when the item arrives.</summary>
public abstract record ChatEntry(DateTimeOffset Timestamp);

public sealed record ChatLine(DateTimeOffset Timestamp, string Nick, string Text) : ChatEntry(Timestamp);

/// <summary>A log item that is not a chat message. <paramref name="Nick"/> is set for join and leave notices.</summary>
public sealed record NoticeEntry(DateTimeOffset Timestamp, NoticeKind Kind, string? Nick = null) : ChatEntry(Timestamp);

public enum SendResult
{
    Sent,

    /// <summary>The text was empty or whitespace; nothing was sent.</summary>
    Empty,

    /// <summary>Not joined (or the connection was lost); nothing was sent or queued.</summary>
    NotJoined,

    /// <summary>Over the 1024-byte limit; nothing was sent.</summary>
    TooLong,
}

/// <summary>What the session needs to know to connect. The URL carries the token, so never log it.</summary>
public sealed record SessionOptions(Uri Url, string Nickname);

/// <summary>Reconnect timing, kept in one place so it is easy to change.</summary>
public static class ReconnectPolicy
{
    /// <summary>Quiet same-nickname retries after a nick-taken rejection on rejoin, before prompting.</summary>
    public const int QuietNickRetries = 3;

    public static readonly TimeSpan QuietNickRetryInterval = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
    ];

    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(15);

    /// <summary>The wait before reconnect attempt <paramref name="failedAttempts"/> (0 for the first): 1s, 2s, 4s, 8s, then 15s.</summary>
    public static TimeSpan DelayBefore(int failedAttempts) =>
        failedAttempts < Delays.Length ? Delays[failedAttempts] : MaxDelay;
}
