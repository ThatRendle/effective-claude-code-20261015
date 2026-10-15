using System.Collections;
using Microsoft.Extensions.Configuration;

namespace NotIrc.Core.Configuration;

/// <summary>
/// Reads settings from an optional <c>notirc.json</c>, then <c>NOTIRC_</c> environment variables,
/// then <c>--server</c>/<c>--token</c>/<c>--nick</c> arguments; later sources win.
/// </summary>
public static class SettingsLoader
{
    public const string DefaultFileName = "notirc.json";
    private const string EnvironmentPrefix = "NOTIRC_";

    /// <param name="args">Command-line arguments.</param>
    /// <param name="filePath">Settings file; defaults to <c>notirc.json</c> in the working directory.</param>
    /// <param name="environment">Environment variables; defaults to the process environment.</param>
    public static SettingsResult Load(
        IEnumerable<string> args,
        string? filePath = null,
        IDictionary? environment = null)
    {
        IConfigurationRoot config;
        try
        {
            config = new ConfigurationBuilder()
                .AddJsonFile(Path.GetFullPath(filePath ?? DefaultFileName), optional: true)
                .AddInMemoryCollection(ReadEnvironment(environment ?? Environment.GetEnvironmentVariables()))
                .AddCommandLine(args.ToArray())
                .Build();
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException)
        {
            return new SettingsResult(null, [$"Could not read the settings file: {ex.Message}"]);
        }

        var server = Clean(config["server"]);
        var token = Clean(config["token"]);
        var nick = Clean(config["nick"]);

        var errors = new List<string>();
        if (server is null)
        {
            errors.Add(Missing("server address", "--server", "NOTIRC_SERVER", "server"));
        }

        if (token is null)
        {
            errors.Add(Missing("token", "--token", "NOTIRC_TOKEN", "token"));
        }

        if (nick is null)
        {
            errors.Add(Missing("nickname", "--nick", "NOTIRC_NICK", "nick"));
        }

        Uri? url = null;
        if (server is not null && token is not null && !TryBuildUrl(server, token, out url, out var urlError))
        {
            errors.Add(urlError);
        }

        return errors.Count == 0
            ? new SettingsResult(new ClientSettings(url!, nick!), [])
            : new SettingsResult(null, errors);
    }

    private static IEnumerable<KeyValuePair<string, string?>> ReadEnvironment(IDictionary environment)
    {
        foreach (DictionaryEntry entry in environment)
        {
            if (entry.Key is string key && key.StartsWith(EnvironmentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return new(key[EnvironmentPrefix.Length..], entry.Value as string);
            }
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Missing(string what, string argument, string variable, string fileKey) =>
        $"The {what} is missing. Supply it with {argument}, the {variable} environment variable, " +
        $"or a \"{fileKey}\" entry in {DefaultFileName}.";

    private static bool TryBuildUrl(string server, string token, out Uri? url, out string error)
    {
        url = null;
        var text = server.Contains("://", StringComparison.Ordinal) ? server : "wss://" + server;

        if (!Uri.TryCreate(text, UriKind.Absolute, out var parsed)
            || parsed.Scheme is not ("ws" or "wss")
            || string.IsNullOrEmpty(parsed.Host))
        {
            error = "The server address is not valid. Use a host with an optional port " +
                    "(example.com or localhost:8080), or a full ws:// or wss:// URL.";
            return false;
        }

        url = new UriBuilder(parsed.Scheme, parsed.Host, parsed.IsDefaultPort ? -1 : parsed.Port)
        {
            Path = "/ws",
            Query = "token=" + Uri.EscapeDataString(token),
        }.Uri;
        error = string.Empty;
        return true;
    }
}
