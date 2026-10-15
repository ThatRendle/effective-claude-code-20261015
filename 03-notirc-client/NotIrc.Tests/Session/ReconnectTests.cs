using NotIrc.Core.Session;
using NotIrc.Core.Transport;
using NotIrc.Tests.Fakes;

namespace NotIrc.Tests.Session;

public class ReconnectTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Drop_while_joined_shows_reconnecting_and_attempts_to_reconnect()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();
        var second = h.Factory.EnqueueConnection();

        first.Drop();
        await h.WaitForStateAsync(SessionState.Reconnecting);
        await h.Time.AdvanceAfterNextDelayAsync(Second);
        await h.WaitForAttemptsAsync(2);

        Assert.Equal(2, h.Factory.Attempts);
        Assert.True(first.Disposed);
        await Wait.ForSentAsync(second, 1);
    }

    [Fact]
    public async Task Reconnect_rejoins_with_the_same_nickname_and_returns_to_joined()
    {
        await using var h = new SessionHarness("emmz");
        var first = await h.JoinAsync();
        var second = h.Factory.EnqueueConnection();

        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(Second);
        await Wait.ForSentAsync(second, 1);
        second.Receive(Frames.JoinOk("ann"));
        await Wait.UntilAsync(() => h.Statuses.Count(s => s.State == SessionState.Joined) == 2, "second join");

        Assert.Equal(Frames.Join("emmz"), Assert.Single(second.Sent));
        Assert.Equal(
            [
                SessionState.Connecting,
                SessionState.Joined,
                SessionState.Reconnecting,
                SessionState.Joined,
            ],
            h.Statuses.Select(s => s.State));
        Assert.Equal(SendResult.Sent, await h.Session.SendAsync("back"));
    }

    [Fact]
    public async Task Unreachable_server_is_retried_after_1_2_4_8_then_15_seconds()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();
        first.Drop();
        await h.WaitForStateAsync(SessionState.Reconnecting);
        var dropped = h.Time.GetUtcNow();

        int[] expectedGaps = [1, 2, 4, 8, 15, 15, 15];
        foreach (var gap in expectedGaps)
        {
            await h.Time.AdvanceAfterNextDelayAsync(TimeSpan.FromSeconds(gap));
        }

        await h.WaitForAttemptsAsync(1 + expectedGaps.Length);
        var attempts = h.Factory.AttemptTimes;
        var gaps = attempts.Skip(1).Select((time, i) => time - (i == 0 ? dropped : attempts[i])).ToList();
        Assert.Equal(expectedGaps.Select(g => TimeSpan.FromSeconds(g)), gaps);
        Assert.Equal(SessionState.Reconnecting, h.Session.Status.State);
    }

    [Fact]
    public async Task Attempt_does_not_start_before_the_delay_has_elapsed()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();
        first.Drop();
        await h.Time.WaitForNextDelayAsync();

        h.Time.AdvanceSeenDelay(TimeSpan.FromMilliseconds(999));
        await Wait.SettleAsync();

        Assert.Equal(1, h.Factory.Attempts);
        h.Time.Advance(TimeSpan.FromMilliseconds(1));
        await h.WaitForAttemptsAsync(2);
    }

    [Fact]
    public async Task Backoff_starts_over_after_a_successful_rejoin()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();
        h.Factory.EnqueueFailure(new TransportException("down"));
        h.Factory.EnqueueFailure(new TransportException("down"));
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
        await h.Time.AdvanceAfterNextDelayAsync(2 * Second);
        await h.Time.AdvanceAfterNextDelayAsync(4 * Second);
        await Wait.ForSentAsync(second, 1);
        second.Receive(Frames.JoinOk());
        await Wait.UntilAsync(() => h.Statuses.Count(s => s.State == SessionState.Joined) == 2, "second join");
        h.Factory.EnqueueConnection();

        second.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(1 * Second);

        await h.WaitForAttemptsAsync(5);
    }

    [Fact]
    public async Task Status_stays_reconnecting_while_attempts_fail()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();

        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
        await h.Time.AdvanceAfterNextDelayAsync(2 * Second);
        await h.WaitForAttemptsAsync(3);

        Assert.Equal(SessionState.Reconnecting, h.Session.Status.State);
        Assert.DoesNotContain(h.Statuses, s => s.State == SessionState.Stopped);
    }

    [Fact]
    public async Task First_connect_failure_keeps_retrying_and_is_still_connecting()
    {
        await using var h = new SessionHarness();
        h.Session.Start();

        await h.WaitForAttemptsAsync(1);
        await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
        await h.WaitForAttemptsAsync(2);

        Assert.Equal(SessionState.Connecting, h.Session.Status.State);
    }

    [Fact]
    public async Task Bad_token_on_first_connect_stops_without_retrying()
    {
        await using var h = new SessionHarness();
        h.Factory.EnqueueFailure(new UnauthorizedTransportException());

        h.Session.Start();
        await h.WaitForStateAsync(SessionState.Stopped);
        h.Time.Advance(TimeSpan.FromMinutes(5));
        await Wait.SettleAsync();

        Assert.Equal(new SessionStatus(SessionState.Stopped, StopReason.TokenRejected), h.Session.Status);
        Assert.Equal(1, h.Factory.Attempts);
        Assert.DoesNotContain(h.Statuses, s => s.State == SessionState.Reconnecting);
    }

    [Fact]
    public async Task Bad_token_during_a_reconnect_stops_reconnecting()
    {
        await using var h = new SessionHarness();
        var first = await h.JoinAsync();
        h.Factory.EnqueueFailure(new UnauthorizedTransportException());

        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
        await h.WaitForStateAsync(SessionState.Stopped);
        h.Time.Advance(TimeSpan.FromMinutes(5));
        await Wait.SettleAsync();

        Assert.Equal(StopReason.TokenRejected, h.Session.Status.StopReason);
        Assert.Equal(2, h.Factory.Attempts);
        Assert.Equal(
            [SessionState.Connecting, SessionState.Joined, SessionState.Reconnecting, SessionState.Stopped],
            h.Statuses.Select(s => s.State));
    }

    [Fact]
    public async Task Rejoin_nick_taken_is_retried_quietly_every_second_and_succeeds_without_a_prompt()
    {
        await using var h = new SessionHarness("emmz");
        var first = await h.JoinAsync();
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
        await Wait.ForSentAsync(second, 1);

        second.Receive(Frames.JoinNickTaken());
        await h.Time.WaitForNextDelayAsync();
        h.Time.AdvanceSeenDelay(TimeSpan.FromMilliseconds(999));
        await Wait.SettleAsync();
        Assert.Single(second.Sent);
        h.Time.Advance(TimeSpan.FromMilliseconds(1));
        await Wait.ForSentAsync(second, 2);
        second.Receive(Frames.JoinOk());
        await Wait.UntilAsync(() => h.Statuses.Count(s => s.State == SessionState.Joined) == 2, "second join");

        Assert.Empty(h.Prompts);
        Assert.Equal([Frames.Join("emmz"), Frames.Join("emmz")], second.Sent);
    }

    [Fact]
    public async Task Rejoin_nickname_that_stays_taken_prompts_after_three_quiet_retries()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue("emmz2");
        var first = await h.JoinAsync();
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
        await Wait.ForSentAsync(second, 1);

        for (var retry = 1; retry <= 3; retry++)
        {
            second.Receive(Frames.JoinNickTaken());
            await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
            await Wait.ForSentAsync(second, 1 + retry);
            Assert.Empty(h.Prompts);
        }

        second.Receive(Frames.JoinNickTaken());
        await Wait.ForSentAsync(second, 5);

        Assert.Equal(["emmz"], h.Prompts);
        Assert.Equal(
            [Frames.Join("emmz"), Frames.Join("emmz"), Frames.Join("emmz"), Frames.Join("emmz"), Frames.Join("emmz2")],
            second.Sent);
    }

    [Fact]
    public async Task Prompt_after_quiet_retries_prompts_again_without_more_quiet_retries()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue("emmz2");
        h.PromptAnswers.Enqueue("emmz3");
        var first = await h.JoinAsync();
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
        await Wait.ForSentAsync(second, 1);
        for (var retry = 1; retry <= 3; retry++)
        {
            second.Receive(Frames.JoinNickTaken());
            await h.Time.AdvanceAfterNextDelayAsync(1 * Second);
            await Wait.ForSentAsync(second, 1 + retry);
        }

        second.Receive(Frames.JoinNickTaken());
        await Wait.ForSentAsync(second, 5);
        second.Receive(Frames.JoinNickTaken());
        await Wait.ForSentAsync(second, 6);

        Assert.Equal(["emmz", "emmz2"], h.Prompts);
        Assert.Equal(Frames.Join("emmz3"), second.Sent[^1]);
    }
}
