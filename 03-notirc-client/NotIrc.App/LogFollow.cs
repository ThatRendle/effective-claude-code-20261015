namespace NotIrc.App;

/// <summary>Decides whether the message log should jump to a new entry: only if the user is already at the bottom.</summary>
public static class LogFollow
{
    private const double Tolerance = 2;

    /// <param name="offsetY">Current scroll offset.</param>
    /// <param name="viewportHeight">Visible height.</param>
    /// <param name="extentHeight">Total content height before the new entry is laid out.</param>
    public static bool ShouldFollow(double offsetY, double viewportHeight, double extentHeight) =>
        extentHeight <= viewportHeight + Tolerance || offsetY + viewportHeight >= extentHeight - Tolerance;
}
