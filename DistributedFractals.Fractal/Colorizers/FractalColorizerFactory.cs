using DistributedFractals.Fractal.Core;

namespace DistributedFractals.Fractal.Colorizers;

public static class FractalColorizerFactory
{
    public static IFractalColorizer Create(FractalColorizerType type) => type switch
    {
        FractalColorizerType.BlackAndWhite => new BlackAndWhiteColorizer(),
        FractalColorizerType.CyclingHsv => new CyclingHsvColorizer(),
        _ => throw new NotSupportedException($"Unsupported colorizer type: {type}")
    };
}
