namespace NotIrc.Core.Transport;

/// <summary>An open connection that exchanges whole text frames with the server.</summary>
public interface ITransport : IAsyncDisposable
{
    /// <summary>Sends one text frame. Throws <see cref="TransportException"/> if the connection is gone.</summary>
    Task SendAsync(string text, CancellationToken cancellationToken);

    /// <summary>
    /// Waits for the next text frame. Returns null once the connection has ended, whether the server
    /// closed it or the network dropped it.
    /// </summary>
    ValueTask<string?> ReceiveAsync(CancellationToken cancellationToken);
}

/// <summary>Opens connections. Separate from <see cref="ITransport"/> so tests can script connect outcomes.</summary>
public interface ITransportFactory
{
    /// <summary>
    /// Opens a connection to <paramref name="url"/>. Throws <see cref="UnauthorizedTransportException"/>
    /// for HTTP 401 and <see cref="TransportException"/> for any other failure to connect.
    /// </summary>
    Task<ITransport> ConnectAsync(Uri url, CancellationToken cancellationToken);
}
