namespace NotIrc.Core.Protocol;

/// <summary>A message received from the server.</summary>
public abstract record ServerMessage;

/// <summary>The nickname was accepted. <paramref name="Users"/> excludes the joining client.</summary>
public sealed record JoinOk(IReadOnlyList<string> Users) : ServerMessage;

/// <summary>The nickname was rejected. The connection stays open.</summary>
public sealed record JoinError(string Reason) : ServerMessage
{
    public const string NickTaken = "nick_taken";
}

public sealed record UserJoined(string Nick) : ServerMessage;

public sealed record UserLeft(string Nick) : ServerMessage;

/// <summary>A channel message, broadcast to everyone including the sender.</summary>
public sealed record ChannelMessage(string Nick, string Text) : ServerMessage;

/// <summary>The server rejected the client's message. The connection stays open.</summary>
public sealed record MessageError(string Reason) : ServerMessage
{
    public const string TooLong = "too_long";
}

/// <summary>
/// A frame the client does not act on: an unrecognised type, malformed JSON, or a known type
/// whose fields are missing or of the wrong kind.
/// </summary>
public sealed record IgnoredMessage : ServerMessage
{
    public static IgnoredMessage Instance { get; } = new();
}
