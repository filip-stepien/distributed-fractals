using DistributedFractals.Core.Core;
using DistributedFractals.Orchestration.Selectors;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Orchestration.Schedulers;

public sealed class FrameScheduler : IFrameScheduler
{
    private readonly IMessageMasterNode _master;
    private readonly IWorkerSelector _selector;
    private readonly int _framesPerWorker;
    private readonly int _totalFrames;

    private readonly Queue<(int index, RenderFractalMessage msg)> _pending;
    private readonly Dictionary<Guid, List<(int index, RenderFractalMessage msg)>> _inFlight = new();
    private readonly SortedDictionary<int, FractalResult> _completed = new();
    private readonly object _lock = new();
    private readonly TaskCompletionSource _allDone = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public FrameScheduler(
        IMessageMasterNode master,
        IEnumerable<(int index, RenderFractalMessage msg)> frames,
        IWorkerSelector selector,
        int framesPerWorker = 1)
    {
        _master = master;
        _selector = selector;
        _framesPerWorker = framesPerWorker;
        _pending = new Queue<(int, RenderFractalMessage)>(frames);
        _totalFrames = _pending.Count;

        if (_totalFrames == 0)
            _allDone.TrySetResult();
    }

    public Task WaitForAllAsync() => _allDone.Task;

    public IReadOnlyList<FractalResult> GetOrderedResults()
    {
        lock (_lock)
            return _completed.Values.ToList();
    }

    public void OnWorkerAvailable(Guid worker)
    {
        lock (_lock)
        {
            _inFlight.TryAdd(worker, []);
            TryDispatch();
        }
    }

    public void OnResultReceived(Guid worker, int frameIndex, FractalResult result)
    {
        lock (_lock)
        {
            if (_inFlight.TryGetValue(worker, out var frames))
                frames.RemoveAll(f => f.index == frameIndex);

            _completed[frameIndex] = result;
            Console.WriteLine($"[MASTER] Frame {frameIndex} received from worker {worker} ({_completed.Count}/{_totalFrames}).");

            if (_completed.Count == _totalFrames)
            {
                _allDone.TrySetResult();
                return;
            }

            TryDispatch();
        }
    }

    public void OnWorkerFailed(Guid worker)
    {
        lock (_lock)
        {
            if (_inFlight.Remove(worker, out var frames))
            {
                foreach (var frame in frames)
                {
                    Console.WriteLine($"[MASTER] Re-queuing frame {frame.index} after worker {worker} failed.");
                    _pending.Enqueue(frame);
                }
            }

            TryDispatch();
        }
    }

    private void TryDispatch()
    {
        while (_pending.Count > 0)
        {
            var workersWithCapacity = _inFlight
                .Where(kv => kv.Value.Count < _framesPerWorker)
                .Select(kv => kv.Key)
                .ToList();

            Guid? worker = _selector.Select(workersWithCapacity);
            if (worker is null) break;

            var frame = _pending.Dequeue();
            _inFlight[worker.Value].Add(frame);
            Console.WriteLine($"[MASTER] Sending frame {frame.index} to worker {worker.Value}.");
            _ = _master.SendToWorkerAsync(worker.Value, frame.msg);
        }
    }
}
