using DistributedFractals.Fractal.Zoom.Interpolations;

namespace DistributedFractals.Fractal.Zoom;

public static class ZoomInterpolationFactory
{
    public static IZoomInterpolation Create(ZoomInterpolationType type) => type switch
    {
        ZoomInterpolationType.Linear => new LinearInterpolation(),
        ZoomInterpolationType.SmoothStep => new SmoothStepInterpolation(),
        _ => throw new NotSupportedException($"Unsupported interpolation type: {type}")
    };
}
