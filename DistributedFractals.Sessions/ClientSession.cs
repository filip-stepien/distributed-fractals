using DistributedFractals.Fractal.Core;
using DistributedFractals.Logging;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatchers;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Sessions;

public sealed class ClientSession : IClientSession
{
    private IMessageClient? _client;
    private CancellationTokenSource? _heartbeatTokenSource;

    public event Action? Connected;
    public event Action? Disconnected;
    public event Action<int>? FrameStarted;
    public event Action<int, TimeSpan, FractalResult>? FrameCompleted;
    public event Action<int>? FrameFailed;

    private void OnFrameStarted(int frameIndex)
    {
        FrameStarted?.Invoke(frameIndex);
    }

    private void OnFrameCompleted(int frameIndex, TimeSpan duration, FractalResult result)
    {
        FrameCompleted?.Invoke(frameIndex, duration, result);
    }

    private void OnFrameFailed(int frameIndex)
    {
        FrameFailed?.Invoke(frameIndex);
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke();
    }

    private async Task StartHeartbeatLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(interval);
        Logger.Log("Heartbeat loop started.");
        
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await _client!.SendToServerAsync(new HeartbeatMessage(_client.Identifier));
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log("Heartbeat loop stopped.");
        }
    }

    public async Task ConnectAsync(string displayName, ClientConnectionSettings connectionSettings)
    {
        ITransportFactory factory = TransportFactoryResolver.Create(connectionSettings);

        _client = factory.CreateClient();

        IMessageDispatcher dispatcher = MessageDispatcherFactory.CreateClient(
            _client,
            onFrameStarted: OnFrameStarted,
            onFrameCompleted: OnFrameCompleted,
            onFrameFailed: OnFrameFailed,
            onDisconnected: OnDisconnected
        );

        _client.MessageReceived += async msg =>
        {
            await dispatcher.DispatchAsync(msg);
        };

        await _client.StartAsync();
        await _client.SendToServerAsync(new JoinMessage(_client.Identifier, displayName));

        Connected?.Invoke();

        _heartbeatTokenSource = new CancellationTokenSource();
        _ = StartHeartbeatLoopAsync(connectionSettings.HeartbeatInterval, _heartbeatTokenSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_heartbeatTokenSource is not null)
        {
            await _heartbeatTokenSource.CancelAsync();
        }

        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
    }
}
