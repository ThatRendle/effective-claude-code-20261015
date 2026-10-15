using System.Net.WebSockets;
using System.Text;
using NotIrc.Core.Transport;

namespace NotIrc.Tests.Transport;

public class WebSocketTransportTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Bad_token_is_reported_as_unauthorized()
    {
        using var server = new LoopbackServer(_ => Task.CompletedTask);
        using var cts = new CancellationTokenSource(Timeout);

        await Assert.ThrowsAsync<UnauthorizedTransportException>(
            () => new WebSocketTransportFactory().ConnectAsync(server.Url("wrong"), cts.Token));
    }

    [Fact]
    public async Task Refused_connection_is_a_transport_failure_but_not_unauthorized()
    {
        using var cts = new CancellationTokenSource(Timeout);

        var ex = await Assert.ThrowsAnyAsync<TransportException>(
            () => new WebSocketTransportFactory().ConnectAsync(LoopbackServer.ClosedPortUrl(), cts.Token));

        Assert.IsNotType<UnauthorizedTransportException>(ex);
    }

    [Fact]
    public async Task Sent_text_reaches_the_server_and_the_reply_is_received()
    {
        using var server = new LoopbackServer(EchoOnceAsync);
        using var cts = new CancellationTokenSource(Timeout);
        await using var transport = await new WebSocketTransportFactory()
            .ConnectAsync(server.Url(LoopbackServer.GoodToken), cts.Token);

        await transport.SendAsync("héllo \U0001F600", cts.Token);
        var reply = await transport.ReceiveAsync(cts.Token);

        Assert.Equal("héllo \U0001F600", reply);
    }

    [Fact]
    public async Task Receive_returns_null_once_the_server_closes()
    {
        using var server = new LoopbackServer(async socket =>
        {
            await SendTextAsync(socket, "last words");
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        });
        using var cts = new CancellationTokenSource(Timeout);
        await using var transport = await new WebSocketTransportFactory()
            .ConnectAsync(server.Url(LoopbackServer.GoodToken), cts.Token);

        Assert.Equal("last words", await transport.ReceiveAsync(cts.Token));
        Assert.Null(await transport.ReceiveAsync(cts.Token));
    }

    [Fact]
    public async Task Receive_returns_null_when_the_server_drops_without_a_close_frame()
    {
        using var server = new LoopbackServer(socket =>
        {
            socket.Dispose();
            return Task.CompletedTask;
        });
        using var cts = new CancellationTokenSource(Timeout);
        await using var transport = await new WebSocketTransportFactory()
            .ConnectAsync(server.Url(LoopbackServer.GoodToken), cts.Token);

        Assert.Null(await transport.ReceiveAsync(cts.Token));
    }

    [Fact]
    public async Task Send_after_the_server_has_closed_throws_a_transport_exception()
    {
        using var server = new LoopbackServer(async socket =>
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None));
        using var cts = new CancellationTokenSource(Timeout);
        await using var transport = await new WebSocketTransportFactory()
            .ConnectAsync(server.Url(LoopbackServer.GoodToken), cts.Token);
        Assert.Null(await transport.ReceiveAsync(cts.Token));

        await Assert.ThrowsAsync<TransportException>(() => transport.SendAsync("too late", cts.Token));
    }

    [Fact]
    public async Task Cancelling_receive_throws_rather_than_reporting_a_close()
    {
        using var server = new LoopbackServer(socket => Task.Delay(Timeout));
        using var connectCts = new CancellationTokenSource(Timeout);
        await using var transport = await new WebSocketTransportFactory()
            .ConnectAsync(server.Url(LoopbackServer.GoodToken), connectCts.Token);
        using var receiveCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await transport.ReceiveAsync(receiveCts.Token));
    }

    private static async Task EchoOnceAsync(WebSocket socket)
    {
        var buffer = new byte[1024];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        await socket.SendAsync(
            buffer.AsMemory(0, result.Count), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
    }

    private static Task SendTextAsync(WebSocket socket, string text) =>
        socket.SendAsync(
            Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
}
