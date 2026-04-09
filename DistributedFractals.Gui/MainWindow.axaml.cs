using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using DistributedFractals.Core.Core;
using DistributedFractals.Core.Generators.Mandelbrot;
using DistributedFractals.Core.Zoom;
using DistributedFractals.Core.Zoom.Interpolations;
using DistributedFractals.Gui.Networking;
using DistributedFractals.Gui.Views;
using DistributedFractals.Server.Messages;

namespace DistributedFractals.Gui;

public partial class MainWindow : Window
{
    /// <summary>Always-visible clients panel (server mode only). Use to report connect/disconnect events.</summary>
    public readonly ClientsPanel ClientsPanel = new();

    private GuiServerNode? _serverNode;
    private GuiClientNode? _clientNode;
    private GuiRenderJob?  _currentJob;

    public MainWindow()
    {
        InitializeComponent();
        ClientsSidebarContent.Content = ClientsPanel;
        ContentArea.Content = new ConnectView();
        ConsoleBorder.IsVisible = false;
    }

    public void NavigateToMain(bool isServerMode, string address, int port, int timeoutOrHeartbeatSeconds, string displayName = "")
    {
        SetConsoleVisible(true);

        if (isServerMode)
        {
            SetSidebarVisible(true);
            ContentArea.Content = new MainView(isServerMode);

            _serverNode = new GuiServerNode(IPAddress.Any, port, TimeSpan.FromSeconds(timeoutOrHeartbeatSeconds));
            _serverNode.ClientRegistered += id => Dispatcher.UIThread.Post(() =>
            {
                string name = _serverNode?.GetDisplayName(id) ?? string.Empty;
                string addr = _serverNode?.Server.GetClientAddress(id) ?? "unknown";
                ClientsPanel.OnClientConnected(id.ToString(), name, addr);
                Log($"Client {(string.IsNullOrWhiteSpace(name) ? id.ToString() : name)} joined from {addr}");
            });
            _serverNode.ClientUnregistered += id => Dispatcher.UIThread.Post(() =>
            {
                ClientsPanel.OnClientDisconnected(id.ToString());
                Log($"Client {id} left");
            });

            Log($"Starting server on 0.0.0.0:{port} (timeout {timeoutOrHeartbeatSeconds}s)...");
            _ = StartServerAsync();
        }
        else
        {
            SetSidebarVisible(false);
            var view = new ClientView(address);
            ContentArea.Content = view;

            if (!IPAddress.TryParse(address, out var ip))
            {
                Log($"Invalid server address: '{address}'.");
                return;
            }

            _clientNode = new GuiClientNode(ip, port, TimeSpan.FromSeconds(timeoutOrHeartbeatSeconds), displayName);
            _clientNode.Connected     += () => Dispatcher.UIThread.Post(view.OnConnected);
            _clientNode.Disconnected  += () => Dispatcher.UIThread.Post(view.OnDisconnected);
            _clientNode.FrameStarted  += idx => Dispatcher.UIThread.Post(() => view.OnFrameStarted(idx));
            _clientNode.FrameCompleted += (idx, dur, result) => Dispatcher.UIThread.Post(() =>
            {
                // WriteableBitmap must be created on the UI thread.
                var bmp = FractalResultBitmap.From(result);
                view.OnFrameCompleted(idx, dur, bmp);
            });
            _clientNode.FrameFailed   += (idx, _) => Dispatcher.UIThread.Post(() => view.OnFrameFailed(idx));

            Log($"Connecting to {address}:{port} (heartbeat {timeoutOrHeartbeatSeconds}s)...");
            _ = StartClientAsync();
        }
    }

    private async Task StartServerAsync()
    {
        try
        {
            await _serverNode!.StartAsync();
            Dispatcher.UIThread.Post(() => Log("Server started. Waiting for clients..."));
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => Log($"Server failed to start: {ex.Message}"));
        }
    }

    private async Task StartClientAsync()
    {
        try
        {
            await _clientNode!.StartAsync();
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => Log($"Connection failed: {ex.Message}"));
        }
    }

    public async void NavigateToConnect()
    {
        SetSidebarVisible(false);
        SetConsoleVisible(false);

        if (_serverNode is not null)
        {
            try { await _serverNode.DisposeAsync(); } catch { }
            _serverNode = null;
        }
        if (_clientNode is not null)
        {
            try { await _clientNode.DisposeAsync(); } catch { }
            _clientNode = null;
        }

        ContentArea.Content = new ConnectView();
    }

    public void Log(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogTextBox.Text = string.IsNullOrEmpty(LogTextBox.Text)
            ? line
            : LogTextBox.Text + Environment.NewLine + line;
        LogTextBox.CaretIndex = LogTextBox.Text.Length;
    }

    private void SetConsoleVisible(bool visible)
    {
        ConsoleBorder.IsVisible   = visible;
        ConsoleSplitter.IsVisible = visible;
        RootGrid.RowDefinitions[1].Height = visible ? new GridLength(5) : new GridLength(0);
        RootGrid.RowDefinitions[2].Height = visible ? new GridLength(150) : new GridLength(0);
    }

    private void SetSidebarVisible(bool visible)
    {
        ClientsSidebarBorder.IsVisible = visible;
        ClientsSplitter.IsVisible      = visible;
        ContentGrid.ColumnDefinitions[1].Width = visible ? new GridLength(5) : new GridLength(0);
        ContentGrid.ColumnDefinitions[2].Width = visible ? new GridLength(180) : new GridLength(0);
    }

    /// <summary>Swap content back to the main editor without tearing down an active session.</summary>
    public void NavigateToMain(bool isServerMode)
    {
        SetConsoleVisible(true);
        if (isServerMode)
        {
            SetSidebarVisible(true);
            ContentArea.Content = new MainView(isServerMode);
        }
        else
        {
            SetSidebarVisible(false);
            ContentArea.Content = new ClientView(string.Empty);
        }
    }

    /// <summary>Kicks off a real distributed render and routes scheduler events to RenderView.</summary>
    public RenderView NavigateToRender(bool isServerMode, RenderJobConfig config)
    {
        var view = new RenderView(isServerMode, config.TotalFrames);
        SetConsoleVisible(true);
        ContentArea.Content = view;

        if (_serverNode is null)
        {
            Log("Cannot render: server is not running.");
            return view;
        }

        // Build interpolated frame sequence from keyframes (same as _ServerTest).
        var sequence = new KeyframeZoomSequenceGenerator<MandelbrotOptions>()
            .Generate(config.BaseOptions, config.Keyframes, config.TotalFrames, new SmoothStepInterpolation())
            .ToList();

        var frames = sequence
            .Select((opts, i) => (i, new RenderFractalMessage(
                _serverNode.Server.Identifier,
                i,
                FractalGeneratorType.Mandelbrot,
                config.Colorizer,
                opts)))
            .ToList();

        var job = new GuiRenderJob(_serverNode, frames, config.OutputPath, config.FrameRate);
        _currentJob = job;

        job.ClientAvailable += id => Dispatcher.UIThread.Post(() =>
        {
            string name = _serverNode?.GetDisplayName(id) ?? string.Empty;
            string addr = _serverNode?.Server.GetClientAddress(id) ?? "unknown";
            view.OnClientConnected(id.ToString(), name, addr);
        });
        job.ClientFailed    += id => Dispatcher.UIThread.Post(() => view.OnClientDisconnected(id.ToString()));
        job.FrameDispatched += (id, idx) => Dispatcher.UIThread.Post(() => view.OnFrameDispatched(id.ToString(), idx));
        job.FrameCompleted  += (id, idx, dur) => Dispatcher.UIThread.Post(() => view.OnFrameCompleted(id.ToString(), idx, dur));
        job.Completed       += path => Dispatcher.UIThread.Post(() => Log($"Render complete. GIF saved: {path}"));
        job.Failed          += ex => Dispatcher.UIThread.Post(() => Log($"Render failed: {ex.Message}"));

        Log($"Starting render: {config.TotalFrames} frames @ {config.FrameRate} fps → {config.OutputPath}");
        _ = job.StartAsync();
        return view;
    }
}
