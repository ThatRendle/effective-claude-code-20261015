using NotIrc.Core.Session;
using NotIrc.Tests.Fakes;

namespace NotIrc.Tests.Session;

public class ConnectAndJoinTests
{
    [Fact]
    public async Task Connects_with_the_configured_url_and_joins_with_the_configured_nickname()
    {
        await using var h = new SessionHarness("emmz");
        var transport = h.Factory.EnqueueConnection();

        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);
        transport.Receive(Frames.JoinOk("ann"));
        await h.WaitForStateAsync(SessionState.Joined);

        Assert.Equal(SessionHarness.Url, Assert.Single(h.Factory.Urls));
        Assert.Equal(Frames.Join("emmz"), Assert.Single(transport.Sent));
        Assert.Equal(
            [SessionState.Connecting, SessionState.Joined],
            h.Statuses.Select(s => s.State));
    }

    [Fact]
    public async Task Status_is_connecting_before_the_join_is_accepted()
    {
        await using var h = new SessionHarness();
        var transport = h.Factory.EnqueueConnection();

        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);

        Assert.Equal(SessionState.Connecting, h.Session.Status.State);
    }

    [Fact]
    public async Task Join_is_the_first_frame_sent()
    {
        await using var h = new SessionHarness();

        var transport = await h.JoinAsync();

        Assert.Equal(Frames.Join("emmz"), transport.Sent[0]);
    }

    [Fact]
    public async Task Chat_cannot_be_sent_before_the_join_is_accepted()
    {
        await using var h = new SessionHarness();
        var transport = h.Factory.EnqueueConnection();
        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);

        var result = await h.Session.SendAsync("too soon");

        Assert.Equal(SendResult.NotJoined, result);
        Assert.Equal(Frames.Join("emmz"), Assert.Single(transport.Sent));
    }

    [Fact]
    public async Task Chat_can_be_sent_once_the_join_is_accepted()
    {
        await using var h = new SessionHarness();
        var transport = await h.JoinAsync();

        var result = await h.Session.SendAsync("hello");

        Assert.Equal(SendResult.Sent, result);
        Assert.Equal(Frames.Send("hello"), transport.Sent[^1]);
    }

    [Fact]
    public async Task Starting_twice_is_an_error()
    {
        await using var h = new SessionHarness();
        h.Factory.EnqueueConnection();
        h.Session.Start();

        Assert.Throws<InvalidOperationException>(h.Session.Start);
    }

    [Fact]
    public async Task Disposing_twice_is_harmless()
    {
        var h = new SessionHarness();
        await h.JoinAsync();

        await h.DisposeAsync();
        await h.DisposeAsync();
    }

    [Fact]
    public async Task Disposing_closes_the_connection()
    {
        var h = new SessionHarness();
        var transport = await h.JoinAsync();

        await h.DisposeAsync();

        Assert.True(transport.Disposed);
    }
}
