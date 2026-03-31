using DistributedFractals.Core.Core;

namespace DistributedFractals.Core.Zoom;

public interface IKeyframeZoomSequenceGenerator<TOptions>
    where TOptions : IBoundedFractalOptions<TOptions>
{
    IEnumerable<TOptions> Generate(
        TOptions options,
        IReadOnlyList<ZoomKeyframe> keyframes,
        int totalFrames,
        IZoomInterpolation interpolation);
}
