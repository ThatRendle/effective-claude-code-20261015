namespace NotIrc.App;

/// <summary>Inserts text into a text box's contents at its selection, the way typing would.</summary>
public static class EmojiInsertion
{
    /// <summary>
    /// Replaces the selection (or inserts at the caret when empty) with <paramref name="emoji"/>.
    /// Offsets are UTF-16 indexes and are clamped to the text; they may be given in either order.
    /// Returns the new text and the caret index just after the inserted emoji.
    /// </summary>
    public static (string Text, int Caret) Insert(string? text, int selectionStart, int selectionEnd, string emoji)
    {
        ArgumentException.ThrowIfNullOrEmpty(emoji);
        text ??= "";
        var start = Math.Clamp(Math.Min(selectionStart, selectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(selectionStart, selectionEnd), 0, text.Length);
        return (string.Concat(text.AsSpan(0, start), emoji, text.AsSpan(end)), start + emoji.Length);
    }
}
