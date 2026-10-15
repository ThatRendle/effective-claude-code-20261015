using NotIrc.Core.Transport;

namespace NotIrc.Tests.Fakes;

/// <summary>
/// Hands out scripted connect outcomes in order. When the script runs out, every further attempt fails as
/// an unreachable server would. Attempt times come from the supplied clock, so tests can assert on delays.
/// </summary>
internal sealed class ScriptedTransportFactory(TimeProvider time) : ITransportFactory
{
    private readonly Queue<object> _script = new();
    private readonly List<DateTimeOffset> _attemptTimes = [];
    private readonly List<Uri> _urls = [];
    private readonly Lock _gate = new();

    public IReadOnlyList<DateTimeOffset> AttemptTimes
    {
        get { lock (_gate) { return _attemptTimes.ToList(); } }
    }

    public IReadOnlyList<Uri> Urls
    {
        get { lock (_gate) { return _urls.ToList(); } }
    }

    public int Attempts => AttemptTimes.Count;

    /// <summary>The next attempt succeeds with this transport.</summary>
    public ScriptedTransport EnqueueConnection()
    {
        var transport = new ScriptedTransport();
        lock (_gate)
        {
            _script.Enqueue(transport);
        }

        return transport;
    }

    /// <summary>The next attempt fails with this exception.</summary>
    public void EnqueueFailure(TransportException failure)
    {
        lock (_gate)
        {
            _script.Enqueue(failure);
        }
    }

    public Task<ITransport> ConnectAsync(Uri url, CancellationToken cancellationToken)
    {
        object outcome;
        lock (_gate)
        {
            _attemptTimes.Add(time.GetUtcNow());
            _urls.Add(url);
            outcome = _script.Count > 0 ? _script.Dequeue() : new TransportException("Scripted server is unreachable.");
        }

        return outcome switch
        {
            ScriptedTransport transport => Task.FromResult<ITransport>(transport),
            TransportException failure => Task.FromException<ITransport>(failure),
            _ => throw new InvalidOperationException("Unexpected script entry."),
        };
    }
}
