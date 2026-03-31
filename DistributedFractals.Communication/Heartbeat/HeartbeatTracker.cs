namespace DistributedFractals.Server.Heartbeat;

public sealed class HeartbeatTracker(Guid client, TimeSpan timeout) : IAsyncDisposable
{
    public event Action<Guid>? ClientDead;

    private readonly PeriodicTimer _timer = new(timeout);
    private readonly CancellationTokenSource _cts = new();
    private DateTime _lastHeartbeat;

    public void Start()
    {
        _lastHeartbeat = DateTime.UtcNow;
        _ = CheckLoopAsync(_cts.Token);
    }

    public void RecordHeartbeat()
    {
        _lastHeartbeat = DateTime.UtcNow;
    }

    private async Task CheckLoopAsync(CancellationToken cancellationToken)
    {
        while (await _timer.WaitForNextTickAsync(cancellationToken))
        {
            if (DateTime.UtcNow - _lastHeartbeat > timeout)
            {
                ClientDead?.Invoke(client);
                return;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _timer.Dispose();
    }
}
