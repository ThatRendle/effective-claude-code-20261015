using NotIrc.App.ViewModels;
using NotIrc.Core.Protocol;
using NotIrc.Core.Session;
using NotIrc.Core.Transport;
using NotIrc.Tests.Fakes;

namespace NotIrc.Tests.App;

public sealed class MainViewModelTests
{
    private static MainViewModel Create(SessionHarness h) => new(h.Session, "ws://test.invalid/ws", a => a());

    [Fact]
    public async Task Shows_status_text_following_the_connection()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);

        var transport = await h.JoinAsync();
        Assert.Equal("Joined", vm.StatusText);

        transport.Drop();
        await h.WaitForStateAsync(SessionState.Reconnecting);
        await Wait.UntilAsync(() => vm.StatusText == "Reconnecting…", "reconnecting status text");
    }

    [Fact]
    public async Task Shows_messages_with_nick_text_and_time_in_arrival_order()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        var transport = await h.JoinAsync("ann");

        transport.Receive(Frames.Message("ann", "one"));
        transport.Receive(Frames.Message("bob", "two"));
        await Wait.UntilAsync(() => vm.Entries.Count == 2, "two entries");

        Assert.Equal(["one", "two"], vm.Entries.Select(e => e.Text));
        Assert.Equal(["ann", "bob"], vm.Entries.Select(e => e.Nick));
        Assert.Matches(@"^\d\d:\d\d:\d\d$", vm.Entries[0].Time);
    }

    [Fact]
    public async Task Keeps_every_message_in_a_long_session()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        var transport = await h.JoinAsync("ann");

        for (var i = 0; i < 600; i++)
        {
            transport.Receive(Frames.Message("ann", $"m{i}"));
        }

        await Wait.UntilAsync(() => vm.Entries.Count == 600, "600 entries");
        Assert.Equal("m0", vm.Entries[0].Text);
    }

    [Fact]
    public async Task Styles_own_messages_and_not_others()
    {
        await using var h = new SessionHarness("emmz");
        using var vm = Create(h);
        var transport = await h.JoinAsync("ann");

        transport.Receive(Frames.Message("emmz", "mine"));
        transport.Receive(Frames.Message("ann", "hers"));
        await Wait.UntilAsync(() => vm.Entries.Count == 2, "two entries");

        Assert.True(vm.Entries[0].IsOwn);
        Assert.False(vm.Entries[1].IsOwn);
    }

    [Fact]
    public async Task Joins_and_leaves_are_notices_and_the_initial_list_is_not()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        var transport = await h.JoinAsync("ann", "bob");

        Assert.Empty(vm.Entries);
        transport.Receive(Frames.UserJoined("cat"));
        transport.Receive(Frames.UserLeft("ann"));
        await Wait.UntilAsync(() => vm.Entries.Count == 2, "two notices");

        Assert.All(vm.Entries, e => Assert.True(e.IsNotice));
        Assert.Equal(["cat joined", "ann left"], vm.Entries.Select(e => e.Text));
    }

    [Fact]
    public async Task Presence_includes_own_nick_and_is_rebuilt_on_each_join()
    {
        await using var h = new SessionHarness("emmz");
        using var vm = Create(h);
        var transport = await h.JoinAsync("ann", "bob");
        Assert.Equal(["ann", "bob", "emmz"], vm.Presence);

        transport.Receive(Frames.UserLeft("ann"));
        await Wait.UntilAsync(() => !vm.Presence.Contains("ann"), "ann removed");
        Assert.Equal(["bob", "emmz"], vm.Presence);
    }

    [Fact]
    public async Task Byte_counter_counts_utf8_bytes_and_flags_over_the_limit()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);

        vm.InputText = string.Concat(Enumerable.Repeat("😀", 300));
        Assert.Equal(1200, vm.ByteCount);
        Assert.Equal("1200/1024", vm.ByteCounterText);
        Assert.True(vm.IsOverLimit);

        vm.InputText = new string('a', MessageLimit.MaxBytes);
        Assert.False(vm.IsOverLimit);

        vm.InputText = new string('a', MessageLimit.MaxBytes + 1);
        Assert.True(vm.IsOverLimit);
    }

    [Fact]
    public async Task Send_is_disabled_when_over_the_limit_empty_or_not_joined()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        vm.InputText = "hello";
        Assert.False(vm.CanSend); // not joined yet

        await h.JoinAsync();
        await Wait.UntilAsync(() => vm.Status.State == SessionState.Joined, "joined status");
        Assert.True(vm.CanSend);

        vm.InputText = "   ";
        Assert.False(vm.CanSend);

        vm.InputText = new string('a', MessageLimit.MaxBytes + 1);
        Assert.False(vm.CanSend);
        Assert.False(vm.SendCommand.CanExecute(null));

        vm.InputText = "ok again";
        Assert.True(vm.CanSend);
    }

    [Fact]
    public async Task Sending_clears_the_input_and_shows_the_message_only_when_echoed()
    {
        await using var h = new SessionHarness("emmz");
        using var vm = Create(h);
        var transport = await h.JoinAsync();
        await Wait.UntilAsync(() => vm.CanSend || vm.Status.State == SessionState.Joined, "joined");

        vm.InputText = "hello";
        await vm.SendCommand.ExecuteAsync(null);

        Assert.Equal("", vm.InputText);
        Assert.Equal(Frames.Send("hello"), transport.Sent[^1]);
        Assert.Empty(vm.Entries);

        transport.Receive(Frames.Message("emmz", "hello"));
        await Wait.UntilAsync(() => vm.Entries.Count == 1, "echo");
        Assert.True(vm.Entries[0].IsOwn);
    }

    [Fact]
    public async Task Submitting_while_not_joined_shows_a_notice_and_keeps_the_text()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);

        vm.InputText = "hello";
        await vm.SendCommand.ExecuteAsync(null);

        Assert.Equal("hello", vm.InputText);
        var notice = Assert.Single(vm.Entries);
        Assert.True(notice.IsNotice);
        Assert.Contains("Not connected", notice.Text);
    }

    [Fact]
    public async Task Server_rejection_shows_a_notice_and_restores_the_text()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        var transport = await h.JoinAsync();
        await Wait.UntilAsync(() => vm.Status.State == SessionState.Joined, "joined status");

        vm.InputText = "too long for the server";
        await vm.SendCommand.ExecuteAsync(null);
        Assert.Equal("", vm.InputText);

        transport.Receive(Frames.MessageTooLong());
        await Wait.UntilAsync(() => vm.Entries.Count == 1, "rejection notice");

        Assert.Contains("rejected", vm.Entries[0].Text);
        Assert.Equal("too long for the server", vm.InputText);
        Assert.Equal(SessionState.Joined, h.Session.Status.State);
    }

    [Fact]
    public async Task Missed_messages_notice_appears_after_a_rejoin_but_not_the_first_join()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        var first = await h.JoinAsync();
        Assert.Empty(vm.Entries);

        var second = h.Factory.EnqueueConnection();
        first.Drop();
        await h.Time.AdvanceAfterNextDelayAsync(ReconnectPolicy.DelayBefore(0));
        await Wait.ForSentAsync(second, 1);
        second.Receive(Frames.JoinOk());

        await Wait.UntilAsync(() => vm.Entries.Count == 1, "gap notice");
        Assert.Contains("missed", vm.Entries[0].Text);
    }

    [Fact]
    public async Task Token_rejection_shows_disconnected_with_the_reason()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        h.Factory.EnqueueFailure(new UnauthorizedTransportException());

        h.Session.Start();
        await Wait.UntilAsync(() => vm.Status.State == SessionState.Stopped, "stopped");

        Assert.Equal("Disconnected: token rejected", vm.StatusText);
        Assert.Contains("rejected the token", vm.ErrorText);
    }

    [Fact]
    public async Task Presence_is_stale_only_while_the_connection_is_lost()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        Assert.False(vm.IsPresenceStale); // connecting: nothing shown yet

        var transport = await h.JoinAsync("ann");
        await Wait.UntilAsync(() => vm.Status.State == SessionState.Joined, "joined");
        Assert.False(vm.IsPresenceStale);

        transport.Drop();
        await Wait.UntilAsync(() => vm.IsPresenceStale, "stale while reconnecting");
        Assert.Equal(["ann", "emmz"], vm.Presence); // still shown, just dimmed

        var second = h.Factory.EnqueueConnection();
        await h.Time.AdvanceAfterNextDelayAsync(ReconnectPolicy.DelayBefore(0));
        await Wait.ForSentAsync(second, 1);
        second.Receive(Frames.JoinOk("bob"));
        await Wait.UntilAsync(() => !vm.IsPresenceStale, "fresh after rejoin");
        Assert.Equal(["bob", "emmz"], vm.Presence);
    }

    [Fact]
    public async Task Presence_is_stale_after_the_client_stops()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        h.Factory.EnqueueFailure(new UnauthorizedTransportException());

        h.Session.Start();
        await Wait.UntilAsync(() => vm.Status.State == SessionState.Stopped, "stopped");

        Assert.True(vm.IsPresenceStale);
    }

    [Fact]
    public async Task Text_typed_while_a_send_is_in_flight_is_kept()
    {
        await using var h = new SessionHarness();
        using var vm = Create(h);
        var transport = await h.JoinAsync();
        await Wait.UntilAsync(() => vm.Status.State == SessionState.Joined, "joined status");
        var release = new TaskCompletionSource();
        transport.HoldSends = release.Task;

        vm.InputText = "first";
        var send = vm.SendCommand.ExecuteAsync(null);
        await Wait.ForSentAsync(transport, 2); // join + the held send
        vm.InputText = "second";
        release.SetResult();
        await send;

        Assert.Equal("second", vm.InputText);
    }
}
