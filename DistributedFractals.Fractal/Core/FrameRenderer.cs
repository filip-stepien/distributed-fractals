using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Generators;
using DistributedFractals.Fractal.Zoom;

namespace DistributedFractals.Fractal.Core;

public static class FrameRenderer
{
    public static Task<FractalResult> RenderAsync(
        IFractalGeneratorOptions options,
        FrameBounds bounds,
        FractalColorizerType colorizerType
    ) {
        IFractalGenerator generator = FractalGeneratorFactory.Create(options.GeneratorType);
        IFractalColorizer colorizer = FractalColorizerFactory.Create(colorizerType);
        return Task.Run(() => generator.Generate(options, bounds, colorizer));
    }
}
