namespace NotIrc.Core.Transport;

/// <summary>The connection could not be opened, or was lost while sending.</summary>
public class TransportException : Exception
{
    public TransportException()
    {
    }

    public TransportException(string message)
        : base(message)
    {
    }

    public TransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>The server answered the connection attempt with HTTP 401: the token is wrong or missing.</summary>
public sealed class UnauthorizedTransportException : TransportException
{
    public UnauthorizedTransportException()
        : base("The server rejected the token (HTTP 401).")
    {
    }

    public UnauthorizedTransportException(Exception innerException)
        : base("The server rejected the token (HTTP 401).", innerException)
    {
    }
}
