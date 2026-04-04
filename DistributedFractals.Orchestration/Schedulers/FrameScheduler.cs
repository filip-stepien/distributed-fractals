using DistributedFractals.Core.Core;
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
    private readonly Dictionary<Guid, List<(RenderFrameMessage msg, DateTime dispatchedAt)>> _inFlight = new();
    private readonly SortedDictionary<int, FractalResult> _completed = new();
    private readonly object _lock = new();
    private readonly TaskCompletionSource _allDone = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public event Action<Guid, int>? FrameDispatched;
    public event Action<Guid, int, TimeSpan>? FrameCompleted;
    public event Action<Guid, int>? FrameFailed;
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

    public void Cancel()
    {
        lock (_lock)
        {
            _pending.Clear();
            _allDone.TrySetCanceled();
        }
    }

    public void OnClientAvailable(Guid client)
    {
        lock (_lock)
        {
            _inFlight.TryAdd(client, new List<(RenderFrameMessage msg, DateTime dispatchedAt)>());
            TryDispatch();
        }
    }

    public void OnResultReceived(Guid client, int frameIndex, FractalResult result)
    {
        lock (_lock)
        {
            DateTime dispatchedAt = default;
            if (_inFlight.TryGetValue(client, out List<(RenderFrameMessage msg, DateTime dispatchedAt)>? frames))
            {
                (RenderFrameMessage msg, DateTime dispatchedAt) match = frames.FirstOrDefault(f => f.msg.FrameIndex == frameIndex);
                dispatchedAt = match.dispatchedAt;
                frames.RemoveAll(f => f.msg.FrameIndex == frameIndex);
            }

            TimeSpan duration = dispatchedAt != default ? DateTime.UtcNow - dispatchedAt : TimeSpan.Zero;

            _completed[frameIndex] = result;
            Console.WriteLine($"[MASTER] Frame {frameIndex} received from client {client} ({_completed.Count}/{_totalFrames}).");
            FrameCompleted?.Invoke(client, frameIndex, duration);

            if (_completed.Count == _totalFrames)
            {
                RenderCompleted?.Invoke();
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
            if (_inFlight.Remove(client, out List<(RenderFrameMessage msg, DateTime dispatchedAt)>? frames))
            {
                foreach ((RenderFrameMessage msg, DateTime dispatchedAt) frame in frames)
                {
                    Console.WriteLine($"[MASTER] Re-queuing frame {frame.msg.FrameIndex} after client {client} failed.");
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
            List<Guid> workersWithCapacity = _inFlight
                .Where(kv => kv.Value.Count < _framesPerClient)
                .Select(kv => kv.Key)
                .ToList();

            Guid? client = _clientSelector.Select(workersWithCapacity);
            if (client is null)
            {
                break;
            }

            RenderFrameMessage msg = _pending.Dequeue();
            _inFlight[client.Value].Add((msg, DateTime.UtcNow));
            Console.WriteLine($"[MASTER] Sending frame {msg.FrameIndex} to client {client.Value}.");
            FrameDispatched?.Invoke(client.Value, msg.FrameIndex);
            _ = _server.SendToClientAsync(client.Value, msg);
        }
    }
}
