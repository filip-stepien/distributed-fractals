using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Generators;

namespace DistributedFractals.Fractal.Zoom;

public interface IKeyframeZoomSequenceGenerator
{
    IEnumerable<FrameBounds> Generate(
        IFractalGeneratorOptions options,
        IReadOnlyList<ZoomKeyframe> keyframes,
        int totalFrames,
        IZoomInterpolation interpolation);
}
