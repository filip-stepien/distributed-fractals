namespace DistributedFractals.Core.Zoom.Interpolations;

public class SmoothStepInterpolation : IZoomInterpolation
{
    public double Interpolate(double t) => t * t * (3.0 - 2.0 * t);
}
