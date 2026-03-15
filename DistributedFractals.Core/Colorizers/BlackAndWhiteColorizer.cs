using System.Numerics;
using DistributedFractals.Core.Core;

namespace DistributedFractals.Core.Colorizers;

public class BlackAndWhiteColorizer : IFractalColorizer
{
    public Vector3 GetColor(ulong iteration, ulong maxIterations)
    {
        // Points that never escaped within the maximum number of iterations are colored black.
        // Points that escape earlier are colored white.
        return iteration == maxIterations
            ? new Vector3(0f, 0f, 0f)
            : new Vector3(1f, 1f, 1f);
    }
}