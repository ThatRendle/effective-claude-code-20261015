using System.Reflection;
using Avalonia.Headless;

namespace NotIrc.Tests.App;

/// <summary>
/// Runs a test body on the headless Avalonia UI thread. <c>Avalonia.Headless.XUnit</c> 12.0.4 is built against
/// xunit.v3 3.2.2 and fails discovery on the 4.0.2 this project uses, so tests go through the session directly.
/// </summary>
internal static class Ui
{
    private static readonly HeadlessUnitTestSession Session =
        HeadlessUnitTestSession.GetOrStartForAssembly(Assembly.GetExecutingAssembly());

    public static Task RunAsync(Func<Task> body) => Session.Dispatch(body, CancellationToken.None);

    public static Task Run(Action body) =>
        Session.Dispatch(
            () =>
            {
                body();
                return Task.CompletedTask;
            },
            CancellationToken.None);
}
