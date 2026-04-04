using DistributedFractals.Core.Core;
using DistributedFractals.Core.Zoom;
using DistributedFractals.Orchestration.Schedulers;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Dispatching;
using DistributedFractals.Server.Heartbeat;
using DistributedFractals.Server.Messages;
using DistributedFractals.Video;

namespace DistributedFractals.Sessions;

public sealed class ServerSession : IServerSession
{
    private HeartbeatMessageServer? _server;
    private FrameScheduler? _scheduler;
    private IMessageDispatcher? _dispatcher;
    private string? _outputPath;
    private int _fps;
    private VideoFormat _outputFormat;

    public event Action<ClientIdentifier>? ClientConnected;
    public event Action<ClientIdentifier>? ClientDisconnected;
    public event Action<ClientIdentifier, int>? FrameDispatched;
    public event Action<ClientIdentifier, int, TimeSpan>? FrameCompleted;
    public event Action<ClientIdentifier, int>? FrameFailed;
    public event Action? RenderCompleted;

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

    private void OnRenderCompleted()
    {
        _ = SaveAsync(_outputPath!, _fps, _outputFormat);
        RenderCompleted?.Invoke();
    }

    private async void OnMessageReceived(BaseMessage msg)
    {
        await _dispatcher!.DispatchAsync(msg);
    }

    private async Task SaveAsync(string outputPath, int fps, VideoFormat format)
    {
        IVideoWriter writer = VideoWriterFactory.Create(outputPath, fps, format);

        foreach (FractalResult frame in _scheduler!.GetOrderedResults())
        {
            await writer.WriteFrameAsync(frame);
        }

        await writer.DisposeAsync();
    }

    private IEnumerable<RenderFrameMessage> BuildFrames(RenderSettings settings)
    {
        IEnumerable<FrameBounds> frameBounds = new KeyframeZoomSequenceGenerator()
            .Generate(
                options: settings.Options,
                keyframes: settings.Keyframes,
                totalFrames: settings.TotalFrames,
                interpolation: settings.Interpolation
            );

        return frameBounds.Select(
            (bounds, i) => new RenderFrameMessage(
                Sender: _server!.Identifier,
                FrameIndex: i,
                GeneratorType: settings.GeneratorType,
                ColorizerType: settings.Colorizer,
                Options: settings.Options,
                Bounds: bounds
            )
        );
    }

    public Task StartAsync(ConnectionSettings connectionSettings)
    {
        ITransportFactory factory = TransportFactoryResolver.FromConnectionSettings(connectionSettings);

        _server = new HeartbeatMessageServer(factory.CreateServer(), connectionSettings.ClientTimeout);

        _server.ClientRegistered += OnClientRegistered;
        _server.ClientUnregistered += OnClientUnregistered;

        return _server.StartAsync();
    }

    public Task StartRenderAsync(RenderSettings renderSettings)
    {
        if (_server is null)
        {
            throw new InvalidOperationException("Call StartAsync first.");
        }

        _outputPath = renderSettings.OutputPath;
        _fps = renderSettings.Fps;
        _outputFormat = renderSettings.OutputFormat;

        _scheduler = new FrameScheduler(
            server: _server,
            frames: BuildFrames(renderSettings),
            clientSelector: renderSettings.ClientSelector,
            framesPerClient: renderSettings.FramesPerClient
        );

        _scheduler.FrameDispatched += OnFrameDispatched;
        _scheduler.FrameCompleted += OnFrameCompleted;
        _scheduler.FrameFailed += OnFrameFailed;
        _scheduler.RenderCompleted += OnRenderCompleted;

        _dispatcher = ServerMessageDispatcherFactory.Create(_server, _scheduler);

        _server.MessageReceived += OnMessageReceived;
        _server.ClientRegistered += _scheduler.OnClientAvailable;
        _server.ClientUnregistered += _scheduler.OnClientFailed;

        foreach (ClientIdentifier client in _server.Clients)
        {
            _scheduler.OnClientAvailable(client);
        }

        return Task.CompletedTask;
    }

    public void CancelRender()
    {
        _scheduler?.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        if (_server is not null)
        {
            await _server.DisposeAsync();
        }
    }
}
