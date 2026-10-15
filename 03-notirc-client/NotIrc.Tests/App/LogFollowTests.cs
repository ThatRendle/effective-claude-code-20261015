using NotIrc.App;

namespace NotIrc.Tests.App;

public sealed class LogFollowTests
{
    [Theory]
    [InlineData(600, 400, 1000, true)] // exactly at the bottom
    [InlineData(599, 400, 1000, true)] // within a pixel or two of the bottom
    [InlineData(0, 400, 1000, false)] // scrolled to the top
    [InlineData(300, 400, 1000, false)] // scrolled up part way
    [InlineData(0, 400, 200, true)] // content shorter than the view
    [InlineData(0, 0, 0, true)] // before first layout
    public void Follows_new_entries_only_when_at_the_bottom(double offset, double viewport, double extent, bool expected) =>
        Assert.Equal(expected, LogFollow.ShouldFollow(offset, viewport, extent));
}
