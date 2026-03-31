using DistributedFractals.Core.Core;

namespace DistributedFractals.Core.Generators.Mandelbrot;

public record MandelbrotOptions(
    ulong Width,
    ulong Height,
    ulong MaxIterations = 100,
    double MinRe = -2.5,
    double MaxRe =  1.0,
    double MinIm = -1.2,
    double MaxIm =  1.2
) : IBoundedFractalOptions<MandelbrotOptions>
{
    public MandelbrotOptions WithBounds(double minRe, double maxRe, double minIm, double maxIm)
        => this with { MinRe = minRe, MaxRe = maxRe, MinIm = minIm, MaxIm = maxIm };
}
