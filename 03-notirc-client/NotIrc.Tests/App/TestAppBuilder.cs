using Avalonia;
using Avalonia.Headless;
using NotIrc.Tests.App;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace NotIrc.Tests.App;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<NotIrc.App.App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
