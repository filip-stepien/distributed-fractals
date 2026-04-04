using DistributedFractals.Core.Core;

namespace DistributedFractals.Core.Zoom;

public interface IKeyframeZoomSequenceGenerator
{
    IEnumerable<FrameBounds> Generate(
        IFractalGeneratorOptions options,
        IReadOnlyList<ZoomKeyframe> keyframes,
        int totalFrames,
        IZoomInterpolation interpolation);
}
