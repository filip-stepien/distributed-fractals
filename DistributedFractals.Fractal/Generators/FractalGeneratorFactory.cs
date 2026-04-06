using DistributedFractals.Fractal.Generators.Mandelbrot;

namespace DistributedFractals.Fractal.Generators;

public static class FractalGeneratorFactory
{
    public static IFractalGenerator Create(FractalGeneratorType type) => type switch
    {
        FractalGeneratorType.Mandelbrot => new MandelbrotGenerator(),
        _ => throw new NotSupportedException($"Unsupported generator type: {type}")
    };
}
