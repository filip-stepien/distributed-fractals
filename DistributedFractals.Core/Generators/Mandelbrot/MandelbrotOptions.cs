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
) : IFractalGeneratorOptions;
