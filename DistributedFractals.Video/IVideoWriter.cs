using DistributedFractals.Core.Core;

namespace DistributedFractals.Video;

public interface IVideoWriter : IAsyncDisposable
{
    Task WriteFrameAsync(FractalResult frame);
}
