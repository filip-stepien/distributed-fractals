using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Zoom;

namespace DistributedFractals.Fractal.Generators;

public interface IFractalGenerator
{
    FractalResult Generate(IFractalGeneratorOptions options, FrameBounds bounds, IFractalColorizer colorizer);
}
