namespace DistributedFractals.Core.Core;

public interface IFractalGenerator<in TOptions>
    where TOptions : IFractalGeneratorOptions
{
    FractalResult Generate(TOptions options, IFractalColorizer colorizer);
}
