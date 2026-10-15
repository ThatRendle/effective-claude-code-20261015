using NotIrc.Core.Session;
using NotIrc.Tests.Fakes;

namespace NotIrc.Tests.Session;

public class NicknamePromptTests
{
    [Fact]
    public async Task Taken_nickname_prompts_prefilled_and_rejoins_on_the_same_connection()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue("emmz2");
        var transport = h.Factory.EnqueueConnection();
        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);

        transport.Receive(Frames.JoinNickTaken());
        await Wait.ForSentAsync(transport, 2);
        transport.Receive(Frames.JoinOk());
        await h.WaitForStateAsync(SessionState.Joined);

        Assert.Equal(["emmz"], h.Prompts);
        Assert.Equal([Frames.Join("emmz"), Frames.Join("emmz2")], transport.Sent);
        Assert.Equal(1, h.Factory.Attempts);
        Assert.Equal("emmz2", h.Session.Nickname);
    }

    [Fact]
    public async Task Replacement_nickname_also_taken_prompts_again()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue("emmz2");
        h.PromptAnswers.Enqueue("emmz3");
        var transport = h.Factory.EnqueueConnection();
        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);

        transport.Receive(Frames.JoinNickTaken());
        await Wait.ForSentAsync(transport, 2);
        transport.Receive(Frames.JoinNickTaken());
        await Wait.ForSentAsync(transport, 3);
        transport.Receive(Frames.JoinOk());
        await h.WaitForStateAsync(SessionState.Joined);

        Assert.Equal(["emmz", "emmz2"], h.Prompts);
        Assert.Equal([Frames.Join("emmz"), Frames.Join("emmz2"), Frames.Join("emmz3")], transport.Sent);
        Assert.Equal("emmz3", h.Session.Nickname);
    }

    [Fact]
    public async Task Declining_the_prompt_leaves_the_client_connected_but_not_joined()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue(null);
        var transport = h.Factory.EnqueueConnection();
        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);

        transport.Receive(Frames.JoinNickTaken());
        await h.WaitForStateAsync(SessionState.NotJoined);
        var result = await h.Session.SendAsync("hello");

        Assert.Equal(SendResult.NotJoined, result);
        Assert.False(transport.Disposed);
        Assert.Equal(1, h.Factory.Attempts);
        Assert.Equal(Frames.Join("emmz"), Assert.Single(transport.Sent));
    }

    [Fact]
    public async Task Whitespace_answer_counts_as_declining()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue("   ");
        var transport = h.Factory.EnqueueConnection();
        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);

        transport.Receive(Frames.JoinNickTaken());

        await h.WaitForStateAsync(SessionState.NotJoined);
        Assert.Equal(Frames.Join("emmz"), Assert.Single(transport.Sent));
    }

    [Fact]
    public async Task Prompt_is_offered_again_on_request_after_declining()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue(null);
        h.PromptAnswers.Enqueue("emmz2");
        var transport = h.Factory.EnqueueConnection();
        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);
        transport.Receive(Frames.JoinNickTaken());
        await h.WaitForStateAsync(SessionState.NotJoined);

        await h.Session.RequestNicknameAsync();
        await Wait.ForSentAsync(transport, 2);
        transport.Receive(Frames.JoinOk());
        await h.WaitForStateAsync(SessionState.Joined);

        Assert.Equal(["emmz", "emmz"], h.Prompts);
        Assert.Equal("emmz2", h.Session.Nickname);
        Assert.Equal(Frames.Join("emmz2"), transport.Sent[1]);
    }

    [Fact]
    public async Task Requesting_the_prompt_while_joined_does_nothing()
    {
        await using var h = new SessionHarness("emmz");
        await h.JoinAsync();

        await h.Session.RequestNicknameAsync();

        Assert.Empty(h.Prompts);
    }

    [Fact]
    public async Task Not_joined_status_is_reported()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue(null);
        var transport = h.Factory.EnqueueConnection();
        h.Session.Start();
        await Wait.ForSentAsync(transport, 1);

        transport.Receive(Frames.JoinNickTaken());
        await h.WaitForStateAsync(SessionState.NotJoined);

        Assert.Equal([SessionState.Connecting, SessionState.NotJoined], h.Statuses.Select(s => s.State));
    }
}
