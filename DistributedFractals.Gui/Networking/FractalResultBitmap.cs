using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DistributedFractals.Fractal.Core;

namespace DistributedFractals.Gui.Networking;

/// <summary>
/// Converts a FractalResult into an Avalonia WriteableBitmap.
/// Must be called on the UI thread (WriteableBitmap construction requirement).
/// </summary>
internal static class FractalResultBitmap
{
    public static WriteableBitmap From(FractalResult result)
    {
        int width  = (int)result.Width;
        int height = (int)result.Height;

        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using var fb = bitmap.Lock();
        unsafe
        {
            byte* dst = (byte*)fb.Address;
            int stride = fb.RowBytes;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int src = (y * width + x) * 3;
                    int off = y * stride + x * 4;
                    dst[off + 0] = result.Pixels[src + 2]; // B
                    dst[off + 1] = result.Pixels[src + 1]; // G
                    dst[off + 2] = result.Pixels[src + 0]; // R
                    dst[off + 3] = 255;                     // A
                }
            }
        }

        return bitmap;
    }
}
