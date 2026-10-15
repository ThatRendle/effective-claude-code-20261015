using NotIrc.App;

namespace NotIrc.Tests.App;

public sealed class PromptShutdownTests
{
    [Fact]
    public async Task Returns_the_dialog_answer_when_nothing_shuts_down()
    {
        using var cts = new CancellationTokenSource();

        var answer = await PromptShutdown.AnswerOrNullOnShutdownAsync(Task.FromResult<string?>("emmz2"), cts.Token);

        Assert.Equal("emmz2", answer);
    }

    [Fact]
    public async Task A_pending_dialog_completes_with_null_when_shutdown_is_signalled()
    {
        using var cts = new CancellationTokenSource();
        var never = new TaskCompletionSource<string?>().Task;

        var pending = PromptShutdown.AnswerOrNullOnShutdownAsync(never, cts.Token);
        Assert.False(pending.IsCompleted);
        await cts.CancelAsync();

        Assert.Null(await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Shutdown_already_signalled_answers_null_without_waiting()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var never = new TaskCompletionSource<string?>().Task;

        Assert.Null(await PromptShutdown.AnswerOrNullOnShutdownAsync(never, cts.Token).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }
}
