namespace NotIrc.Core.Protocol;

/// <summary>A message the client sends to the server.</summary>
public abstract record ClientMessage;

/// <summary>Registers the client's nickname. Must be the first message after the connection opens.</summary>
public sealed record JoinRequest(string Nick) : ClientMessage;

/// <summary>Sends text to the channel. Only valid after the server has accepted a join.</summary>
public sealed record SendTextRequest(string Text) : ClientMessage;
