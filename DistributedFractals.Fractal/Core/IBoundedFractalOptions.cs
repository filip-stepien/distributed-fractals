namespace DistributedFractals.Core.Core;

public interface IBoundedFractalOptions<TSelf> : IFractalGeneratorOptions
    where TSelf : IBoundedFractalOptions<TSelf>
{
    double MinRe { get; }
    double MaxRe { get; }
    double MinIm { get; }
    double MaxIm { get; }

    TSelf WithBounds(double minRe, double maxRe, double minIm, double maxIm);
}
