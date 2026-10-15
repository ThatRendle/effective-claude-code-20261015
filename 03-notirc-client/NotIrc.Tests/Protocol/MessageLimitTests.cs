using NotIrc.Core.Protocol;

namespace NotIrc.Tests.Protocol;

public class MessageLimitTests
{
    [Fact]
    public void Ascii_counts_one_byte_per_character()
    {
        Assert.Equal(5, MessageLimit.ByteCount("hello"));
    }

    [Fact]
    public void Empty_text_is_zero_bytes_and_within_the_limit()
    {
        Assert.Equal(0, MessageLimit.ByteCount(string.Empty));
        Assert.True(MessageLimit.IsWithinLimit(string.Empty));
    }

    [Theory]
    [InlineData("é", 2)]
    [InlineData("€", 3)]
    [InlineData("漢", 3)]
    [InlineData("\U0001F600", 4)]
    public void Multibyte_characters_count_their_utf8_bytes(string text, int expectedBytes)
    {
        Assert.Equal(expectedBytes, MessageLimit.ByteCount(text));
    }

    [Fact]
    public void Exactly_1024_bytes_is_within_the_limit()
    {
        var text = new string('a', 1024);

        Assert.Equal(1024, MessageLimit.ByteCount(text));
        Assert.True(MessageLimit.IsWithinLimit(text));
    }

    [Fact]
    public void One_byte_over_is_not_within_the_limit()
    {
        var text = new string('a', 1025);

        Assert.Equal(1025, MessageLimit.ByteCount(text));
        Assert.False(MessageLimit.IsWithinLimit(text));
    }

    [Fact]
    public void Three_hundred_emoji_are_under_1024_characters_but_over_the_limit()
    {
        var text = string.Concat(Enumerable.Repeat("\U0001F600", 300));

        Assert.Equal(600, text.Length);
        Assert.Equal(1200, MessageLimit.ByteCount(text));
        Assert.False(MessageLimit.IsWithinLimit(text));
    }

    [Fact]
    public void A_multibyte_character_straddling_the_limit_is_over()
    {
        // 1022 ASCII bytes plus a 3-byte character is 1025 bytes from 1023 characters.
        var text = new string('a', 1022) + "€";

        Assert.Equal(1025, MessageLimit.ByteCount(text));
        Assert.False(MessageLimit.IsWithinLimit(text));
    }

    [Fact]
    public void Surrounding_whitespace_counts_because_the_same_string_is_sent()
    {
        Assert.Equal(7, MessageLimit.ByteCount("  hi   "));
    }
}
