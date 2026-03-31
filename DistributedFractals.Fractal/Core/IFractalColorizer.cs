using System.Numerics;

namespace DistributedFractals.Core.Core;

public interface IFractalColorizer
{
    Vector3 GetColor(ulong iteration, ulong maxIterations);
}