using DistributedFractals.Fractal.Zoom;

namespace DistributedFractals.Fractal.Generators;

public interface IFractalGeneratorOptions
{
    FractalGeneratorType GeneratorType { get; }
    ulong Width { get; }
    ulong Height { get; }
    FrameBounds DefaultBounds { get; }
}