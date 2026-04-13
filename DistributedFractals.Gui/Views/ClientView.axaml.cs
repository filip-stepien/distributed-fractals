using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace DistributedFractals.Gui.Views;

public partial class ClientView : UserControl
{
    private readonly string _serverAddress;
    private int _totalFrames;

    private static readonly Color AccentColor = Color.FromRgb(0x4A, 0x9E, 0xFF);
    private static readonly Color GreenColor  = Color.FromRgb(0x22, 0xC5, 0x5E);
    private static readonly Color RedColor    = Color.FromRgb(0xEF, 0x44, 0x44);
    private static readonly Color MutedColor  = Color.FromRgb(0x6B, 0x72, 0x80);
    private static readonly Color TextColor   = Color.FromRgb(0xE2, 0xE8, 0xF0);

    public ClientView(string serverAddress)
    {
        _serverAddress = serverAddress;
        InitializeComponent();
        ConnectionText.Text = serverAddress;
        SetConnected(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    public void OnConnected()
    {
        SetConnected(true);
        Log($"Connected to {_serverAddress}.");
    }

    public void OnDisconnected()
    {
        SetConnected(false);
        StatusDot.Fill        = new SolidColorBrush(RedColor);
        StatusText.Text       = "Disconnected";
        StatusText.Foreground = new SolidColorBrush(RedColor);
        CurrentFrameText.Text = "—";
        Log($"Disconnected from {_serverAddress}.");
    }

    public void SetTotalFrames(int total) => _totalFrames = total;

    public void OnFrameStarted(int frameIndex)
    {
        StatusDot.Fill        = new SolidColorBrush(AccentColor);
        StatusText.Text       = "Rendering";
        StatusText.Foreground = new SolidColorBrush(AccentColor);
        CurrentFrameText.Text = _totalFrames > 0
            ? $"Frame {frameIndex} / {_totalFrames}"
            : $"Frame {frameIndex}";
    }

    public void OnFrameCompleted(int frameIndex, TimeSpan duration, WriteableBitmap? preview = null)
    {
        CurrentFrameText.Text = "—";
        StatusDot.Fill        = new SolidColorBrush(GreenColor);
        StatusText.Text       = "Idle";
        StatusText.Foreground = new SolidColorBrush(GreenColor);

        if (preview is not null)
        {
            FramePreview.Source       = preview;
            PlaceholderText.IsVisible = false;
        }
    }

    public void OnFrameFailed(int frameIndex)
    {
        CurrentFrameText.Text = "—";
        StatusDot.Fill        = new SolidColorBrush(GreenColor);
        StatusText.Text       = "Idle";
        StatusText.Foreground = new SolidColorBrush(GreenColor);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private void SetConnected(bool connected)
    {
        var brush = new SolidColorBrush(connected ? GreenColor : RedColor);
        ConnectionDot.Fill        = brush;
        ConnectionLabel.Foreground = brush;
        ConnectionText.Foreground  = brush;
    }

    // ── Navigation ────────────────────────────────────────────────────────────────

    private async void OnBackClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmDialog(
            "Disconnect?",
            "You will be disconnected from the server and no longer participate in the rendering process.",
            "Disconnect",
            "Stay",
            danger: true,
            windowTitle: "Warning"
        );
        var result = await dialog.ShowDialog<bool?>(TopLevel.GetTopLevel(this) as Window);
        if (result is not true) return;

        if (VisualRoot is MainWindow window)
            window.NavigateToConnect();
    }

    // ── Log ───────────────────────────────────────────────────────────────────────

    private void Log(string message) => (VisualRoot as MainWindow)?.Log(message);

}
