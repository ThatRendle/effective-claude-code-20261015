using System.Collections;
using NotIrc.Core.Configuration;

namespace NotIrc.Tests.Configuration;

public sealed class SettingsLoaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("notirc-settings").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string WriteFile(string json)
    {
        var path = Path.Combine(_dir, "notirc.json");
        File.WriteAllText(path, json);
        return path;
    }

    private string MissingFile => Path.Combine(_dir, "absent.json");

    private static Hashtable Env(params (string Key, string Value)[] pairs) =>
        new(pairs.ToDictionary(p => p.Key, p => (object)p.Value));

    private const string FullFile = """{ "server": "file.example.com", "token": "file-token", "nick": "filenick" }""";

    [Fact]
    public void File_only_supplies_all_three_values()
    {
        var result = SettingsLoader.Load([], WriteFile(FullFile), Env());

        var settings = Assert.IsType<ClientSettings>(result.Settings);
        Assert.Equal("filenick", settings.Nickname);
        Assert.Equal(new Uri("wss://file.example.com/ws?token=file-token"), settings.ConnectionUrl);
    }

    [Fact]
    public void Argument_overrides_file_for_the_server_address()
    {
        var result = SettingsLoader.Load(["--server", "arg.example.com"], WriteFile(FullFile), Env());

        Assert.Equal(new Uri("wss://arg.example.com/ws?token=file-token"), result.Settings!.ConnectionUrl);
    }

    [Fact]
    public void Environment_overrides_file_for_the_nickname()
    {
        var result = SettingsLoader.Load([], WriteFile(FullFile), Env(("NOTIRC_NICK", "envnick")));

        Assert.Equal("envnick", result.Settings!.Nickname);
    }

    [Fact]
    public void Argument_overrides_environment_which_overrides_file()
    {
        var env = Env(("NOTIRC_NICK", "envnick"), ("NOTIRC_TOKEN", "env-token"));

        var result = SettingsLoader.Load(["--nick", "argnick"], WriteFile(FullFile), env);

        Assert.Equal("argnick", result.Settings!.Nickname);
        Assert.Equal(new Uri("wss://file.example.com/ws?token=env-token"), result.Settings.ConnectionUrl);
    }

    [Fact]
    public void Works_without_a_settings_file()
    {
        var env = Env(("NOTIRC_SERVER", "h"), ("NOTIRC_TOKEN", "t"), ("NOTIRC_NICK", "n"));

        Assert.True(SettingsLoader.Load([], MissingFile, env).IsValid);
    }

    [Fact]
    public void Ignores_environment_variables_without_the_prefix()
    {
        var result = SettingsLoader.Load([], MissingFile, Env(("SERVER", "h"), ("TOKEN", "t"), ("NICK", "n")));

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void Missing_token_is_reported_with_every_way_to_supply_it()
    {
        var result = SettingsLoader.Load(["--server", "h", "--nick", "n"], MissingFile, Env());

        Assert.Null(result.Settings);
        var error = Assert.Single(result.Errors);
        Assert.Contains("token", error);
        Assert.Contains("--token", error);
        Assert.Contains("NOTIRC_TOKEN", error);
        Assert.Contains("notirc.json", error);
    }

    [Fact]
    public void Reports_every_missing_setting_at_once()
    {
        var result = SettingsLoader.Load([], MissingFile, Env());

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Contains("--server"));
        Assert.Contains(result.Errors, e => e.Contains("--token"));
        Assert.Contains(result.Errors, e => e.Contains("--nick"));
    }

    [Fact]
    public void Blank_values_count_as_missing()
    {
        var result = SettingsLoader.Load(["--server", "h", "--token", "  ", "--nick", "n"], MissingFile, Env());

        Assert.Contains("token", Assert.Single(result.Errors));
    }

    [Theory]
    [InlineData("example.com", "wss://example.com/ws?token=t")]
    [InlineData("example.com:9000", "wss://example.com:9000/ws?token=t")]
    [InlineData("localhost:8080", "wss://localhost:8080/ws?token=t")]
    [InlineData("wss://example.com", "wss://example.com/ws?token=t")]
    [InlineData("wss://example.com/", "wss://example.com/ws?token=t")]
    [InlineData("ws://localhost:8080", "ws://localhost:8080/ws?token=t")]
    [InlineData("ws://localhost", "ws://localhost/ws?token=t")]
    public void Normalises_the_server_address_to_a_connection_url(string server, string expected)
    {
        var result = SettingsLoader.Load(["--server", server, "--token", "t", "--nick", "n"], MissingFile, Env());

        Assert.Equal(new Uri(expected), result.Settings!.ConnectionUrl);
    }

    [Fact]
    public void Local_development_server_keeps_the_non_secure_scheme()
    {
        var result = SettingsLoader.Load(
            ["--server", "ws://localhost:8080", "--token", "any", "--nick", "n"], MissingFile, Env());

        Assert.Equal("ws", result.Settings!.ConnectionUrl.Scheme);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("ws://")]
    public void Rejects_an_unusable_server_address(string server)
    {
        var result = SettingsLoader.Load(["--server", server, "--token", "t", "--nick", "n"], MissingFile, Env());

        Assert.Null(result.Settings);
        Assert.Contains("server address is not valid", Assert.Single(result.Errors));
    }

    [Fact]
    public void Escapes_special_characters_in_the_token()
    {
        var result = SettingsLoader.Load(["--server", "h", "--token", "a&b=c d", "--nick", "n"], MissingFile, Env());

        Assert.Equal("?token=a%26b%3Dc%20d", result.Settings!.ConnectionUrl.Query);
    }

    [Fact]
    public void A_malformed_settings_file_is_reported_not_thrown()
    {
        var result = SettingsLoader.Load([], WriteFile("{ not json"), Env());

        Assert.Null(result.Settings);
        Assert.Contains("settings file", Assert.Single(result.Errors));
    }

    [Fact]
    public void The_token_never_appears_in_display_text_or_error_text()
    {
        const string secret = "s3cret-token";
        var good = SettingsLoader.Load(["--server", "h", "--token", secret, "--nick", "n"], MissingFile, Env());
        var bad = SettingsLoader.Load(["--server", "http://h", "--token", secret, "--nick", "n"], MissingFile, Env());

        Assert.DoesNotContain(secret, good.Settings!.ToString());
        Assert.DoesNotContain(secret, good.Settings.DisplayAddress);
        Assert.All(bad.Errors, e => Assert.DoesNotContain(secret, e));
    }

    [Fact]
    public void The_settings_file_is_ignored_by_version_control()
    {
        Assert.Contains("notirc.json", File.ReadAllLines(RepoRoot.File(".gitignore")).Select(l => l.Trim()));
    }
}

public sealed class DocumentedLaunchTests
{
    [Fact]
    public void The_example_file_loads_with_its_placeholder_values()
    {
        var result = SettingsLoader.Load([], RepoRoot.File("notirc.example.json"), new Hashtable());

        Assert.True(result.IsValid);
        Assert.Equal("your-nickname", result.Settings!.Nickname);
    }

    [Theory]
    [InlineData("--server chat.example.com --token tok --nick emmz", "wss://chat.example.com/ws?token=tok")]
    [InlineData("--server ws://localhost:8080 --token any-token --nick emmz", "ws://localhost:8080/ws?token=any-token")]
    public void The_documented_argument_forms_load(string args, string expectedUrl)
    {
        var result = SettingsLoader.Load(args.Split(' '), RepoRoot.File("absent.json"), new Hashtable());

        Assert.Equal(new Uri(expectedUrl), result.Settings!.ConnectionUrl);
    }
}
