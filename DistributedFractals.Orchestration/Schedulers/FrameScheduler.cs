using DistributedFractals.Core.Core;
using DistributedFractals.Orchestration.Selectors;
using DistributedFractals.Server.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Orchestration.Schedulers;

public sealed class FrameScheduler : IFrameScheduler
{
    private readonly IMessageServer _master;
    private readonly IClientSelector _selector;
    private readonly int _framesPerWorker;
    private readonly int _totalFrames;

    private readonly Queue<(int index, RenderFractalMessage msg)> _pending;
    private readonly Dictionary<Guid, List<(int index, RenderFractalMessage msg)>> _inFlight = new();
    private readonly SortedDictionary<int, FractalResult> _completed = new();
    private readonly object _lock = new();
    private readonly TaskCompletionSource _allDone = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public FrameScheduler(
        IMessageServer master,
        IEnumerable<(int index, RenderFractalMessage msg)> frames,
        IClientSelector selector,
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

    public void OnClientAvailable(Guid client)
    {
        lock (_lock)
        {
            _inFlight.TryAdd(client, []);
            TryDispatch();
        }
    }

    public void OnResultReceived(Guid client, int frameIndex, FractalResult result)
    {
        lock (_lock)
        {
            if (_inFlight.TryGetValue(client, out var frames))
                frames.RemoveAll(f => f.index == frameIndex);

            _completed[frameIndex] = result;
            Console.WriteLine($"[MASTER] Frame {frameIndex} received from client {client} ({_completed.Count}/{_totalFrames}).");

            if (_completed.Count == _totalFrames)
            {
                _allDone.TrySetResult();
                return;
            }

            TryDispatch();
        }
    }

    public void OnClientFailed(Guid client)
    {
        lock (_lock)
        {
            if (_inFlight.Remove(client, out var frames))
            {
                foreach (var frame in frames)
                {
                    Console.WriteLine($"[MASTER] Re-queuing frame {frame.index} after client {client} failed.");
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

            Guid? client = _selector.Select(workersWithCapacity);
            if (client is null) break;

            var frame = _pending.Dequeue();
            _inFlight[client.Value].Add(frame);
            Console.WriteLine($"[MASTER] Sending frame {frame.index} to client {client.Value}.");
            _ = _master.SendToClientAsync(client.Value, frame.msg);
        }
    }
}
