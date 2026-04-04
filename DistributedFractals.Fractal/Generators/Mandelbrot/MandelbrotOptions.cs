using DistributedFractals.Core.Core;
using DistributedFractals.Core.Zoom;

namespace DistributedFractals.Core.Generators.Mandelbrot;

public record MandelbrotOptions(
    ulong Width,
    ulong Height,
    ulong MaxIterations = 100
) : IFractalGeneratorOptions
{
    public FrameBounds DefaultBounds { get; } = new(-2.5, 1.0, -1.2, 1.2);
}
