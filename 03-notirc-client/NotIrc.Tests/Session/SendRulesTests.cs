using NotIrc.Core.Session;
using NotIrc.Tests.Fakes;

namespace NotIrc.Tests.Session;

public class SendRulesTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Text_is_sent_exactly_as_typed()
    {
        await using var h = new SessionHarness();
        var transport = await h.JoinAsync();

        var result = await h.Session.SendAsync("  padded text \n");

        Assert.Equal(SendResult.Sent, result);
        Assert.Equal(Frames.Send("  padded text \n"), transport.Sent[^1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task Empty_or_whitespace_input_sends_nothing_and_raises_no_notice(string text)
    {
        await using var h = new SessionHarness();
        var transport = await h.JoinAsync();

        var result = await h.Session.SendAsync(text);

        Assert.Equal(SendResult.Empty, result);
        Assert.Single(transport.Sent);
        Assert.Empty(h.Entries);
    }

    [Fact]
    public async Task Over_limit_text_is_blocked_and_not_sent()
    {
        await using var h = new SessionHarness();
        var transport = await h.JoinAsync();
        var emoji = string.Concat(Enumerable.Repeat("\U0001F600", 300));

        var result = await h.Session.SendAsync(emoji);

        Assert.Equal(SendResult.TooLong, result);
        Assert.Single(transport.Sent);
    }

    [Fact]
    public async Task Text_of_exactly_1024_bytes_is_sent()
    {
        await using var h = new SessionHarness();
        var transport = await h.JoinAsync();

        var result = await h.Session.SendAsync(new string('a', 1024));

        Assert.Equal(SendResult.Sent, result);
        Assert.Equal(2, transport.Sent.Count);
    }

    [Fact]
    public async Task Text_one_byte_over_is_blocked_then_allowed_once_edited_back_under()
    {
        await using var h = new SessionHarness();
        var transport = await h.JoinAsync();

        var over = await h.Session.SendAsync(new string('a', 1025));
        var under = await h.Session.SendAsync(new string('a', 1024));

        Assert.Equal(SendResult.TooLong, over);
        Assert.Equal(SendResult.Sent, under);
        Assert.Equal(2, transport.Sent.Count);
    }

    [Fact]
    public async Task Sending_while_reconnecting_is_refused_with_a_notice_and_nothing_is_sent()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();
        first.Drop();
        await h.WaitForStateAsync(SessionState.Reconnecting);

        var result = await h.Session.SendAsync("anyone there?");

        Assert.Equal(SendResult.NotJoined, result);
        Assert.Equal(NoticeKind.NotConnected, Assert.IsType<NoticeEntry>(Assert.Single(h.Entries)).Kind);
        Assert.Single(first.Sent);
    }

    [Fact]
    public async Task A_refused_message_is_not_sent_after_rejoining()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.WaitForStateAsync(SessionState.Reconnecting);
        await h.Session.SendAsync("anyone there?");

        await h.Time.AdvanceAfterNextDelayAsync(Second);
        await Wait.ForSentAsync(second, 1);
        second.Receive(Frames.JoinOk());
        await Wait.UntilAsync(() => h.Statuses.Count(s => s.State == SessionState.Joined) == 2, "second join");
        await Wait.SettleAsync();

        Assert.Equal(Frames.Join("emmz"), Assert.Single(second.Sent));
    }

    [Fact]
    public async Task Send_on_a_connection_that_just_dropped_is_refused_not_thrown()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();
        first.Drop();

        var result = await h.Session.SendAsync("racing the drop");

        Assert.Equal(SendResult.NotJoined, result);
    }

    [Fact]
    public async Task Server_too_long_rejection_is_surfaced_and_the_connection_stays()
    {
        await using var h = new SessionHarness();
        var transport = await h.JoinAsync();

        transport.Receive(Frames.MessageTooLong());
        await h.WaitForEntriesAsync(1);

        Assert.Equal(NoticeKind.MessageRejected, Assert.IsType<NoticeEntry>(Assert.Single(h.Entries)).Kind);
        Assert.Equal(SessionState.Joined, h.Session.Status.State);
        Assert.Equal(SendResult.Sent, await h.Session.SendAsync("shorter"));
    }
}

public class SendRaceTests
{
    [Fact]
    public async Task Sending_after_the_session_is_disposed_is_refused_not_thrown()
    {
        var h = new SessionHarness("emmz");
        await h.JoinAsync();
        await h.DisposeAsync();

        var result = await h.Session.SendAsync("too late");

        Assert.Equal(SendResult.NotJoined, result);
    }

    [Fact]
    public async Task A_second_nickname_request_while_a_prompt_is_open_is_ignored()
    {
        var time = new TrackingTimeProvider();
        var factory = new ScriptedTransportFactory(time);
        var answer = new TaskCompletionSource<string?>();
        var prompts = 0;
        Task<string?> Prompt(string _) =>
            Interlocked.Increment(ref prompts) == 1 ? Task.FromResult<string?>(null) : answer.Task;
        await using var session = new ChannelSession(factory, new SessionOptions(SessionHarness.Url, "emmz"), Prompt, time);
        var transport = factory.EnqueueConnection();
        session.Start();
        await Wait.ForSentAsync(transport, 1);
        transport.Receive(Frames.JoinNickTaken());
        await Wait.UntilAsync(() => session.Status.State == SessionState.NotJoined, "not joined");

        var first = session.RequestNicknameAsync();
        await Wait.UntilAsync(() => Volatile.Read(ref prompts) == 2, "the second prompt to open");
        await session.RequestNicknameAsync(); // a repeated click while the prompt is open

        Assert.Equal(2, Volatile.Read(ref prompts));
        answer.SetResult("emmz2");
        await first;
        await Wait.ForSentAsync(transport, 2);
        Assert.Equal(Frames.Join("emmz2"), transport.Sent[1]);
        Assert.Equal(2, Volatile.Read(ref prompts));
    }
}
