using DistributedFractals.Fractal.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IFrameResultReceiver
{
    void OnResultReceived(Guid client, int frameIndex, FractalResult result, TimeSpan renderDuration);
    void OnBatchResultReceived(Guid client, int batchId, IReadOnlyList<RenderFrameResult> results, TimeSpan renderDuration);
}
