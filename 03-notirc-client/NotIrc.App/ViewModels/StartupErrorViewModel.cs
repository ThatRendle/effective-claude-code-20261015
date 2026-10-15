namespace NotIrc.App.ViewModels;

/// <summary>What to show when the settings are missing or invalid and the client will not connect.</summary>
public sealed class StartupErrorViewModel(IReadOnlyList<string> errors)
{
    public string Heading { get; } = "NotIrc can't start";

    public IReadOnlyList<string> Errors { get; } = errors;

    public string Hint { get; } = "Nothing has been connected. Fix the settings above and start the client again; see README.md.";
}
