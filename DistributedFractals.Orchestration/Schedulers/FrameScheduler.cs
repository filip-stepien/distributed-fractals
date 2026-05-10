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
    private readonly int _framesPerBatch;
    private readonly int _maxBatchesPerClient;
    private readonly int _totalFrames;
    private readonly System.Diagnostics.Stopwatch _renderClock = System.Diagnostics.Stopwatch.StartNew();

    private readonly Queue<RenderBatchMessage> _pending;
    private readonly Dictionary<ClientIdentifier, List<(RenderBatchMessage msg, DateTime dispatchedAt)>> _inFlight = new();
    private readonly SortedDictionary<int, FractalResult> _completed = new();
    private readonly List<FrameTiming> _timings = new();
    private readonly List<BatchTiming> _batchTimings = new();
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
        int framesPerBatch = 1,
        int maxBatchesPerClient = 1)
    {
        _server = server;
        _clientSelector = clientSelector;
        _framesPerBatch = Math.Max(1, framesPerBatch);
        _maxBatchesPerClient = Math.Max(1, maxBatchesPerClient);

        List<RenderFrameMessage> frameList = frames.ToList();
        _pending = BuildBatches(frameList);
        _totalFrames = frameList.Count;

        if (_totalFrames == 0)
        {
            _renderClock.Stop();
            _allDone.TrySetResult();
        }
    }

    public TimeSpan RenderElapsed => _renderClock.Elapsed;

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

            var sb = new StringBuilder();
            sb.AppendLine("=== Render Timing Report ===");

            IEnumerable<IGrouping<string, FrameTiming>> framesByClient = _timings
                .OrderBy(t => t.FrameIndex)
                .GroupBy(t => t.Client.DisplayName ?? t.Client.Id.ToString());

            IEnumerable<IGrouping<string, BatchTiming>> batchesByClient = _batchTimings
                .OrderBy(t => t.BatchId)
                .GroupBy(t => t.Client.DisplayName ?? t.Client.Id.ToString());

            int clientCount = framesByClient.Count();
            sb.AppendLine($"Frames: {_timings.Count}  |  Batches: {_batchTimings.Count}  |  Clients: {clientCount}  |  Frames/batch: {_framesPerBatch}");
            sb.AppendLine();
            sb.AppendLine($"{"Frame",-6}  {"Batch",-6}  {"Client",-16}  {"Render",10}");
            sb.AppendLine($"{"------",-6}  {"------",-6}  {"----------------",-16}  {"----------",10}");

            foreach (FrameTiming t in _timings.OrderBy(t => t.FrameIndex))
            {
                string clientLabel = t.Client.DisplayName ?? t.Client.Id.ToString();
                sb.AppendLine($"{t.FrameIndex,-6}  {t.BatchId,-6}  {clientLabel,-16}  {t.RenderDuration.TotalSeconds,9:F3}s");
            }

            sb.AppendLine();
            sb.AppendLine("--- Batches ---");
            sb.AppendLine($"{"Batch",-6}  {"Frames",-9}  {"Client",-16}  {"Roundtrip",10}  {"Render",10}  {"Comm",10}");
            sb.AppendLine($"{"------",-6}  {"---------",-9}  {"----------------",-16}  {"----------",10}  {"----------",10}  {"----------",10}");

            foreach (BatchTiming t in _batchTimings.OrderBy(t => t.BatchId))
            {
                string clientLabel = t.Client.DisplayName ?? t.Client.Id.ToString();
                string range = t.FirstFrameIndex == t.LastFrameIndex
                    ? t.FirstFrameIndex.ToString()
                    : $"{t.FirstFrameIndex}-{t.LastFrameIndex}";
                sb.AppendLine($"{t.BatchId,-6}  {range,-9}  {clientLabel,-16}  {t.Roundtrip.TotalSeconds,9:F3}s  {t.RenderDuration.TotalSeconds,9:F3}s  {t.CommunicationOverhead.TotalSeconds,9:F3}s");
            }

            sb.AppendLine();
            sb.AppendLine("--- Summary ---");
            double avgFrameRender = _timings.Average(t => t.RenderDuration.TotalSeconds);
            double avgBatchRoundtrip = _batchTimings.Average(t => t.Roundtrip.TotalSeconds);
            double avgBatchRender = _batchTimings.Average(t => t.RenderDuration.TotalSeconds);
            double avgBatchComm = _batchTimings.Average(t => t.CommunicationOverhead.TotalSeconds);
            sb.AppendLine($"  Avg frame render:    {avgFrameRender:F3}s");
            sb.AppendLine($"  Avg batch roundtrip: {avgBatchRoundtrip:F3}s");
            sb.AppendLine($"  Avg batch render:    {avgBatchRender:F3}s");
            sb.AppendLine($"  Avg batch comm:      {avgBatchComm:F3}s");

            sb.AppendLine();
            sb.AppendLine("--- Per client ---");
            foreach (IGrouping<string, FrameTiming> frameGroup in framesByClient)
            {
                int frames = frameGroup.Count();
                int batches = batchesByClient.FirstOrDefault(g => g.Key == frameGroup.Key)?.Count() ?? 0;
                double avgFrameRenderForClient = frameGroup.Average(t => t.RenderDuration.TotalSeconds);
                sb.AppendLine($"  {frameGroup.Key,-16}  {frames} frame(s)  {batches} batch(es)  avg frame render {avgFrameRenderForClient:F3}s");
            }

            return sb.ToString().TrimEnd();
        }
    }

    public void Cancel()
    {
        lock (_lock)
        {
            _pending.Clear();
            _renderClock.Stop();
            _allDone.TrySetCanceled();
        }
    }

    public void OnClientAvailable(ClientIdentifier client)
    {
        lock (_lock)
        {
            _inFlight.TryAdd(client, new List<(RenderBatchMessage msg, DateTime dispatchedAt)>());
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

            if (!TryTakeBatchContainingFrame(client, frameIndex, out RenderBatchMessage? batch, out DateTime dispatchedAt))
            {
                return;
            }

            CompleteBatch(
                client,
                batch,
                dispatchedAt,
                [new RenderFrameResult(frameIndex, result, renderDuration)],
                renderDuration
            );
        }
    }

    public void OnBatchResultReceived(Guid clientId, int batchId, IReadOnlyList<RenderFrameResult> results, TimeSpan renderDuration)
    {
        lock (_lock)
        {
            ClientIdentifier? client = _inFlight.Keys.FirstOrDefault(c => c.Id == clientId);
            if (client is null)
            {
                return;
            }

            if (!TryTakeBatch(client, batchId, out RenderBatchMessage? batch, out DateTime dispatchedAt))
            {
                return;
            }

            CompleteBatch(client, batch, dispatchedAt, results, renderDuration);
        }
    }

    public void OnClientFailed(ClientIdentifier client)
    {
        lock (_lock)
        {
            if (_inFlight.Remove(client, out List<(RenderBatchMessage msg, DateTime dispatchedAt)>? batches))
            {
                foreach ((RenderBatchMessage msg, DateTime dispatchedAt) batch in batches)
                {
                    Logger.Log($"Re-queuing batch {batch.msg.BatchId} after client {client.DisplayName} failed.");
                    _pending.Enqueue(batch.msg);

                    foreach (RenderFrameMessage frame in batch.msg.Frames)
                    {
                        FrameFailed?.Invoke(client, frame.FrameIndex);
                    }
                }
            }

            TryDispatch();
        }
    }

    private Queue<RenderBatchMessage> BuildBatches(IReadOnlyList<RenderFrameMessage> frames)
    {
        Queue<RenderBatchMessage> batches = new();
        int batchId = 0;

        for (int i = 0; i < frames.Count; i += _framesPerBatch)
        {
            List<RenderFrameMessage> batchFrames = frames
                .Skip(i)
                .Take(_framesPerBatch)
                .ToList();

            batches.Enqueue(new RenderBatchMessage(_server.Identifier, batchId++, batchFrames));
        }

        return batches;
    }

    private bool TryTakeBatch(
        ClientIdentifier client,
        int batchId,
        out RenderBatchMessage batch,
        out DateTime dispatchedAt)
    {
        List<(RenderBatchMessage msg, DateTime dispatchedAt)> batches = _inFlight[client];
        int index = batches.FindIndex(b => b.msg.BatchId == batchId);

        if (index < 0)
        {
            batch = default!;
            dispatchedAt = default;
            return false;
        }

        (batch, dispatchedAt) = batches[index];
        batches.RemoveAt(index);
        return true;
    }

    private bool TryTakeBatchContainingFrame(
        ClientIdentifier client,
        int frameIndex,
        out RenderBatchMessage batch,
        out DateTime dispatchedAt)
    {
        List<(RenderBatchMessage msg, DateTime dispatchedAt)> batches = _inFlight[client];
        int index = batches.FindIndex(b => b.msg.Frames.Any(f => f.FrameIndex == frameIndex));

        if (index < 0)
        {
            batch = default!;
            dispatchedAt = default;
            return false;
        }

        (batch, dispatchedAt) = batches[index];
        batches.RemoveAt(index);
        return true;
    }

    private void CompleteBatch(
        ClientIdentifier client,
        RenderBatchMessage batch,
        DateTime dispatchedAt,
        IReadOnlyList<RenderFrameResult> results,
        TimeSpan batchRenderDuration)
    {
        TimeSpan roundtrip = dispatchedAt != default ? DateTime.UtcNow - dispatchedAt : TimeSpan.Zero;
        int firstFrame = batch.Frames.Min(f => f.FrameIndex);
        int lastFrame = batch.Frames.Max(f => f.FrameIndex);

        _batchTimings.Add(new BatchTiming(
            client,
            batch.BatchId,
            firstFrame,
            lastFrame,
            batch.Frames.Count,
            roundtrip,
            batchRenderDuration
        ));

        foreach (RenderFrameResult result in results)
        {
            _timings.Add(new FrameTiming(client, result.FrameIndex, batch.BatchId, result.RenderDuration));
            _completed[result.FrameIndex] = result.Result;
            FrameCompleted?.Invoke(client, result.FrameIndex, result.RenderDuration);
        }

        Logger.Log($"Batch {batch.BatchId} received from client {client.DisplayName} ({_completed.Count}/{_totalFrames}).");

        if (_completed.Count == _totalFrames)
        {
            _renderClock.Stop();
            RenderCompleted?.Invoke();
            _allDone.TrySetResult();
            return;
        }

        TryDispatch();
    }

    private void TryDispatch()
    {
        while (_pending.Count > 0)
        {
            List<ClientIdentifier> clientsWithCapacity = _inFlight
                .Where(kv => kv.Value.Count < _maxBatchesPerClient)
                .Select(kv => kv.Key)
                .ToList();

            ClientIdentifier? client = _clientSelector.Select(clientsWithCapacity);
            if (client is null)
            {
                break;
            }

            RenderBatchMessage msg = _pending.Dequeue();
            _inFlight[client].Add((msg, DateTime.UtcNow));
            Logger.Log($"Sending batch {msg.BatchId} ({msg.Frames.Count} frame(s), {FormatFrameRange(msg.Frames)}) to client {client.DisplayName}.");

            foreach (RenderFrameMessage frame in msg.Frames)
            {
                FrameDispatched?.Invoke(client, frame.FrameIndex);
            }

            _ = _server.SendToClientAsync(client, msg);
        }
    }

    private static string FormatFrameRange(IReadOnlyList<RenderFrameMessage> frames)
    {
        if (frames.Count == 0)
        {
            return "-";
        }

        int first = frames.Min(f => f.FrameIndex);
        int last = frames.Max(f => f.FrameIndex);
        return first == last ? first.ToString() : $"{first}-{last}";
    }
}
