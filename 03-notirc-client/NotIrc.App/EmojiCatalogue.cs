namespace NotIrc.App;

/// <summary>A named group of emoji shown together in the picker.</summary>
public sealed record EmojiCategory(string Name, IReadOnlyList<string> Emoji);

/// <summary>
/// The emoji offered by the picker. Entries are single code point (or code point plus variation selector) emoji,
/// so they render without joiner-sequence support in the OS font.
/// </summary>
public static class EmojiCatalogue
{
    public static IReadOnlyList<EmojiCategory> Categories { get; } =
    [
        new("Smileys",
        [
            "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇", "🙂", "😉", "😍", "😘", "😋", "😜",
            "🤔", "😐", "😴", "😎", "🤓", "😢", "😭", "😡", "😱", "😳", "🙄", "😬", "🤯", "🥳", "🥺", "🤗",
        ]),
        new("Gestures",
        [
            "👍", "👎", "👌", "✌️", "🤞", "🤝", "👏", "🙌", "🙏", "💪", "👋", "🤙", "👀", "🧠", "🫡", "🫶",
        ]),
        new("Hearts",
        [
            "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "💔", "💕", "💖", "💯",
        ]),
        new("Things",
        [
            "🎉", "🎂", "🎁", "☕", "🍕", "🍺", "🚀", "💡", "🔥", "⭐", "🌈", "☀️", "🌙", "💻", "📌", "🐛",
        ]),
        new("Symbols",
        [
            "✅", "❌", "⚠️", "❓", "❗", "➕", "➖", "➡️", "⬆️", "⬇️", "🔔", "🔒",
        ]),
    ];
}
