using NotIrc.App;

namespace NotIrc.Tests.App;

public class EmojiTests
{
    [Fact]
    public void Catalogue_has_named_non_empty_categories_without_duplicates()
    {
        Assert.NotEmpty(EmojiCatalogue.Categories);
        Assert.All(EmojiCatalogue.Categories, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Name));
            Assert.NotEmpty(c.Emoji);
            Assert.All(c.Emoji, e => Assert.False(string.IsNullOrWhiteSpace(e)));
        });
        Assert.Equal(
            EmojiCatalogue.Categories.Count,
            EmojiCatalogue.Categories.Select(c => c.Name).Distinct().Count());

        var all = EmojiCatalogue.Categories.SelectMany(c => c.Emoji).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public void Inserts_at_the_caret_and_moves_it_after_the_emoji()
    {
        var (text, caret) = EmojiInsertion.Insert("hello world", 5, 5, "😀");

        Assert.Equal("hello😀 world", text);
        Assert.Equal(7, caret);
    }

    [Fact]
    public void Replaces_the_selection()
    {
        var (text, caret) = EmojiInsertion.Insert("hello world", 6, 11, "🚀");

        Assert.Equal("hello 🚀", text);
        Assert.Equal(8, caret);
    }

    [Fact]
    public void Reversed_selection_offsets_are_treated_as_a_range()
    {
        Assert.Equal(("a🚀c", 3), EmojiInsertion.Insert("abc", 2, 1, "🚀"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Handles_empty_text(string? input)
    {
        Assert.Equal(("😀", 2), EmojiInsertion.Insert(input, 0, 0, "😀"));
    }

    [Fact]
    public void Inserts_at_both_ends()
    {
        Assert.Equal(("😀abc", 2), EmojiInsertion.Insert("abc", 0, 0, "😀"));
        Assert.Equal(("abc😀", 5), EmojiInsertion.Insert("abc", 3, 3, "😀"));
    }

    [Fact]
    public void Out_of_range_offsets_are_clamped()
    {
        Assert.Equal(("ab😀", 4), EmojiInsertion.Insert("ab", 10, 20, "😀"));
        Assert.Equal(("😀ab", 2), EmojiInsertion.Insert("ab", -5, -1, "😀"));
    }

    [Fact]
    public void Surrogate_pairs_in_existing_text_are_kept_whole()
    {
        var (text, caret) = EmojiInsertion.Insert("😀😀", 2, 2, "🚀");

        Assert.Equal("😀🚀😀", text);
        Assert.Equal(4, caret);
        Assert.True(char.IsHighSurrogate(text[0]) && char.IsLowSurrogate(text[1]));
    }

    [Fact]
    public void Empty_emoji_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => EmojiInsertion.Insert("abc", 0, 0, ""));
    }
}
