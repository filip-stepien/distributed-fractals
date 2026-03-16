using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using DistributedFractals.Core.Colorizers;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;

namespace DistributedFractals.Gui;

public class MainWindow : Window
{
    private readonly NumericUpDown _maxIterationsInput;
    private readonly Button _generateButton;
    private readonly Image _fractalImage;

    public MainWindow()
    {
        Title = "Distributed Fractals";
        Width = 900;
        Height = 700;

        _maxIterationsInput = new NumericUpDown
        {
            Value = 1000,
            Minimum = 10,
            Maximum = 100000,
            Increment = 100,
            Width = 150
        };

        _generateButton = new Button { Content = "Generate" };
        _generateButton.Click += OnGenerateClick;

        _fractalImage = new Image { Stretch = Avalonia.Media.Stretch.Uniform };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Avalonia.Thickness(8),
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "Max Iterations:"
                },
                _maxIterationsInput,
                _generateButton
            }
        };

        DockPanel.SetDock(toolbar, Dock.Top);

        Content = new DockPanel
        {
            Children = { toolbar, _fractalImage }
        };
    }

    private async void OnGenerateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _generateButton.IsEnabled = false;
        _generateButton.Content = "Generating...";

        try
        {
            int width = 800;
            int height = 600;
            ulong maxIterations = (ulong)(_maxIterationsInput.Value ?? 1000);

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

            _fractalImage.Source = bitmap;
        }
        finally
        {
            _generateButton.IsEnabled = true;
            _generateButton.Content = "Generate";
        }
    }
}
