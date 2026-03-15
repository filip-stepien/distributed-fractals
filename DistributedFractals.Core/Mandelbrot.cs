namespace DistributedFractals.Core;

using System.Drawing;

public class MandelbrotGenerator
{
    public Bitmap Generate(int width, int height, int maxIterations = 100)
    {
        var bmp = new Bitmap(width, height);

        for (int px = 0; px < width; px++)
        {
            for (int py = 0; py < height; py++)
            {
                double x0 = (px / (double)width) * 3.5 - 2.5;
                double y0 = (py / (double)height) * 2.0 - 1.0;

                double x = 0, y = 0;
                int iter = 0;

                while (x * x + y * y <= 4 && iter < maxIterations)
                {
                    double xTemp = x * x - y * y + x0;
                    y = 2 * x * y + y0;
                    x = xTemp;
                    iter++;
                }

                int brightness = iter == maxIterations ? 0 : (int)(255.0 * iter / maxIterations);
                bmp.SetPixel(px, py, Color.FromArgb(brightness, brightness, brightness));
            }
        }

        return bmp;
    }
}
