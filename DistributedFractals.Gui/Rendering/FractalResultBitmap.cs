using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DistributedFractals.Fractal.Core;

namespace DistributedFractals.Gui.Rendering;

internal static class FractalResultBitmap
{
    public static WriteableBitmap From(FractalResult result)
    {
        int width = (int)result.Width;
        int height = (int)result.Height;

        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using var frameBuffer = bitmap.Lock();
        unsafe
        {
            byte* destination = (byte*)frameBuffer.Address;
            int stride = frameBuffer.RowBytes;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceOffset = (y * width + x) * 3;
                    int targetOffset = y * stride + x * 4;

                    destination[targetOffset] = result.Pixels[sourceOffset + 2];
                    destination[targetOffset + 1] = result.Pixels[sourceOffset + 1];
                    destination[targetOffset + 2] = result.Pixels[sourceOffset];
                    destination[targetOffset + 3] = 255;
                }
            }
        }

        return bitmap;
    }
}
