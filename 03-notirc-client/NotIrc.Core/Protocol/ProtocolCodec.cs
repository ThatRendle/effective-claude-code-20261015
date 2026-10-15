using System.Text.Json;

namespace NotIrc.Core.Protocol;

/// <summary>Encodes client messages and decodes server messages as the JSON text frames in NOTIRC.md.</summary>
public static class ProtocolCodec
{
    public static string Encode(ClientMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return message switch
        {
            JoinRequest join => JsonSerializer.Serialize(new { type = "join", nick = join.Nick }),
            SendTextRequest send => JsonSerializer.Serialize(new { type = "message", text = send.Text }),
            _ => throw new ArgumentException($"Unsupported client message {message.GetType().Name}.", nameof(message)),
        };
    }

    /// <summary>
    /// Decodes one text frame. Anything the client should not act on yields <see cref="IgnoredMessage"/>
    /// rather than an exception, because the server is authoritative and may add types later.
    /// </summary>
    public static ServerMessage Decode(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using var document = JsonDocument.Parse(json);
            return Decode(document.RootElement);
        }
        catch (JsonException)
        {
            return IgnoredMessage.Instance;
        }
    }

    private static ServerMessage Decode(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || GetString(root, "type") is not { } type)
        {
            return IgnoredMessage.Instance;
        }

        return type switch
        {
            "join_ok" => DecodeJoinOk(root),
            "join_error" => GetString(root, "reason") is { } reason ? new JoinError(reason) : IgnoredMessage.Instance,
            "user_joined" => GetString(root, "nick") is { } nick ? new UserJoined(nick) : IgnoredMessage.Instance,
            "user_left" => GetString(root, "nick") is { } nick ? new UserLeft(nick) : IgnoredMessage.Instance,
            "message" => GetString(root, "nick") is { } sender && GetString(root, "text") is { } text
                ? new ChannelMessage(sender, text)
                : IgnoredMessage.Instance,
            "message_error" => GetString(root, "reason") is { } reason ? new MessageError(reason) : IgnoredMessage.Instance,
            _ => IgnoredMessage.Instance,
        };
    }

    private static ServerMessage DecodeJoinOk(JsonElement root)
    {
        if (!root.TryGetProperty("users", out var users) || users.ValueKind != JsonValueKind.Array)
        {
            return IgnoredMessage.Instance;
        }

        var nicks = new List<string>();
        foreach (var user in users.EnumerateArray())
        {
            if (user.ValueKind != JsonValueKind.String)
            {
                return IgnoredMessage.Instance;
            }

            nicks.Add(user.GetString()!);
        }

        return new JoinOk(nicks);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
