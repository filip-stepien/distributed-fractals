using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Orchestration.Schedulers;
using DistributedFractals.Orchestration.Selectors;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;
using DistributedFractals.Video.Gif;

namespace DistributedFractals.Gui.Networking;

/// <summary>
/// One render job: distributes frames across connected clients via a FrameScheduler,
/// tracks per-frame timing, and writes the result as an animated GIF on completion.
/// Surfaces progress events the GUI consumes to drive RenderView.
/// </summary>
public sealed class GuiRenderJob : IFrameResultReceiver
{
    private readonly GuiServerNode _serverNode;
    private readonly FrameScheduler _scheduler;
    private readonly string _outputPath;
    private readonly int _frameRate;

    private readonly HashSet<ClientIdentifier> _knownClients = new();
    private readonly object _lock = new();

    public int TotalFrames { get; }

    public event Action<ClientIdentifier>? ClientAvailable;
    public event Action<ClientIdentifier, int>? FrameDispatched;
    public event Action<ClientIdentifier, int, TimeSpan>? FrameCompleted;
    public event Action<ClientIdentifier>? ClientFailed;
    public event Action<string>? Completed;
    public event Action<string>? TimingReportReady;
    public event Action<Exception>? Failed;

    public GuiRenderJob(
        GuiServerNode serverNode,
        IEnumerable<RenderFrameMessage> frames,
        string outputPath,
        int frameRate)
    {
        _serverNode = serverNode;
        _outputPath = outputPath;
        _frameRate = frameRate;

        var frameList = new List<RenderFrameMessage>(frames);
        TotalFrames = frameList.Count;

        _scheduler = new FrameScheduler(serverNode.Server, frameList, new RoundRobinClientSelector(), framesPerClient: 1);
        _scheduler.FrameDispatched += (client, frameIndex) => FrameDispatched?.Invoke(client, frameIndex);
        _scheduler.FrameCompleted  += (client, frameIndex, duration) => FrameCompleted?.Invoke(client, frameIndex, duration);
    }

    public Task StartAsync()
    {
        _serverNode.SetCurrentRenderReceiver(this);

        // Subscribe BEFORE snapshotting Clients so we can't miss a client that
        // connects between the snapshot and the subscription. The HashSet de-dupes
        // the case where a client appears in both the snapshot and the event.
        _serverNode.Server.ClientRegistered   += OnClientRegistered;
        _serverNode.Server.ClientUnregistered += OnClientUnregistered;

        foreach (ClientIdentifier client in _serverNode.Server.Clients)
            OnClientRegistered(client);

        return Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        try
        {
            await _scheduler.WaitForAllAsync();

            TimingReportReady?.Invoke(_scheduler.GetTimingReport());

            var writer = new GifVideoWriter(_outputPath, _frameRate, repeat: true);
            foreach (FractalResult frame in _scheduler.GetOrderedResults())
                await writer.WriteFrameAsync(frame);
            await writer.DisposeAsync();

            Completed?.Invoke(_outputPath);
        }
        catch (Exception ex)
        {
            Failed?.Invoke(ex);
        }
        finally
        {
            _serverNode.Server.ClientRegistered   -= OnClientRegistered;
            _serverNode.Server.ClientUnregistered -= OnClientUnregistered;
            _serverNode.SetCurrentRenderReceiver(null);
        }
    }

    private void OnClientRegistered(ClientIdentifier client)
    {
        lock (_lock)
        {
            if (!_knownClients.Add(client)) return;
        }
        _scheduler.OnClientAvailable(client);
        ClientAvailable?.Invoke(client);
    }

    private void OnClientUnregistered(ClientIdentifier client)
    {
        lock (_lock)
        {
            if (!_knownClients.Remove(client)) return;
        }
        _scheduler.OnClientFailed(client);
        ClientFailed?.Invoke(client);
    }

    void IFrameResultReceiver.OnResultReceived(Guid clientId, int frameIndex, FractalResult result, TimeSpan renderDuration)
    {
        _scheduler.OnResultReceived(clientId, frameIndex, result, renderDuration);
    }
}
