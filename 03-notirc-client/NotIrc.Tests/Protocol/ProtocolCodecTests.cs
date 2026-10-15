using System.Text.Json;
using NotIrc.Core.Protocol;

namespace NotIrc.Tests.Protocol;

public class ProtocolCodecTests
{
    [Fact]
    public void Join_encodes_as_documented()
    {
        var json = ProtocolCodec.Encode(new JoinRequest("emmz"));

        AssertJson("""{"type":"join","nick":"emmz"}""", json);
    }

    [Fact]
    public void Send_encodes_as_documented()
    {
        var json = ProtocolCodec.Encode(new SendTextRequest("hello"));

        AssertJson("""{"type":"message","text":"hello"}""", json);
    }

    [Fact]
    public void Text_with_quotes_and_unicode_survives_encoding()
    {
        var json = ProtocolCodec.Encode(new SendTextRequest("say \"hi\" \U0001F600 café"));

        using var document = JsonDocument.Parse(json);
        Assert.Equal("say \"hi\" \U0001F600 café", document.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public void Join_ok_decodes_the_user_list()
    {
        var message = ProtocolCodec.Decode("""{"type":"join_ok","users":["ann","bob"]}""");

        var ok = Assert.IsType<JoinOk>(message);
        Assert.Equal(["ann", "bob"], ok.Users);
    }

    [Fact]
    public void Join_ok_may_have_an_empty_user_list()
    {
        var message = ProtocolCodec.Decode("""{"type":"join_ok","users":[]}""");

        Assert.Empty(Assert.IsType<JoinOk>(message).Users);
    }

    [Fact]
    public void Join_error_decodes_the_reason()
    {
        var message = ProtocolCodec.Decode("""{"type":"join_error","reason":"nick_taken"}""");

        Assert.Equal(JoinError.NickTaken, Assert.IsType<JoinError>(message).Reason);
    }

    [Fact]
    public void User_joined_decodes_the_nick()
    {
        var message = ProtocolCodec.Decode("""{"type":"user_joined","nick":"ann"}""");

        Assert.Equal("ann", Assert.IsType<UserJoined>(message).Nick);
    }

    [Fact]
    public void User_left_decodes_the_nick()
    {
        var message = ProtocolCodec.Decode("""{"type":"user_left","nick":"ann"}""");

        Assert.Equal("ann", Assert.IsType<UserLeft>(message).Nick);
    }

    [Fact]
    public void Channel_message_decodes_nick_and_text()
    {
        var message = ProtocolCodec.Decode("""{"type":"message","nick":"ann","text":"hi é"}""");

        var chat = Assert.IsType<ChannelMessage>(message);
        Assert.Equal("ann", chat.Nick);
        Assert.Equal("hi é", chat.Text);
    }

    [Fact]
    public void Message_error_decodes_the_reason()
    {
        var message = ProtocolCodec.Decode("""{"type":"message_error","reason":"too_long"}""");

        Assert.Equal(MessageError.TooLong, Assert.IsType<MessageError>(message).Reason);
    }

    [Fact]
    public void Unknown_type_is_ignored()
    {
        var message = ProtocolCodec.Decode("""{"type":"typing","nick":"ann"}""");

        Assert.IsType<IgnoredMessage>(message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"nick":"ann"}""")]
    [InlineData("""{"type":7}""")]
    [InlineData("""{"type":"join_ok"}""")]
    [InlineData("""{"type":"join_ok","users":["ann",3]}""")]
    [InlineData("""{"type":"message","nick":"ann"}""")]
    [InlineData("""{"type":"user_left"}""")]
    public void Malformed_or_incomplete_frames_are_ignored(string frame)
    {
        Assert.IsType<IgnoredMessage>(ProtocolCodec.Decode(frame));
    }

    private static void AssertJson(string expected, string actual)
    {
        using var expectedDocument = JsonDocument.Parse(expected);
        using var actualDocument = JsonDocument.Parse(actual);
        Assert.Equal(expectedDocument.RootElement.GetRawText(), actualDocument.RootElement.GetRawText());
    }
}
