using System.Linq;
using System.Text;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Logging;
using DistributedFractals.Orchestration.Selectors;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Orchestration.Schedulers;

public sealed class FrameScheduler : IFrameScheduler
{
    private readonly IMessageServer _server;
    private readonly IClientSelector _clientSelector;
    private readonly int _framesPerClient;
    private readonly int _totalFrames;

    private readonly Queue<RenderFrameMessage> _pending;
    private readonly Dictionary<ClientIdentifier, List<(RenderFrameMessage msg, DateTime dispatchedAt)>> _inFlight = new();
    private readonly SortedDictionary<int, FractalResult> _completed = new();
    private readonly List<FrameTiming> _timings = new();
    private readonly object _lock = new();
    private readonly TaskCompletionSource _allDone = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public event Action<ClientIdentifier, int>? FrameDispatched;
    public event Action<ClientIdentifier, int, TimeSpan>? FrameCompleted;
    public event Action<ClientIdentifier, int>? FrameFailed;
    public event Action? RenderCompleted;

    public FrameScheduler(
        IMessageServer server,
        IEnumerable<RenderFrameMessage> frames,
        IClientSelector clientSelector,
        int framesPerClient = 1)
    {
        _server = server;
        _clientSelector = clientSelector;
        _framesPerClient = framesPerClient;
        _pending = new Queue<RenderFrameMessage>(frames);
        _totalFrames = _pending.Count;

        if (_totalFrames == 0)
        {
            _allDone.TrySetResult();
        }
    }

    public Task WaitForAllAsync() => _allDone.Task;

    public IReadOnlyList<FractalResult> GetOrderedResults()
    {
        lock (_lock)
        {
            return _completed.Values.ToList();
        }
    }

    public string GetTimingReport()
    {
        lock (_lock)
        {
            if (_timings.Count == 0)
                return "No timing data available.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Render Timing Report ===");

            IEnumerable<IGrouping<string, FrameTiming>> byClient = _timings
                .OrderBy(t => t.FrameIndex)
                .GroupBy(t => t.Client.DisplayName ?? t.Client.Id.ToString());

            int clientCount = byClient.Count();
            sb.AppendLine($"Frames: {_timings.Count}  |  Clients: {clientCount}");
            sb.AppendLine();
            sb.AppendLine($"{"Frame",-6}  {"Client",-16}  {"Roundtrip",10}  {"Render",10}  {"Comm",10}");
            sb.AppendLine($"{"------",-6}  {"----------------",-16}  {"----------",10}  {"----------",10}  {"----------",10}");

            foreach (FrameTiming t in _timings.OrderBy(t => t.FrameIndex))
            {
                string clientLabel = t.Client.DisplayName ?? t.Client.Id.ToString();
                sb.AppendLine($"{t.FrameIndex,-6}  {clientLabel,-16}  {t.Roundtrip.TotalSeconds,9:F3}s  {t.RenderDuration.TotalSeconds,9:F3}s  {t.CommunicationOverhead.TotalSeconds,9:F3}s");
            }

            sb.AppendLine();
            sb.AppendLine("--- Summary ---");
            double avgRoundtrip = _timings.Average(t => t.Roundtrip.TotalSeconds);
            double avgRender    = _timings.Average(t => t.RenderDuration.TotalSeconds);
            double avgComm      = _timings.Average(t => t.CommunicationOverhead.TotalSeconds);
            sb.AppendLine($"  Avg roundtrip:       {avgRoundtrip:F3}s");
            sb.AppendLine($"  Avg render:          {avgRender:F3}s");
            sb.AppendLine($"  Avg comm overhead:   {avgComm:F3}s");

            sb.AppendLine();
            sb.AppendLine("--- Per client ---");
            foreach (IGrouping<string, FrameTiming> group in byClient)
            {
                int frames       = group.Count();
                double avgRt     = group.Average(t => t.Roundtrip.TotalSeconds);
                double avgRnd    = group.Average(t => t.RenderDuration.TotalSeconds);
                double avgCm     = group.Average(t => t.CommunicationOverhead.TotalSeconds);
                sb.AppendLine($"  {group.Key,-16}  {frames} frame(s)  avg roundtrip {avgRt:F3}s  avg render {avgRnd:F3}s  avg comm {avgCm:F3}s");
            }

            return sb.ToString().TrimEnd();
        }
    }

    public void Cancel()
    {
        lock (_lock)
        {
            _pending.Clear();
            _allDone.TrySetCanceled();
        }
    }

    public void OnClientAvailable(ClientIdentifier client)
    {
        lock (_lock)
        {
            _inFlight.TryAdd(client, new List<(RenderFrameMessage msg, DateTime dispatchedAt)>());
            TryDispatch();
        }
    }

    public void OnResultReceived(Guid clientId, int frameIndex, FractalResult result, TimeSpan renderDuration)
    {
        lock (_lock)
        {
            ClientIdentifier? client = _inFlight.Keys.FirstOrDefault(c => c.Id == clientId);
            if (client is null)
            {
                return;
            }

            DateTime dispatchedAt = default;
            List<(RenderFrameMessage msg, DateTime dispatchedAt)> frames = _inFlight[client];
            (RenderFrameMessage msg, DateTime dispatchedAt) match = frames.FirstOrDefault(f => f.msg.FrameIndex == frameIndex);
            dispatchedAt = match.dispatchedAt;
            frames.RemoveAll(f => f.msg.FrameIndex == frameIndex);

            TimeSpan roundtrip = dispatchedAt != default ? DateTime.UtcNow - dispatchedAt : TimeSpan.Zero;
            _timings.Add(new FrameTiming(client, frameIndex, roundtrip, renderDuration));

            _completed[frameIndex] = result;
            Logger.Log($"Frame {frameIndex} received from client {client.DisplayName} ({_completed.Count}/{_totalFrames}).");
            FrameCompleted?.Invoke(client, frameIndex, roundtrip);

            if (_completed.Count == _totalFrames)
            {
                RenderCompleted?.Invoke();
                _allDone.TrySetResult();
                return;
            }

            TryDispatch();
        }
    }

    public void OnClientFailed(ClientIdentifier client)
    {
        lock (_lock)
        {
            if (_inFlight.Remove(client, out List<(RenderFrameMessage msg, DateTime dispatchedAt)>? frames))
            {
                foreach ((RenderFrameMessage msg, DateTime dispatchedAt) frame in frames)
                {
                    Logger.Log($"Re-queuing frame {frame.msg.FrameIndex} after client {client.DisplayName} failed.");
                    _pending.Enqueue(frame.msg);
                    FrameFailed?.Invoke(client, frame.msg.FrameIndex);
                }
            }

            TryDispatch();
        }
    }

    private void TryDispatch()
    {
        while (_pending.Count > 0)
        {
            List<ClientIdentifier> clientsWithCapacity = _inFlight
                .Where(kv => kv.Value.Count < _framesPerClient)
                .Select(kv => kv.Key)
                .ToList();

            ClientIdentifier? client = _clientSelector.Select(clientsWithCapacity);
            if (client is null)
            {
                break;
            }

            RenderFrameMessage msg = _pending.Dequeue();
            _inFlight[client].Add((msg, DateTime.UtcNow));
            Logger.Log($"Sending frame {msg.FrameIndex} to client {client.DisplayName}.");
            FrameDispatched?.Invoke(client, msg.FrameIndex);
            _ = _server.SendToClientAsync(client, msg);
        }
    }
}
