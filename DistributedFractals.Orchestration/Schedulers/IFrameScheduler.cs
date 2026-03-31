using DistributedFractals.Core.Core;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Orchestration.Schedulers;

public interface IFrameScheduler : IFrameResultReceiver
{
    Task WaitForAllAsync();
    IReadOnlyList<FractalResult> GetOrderedResults();

    void OnWorkerAvailable(Guid worker);
    void OnWorkerFailed(Guid worker);
}
