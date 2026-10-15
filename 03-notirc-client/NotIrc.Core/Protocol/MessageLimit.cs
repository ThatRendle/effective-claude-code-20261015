using System.Text;

namespace NotIrc.Core.Protocol;

/// <summary>The server's message size limit, which counts UTF-8 bytes rather than characters.</summary>
public static class MessageLimit
{
    public const int MaxBytes = 1024;

    /// <summary>The number of bytes <paramref name="text"/> occupies on the wire, exactly as it would be sent.</summary>
    public static int ByteCount(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Encoding.UTF8.GetByteCount(text);
    }

    /// <summary>True when <paramref name="text"/> is at most <see cref="MaxBytes"/> bytes.</summary>
    public static bool IsWithinLimit(string text) => ByteCount(text) <= MaxBytes;
}
