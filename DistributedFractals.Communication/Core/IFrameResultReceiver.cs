using DistributedFractals.Fractal.Core;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Server.Core;

public interface IFrameResultReceiver
{
    void OnResultReceived(Guid client, Guid renderJobId, int frameIndex, FractalResult result, TimeSpan renderDuration);
    void OnBatchResultReceived(Guid client, Guid renderJobId, int batchId, IReadOnlyList<RenderFrameResult> results, TimeSpan renderDuration);
}
