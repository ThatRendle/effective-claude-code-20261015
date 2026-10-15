using System.Globalization;
using NotIrc.Core.Session;

namespace NotIrc.App.ViewModels;

/// <summary>One row of the message log: a chat line (own or someone else's) or a notice.</summary>
public sealed class EntryViewModel
{
    private EntryViewModel(string time, string? nick, string text, bool isOwn, bool isNotice)
    {
        Time = time;
        Nick = nick;
        Text = text;
        IsOwn = isOwn;
        IsNotice = isNotice;
    }

    public string Time { get; }

    /// <summary>The sender, for chat lines; null for notices.</summary>
    public string? Nick { get; }

    public string Text { get; }

    public bool IsOwn { get; }

    public bool IsNotice { get; }

    public bool IsChat => !IsNotice;

    /// <param name="currentNickname">The user's nickname when the entry arrived, to style own messages.</param>
    public static EntryViewModel From(ChatEntry entry, string currentNickname)
    {
        var time = entry.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return entry switch
        {
            ChatLine line => new EntryViewModel(time, line.Nick, line.Text, line.Nick == currentNickname, false),
            NoticeEntry notice => new EntryViewModel(time, null, NoticeText(notice), false, true),
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, "Unknown log entry."),
        };
    }

    private static string NoticeText(NoticeEntry notice) => notice.Kind switch
    {
        NoticeKind.UserJoined => $"{notice.Nick} joined",
        NoticeKind.UserLeft => $"{notice.Nick} left",
        NoticeKind.MissedMessages => "Reconnected. Messages sent while you were away may have been missed.",
        NoticeKind.MessageRejected => "The server rejected your message because it was too long. Edit it and send again.",
        NoticeKind.NotConnected => "Not connected. Your message was not sent.",
        _ => throw new ArgumentOutOfRangeException(nameof(notice), notice.Kind, "Unknown notice kind."),
    };
}
