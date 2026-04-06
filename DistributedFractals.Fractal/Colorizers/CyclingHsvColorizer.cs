using System.Numerics;
using DistributedFractals.Fractal.Core;

namespace DistributedFractals.Fractal.Colorizers;

public class CyclingHsvColorizer(float cycleSpeed = 5f) : IFractalColorizer
{
    public Vector3 GetColor(ulong iteration, ulong maxIterations)
    {
        if (iteration == maxIterations)
            return Vector3.Zero;

        float t = (float)iteration / maxIterations * cycleSpeed % 1f;
        return HsvToRgb(t * 360f, saturation: 0.8f, value: 1f);
    }

    private static Vector3 HsvToRgb(float h, float saturation, float value)
    {
        float c = value * saturation;
        float x = c * (1f - MathF.Abs(h / 60f % 2f - 1f));
        float m = value - c;

        var (r, g, b) = h switch
        {
            < 60  => (c, x, 0f),
            < 120 => (x, c, 0f),
            < 180 => (0f, c, x),
            < 240 => (0f, x, c),
            < 300 => (x, 0f, c),
            _     => (c, 0f, x),
        };

        return new Vector3(r + m, g + m, b + m);
    }
}
