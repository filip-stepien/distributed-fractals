using DistributedFractals.Core.Core;

namespace DistributedFractals.Server.Core;

public interface IFrameResultReceiver
{
    void OnResultReceived(Guid worker, int frameIndex, FractalResult result);
}
