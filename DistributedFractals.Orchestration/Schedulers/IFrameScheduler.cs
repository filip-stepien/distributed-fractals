using DistributedFractals.Core.Core;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Orchestration.Schedulers;

public interface IFrameScheduler : IFrameResultReceiver
{
    event Action<Guid, int>? FrameDispatched;
    event Action<Guid, int, TimeSpan>? FrameCompleted;
    event Action<Guid, int>? FrameFailed;
    event Action? RenderCompleted;

    Task WaitForAllAsync();
    IReadOnlyList<FractalResult> GetOrderedResults();
    void Cancel();

    void OnClientAvailable(Guid client);
    void OnClientFailed(Guid client);
}
