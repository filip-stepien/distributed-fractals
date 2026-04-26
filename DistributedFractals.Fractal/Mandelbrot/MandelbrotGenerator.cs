using System.Numerics;
using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Mandelbrot;
using DistributedFractals.Fractal.Zoom;

namespace DistributedFractals.Fractal.Generators.Mandelbrot;

public class MandelbrotGenerator : FractalGeneratorBase<MandelbrotOptions>
{
    protected override FractalResult Generate(MandelbrotOptions options, FrameBounds bounds, IFractalColorizer colorizer)
    {
        // read generator options
        ulong width = options.Width;
        ulong height = options.Height;
        ulong maxIterations = options.MaxIterations;
        double minRe = bounds.MinRe;
        double maxRe = bounds.MaxRe;
        double minIm = bounds.MinIm;
        double maxIm = bounds.MaxIm;

        byte[] pixels = new byte[width * height * 3];

        Parallel.For(0, (long)height, pixelY =>
        {
            for (ulong pixelX = 0; pixelX < width; pixelX++)
            {
                double normalizedPixelX = pixelX / (double)(width - 1);
                double normalizedPixelY = pixelY / (double)(height - 1);
                double cReal = minRe + normalizedPixelX * (maxRe - minRe);
                double cImaginary = minIm + normalizedPixelY * (maxIm - minIm);

                Complex c = new(cReal, cImaginary);
                Complex z = Complex.Zero;
                ulong iteration = 0;

                while (z.Real * z.Real + z.Imaginary * z.Imaginary <= 4.0 && iteration < maxIterations)
                {
                    z = z * z + c;
                    iteration++;
                }

                Vector3 color = colorizer.GetColor(iteration, maxIterations);
                long offset = (pixelY * (long)width + (long)pixelX) * 3;
                pixels[offset]     = (byte)(color.X * 255);
                pixels[offset + 1] = (byte)(color.Y * 255);
                pixels[offset + 2] = (byte)(color.Z * 255);
            }
        });

        return new FractalResult(width, height, pixels);
    }
}
