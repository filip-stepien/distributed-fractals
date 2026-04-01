using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace DistributedFractals.Gui.Views;

public partial class RenderView : UserControl
{
    private readonly bool _isServerMode;
    private readonly int _totalFrames;
    private readonly DispatcherTimer _timer;
    private readonly DateTime _renderStart;

    private static readonly Color AccentColor  = Color.FromRgb(0x4A, 0x9E, 0xFF);
    private static readonly Color GreenColor   = Color.FromRgb(0x22, 0xC5, 0x5E);
    private static readonly Color RedColor     = Color.FromRgb(0xEF, 0x44, 0x44);
    private static readonly Color MutedColor   = Color.FromRgb(0x6B, 0x72, 0x80);
    private static readonly Color TextColor    = Color.FromRgb(0xE2, 0xE8, 0xF0);
    private static readonly Color SurfaceColor = Color.FromRgb(0x1C, 0x1F, 0x26);
    private static readonly Color BorderColor  = Color.FromRgb(0x2A, 0x2D, 0x35);

    private readonly Dictionary<string, ClientCard> _cards = new();

    private int _framesDone;
    private int _framesInFlight;
    private int _framesFailed;

    public RenderView(bool isServerMode, int totalFrames)
    {
        _isServerMode = isServerMode;
        _totalFrames  = totalFrames;
        _renderStart  = DateTime.Now;

        InitializeComponent();

        ProgressText.Text       = $"0 / {totalFrames} frames";
        OverallProgress.Maximum = totalFrames;


        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateElapsed();
        _timer.Start();
    }

    // ── Client card ───────────────────────────────────────────────────────────────

    private sealed class ClientCard
    {
        public required Ellipse     StatusDot      { get; init; }
        public required TextBlock   StatusLabel    { get; init; }
        public required TextBlock   FramesCount    { get; init; }
        public required TextBlock   AvgTimeText    { get; init; }
        public required TextBlock   CurrentInfo    { get; init; }
        public required Border      CardBorder     { get; init; }

        public int FramesDone       { get; set; }
        public int FramesFailed     { get; set; }
        public double TotalMs       { get; set; }
        public int UpdateCounter    { get; set; }
    }

    private ClientCard BuildCard(string address)
    {
        // Status row
        var dot = new Ellipse
        {
            Width = 8, Height = 8,
            Fill = new SolidColorBrush(GreenColor),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var addressText = new TextBlock
        {
            Text = address,
            FontSize = 13, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(TextColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var statusLabel = new TextBlock
        {
            Text = "CONNECTED",
            FontSize = 10, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(GreenColor),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        Grid.SetColumn(dot, 0);
        Grid.SetColumn(addressText, 1);
        Grid.SetColumn(statusLabel, 2);
        headerGrid.Children.Add(dot);
        headerGrid.Children.Add(addressText);
        headerGrid.Children.Add(statusLabel);

        // Divider
        var divider = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(BorderColor),
            Margin = new Thickness(0, 12, 0, 12)
        };

        // Stats row
        var framesCount = new TextBlock
        {
            Text = "0",
            FontSize = 28, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextColor)
        };
        var framesLabel = new TextBlock
        {
            Text = "FRAMES",
            FontSize = 10, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(MutedColor),
            Margin = new Thickness(0, 2, 0, 0)
        };
        var framesCol = new StackPanel { Spacing = 0 };
        framesCol.Children.Add(framesCount);
        framesCol.Children.Add(framesLabel);

        var avgTimeText = new TextBlock
        {
            Text = "—",
            FontSize = 28, FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(TextColor),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        var avgTimeLabel = new TextBlock
        {
            Text = "AVG TIME",
            FontSize = 10, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(MutedColor),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 2, 0, 0)
        };
        var avgTimeCol = new StackPanel { Spacing = 0 };
        avgTimeCol.Children.Add(avgTimeText);
        avgTimeCol.Children.Add(avgTimeLabel);

        var statsGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        Grid.SetColumn(framesCol, 0);
        Grid.SetColumn(avgTimeCol, 1);
        statsGrid.Children.Add(framesCol);
        statsGrid.Children.Add(avgTimeCol);

        // Current frame info
        var frameLabel = new TextBlock
        {
            Text = "Frame ",
            FontSize = 11,
            Foreground = new SolidColorBrush(MutedColor),
            Margin = new Thickness(0, 10, 0, 0)
        };
        var currentInfo = new TextBlock
        {
            Text = "—",
            FontSize = 11,
            Foreground = new SolidColorBrush(MutedColor),
            Margin = new Thickness(0, 10, 0, 0)
        };
        var currentRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Children = { frameLabel, currentInfo }
        };

        var content = new StackPanel { Margin = new Thickness(16) };
        content.Children.Add(headerGrid);
        content.Children.Add(divider);
        content.Children.Add(statsGrid);
        content.Children.Add(currentRow);

        var card = new Border
        {
            Width = 240,
            Background = new SolidColorBrush(SurfaceColor),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(6),
            Child = content
        };

        return new ClientCard
        {
            CardBorder   = card,
            StatusDot    = dot,
            StatusLabel  = statusLabel,
            FramesCount  = framesCount,
            AvgTimeText  = avgTimeText,
            CurrentInfo  = currentInfo
        };
    }

    private void SetCardConnected(ClientCard card, bool connected)
    {
        var color = connected ? GreenColor : RedColor;
        card.StatusLabel.Text       = connected ? "CONNECTED" : "DISCONNECTED";
        card.StatusLabel.Foreground = new SolidColorBrush(color);
        card.StatusDot.Fill         = new SolidColorBrush(color);
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    public void OnClientConnected(string clientId, string address)
    {
        if (_cards.ContainsKey(clientId)) return;
        var card = BuildCard(address);
        SetCardConnected(card, true);
        _cards[clientId] = card;
        ClientCardsPanel.Children.Add(card.CardBorder);
        Log($"Client {address} connected.");
    }

    public void OnFrameDispatched(string clientId, int frameIndex)
    {
        _framesInFlight++;


        if (!_cards.TryGetValue(clientId, out var card)) return;
        card.CurrentInfo.Text = frameIndex.ToString();
    }

    public void OnFrameCompleted(string clientId, int frameIndex, TimeSpan duration, WriteableBitmap? thumbnail = null)
    {
        _framesInFlight = Math.Max(0, _framesInFlight - 1);
        _framesDone++;


        ProgressText.Text = $"{_framesDone} / {_totalFrames} frames";
        OverallProgress.Value = _framesDone;

        if (_framesDone == _totalFrames) MarkFinished();

        if (!_cards.TryGetValue(clientId, out var card)) return;
        card.FramesDone++;
        card.TotalMs += duration.TotalMilliseconds;
        card.UpdateCounter++;
        card.FramesCount.Text = card.FramesDone.ToString();

        // Update avg time every 5 frames to avoid flickering
        if (card.UpdateCounter >= 5 || card.FramesDone == 1)
        {
            double avgMs = card.TotalMs / card.FramesDone;
            card.AvgTimeText.Text = avgMs >= 1000 ? $"{avgMs / 1000:F1}s" : $"{avgMs:F0}ms";
            card.UpdateCounter = 0;
        }

    }

    public void OnFrameFailed(string clientId, int frameIndex)
    {
        _framesInFlight = Math.Max(0, _framesInFlight - 1);
        _framesFailed++;


        if (!_cards.TryGetValue(clientId, out var card)) return;
        card.FramesFailed++;
        card.CurrentInfo.Text = frameIndex.ToString();
    }

    public void OnClientDisconnected(string clientId)
    {
        if (!_cards.TryGetValue(clientId, out var card)) return;
        SetCardConnected(card, false);
        card.CurrentInfo.Foreground = new SolidColorBrush(MutedColor);
        Log($"Client {clientId} disconnected.");
    }

    // ── Finish / elapsed ──────────────────────────────────────────────────────────

    private void MarkFinished()
    {
        _timer.Stop();

        ProgressDot.Fill              = new SolidColorBrush(GreenColor);
        ProgressStatusText.Text       = "Complete";
        ProgressStatusText.Foreground = new SolidColorBrush(GreenColor);
        OverallProgress.Foreground    = new SolidColorBrush(GreenColor);
        CancelButton.IsEnabled        = false;
    }

    private void UpdateElapsed() { }

    // ── Navigation ────────────────────────────────────────────────────────────────

    private async void OnBackClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmDialog(
            "Cancel render?",
            "Going back will cancel the current render. This cannot be undone.",
            "Cancel render",
            "Keep rendering",
            danger: true,
            windowTitle: "Warning"
        );
        var result = await dialog.ShowDialog<bool?>(TopLevel.GetTopLevel(this) as Window);
        if (result is not true) return;

        _timer.Stop();
        if (VisualRoot is MainWindow window)
            window.NavigateToMain(_isServerMode);
    }

    private async void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmDialog(
            "Cancel render?",
            "This will stop the current render. This cannot be undone.",
            "Cancel render",
            "Keep rendering",
            danger: true,
            windowTitle: "Warning"
        );
        var result = await dialog.ShowDialog<bool?>(TopLevel.GetTopLevel(this) as Window);
        if (result is not true) return;

        _timer.Stop();

        ProgressDot.Fill              = new SolidColorBrush(RedColor);
        ProgressStatusText.Text       = "Cancelled";
        ProgressStatusText.Foreground = new SolidColorBrush(RedColor);
        OverallProgress.Foreground    = new SolidColorBrush(RedColor);
        CancelButton.IsEnabled        = false;
        Log("Render cancelled.");
    }

    // ── Log ───────────────────────────────────────────────────────────────────────

    private void Log(string message)
    {
        (VisualRoot as MainWindow)?.Log(message);
    }

    // ── Mock simulation ───────────────────────────────────────────────────────────

    public void StartMockRender() => _ = RunMockAsync();

    private async Task RunMockAsync()
    {
        var rng = new Random(42);
        var clients = new[]
        {
            ("client-1", "192.168.1.10"),
            ("client-2", "192.168.1.11"),
            ("client-3", "192.168.1.12"),
            ("client-4", "192.168.1.13"),
            ("client-5", "192.168.1.14"),
            ("client-6", "192.168.1.15"),
            ("client-7", "192.168.1.16"),
            ("client-8", "192.168.1.17"),
        };

        foreach (var (id, addr) in clients)
        {
            OnClientConnected(id, addr);
            await Task.Delay(rng.Next(150, 500));
        }

        int ci = 0;
        var inFlight = new List<Task>();

        for (int f = 0; f < _totalFrames; f++)
        {
            var (clientId, _) = clients[ci % clients.Length];
            ci++;

            int    frame    = f;
            string client   = clientId;
            int    renderMs = rng.Next(200, 1500);
            bool   fail     = false;

            OnFrameDispatched(client, frame);

            inFlight.Add(Task.Delay(renderMs).ContinueWith(_ =>
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (fail)
                        OnFrameFailed(client, frame);
                    else
                        OnFrameCompleted(client, frame, TimeSpan.FromMilliseconds(renderMs));
                })));

            await Task.Delay(rng.Next(30, 80));
        }

        await Task.WhenAll(inFlight);
    }

}
