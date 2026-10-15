namespace NotIrc.App;

/// <summary>Lets a pending dialog be abandoned when the app is shutting down, so disposing the session cannot wait on a dialog the UI thread can no longer show.</summary>
public static class PromptShutdown
{
    /// <summary>Returns the dialog's answer, or null as soon as <paramref name="shutdown"/> is cancelled, whichever comes first.</summary>
    public static async Task<string?> AnswerOrNullOnShutdownAsync(Task<string?> dialog, CancellationToken shutdown)
    {
        var abandoned = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = shutdown.Register(() => abandoned.TrySetResult(null));

        var first = await Task.WhenAny(dialog, abandoned.Task).ConfigureAwait(false);
        return first == dialog ? await dialog.ConfigureAwait(false) : null;
    }
}
