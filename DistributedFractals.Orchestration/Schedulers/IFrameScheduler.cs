using DistributedFractals.Core.Core;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Orchestration.Schedulers;

public interface IFrameScheduler : IFrameResultReceiver
{
    event Action<ClientIdentifier, int>? FrameDispatched;
    event Action<ClientIdentifier, int, TimeSpan>? FrameCompleted;
    event Action<ClientIdentifier, int>? FrameFailed;
    event Action? RenderCompleted;

    Task WaitForAllAsync();
    IReadOnlyList<FractalResult> GetOrderedResults();
    void Cancel();

    void OnClientAvailable(ClientIdentifier client);
    void OnClientFailed(ClientIdentifier client);
}
