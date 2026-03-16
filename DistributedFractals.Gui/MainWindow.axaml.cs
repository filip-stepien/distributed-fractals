using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using DistributedFractals.Core.Colorizers;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;

namespace DistributedFractals.Gui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnGenerateClick(object? sender, RoutedEventArgs e)
    {
        GenerateButton.IsEnabled = false;
        GenerateButton.Content = "Generating...";

        try
        {
            int width = 800;
            int height = 600;
            ulong maxIterations = (ulong)(MaxIterationsInput.Value ?? 1000);

            var options = new MandelbrotOptions(
                Width: (ulong)width,
                Height: (ulong)height,
                MaxIterations: maxIterations
            );

            var generator = new MandelbrotGenerator();
            var colorizer = new BlackAndWhiteColorizer();

            FractalResult result = await Task.Run(() => generator.Generate(options, colorizer));

            var bitmap = new WriteableBitmap(
                new Avalonia.PixelSize(width, height),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Opaque);

            using (var fb = bitmap.Lock())
            {
                unsafe
                {
                    byte* ptr = (byte*)fb.Address;
                    int stride = fb.RowBytes;

                    foreach (FractalPoint point in result.FractalPoints)
                    {
                        int x = (int)point.Coordinates.X;
                        int y = (int)point.Coordinates.Y;
                        byte r = (byte)(point.Color.X * 255);
                        byte g = (byte)(point.Color.Y * 255);
                        byte b = (byte)(point.Color.Z * 255);

                        int offset = y * stride + x * 4;
                        ptr[offset + 0] = b;     // B
                        ptr[offset + 1] = g;     // G
                        ptr[offset + 2] = r;     // R
                        ptr[offset + 3] = 255;   // A
                    }
                }
            }

            FractalImage.Source = bitmap;
        }
        finally
        {
            GenerateButton.IsEnabled = true;
            GenerateButton.Content = "Generate";
        }
    }
}
