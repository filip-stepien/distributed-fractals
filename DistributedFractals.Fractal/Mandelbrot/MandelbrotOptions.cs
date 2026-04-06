using DistributedFractals.Fractal.Generators;
using DistributedFractals.Fractal.Zoom;

namespace DistributedFractals.Fractal.Mandelbrot;

public record MandelbrotOptions(
    ulong Width,
    ulong Height,
    ulong MaxIterations = 100
) : IFractalGeneratorOptions
{
    public FractalGeneratorType GeneratorType { get; } = FractalGeneratorType.Mandelbrot;
    public FrameBounds DefaultBounds { get; } = new(-2.5, 1.0, -1.2, 1.2);
}
