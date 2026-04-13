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
            byte* ptr = (byte*)fb.Address;
            int stride = fb.RowBytes;
            foreach (FractalPoint p in result.FractalPoints)
            {
                int x = (int)p.Coordinates.X;
                int y = (int)p.Coordinates.Y;
                if ((uint)x >= (uint)width || (uint)y >= (uint)height) continue;
                int off = y * stride + x * 4;
                ptr[off + 0] = (byte)(p.Color.Z * 255); // B
                ptr[off + 1] = (byte)(p.Color.Y * 255); // G
                ptr[off + 2] = (byte)(p.Color.X * 255); // R
                ptr[off + 3] = 255;                      // A
            }
        }

        return bitmap;
    }
}
