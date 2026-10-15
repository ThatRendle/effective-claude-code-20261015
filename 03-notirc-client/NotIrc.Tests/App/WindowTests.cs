using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NotIrc.App;
using NotIrc.App.ViewModels;
using NotIrc.Core.Protocol;
using NotIrc.Core.Session;
using NotIrc.Tests.Fakes;

namespace NotIrc.Tests.App;

public sealed class WindowTests
{
    /// <summary>Waits on the UI thread, running posted work, until <paramref name="condition"/> holds.</summary>
    private static async Task UiUntilAsync(Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            Dispatcher.UIThread.RunJobs();
            if (condition())
            {
                return;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Timed out waiting for {description}.");
            }

            await Task.Delay(1);
        }
    }

    private static (MainWindow Window, MainViewModel Vm) Show(SessionHarness h)
    {
        var vm = new MainViewModel(h.Session, "ws://test.invalid/ws", a => Dispatcher.UIThread.Post(a));
        var window = new MainWindow(vm);
        window.Show();
        return (window, vm);
    }

    [Fact]
    public Task Renders_status_nickname_messages_and_presence() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness("emmz");
        var (window, vm) = Show(h);
        using var _ = vm;

        var transport = await h.JoinAsync("ann");
        transport.Receive(Frames.Message("ann", "hello there"));
        await UiUntilAsync(() => vm.Entries.Count == 1, "message");

        Assert.Equal("Joined", window.FindControl<TextBlock>("StatusLabel")!.Text);
        Assert.Equal("emmz", window.FindControl<TextBlock>("NicknameLabel")!.Text);
        Assert.Equal(["ann", "emmz"], window.FindControl<ListBox>("PresenceList")!.Items.Cast<string>());
        Assert.Single(window.FindControl<ItemsControl>("LogList")!.Items);
        Assert.False(window.FindControl<Border>("ErrorBanner")!.IsVisible);
        window.Close();
    });

    [Fact]
    public Task Over_limit_input_shows_the_warning_and_disables_send() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness();
        var (window, vm) = Show(h);
        using var _ = vm;
        await h.JoinAsync();
        await UiUntilAsync(() => vm.Status.State == SessionState.Joined, "joined");

        var send = window.FindControl<Button>("SendButton")!;
        var warning = window.FindControl<TextBlock>("TooLongWarning")!;

        window.FindControl<TextBox>("InputBox")!.Text = "hi";
        Assert.True(send.IsEnabled);
        Assert.False(warning.IsVisible);

        window.FindControl<TextBox>("InputBox")!.Text = new string('a', MessageLimit.MaxBytes + 1);
        Assert.False(send.IsEnabled);
        Assert.True(warning.IsVisible);
        Assert.Equal("1025/1024", window.FindControl<TextBlock>("ByteCounter")!.Text);

        window.FindControl<TextBox>("InputBox")!.Text = new string('a', MessageLimit.MaxBytes);
        Assert.True(send.IsEnabled);
        Assert.False(warning.IsVisible);
        window.Close();
    });

    [Fact]
    public Task Send_is_disabled_until_joined() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness();
        var (window, vm) = Show(h);
        using var _ = vm;

        window.FindControl<TextBox>("InputBox")!.Text = "hello";

        Assert.False(window.FindControl<Button>("SendButton")!.IsEnabled);
        window.Close();
    });

    [Fact]
    public Task Token_rejection_shows_the_error_banner() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness();
        var (window, vm) = Show(h);
        using var _ = vm;
        h.Factory.EnqueueFailure(new NotIrc.Core.Transport.UnauthorizedTransportException());

        h.Session.Start();
        await UiUntilAsync(() => vm.Status.State == SessionState.Stopped, "stopped");

        Assert.True(window.FindControl<Border>("ErrorBanner")!.IsVisible);
        Assert.Equal("Disconnected: token rejected", window.FindControl<TextBlock>("StatusLabel")!.Text);
        window.Close();
    });

    [Fact]
    public Task Nickname_prompt_is_prefilled_and_returns_the_confirmed_nickname() => Ui.RunAsync(async () =>
    {
        var owner = new Window();
        owner.Show();
        var dialog = new NicknamePromptWindow("emmz");

        var result = dialog.ShowDialog<string?>(owner);
        Assert.Equal("emmz", dialog.FindControl<TextBox>("NicknameBox")!.Text);
        Assert.Contains("emmz", dialog.FindControl<TextBlock>("MessageText")!.Text);

        dialog.FindControl<TextBox>("NicknameBox")!.Text = "  emmz2 ";
        dialog.FindControl<Button>("OkButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal("emmz2", await result);
        owner.Close();
    });

    [Fact]
    public Task Nickname_prompt_cancel_and_close_answer_null() => Ui.RunAsync(async () =>
    {
        var owner = new Window();
        owner.Show();

        var cancelled = new NicknamePromptWindow("emmz");
        var cancelResult = cancelled.ShowDialog<string?>(owner);
        cancelled.FindControl<Button>("CancelButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Null(await cancelResult);

        var closed = new NicknamePromptWindow("emmz");
        var closeResult = closed.ShowDialog<string?>(owner);
        closed.Close();
        Assert.Null(await closeResult);
        owner.Close();
    });

    [Fact]
    public Task Closing_the_owner_completes_an_open_nickname_prompt_with_null() => Ui.RunAsync(async () =>
    {
        var owner = new Window();
        owner.Show();
        var dialog = new NicknamePromptWindow("emmz");
        var result = dialog.ShowDialog<string?>(owner);

        owner.Close();

        Assert.Null(await result);
    });

    [Fact]
    public Task Nickname_prompt_ok_is_disabled_for_a_blank_nickname() => Ui.Run(() =>
    {
        var dialog = new NicknamePromptWindow("emmz");
        dialog.Show();

        dialog.FindControl<TextBox>("NicknameBox")!.Text = "   ";

        Assert.False(dialog.FindControl<Button>("OkButton")!.IsEnabled);
        dialog.Close();
    });

    [Fact]
    public Task Startup_error_view_lists_each_problem() => Ui.Run(() =>
    {
        List<string> errors = ["The token is missing. Supply it with --token.", "The nickname is missing."];
        var window = new StartupErrorWindow(new StartupErrorViewModel(errors));
        window.Show();

        Assert.Equal("NotIrc can't start", window.FindControl<TextBlock>("HeadingText")!.Text);
        Assert.Equal(errors, window.FindControl<ItemsControl>("ErrorList")!.Items.Cast<string>());
        Assert.Contains("Nothing has been connected", window.FindControl<TextBlock>("HintText")!.Text);
        window.Close();
    });

    [Fact]
    public Task Own_rows_have_a_translucent_background_and_other_rows_have_none() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness("emmz");
        var (window, vm) = Show(h);
        using var _ = vm;
        var transport = await h.JoinAsync("ann");

        transport.Receive(Frames.Message("emmz", "mine"));
        transport.Receive(Frames.Message("ann", "hers"));
        await UiUntilAsync(() => vm.Entries.Count == 2, "two messages");
        Dispatcher.UIThread.RunJobs();

        var rows = window.GetVisualDescendants().OfType<Border>()
            .Where(b => b.DataContext is EntryViewModel)
            .ToDictionary(b => ((EntryViewModel)b.DataContext!).Text);

        var own = Assert.IsAssignableFrom<ISolidColorBrush>(rows["mine"].Background);
        Assert.True(own.Opacity * own.Color.A < 255, "own background must be translucent so theme text stays readable");
        Assert.Null(rows["hers"].Background);
        window.Close();
    });

    [Fact]
    public Task Presence_list_is_dimmed_while_reconnecting() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness("emmz");
        var (window, vm) = Show(h);
        using var _ = vm;
        var transport = await h.JoinAsync("ann");
        await UiUntilAsync(() => vm.Status.State == SessionState.Joined, "joined");
        var list = window.FindControl<ListBox>("PresenceList")!;
        Assert.Equal(1, list.Opacity);

        transport.Drop();
        await UiUntilAsync(() => vm.Status.State == SessionState.Reconnecting, "reconnecting");

        Assert.True(list.Opacity < 1, "presence list should be dimmed while reconnecting");
        window.Close();
    });

    /// <summary>Opens the emoji picker and clicks the button showing <paramref name="emoji"/>.</summary>
    private static async Task PickEmojiAsync(Window window, string emoji)
    {
        var flyout = (Flyout)window.FindControl<Button>("EmojiButton")!.Flyout!;
        if (!flyout.IsOpen)
        {
            flyout.ShowAt(window.FindControl<Button>("EmojiButton")!);
        }

        Button? pick = null;
        await UiUntilAsync(
            () => (pick = ((Control)flyout.Content!).GetVisualDescendants().OfType<Button>().FirstOrDefault(b => b.Content as string == emoji)) is not null,
            $"emoji {emoji} in the picker");
        pick!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    [Fact]
    public Task Picking_an_emoji_inserts_it_at_the_caret_and_refocuses_the_input() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness();
        var (window, vm) = Show(h);
        using var _ = vm;
        var input = window.FindControl<TextBox>("InputBox")!;
        input.Text = "hello world";
        input.SelectionStart = 5;
        input.SelectionEnd = 5;

        await PickEmojiAsync(window, "😀");

        Assert.Equal("hello😀 world", input.Text);
        Assert.Equal("hello😀 world", vm.InputText);
        Assert.Equal(7, input.CaretIndex);
        Assert.True(input.IsFocused);
        window.Close();
    });

    [Fact]
    public Task Picking_an_emoji_replaces_the_selection_even_while_disconnected() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness();
        var (window, vm) = Show(h);
        using var _ = vm;
        Assert.NotEqual(SessionState.Joined, vm.Status.State);
        var input = window.FindControl<TextBox>("InputBox")!;
        input.Text = "hello world";
        input.SelectionStart = 6;
        input.SelectionEnd = 11;

        await PickEmojiAsync(window, "🚀");

        Assert.Equal("hello 🚀", input.Text);
        window.Close();
    });

    [Fact]
    public Task Picked_emoji_obey_the_byte_limit() => Ui.RunAsync(async () =>
    {
        await using var h = new SessionHarness();
        var (window, vm) = Show(h);
        using var _ = vm;
        await h.JoinAsync();
        await UiUntilAsync(() => vm.Status.State == SessionState.Joined, "joined");
        var input = window.FindControl<TextBox>("InputBox")!;
        var send = window.FindControl<Button>("SendButton")!;
        var warning = window.FindControl<TextBlock>("TooLongWarning")!;
        input.Text = new string('a', 1022);
        input.CaretIndex = 1022;
        Assert.True(send.IsEnabled);

        await PickEmojiAsync(window, "😀");

        Assert.Equal("1026/1024", window.FindControl<TextBlock>("ByteCounter")!.Text);
        Assert.True(warning.IsVisible);
        Assert.False(send.IsEnabled);

        input.Text = new string('a', 1022);
        Assert.Equal("1022/1024", window.FindControl<TextBlock>("ByteCounter")!.Text);
        Assert.False(warning.IsVisible);
        Assert.True(send.IsEnabled);
        window.Close();
    });
}
