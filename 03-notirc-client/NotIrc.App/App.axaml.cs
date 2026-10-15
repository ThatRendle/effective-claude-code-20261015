using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using NotIrc.App.ViewModels;
using NotIrc.Core.Configuration;
using NotIrc.Core.Session;
using NotIrc.Core.Transport;

namespace NotIrc.App;

public partial class App : Application
{
    private static readonly TimeSpan ShutdownWait = TimeSpan.FromSeconds(3);

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = SettingsLoader.Load(desktop.Args ?? []);
            desktop.MainWindow = settings.Settings is { } valid
                ? StartChat(desktop, valid)
                : new StartupErrorWindow(new StartupErrorViewModel(settings.Errors));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindow StartChat(IClassicDesktopStyleApplicationLifetime desktop, ClientSettings settings)
    {
        MainWindow? window = null;
        var shuttingDown = new CancellationTokenSource(); // lives for the whole run; cancelled at exit
        var session = new ChannelSession(
            new WebSocketTransportFactory(),
            new SessionOptions(settings.ConnectionUrl, settings.Nickname),
            rejected => PromptShutdown.AnswerOrNullOnShutdownAsync(PromptForNicknameAsync(window, rejected), shuttingDown.Token));
        var viewModel = new MainViewModel(session, settings.DisplayAddress, action => Dispatcher.UIThread.Post(action));
        window = new MainWindow(viewModel);

        // Connect once the window is on screen, so the first status change is seen.
        window.Opened += (_, _) => session.Start();

        // Disposing waits for the session loop, which may be waiting on a nickname dialog that needs this (blocked)
        // UI thread. Signal shutdown first so a pending prompt answers null straight away.
        desktop.Exit += (_, _) =>
        {
            shuttingDown.Cancel();
            viewModel.Dispose();
            session.DisposeAsync().AsTask().Wait(ShutdownWait);
        };
        return window;
    }

    private static Task<string?> PromptForNicknameAsync(Window? owner, string rejected) =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (owner is not { IsVisible: true })
            {
                return null;
            }

            return await new NicknamePromptWindow(rejected).ShowDialog<string?>(owner);
        });
}
