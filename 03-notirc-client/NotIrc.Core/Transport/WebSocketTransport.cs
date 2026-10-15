using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace NotIrc.Core.Transport;

/// <summary>Opens <see cref="ClientWebSocket"/> connections.</summary>
public sealed class WebSocketTransportFactory : ITransportFactory
{
    public async Task<ITransport> ConnectAsync(Uri url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);

        var socket = new ClientWebSocket();
        socket.Options.CollectHttpResponseDetails = true;
        var connected = false;
        try
        {
            await socket.ConnectAsync(url, cancellationToken).ConfigureAwait(false);
            connected = true;
            return new WebSocketTransport(socket);
        }
        catch (Exception ex) when (ex is WebSocketException or HttpRequestException)
        {
            // The 401 arrives as the upgrade response, before any WebSocket exists, so only the
            // collected response details tell it apart from a network failure.
            if (socket.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedTransportException(ex);
            }

            throw new TransportException("Could not connect to the server.", ex);
        }
        finally
        {
            if (!connected)
            {
                socket.Dispose();
            }
        }
    }
}

internal sealed class WebSocketTransport : ITransport
{
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

    private readonly ClientWebSocket _socket;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public WebSocketTransport(ClientWebSocket socket)
    {
        _socket = socket;
    }

    public async Task SendAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            throw new TransportException("The connection was closed.", ex);
        }

        try
        {
            // ClientWebSocket still allows sends after the server's close frame (half-closed), but
            // nothing will answer them, so treat anything but Open as a lost connection.
            if (_socket.State != WebSocketState.Open)
            {
                throw new TransportException("The connection is not open.");
            }

            var bytes = Encoding.UTF8.GetBytes(text);
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is WebSocketException or InvalidOperationException or ObjectDisposedException)
        {
            throw new TransportException("The connection was lost while sending.", ex);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async ValueTask<string?> ReceiveAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var message = new MemoryStream();
        while (true)
        {
            ValueWebSocketReceiveResult result;
            try
            {
                result = await _socket.ReceiveAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException)
            {
                // A dropped network and a close frame both mean "the connection has ended".
                return null;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            message.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                return Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            }

            // The protocol is text-only; a binary frame is not for us.
            message.SetLength(0);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                using var timeout = new CancellationTokenSource(CloseTimeout);
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            // Closing is best effort: the connection is being discarded either way.
        }
        finally
        {
            // _sendLock is deliberately not disposed: a send racing this dispose must fail on the socket (a
            // TransportException), not on a disposed lock. A SemaphoreSlim holds no unmanaged resources here.
            _socket.Dispose();
        }
    }
}
