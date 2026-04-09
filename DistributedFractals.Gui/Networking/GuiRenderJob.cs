using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedFractals.Core.Core;
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
    private readonly TrackingMessageServer _trackingServer;
    private readonly FrameScheduler _scheduler;
    private readonly string _outputPath;
    private readonly int _frameRate;

    private readonly Dictionary<int, DateTime> _dispatchedAt = new();
    private readonly HashSet<Guid> _knownClients = new();
    private readonly object _lock = new();

    public int TotalFrames { get; }

    public event Action<Guid>? ClientAvailable;
    public event Action<Guid, int>? FrameDispatched;
    public event Action<Guid, int, TimeSpan>? FrameCompleted;
    public event Action<Guid>? ClientFailed;
    public event Action<string>? Completed;
    public event Action<Exception>? Failed;

    public GuiRenderJob(
        GuiServerNode serverNode,
        IEnumerable<(int index, RenderFractalMessage msg)> frames,
        string outputPath,
        int frameRate)
    {
        _serverNode = serverNode;
        _outputPath = outputPath;
        _frameRate = frameRate;

        var frameList = new List<(int, RenderFractalMessage)>(frames);
        TotalFrames = frameList.Count;

        _trackingServer = new TrackingMessageServer(serverNode.Server);
        _trackingServer.RenderFrameSent += OnFrameSent;

        _scheduler = new FrameScheduler(_trackingServer, frameList, new RoundRobinClientSelector(), framesPerWorker: 1);
    }

    public Task StartAsync()
    {
        _serverNode.SetCurrentRenderReceiver(this);

        // Subscribe BEFORE snapshotting Clients so we can't miss a client that
        // connects between the snapshot and the subscription. The HashSet de-dupes
        // the case where a client appears in both the snapshot and the event.
        _serverNode.Server.ClientRegistered   += OnClientRegistered;
        _serverNode.Server.ClientUnregistered += OnClientUnregistered;

        foreach (Guid id in _serverNode.Server.Clients)
            OnClientRegistered(id);

        return Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        try
        {
            await _scheduler.WaitForAllAsync();

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

    private void OnClientRegistered(Guid id)
    {
        lock (_lock)
        {
            if (!_knownClients.Add(id)) return;
        }
        _scheduler.OnClientAvailable(id);
        ClientAvailable?.Invoke(id);
    }

    private void OnClientUnregistered(Guid id)
    {
        lock (_lock)
        {
            if (!_knownClients.Remove(id)) return;
        }
        _scheduler.OnClientFailed(id);
        ClientFailed?.Invoke(id);
    }

    private void OnFrameSent(Guid clientId, RenderFractalMessage msg)
    {
        lock (_lock) _dispatchedAt[msg.FrameIndex] = DateTime.UtcNow;
        FrameDispatched?.Invoke(clientId, msg.FrameIndex);
    }

    void IFrameResultReceiver.OnResultReceived(Guid client, int frameIndex, FractalResult result)
    {
        TimeSpan duration;
        lock (_lock)
        {
            if (_dispatchedAt.TryGetValue(frameIndex, out DateTime started))
            {
                duration = DateTime.UtcNow - started;
                _dispatchedAt.Remove(frameIndex);
            }
            else
            {
                duration = TimeSpan.Zero;
            }
        }

        _scheduler.OnResultReceived(client, frameIndex, result);
        FrameCompleted?.Invoke(client, frameIndex, duration);
    }
}
