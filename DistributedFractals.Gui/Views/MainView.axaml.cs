using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DistributedFractals.Fractal.Colorizers;
using DistributedFractals.Fractal.Core;
using DistributedFractals.Fractal.Generators.Mandelbrot;
using DistributedFractals.Fractal.Mandelbrot;
using DistributedFractals.Fractal.Zoom;
using DistributedFractals.Gui.Networking;

namespace DistributedFractals.Gui.Views;

public partial class MainView : UserControl
{
    private readonly bool _isServerMode;

    // Fractal state
    private MandelbrotOptions _previewOptions = BuildBaseOptions(800, 600, 500);
    private FrameBounds _previewBounds;
    private readonly List<ZoomKeyframe> _keyframes = [];

    // Default Mandelbrot framing: full set centered at (-0.75, 0) with Re range 3.5.
    // Im range is derived from the pixel aspect ratio so the generated image is not stretched
    // (MandelbrotGenerator maps pixels 1:1 onto the complex-plane bounds).
    private const double DefaultReRange  = 3.5;
    private const double DefaultCenterRe = -0.75;
    private const double DefaultCenterIm = 0.0;

    private static MandelbrotOptions BuildBaseOptions(int width, int height, ulong maxIter)
        => new(Width: (ulong)width, Height: (ulong)height, MaxIterations: maxIter);

    private static FrameBounds BuildDefaultBounds(int width, int height)
    {
        double imRange = DefaultReRange * height / width;
        return new FrameBounds(
            MinRe: DefaultCenterRe - DefaultReRange / 2.0,
            MaxRe: DefaultCenterRe + DefaultReRange / 2.0,
            MinIm: DefaultCenterIm - imRange / 2.0,
            MaxIm: DefaultCenterIm + imRange / 2.0);
    }

    // Selection state
    private Point _selectionStart;
    private bool _isSelecting;
    private bool _selectingForKeyframe;
    private int _selectedKeyframeIndex = -1;

    private static readonly Color AccentColor  = Color.FromRgb(0x4A, 0x9E, 0xFF);
    private static readonly Color GreenColor   = Color.FromRgb(0x22, 0xC5, 0x5E);
    private static readonly Color SurfaceColor = Color.FromRgb(0x1C, 0x1F, 0x26);
    private static readonly Color BorderColor  = Color.FromRgb(0x2A, 0x2D, 0x35);
    private static readonly Color TextColor    = Color.FromRgb(0xE2, 0xE8, 0xF0);
    private static readonly Color MutedColor   = Color.FromRgb(0x6B, 0x72, 0x80);

    public MainView(bool isServerMode)
    {
        _isServerMode = isServerMode;
        _previewBounds = BuildDefaultBounds(800, 600);
        InitializeComponent();

        RebuildKeyframesList();
        Log("Application started. Click 'Default' to add the full view keyframe, then select a keyframe to preview and use + to add more.");
    }

    // ── Mode ─────────────────────────────────────────────────────────────────────

    // ── Preview render ────────────────────────────────────────────────────────────

    private Task GeneratePreviewForKeyframeAsync(ZoomKeyframe kf)
    {
        int width  = (int)(WidthInput.Value  ?? 800);
        int height = (int)(HeightInput.Value ?? 600);
        ulong maxIter = (ulong)(MaxIterationsInput.Value ?? 500);

        // Use the same base bounds the renderer will use, so the preview matches
        // KeyframeZoomSequenceGenerator's math exactly (it scales reRange/imRange by kf.Scale).
        FrameBounds defaultBounds = BuildDefaultBounds(width, height);
        double halfRe = (defaultBounds.MaxRe - defaultBounds.MinRe) * kf.Scale / 2.0;
        double halfIm = (defaultBounds.MaxIm - defaultBounds.MinIm) * kf.Scale / 2.0;

        FrameBounds kfBounds = new(
            MinRe: kf.CenterRe - halfRe,
            MaxRe: kf.CenterRe + halfRe,
            MinIm: kf.CenterIm - halfIm,
            MaxIm: kf.CenterIm + halfIm);

        return GeneratePreviewAsync(
            BuildBaseOptions(width, height, maxIter),
            kfBounds);
    }

    private async Task GeneratePreviewAsync(MandelbrotOptions? overrideOptions = null, FrameBounds? overrideBounds = null)
    {
        RenderOverlay.IsVisible = true;

        try
        {
            int width  = (int)(WidthInput.Value  ?? 800);
            int height = (int)(HeightInput.Value ?? 600);
            ulong maxIter = (ulong)(MaxIterationsInput.Value ?? 500);

            _previewOptions = overrideOptions ?? BuildBaseOptions(width, height, maxIter);
            _previewBounds  = overrideBounds  ?? BuildDefaultBounds(width, height);

            IFractalColorizer colorizer = ColorizerCombo.SelectedIndex == 0
                ? new BlackAndWhiteColorizer()
                : new CyclingHsvColorizer();

            MandelbrotOptions opts   = _previewOptions;
            FrameBounds       bounds = _previewBounds;

            FractalResult result = await Task.Run(() =>
                new MandelbrotGenerator().Generate(opts, bounds, colorizer));

            var bitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Opaque);

            using (var fb = bitmap.Lock())
            {
                unsafe
                {
                    byte* ptr = (byte*)fb.Address;
                    int stride = fb.RowBytes;
                    foreach (FractalPoint p in result.FractalPoints)
                    {
                        int x  = (int)p.Coordinates.X;
                        int y  = (int)p.Coordinates.Y;
                        int off = y * stride + x * 4;
                        ptr[off + 0] = (byte)(p.Color.Z * 255); // B
                        ptr[off + 1] = (byte)(p.Color.Y * 255); // G
                        ptr[off + 2] = (byte)(p.Color.X * 255); // R
                        ptr[off + 3] = 255;                      // A
                    }
                }
            }

            FractalImage.Source = bitmap;
            RenderOverlay.IsVisible = false;
            Log($"Preview rendered ({width}×{height}, {maxIter} iterations).");
        }
        catch (Exception ex)
        {
            RenderOverlay.IsVisible = false;
            Log($"Error: {ex.Message}");
        }
    }

    // ── Canvas pointer events ─────────────────────────────────────────────────────

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_selectingForKeyframe) return;

        if (e.GetCurrentPoint(SelectionCanvas).Properties.IsRightButtonPressed)
        {
            _selectingForKeyframe = false;
            _isSelecting = false;
            SelectionCanvas.Cursor = Cursor.Default;
            SelectionHintBanner.IsVisible = false;
            SelectionRect.IsVisible = false;
            return;
        }

        _selectionStart = e.GetPosition(SelectionCanvas);
        _isSelecting = true;

        Canvas.SetLeft(SelectionRect, _selectionStart.X);
        Canvas.SetTop(SelectionRect,  _selectionStart.Y);
        SelectionRect.Width  = 0;
        SelectionRect.Height = 0;
        SelectionRect.IsVisible = true;

        e.Pointer.Capture(SelectionCanvas);
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting) return;

        var pos = e.GetPosition(SelectionCanvas);
        double dx = pos.X - _selectionStart.X;

        // Constrain to same aspect ratio as the fractal image
        double aspect = (double)_previewOptions.Width / _previewOptions.Height;
        double w = Math.Abs(dx);
        double h = w / aspect;

        // Extend left or right based on drag direction; height always extends downward
        double x = dx >= 0 ? _selectionStart.X : _selectionStart.X - w;
        double y = _selectionStart.Y;

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect,  y);
        SelectionRect.Width  = w;
        SelectionRect.Height = h;
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSelecting) return;

        _isSelecting = false;
        e.Pointer.Capture(null);

        if (SelectionRect.Width < 8)
        {
            SelectionRect.IsVisible = false;
            _selectingForKeyframe = false;
            SelectionCanvas.Cursor = Cursor.Default;
            return;
        }

        if (_selectingForKeyframe)
        {
            AddKeyframeFromSelection();
            _selectingForKeyframe = false;
            SelectionCanvas.Cursor = Cursor.Default;
            SelectionHintBanner.IsVisible = false;
        }
    }

    // ── Keyframe from selection ───────────────────────────────────────────────────

    private void AddKeyframeFromSelection()
    {
        double selLeft = Canvas.GetLeft(SelectionRect);
        double selTop  = Canvas.GetTop(SelectionRect);
        double selW    = SelectionRect.Width;

        double imgW     = _previewOptions.Width;
        double imgH     = _previewOptions.Height;
        double canvasW  = SelectionCanvas.Bounds.Width;
        double canvasH  = SelectionCanvas.Bounds.Height;

        // Where is the image rendered inside the canvas (Stretch.Uniform)?
        double renderScale = Math.Min(canvasW / imgW, canvasH / imgH);
        double offX = (canvasW - imgW * renderScale) / 2.0;
        double offY = (canvasH - imgH * renderScale) / 2.0;

        // Selection center in image-pixel space
        double selH = SelectionRect.Height;
        double cImgX = (selLeft + selW / 2.0 - offX) / renderScale;
        double cImgY = (selTop  + selH / 2.0 - offY) / renderScale;

        // Map image pixels → complex plane coordinates using current preview bounds
        double reRange = _previewBounds.MaxRe - _previewBounds.MinRe;
        double imRange = _previewBounds.MaxIm - _previewBounds.MinIm;
        double centerRe = _previewBounds.MinRe + (cImgX / imgW) * reRange;
        double centerIm = _previewBounds.MinIm + (cImgY / imgH) * imRange;

        // Scale is interpreted by KeyframeZoomSequenceGenerator as a fraction of the *base*
        // complex range, NOT the current preview's range. If we're already zoomed in, the
        // current preview itself only covers `currentScale` of the base range, so the new
        // keyframe's scale must be that compounded with the selection's fraction of the preview.
        double currentScale = (_previewBounds.MaxRe - _previewBounds.MinRe) / DefaultReRange;
        double selectionFraction = selW / renderScale / imgW;
        double scale = currentScale * selectionFraction;

        AddKeyframe(new ZoomKeyframe(T: 0, CenterRe: centerRe, CenterIm: centerIm, Scale: scale));
        SelectionRect.IsVisible = false;
        Log($"Keyframe {_keyframes.Count - 1} added — Re: {centerRe:F4}, Im: {centerIm:F4}, Scale: {scale:G3}");
    }

    // ── Keyframe list ─────────────────────────────────────────────────────────────

    private void RedistributeT()
    {
        int count = _keyframes.Count;
        for (int i = 0; i < count; i++)
        {
            double t = count == 1 ? 0.0 : (double)i / (count - 1);
            _keyframes[i] = _keyframes[i] with { T = t };
        }
    }

    private void RebuildKeyframesList()
    {
        KeyframesList.Children.Clear();
        bool empty = _keyframes.Count == 0;
        KeyframesEmptyText.IsVisible = empty;
        bool hasSelection = _selectedKeyframeIndex >= 0 && _selectedKeyframeIndex < _keyframes.Count;
        NoKeyframePlaceholder.IsVisible = !hasSelection;
        AddKeyframeButton.IsEnabled = hasSelection;
        for (int i = 0; i < _keyframes.Count; i++)
            KeyframesList.Children.Add(BuildKeyframeItem(i, _keyframes[i]));
        // Re-apply highlight on the selected item after rebuild
        if (hasSelection && KeyframesList.Children[_selectedKeyframeIndex] is Border b)
            b.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x28, 0x35));
    }

    private Border MakeItemBtn(string text, double fontSize, bool enabled,
        EventHandler<PointerPressedEventArgs> handler)
    {
        var label = new TextBlock
        {
            Text                = text,
            FontSize            = fontSize,
            Foreground          = enabled
                                    ? new SolidColorBrush(MutedColor)
                                    : new SolidColorBrush(Color.FromArgb(0x30, 0x6B, 0x72, 0x80)),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center
        };
        var hoverBg = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));
        var btn = new Border
        {
            Padding             = new Thickness(5, 0),
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch,
            Background          = Brushes.Transparent,
            CornerRadius        = new CornerRadius(4),
            Cursor              = enabled ? new Cursor(StandardCursorType.Hand) : Cursor.Default,
            Child               = label
        };
        if (enabled)
        {
            btn.PointerEntered  += (_, _) => btn.Background = hoverBg;
            btn.PointerExited   += (_, _) => btn.Background = Brushes.Transparent;
            btn.PointerPressed  += handler;
        }
        return btn;
    }

    private Control BuildKeyframeItem(int index, ZoomKeyframe kf)
    {
        var item = new Border
        {
            Height          = 52,
            Background      = new SolidColorBrush(SurfaceColor),
            BorderBrush     = new SolidColorBrush(BorderColor),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding         = new Thickness(12, 6),
            Cursor          = new Cursor(StandardCursorType.Hand),
            Tag             = index
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

        int idx = index;

        // Details
        var details = new StackPanel
        {
            Spacing           = 1,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        details.Children.Add(new TextBlock
        {
            Text       = $"Re: {kf.CenterRe:F4}  Im: {kf.CenterIm:F4}",
            FontSize   = 12,
            Foreground = new SolidColorBrush(TextColor)
        });
        details.Children.Add(new TextBlock
        {
            Text       = $"Scale: {kf.Scale:G4}  t: {kf.T:F2}",
            FontSize   = 11,
            Foreground = new SolidColorBrush(MutedColor)
        });
        Grid.SetColumn(details, 0);

        // Right controls: ▲ ▼ × horizontal
        var controls = new StackPanel
        {
            Orientation       = Avalonia.Layout.Orientation.Horizontal,
            Spacing           = 0,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Margin            = new Thickness(4, 0, 0, 0)
        };

        controls.Children.Add(MakeItemBtn("\u25B2", 11,
            index > 0,
            (_, e) => { e.Handled = true; MoveKeyframe(idx, -1); }));
        controls.Children.Add(MakeItemBtn("\u25BC", 11,
            index < _keyframes.Count - 1,
            (_, e) => { e.Handled = true; MoveKeyframe(idx, 1); }));
        controls.Children.Add(MakeItemBtn("×", 15,
            enabled: true,
            (_, e) => { e.Handled = true; RemoveKeyframe(idx); }));
        Grid.SetColumn(controls, 1);

        grid.Children.Add(details);
        grid.Children.Add(controls);
        item.Child = grid;

        item.PointerPressed += (_, _) => SelectKeyframe(idx, item);
        return item;
    }

    private void MoveKeyframe(int index, int direction)
    {
        int newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _keyframes.Count) return;

        (_keyframes[index], _keyframes[newIndex]) = (_keyframes[newIndex], _keyframes[index]);
        RedistributeT();

        if (_selectedKeyframeIndex == index)
            _selectedKeyframeIndex = newIndex;
        else if (_selectedKeyframeIndex == newIndex)
            _selectedKeyframeIndex = index;

        RebuildKeyframesList();
        Log($"Keyframe {index} moved {(direction < 0 ? "up" : "down")}.");
    }

    private void SelectKeyframe(int index, Border item)
    {
        _selectedKeyframeIndex = index;
        foreach (var child in KeyframesList.Children)
            if (child is Border b)
                b.Background = new SolidColorBrush(SurfaceColor);
        item.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x28, 0x35));
        NoKeyframePlaceholder.IsVisible = false;
        AddKeyframeButton.IsEnabled = true;
        _ = GeneratePreviewForKeyframeAsync(_keyframes[index]);
    }

    // ── Keyframe buttons ──────────────────────────────────────────────────────────

    private void AddKeyframe(ZoomKeyframe kf)
    {
        _keyframes.Add(kf);
        _selectedKeyframeIndex = _keyframes.Count - 1;
        RedistributeT();
        RebuildKeyframesList();
        _ = GeneratePreviewForKeyframeAsync(_keyframes[_selectedKeyframeIndex]);
    }

    private void OnAddDefaultKeyframeClick(object? sender, RoutedEventArgs e)
    {
        AddKeyframe(new ZoomKeyframe(T: 0, CenterRe: -0.75, CenterIm: 0.0, Scale: 1.0));
        Log($"Default keyframe added (Re: -0.75, Im: 0, Scale: 1).");
    }

    private void OnAddKeyframeClick(object? sender, RoutedEventArgs e)
    {
        _selectingForKeyframe = true;
        SelectionCanvas.Cursor = new Cursor(StandardCursorType.Cross);
        SelectionHintBanner.IsVisible = true;
    }

    private void OnCancelSelectionClick(object? sender, RoutedEventArgs e)
    {
        _selectingForKeyframe = false;
        SelectionCanvas.Cursor = Cursor.Default;
        SelectionHintBanner.IsVisible = false;
        SelectionRect.IsVisible = false;
    }

    private void OnAddManualKeyframeClick(object? sender, RoutedEventArgs e)
    {
        if (!double.TryParse(ManualReInput.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double re))
        {
            Log("Invalid Re value.");
            return;
        }
        if (!double.TryParse(ManualImInput.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double im))
        {
            Log("Invalid Im value.");
            return;
        }
        if (!double.TryParse(ManualScaleInput.Text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double scale) || scale <= 0)
        {
            Log("Invalid Scale value (must be > 0).");
            return;
        }

        AddKeyframe(new ZoomKeyframe(T: 0, CenterRe: re, CenterIm: im, Scale: scale));
        Log($"Keyframe added — Re: {re:F4}, Im: {im:F4}, Scale: {scale:G3}");
    }

    private void OnRemoveKeyframeClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedKeyframeIndex >= 0)
            RemoveKeyframe(_selectedKeyframeIndex);
    }

    private void RemoveKeyframe(int index)
    {
        if (index < 0 || index >= _keyframes.Count) return;
        _keyframes.RemoveAt(index);
        if (_selectedKeyframeIndex == index)
        {
            _selectedKeyframeIndex = -1;
            FractalImage.Source = null;
        }
        else if (_selectedKeyframeIndex > index)
            _selectedKeyframeIndex--;
        RedistributeT();
        RebuildKeyframesList();
        Log($"Keyframe {index} removed.");
    }

    // ── Output ────────────────────────────────────────────────────────────────────

    // ── Navigation ───────────────────────────────────────────────────────────────

    private async void OnBackClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new ConfirmDialog(
            "Disconnect?",
            "Going back will shut down the server and disconnect all connected clients.",
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

    // ── Render ────────────────────────────────────────────────────────────────────

    private async void OnRenderClick(object? sender, RoutedEventArgs e)
    {
        if (_keyframes.Count < 2)
        {
            var alert = new ConfirmDialog(
                "Not enough keyframes",
                "At least 2 keyframes are required to render an animation.",
                "OK",
                null
            );
            await alert.ShowDialog<bool?>(TopLevel.GetTopLevel(this) as Window);
            return;
        }

        int totalFrames = (int)(TotalFramesInput.Value ?? 120);
        int fps         = (int)(FrameRateInput.Value  ?? 24);
        int width       = (int)(WidthInput.Value      ?? 800);
        int height      = (int)(HeightInput.Value     ?? 600);

        var dialog = new RenderDialog(width, height, totalFrames, fps);
        var result = await dialog.ShowDialog<bool?>(TopLevel.GetTopLevel(this) as Window);

        if (result is not true || VisualRoot is not MainWindow window) return;
        if (string.IsNullOrWhiteSpace(dialog.OutputPath))
        {
            Log("No output path selected.");
            return;
        }

        ulong maxIter = (ulong)(MaxIterationsInput.Value ?? 500);
        var baseOptions = BuildBaseOptions(width, height, maxIter);
        var colorizerType = ColorizerCombo.SelectedIndex == 0
            ? FractalColorizerType.BlackAndWhite
            : FractalColorizerType.CyclingHsv;

        var config = new RenderJobConfig(
            BaseOptions: baseOptions,
            Keyframes: _keyframes.ToList(),
            TotalFrames: totalFrames,
            FrameRate: fps,
            Colorizer: colorizerType,
            OutputPath: dialog.OutputPath
        );

        window.NavigateToRender(_isServerMode, config);
    }

    // ── Log ───────────────────────────────────────────────────────────────────────

    private void Log(string message)
    {
        (VisualRoot as MainWindow)?.Log(message);
    }
}
