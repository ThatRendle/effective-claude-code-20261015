using NotIrc.Core.Session;
using NotIrc.Core.Transport;
using NotIrc.Tests.Fakes;

namespace NotIrc.Tests.Session;

public class PresenceAndLogTests
{
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Initial_list_holds_the_other_users_and_the_users_own_nickname()
    {
        await using var h = new SessionHarness("emmz");

        await h.JoinAsync("bob", "ann");

        Assert.Equal(["ann", "bob", "emmz"], h.Session.Presence);
        Assert.Equal(["ann", "bob", "emmz"], Assert.Single(h.PresenceUpdates));
    }

    [Fact]
    public async Task Someone_arriving_is_added()
    {
        await using var h = new SessionHarness("emmz");
        var transport = await h.JoinAsync("ann");

        transport.Receive(Frames.UserJoined("bob"));
        await h.WaitForEntriesAsync(1);

        Assert.Equal(["ann", "bob", "emmz"], h.Session.Presence);
    }

    [Fact]
    public async Task Someone_leaving_is_removed()
    {
        await using var h = new SessionHarness("emmz");
        var transport = await h.JoinAsync("ann", "bob");

        transport.Receive(Frames.UserLeft("ann"));
        await h.WaitForEntriesAsync(1);

        Assert.Equal(["bob", "emmz"], h.Session.Presence);
    }

    [Fact]
    public async Task Join_and_leave_are_announced_with_a_timestamp_in_the_log()
    {
        await using var h = new SessionHarness("emmz");
        var transport = await h.JoinAsync("ann");
        var arrival = h.Time.GetLocalNow();

        transport.Receive(Frames.UserJoined("bob"));
        await h.WaitForEntriesAsync(1);
        h.Time.Advance(Second);
        var departure = h.Time.GetLocalNow();
        transport.Receive(Frames.UserLeft("ann"));
        await h.WaitForEntriesAsync(2);

        Assert.Equal(
            [
                new NoticeEntry(arrival, NoticeKind.UserJoined, "bob"),
                new NoticeEntry(departure, NoticeKind.UserLeft, "ann"),
            ],
            h.Entries);
    }

    [Fact]
    public async Task The_initial_list_adds_no_join_notices()
    {
        await using var h = new SessionHarness("emmz");

        await h.JoinAsync("ann", "bob");
        await Wait.SettleAsync();

        Assert.Empty(h.Entries);
    }

    [Fact]
    public async Task Rejoin_replaces_the_list_so_people_who_left_during_the_drop_disappear()
    {
        await using var h = new SessionHarness("emmz");
        var first = await h.JoinAsync("ann", "bob");
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(Second);
        await Wait.ForSentAsync(second, 1);

        second.Receive(Frames.JoinOk("bob"));
        await Wait.UntilAsync(() => h.PresenceUpdates.Count == 2, "rebuilt presence");

        Assert.Equal(["bob", "emmz"], h.Session.Presence);
    }

    [Fact]
    public async Task Rejoin_replaces_the_list_so_people_who_arrived_during_the_drop_appear()
    {
        await using var h = new SessionHarness("emmz");
        var first = await h.JoinAsync("ann");
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(Second);
        await Wait.ForSentAsync(second, 1);

        second.Receive(Frames.JoinOk("ann", "cat"));
        await Wait.UntilAsync(() => h.PresenceUpdates.Count == 2, "rebuilt presence");

        Assert.Equal(["ann", "cat", "emmz"], h.Session.Presence);
    }

    [Fact]
    public async Task Rejoin_under_a_replacement_nickname_lists_that_nickname()
    {
        await using var h = new SessionHarness("emmz");
        h.PromptAnswers.Enqueue("emmz2");
        var first = await h.JoinAsync();
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(Second);
        await Wait.ForSentAsync(second, 1);
        for (var retry = 1; retry <= 3; retry++)
        {
            second.Receive(Frames.JoinNickTaken());
            await h.Time.AdvanceAfterNextDelayAsync(Second);
            await Wait.ForSentAsync(second, 1 + retry);
        }

        second.Receive(Frames.JoinNickTaken());
        await Wait.ForSentAsync(second, 5);
        second.Receive(Frames.JoinOk("ann"));
        await Wait.UntilAsync(() => h.PresenceUpdates.Count == 2, "rebuilt presence");

        Assert.Equal(["ann", "emmz2"], h.Session.Presence);
    }

    [Fact]
    public async Task Channel_messages_are_logged_in_arrival_order_with_the_receive_time()
    {
        await using var h = new SessionHarness("emmz");
        var transport = await h.JoinAsync("ann");
        var t0 = h.Time.GetLocalNow();

        transport.Receive(Frames.Message("ann", "first"));
        await h.WaitForEntriesAsync(1);
        h.Time.Advance(Second);
        var t1 = h.Time.GetLocalNow();
        transport.Receive(Frames.Message("bob", "second"));
        await h.WaitForEntriesAsync(2);
        transport.Receive(Frames.Message("ann", "third"));
        await h.WaitForEntriesAsync(3);

        Assert.Equal(
            [
                new ChatLine(t0, "ann", "first"),
                new ChatLine(t1, "bob", "second"),
                new ChatLine(t1, "ann", "third"),
            ],
            h.Entries);
    }

    [Fact]
    public async Task Own_message_is_delivered_once_with_the_nickname_attached()
    {
        await using var h = new SessionHarness("emmz");
        var transport = await h.JoinAsync();

        await h.Session.SendAsync("hello");
        Assert.Empty(h.Entries);
        transport.Receive(Frames.Message("emmz", "hello"));
        await h.WaitForEntriesAsync(1);
        await Wait.SettleAsync();

        var line = Assert.IsType<ChatLine>(Assert.Single(h.Entries));
        Assert.Equal(h.Session.Nickname, line.Nick);
    }

    [Fact]
    public async Task Many_messages_are_all_kept()
    {
        await using var h = new SessionHarness("emmz");
        var transport = await h.JoinAsync();

        for (var i = 0; i < 700; i++)
        {
            transport.Receive(Frames.Message("ann", $"m{i}"));
        }

        await h.WaitForEntriesAsync(700);
        Assert.Equal("m0", ((ChatLine)h.Entries[0]).Text);
        Assert.Equal("m699", ((ChatLine)h.Entries[699]).Text);
    }

    [Fact]
    public async Task Unknown_message_types_are_ignored_without_error()
    {
        await using var h = new SessionHarness("emmz");
        var transport = await h.JoinAsync("ann");

        transport.Receive(Frames.Unknown());
        transport.Receive("not json at all");
        transport.Receive(Frames.Message("ann", "after"));
        await h.WaitForEntriesAsync(1);

        Assert.Equal("after", Assert.IsType<ChatLine>(Assert.Single(h.Entries)).Text);
        Assert.Equal(SessionState.Joined, h.Session.Status.State);
    }

    [Fact]
    public async Task First_join_adds_no_missed_messages_notice()
    {
        await using var h = new SessionHarness("emmz");

        await h.JoinAsync("ann");
        await Wait.SettleAsync();

        Assert.DoesNotContain(h.Entries, e => e is NoticeEntry { Kind: NoticeKind.MissedMessages });
    }

    [Fact]
    public async Task Rejoin_after_a_drop_adds_one_missed_messages_notice_at_that_point()
    {
        await using var h = new SessionHarness("emmz");
        var first = await h.JoinAsync("ann");
        first.Receive(Frames.Message("ann", "before"));
        await h.WaitForEntriesAsync(1);
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(Second);
        await Wait.ForSentAsync(second, 1);

        second.Receive(Frames.JoinOk("ann"));
        await h.WaitForEntriesAsync(2);
        second.Receive(Frames.Message("ann", "after"));
        await h.WaitForEntriesAsync(3);

        Assert.IsType<ChatLine>(h.Entries[0]);
        Assert.Equal(NoticeKind.MissedMessages, Assert.IsType<NoticeEntry>(h.Entries[1]).Kind);
        Assert.IsType<ChatLine>(h.Entries[2]);
    }

    [Fact]
    public async Task Failed_attempts_do_not_each_add_a_missed_messages_notice()
    {
        await using var h = new SessionHarness("emmz");
        var first = await h.JoinAsync();
        h.Factory.EnqueueFailure(new TransportException("down"));
        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(Second);
        await h.Time.AdvanceAfterNextDelayAsync(2 * Second);
        await Wait.ForSentAsync(second, 1);

        second.Receive(Frames.JoinOk());
        await h.WaitForEntriesAsync(1);

        Assert.Equal(NoticeKind.MissedMessages, Assert.IsType<NoticeEntry>(Assert.Single(h.Entries)).Kind);
    }
}
