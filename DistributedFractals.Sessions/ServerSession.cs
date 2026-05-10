using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Zoom;
using DistributedFractals.Orchestration.Schedulers;
using DistributedFractals.Orchestration.Selectors;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatchers;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;
using DistributedFractals.Video;

namespace DistributedFractals.Sessions;

public sealed class ServerSession : IServerSession, IFrameResultReceiver
{
    private HeartbeatMessageServer? _server;
    private FrameScheduler? _scheduler;
    private IMessageDispatcher? _dispatcher;

    public event Action<ClientIdentifier>? ClientConnected;
    public event Action<ClientIdentifier>? ClientDisconnected;
    public event Action<ClientIdentifier, int>? FrameDispatched;
    public event Action<ClientIdentifier, int, TimeSpan>? FrameCompleted;
    public event Action<ClientIdentifier, int>? FrameFailed;
    public event Action<string>? TimingReportReady;
    public event Action? RenderCompleted;
    public event Action<Exception>? RenderFailed;

    public IReadOnlyCollection<ClientIdentifier> Clients => _server?.Clients ?? [];

    private void OnClientRegistered(ClientIdentifier client)
    {
        ClientConnected?.Invoke(client);
    }

    private void OnClientUnregistered(ClientIdentifier client)
    {
        ClientDisconnected?.Invoke(client);
    }

    private void OnFrameDispatched(ClientIdentifier client, int frameIndex)
    {
        FrameDispatched?.Invoke(client, frameIndex);
    }

    private void OnFrameCompleted(ClientIdentifier client, int frameIndex, TimeSpan duration)
    {
        FrameCompleted?.Invoke(client, frameIndex, duration);
    }

    private void OnFrameFailed(ClientIdentifier client, int frameIndex)
    {
        FrameFailed?.Invoke(client, frameIndex);
    }

    private async void OnMessageReceived(BaseMessage msg)
    {
        await _dispatcher!.DispatchAsync(msg);
    }

    private async Task SaveAsync(FrameScheduler scheduler, string outputPath, int fps, VideoFormat format)
    {
        IVideoWriter writer = VideoWriterFactory.Create(outputPath, fps, format);

        foreach (FractalResult frame in scheduler.GetOrderedResults())
        {
            await writer.WriteFrameAsync(frame);
        }

        await writer.DisposeAsync();
    }

    private async Task CompleteRenderAsync(FrameScheduler scheduler, RenderSettings settings)
    {
        try
        {
            await scheduler.WaitForAllAsync();
            TimingReportReady?.Invoke($"Render wall-clock time: {scheduler.RenderElapsed.TotalSeconds:F3}s");
            TimingReportReady?.Invoke(scheduler.GetTimingReport());
            await SaveAsync(scheduler, settings.OutputPath, settings.Fps, settings.OutputFormat);
            RenderCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            RenderFailed?.Invoke(ex);
        }
        finally
        {
            DetachScheduler(scheduler);
        }
    }

    private IEnumerable<RenderFrameMessage> BuildFrames(RenderSettings settings)
    {
        IEnumerable<FrameBounds> frameBounds = new KeyframeZoomSequenceGenerator()
            .Generate(
                options: settings.Options,
                keyframes: settings.Keyframes,
                totalFrames: settings.TotalFrames,
                interpolation: ZoomInterpolationFactory.Create(settings.Interpolation)
            );

        return frameBounds.Select(
            (bounds, i) => new RenderFrameMessage(
                Sender: _server!.Identifier,
                FrameIndex: i,
                ColorizerType: settings.Colorizer,
                Options: settings.Options,
                Bounds: bounds
            )
        );
    }

    public Task StartAsync(ServerConnectionSettings connectionSettings)
    {
        ITransportFactory factory = TransportFactoryResolver.Create(connectionSettings);

        _server = new HeartbeatMessageServer(factory.CreateServer(), connectionSettings.ClientTimeout);
        _dispatcher = ServerMessageDispatcherFactory.Create(_server, this);

        _server.ClientRegistered += OnClientRegistered;
        _server.ClientUnregistered += OnClientUnregistered;
        _server.MessageReceived += OnMessageReceived;

        return _server.StartAsync();
    }

    public Task StartRenderAsync(RenderSettings renderSettings)
    {
        if (_server is null)
        {
            throw new InvalidOperationException("Call StartAsync first.");
        }

        CancelRender();

        _scheduler = new FrameScheduler(
            server: _server,
            frames: BuildFrames(renderSettings),
            clientSelector: ClientSelectorFactory.Create(renderSettings.ClientSelector),
            framesPerBatch: renderSettings.FramesPerBatch
        );

        _scheduler.FrameDispatched += OnFrameDispatched;
        _scheduler.FrameCompleted += OnFrameCompleted;
        _scheduler.FrameFailed += OnFrameFailed;

        _server.ClientRegistered += _scheduler.OnClientAvailable;
        _server.ClientUnregistered += _scheduler.OnClientFailed;

        foreach (ClientIdentifier client in _server.Clients)
        {
            _scheduler.OnClientAvailable(client);
        }

        _ = CompleteRenderAsync(_scheduler, renderSettings);

        return Task.CompletedTask;
    }

    public void CancelRender()
    {
        FrameScheduler? scheduler = _scheduler;
        if (scheduler is null)
        {
            return;
        }

        scheduler.Cancel();
        DetachScheduler(scheduler);
    }

    public string? GetClientAddress(ClientIdentifier client)
    {
        return _server?.GetClientAddress(client.Id);
    }

    void IFrameResultReceiver.OnResultReceived(Guid clientId, int frameIndex, FractalResult result, TimeSpan renderDuration)
    {
        _scheduler?.OnResultReceived(clientId, frameIndex, result, renderDuration);
    }

    void IFrameResultReceiver.OnBatchResultReceived(Guid clientId, int batchId, IReadOnlyList<RenderFrameResult> results, TimeSpan renderDuration)
    {
        _scheduler?.OnBatchResultReceived(clientId, batchId, results, renderDuration);
    }

    private void DetachScheduler(FrameScheduler scheduler)
    {
        if (_server is not null)
        {
            _server.ClientRegistered -= scheduler.OnClientAvailable;
            _server.ClientUnregistered -= scheduler.OnClientFailed;
        }

        scheduler.FrameDispatched -= OnFrameDispatched;
        scheduler.FrameCompleted -= OnFrameCompleted;
        scheduler.FrameFailed -= OnFrameFailed;

        if (ReferenceEquals(_scheduler, scheduler))
        {
            _scheduler = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        CancelRender();

        if (_server is not null)
        {
            _server.MessageReceived -= OnMessageReceived;
            _server.ClientRegistered -= OnClientRegistered;
            _server.ClientUnregistered -= OnClientUnregistered;
            await _server.DisposeAsync();
        }
    }
}
