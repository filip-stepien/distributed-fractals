using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DistributedFractals.Gui.Views;

public partial class RenderDialog : Window
{
    public string? OutputPath { get; private set; }

    public RenderDialog(int width, int height, int totalFrames, int fps)
    {
        InitializeComponent();

        double seconds = (double)totalFrames / fps;
        SummaryResolution.Text = $"{width} × {height}";
        SummaryFrames.Text     = $"{totalFrames} @ {fps} fps";
        SummaryDuration.Text   = $"{seconds:F1}s";
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title             = "Choose output location",
            SuggestedFileName = "render.gif",
            FileTypeChoices   =
            [
                new Avalonia.Platform.Storage.FilePickerFileType("GIF") { Patterns = ["*.gif"] }
            ]
        });

        if (file is not null)
        {
            OutputPathInput.Text = file.Path.LocalPath;
            StartButton.IsEnabled = true;
        }
    }

    private void OnStartClick(object? sender, RoutedEventArgs e)
    {
        OutputPath = OutputPathInput.Text;
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
