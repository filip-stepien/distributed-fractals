using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Zoom;

namespace DistributedFractals.Fractal.Generators;

public abstract class FractalGeneratorBase<TOptions> : IFractalGenerator
    where TOptions : IFractalGeneratorOptions
{
    public FractalResult Generate(IFractalGeneratorOptions options, FrameBounds bounds, IFractalColorizer colorizer)
    {
        if (options is not TOptions typed)
        {
            throw new InvalidOperationException(
                $"Expected {typeof(TOptions).Name}, got {options.GetType().Name}.");
        }
        return Generate(typed, bounds, colorizer);
    }

    protected abstract FractalResult Generate(TOptions options, FrameBounds bounds, IFractalColorizer colorizer);
}
