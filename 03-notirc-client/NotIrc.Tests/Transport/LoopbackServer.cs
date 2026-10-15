using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace NotIrc.Tests.Transport;

/// <summary>
/// A minimal in-process server on 127.0.0.1. Requests with the wrong token get HTTP 401 before any
/// upgrade, as notIRC does; others are upgraded and handed to <paramref name="onSocket"/>.
/// </summary>
internal sealed class LoopbackServer : IDisposable
{
    public const string GoodToken = "good";

    private readonly HttpListener _listener = new();
    private readonly Task _loop;

    public LoopbackServer(Func<WebSocket, Task> onSocket)
    {
        Port = FreePort();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        _listener.Start();
        _loop = Task.Run(() => AcceptLoopAsync(onSocket));
    }

    public int Port { get; }

    public Uri Url(string token) => new($"ws://127.0.0.1:{Port}/ws?token={token}");

    /// <summary>A URL on a port where nothing is listening.</summary>
    public static Uri ClosedPortUrl() => new($"ws://127.0.0.1:{FreePort()}/ws?token={GoodToken}");

    public void Dispose()
    {
        _listener.Close();
        try
        {
            _loop.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // The accept loop ends with an exception when the listener closes.
        }
    }

    private async Task AcceptLoopAsync(Func<WebSocket, Task> onSocket)
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
            {
                return;
            }

            if (context.Request.QueryString["token"] != GoodToken)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.Close();
                continue;
            }

            var accepted = await context.AcceptWebSocketAsync(subProtocol: null);
            _ = Task.Run(() => onSocket(accepted.WebSocket));
        }
    }

    private static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        try
        {
            return ((IPEndPoint)probe.LocalEndpoint).Port;
        }
        finally
        {
            probe.Stop();
        }
    }
}
