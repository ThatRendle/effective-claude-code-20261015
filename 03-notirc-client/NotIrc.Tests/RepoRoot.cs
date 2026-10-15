namespace NotIrc.Tests;

/// <summary>Finds files in the repository root (the folder holding <c>NotIrc.slnx</c>) from the test output folder.</summary>
internal static class RepoRoot
{
    public static string Path { get; } = Find();

    public static string File(string name) => System.IO.Path.Combine(Path, name);

    private static string Find()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir, "NotIrc.slnx")))
        {
            dir = System.IO.Path.GetDirectoryName(dir);
        }

        return dir ?? throw new InvalidOperationException("Could not find NotIrc.slnx above the test output folder.");
    }
}
