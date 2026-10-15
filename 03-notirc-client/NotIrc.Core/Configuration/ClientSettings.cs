namespace NotIrc.Core.Configuration;

/// <summary>Validated launch settings. The connection URL carries the token, so it is never printed.</summary>
public sealed class ClientSettings
{
    internal ClientSettings(Uri connectionUrl, string nickname)
    {
        ConnectionUrl = connectionUrl;
        Nickname = nickname;
    }

    /// <summary>The full <c>ws(s)://host/ws?token=…</c> URL. Pass it to the transport only; never display it.</summary>
    public Uri ConnectionUrl { get; }

    public string Nickname { get; }

    /// <summary>The URL with the token query removed, safe for status text and logs.</summary>
    public string DisplayAddress => ConnectionUrl.GetLeftPart(UriPartial.Path);

    public override string ToString() => $"{Nickname} @ {DisplayAddress}";
}

/// <summary>Either valid settings, or one error message per missing or invalid setting.</summary>
public sealed class SettingsResult
{
    internal SettingsResult(ClientSettings? settings, IReadOnlyList<string> errors)
    {
        Settings = settings;
        Errors = errors;
    }

    public ClientSettings? Settings { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Settings is not null;
}
