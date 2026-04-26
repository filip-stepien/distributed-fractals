using DistributedFractals.Fractal.Core;

namespace DistributedFractals.Server.Core;

public interface IFrameResultReceiver
{
    void OnResultReceived(Guid client, int frameIndex, FractalResult result, TimeSpan renderDuration);
}
