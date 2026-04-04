using DistributedFractals.Core.Zoom;

namespace DistributedFractals.Core.Core;

public interface IFractalGenerator<in TOptions>
    where TOptions : IFractalGeneratorOptions
{
    FractalResult Generate(TOptions options, FrameBounds bounds, IFractalColorizer colorizer);
}
