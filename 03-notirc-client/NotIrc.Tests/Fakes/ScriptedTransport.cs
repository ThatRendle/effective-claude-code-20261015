using System.Threading.Channels;
using NotIrc.Core.Transport;

namespace NotIrc.Tests.Fakes;

/// <summary>An <see cref="ITransport"/> the test drives by hand: queue inbound frames, drop it, read what was sent.</summary>
internal sealed class ScriptedTransport : ITransport
{
    private readonly Channel<string?> _inbound = Channel.CreateUnbounded<string?>();
    private readonly List<string> _sent = [];
    private readonly Lock _gate = new();
    private bool _closed;
    private bool _ended;

    public bool Disposed { get; private set; }

    public IReadOnlyList<string> Sent
    {
        get { lock (_gate) { return _sent.ToList(); } }
    }

    /// <summary>Delivers a frame to the client.</summary>
    public void Receive(string frame) => _inbound.Writer.TryWrite(frame);

    /// <summary>Ends the connection as the client sees a network drop: receive returns null, sends throw.</summary>
    public void Drop()
    {
        lock (_gate)
        {
            _closed = true;
        }

        _inbound.Writer.TryWrite(null);
    }

    public Task SendAsync(string text, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_closed)
            {
                throw new TransportException("Scripted connection is closed.");
            }

            _sent.Add(text);
        }

        return Task.CompletedTask;
    }

    public async ValueTask<string?> ReceiveAsync(CancellationToken cancellationToken)
    {
        if (_ended)
        {
            return null;
        }

        var frame = await _inbound.Reader.ReadAsync(cancellationToken);
        if (frame is null)
        {
            _ended = true;
        }

        return frame;
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            _closed = true;
        }

        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
