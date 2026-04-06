using System.Numerics;

namespace DistributedFractals.Fractal.Colorizers;

public interface IFractalColorizer
{
    Vector3 GetColor(ulong iteration, ulong maxIterations);
}