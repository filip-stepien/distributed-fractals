using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DistributedFractals.Gui.Rendering;
using DistributedFractals.Gui.Views;
using DistributedFractals.Logging;
using DistributedFractals.Sessions;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Gui;

public partial class MainWindow : Window
{
    public readonly ClientsPanel ClientsPanel = new();

    private IServerSession? _serverSession;
    private IClientSession? _clientSession;

    private Action<ClientIdentifier, int>? _renderFrameDispatched;
    private Action<ClientIdentifier, int, TimeSpan>? _renderFrameCompleted;
    private Action<ClientIdentifier, int>? _renderFrameFailed;
    private Action<ClientIdentifier>? _renderClientConnected;
    private Action<ClientIdentifier>? _renderClientDisconnected;
    private Action<string>? _renderTimingReportReady;
    private Action? _renderCompleted;
    private Action<Exception>? _renderFailed;

    public MainWindow()
    {
        InitializeComponent();
        Logger.Initialize(NullLogger.Instance);

        ClientsSidebarContent.Content = ClientsPanel;
        ContentArea.Content = new ConnectView();
    }

    public void NavigateToMain(
        bool isServerMode,
        string address,
        int port,
        int timeoutOrHeartbeatSeconds,
        string displayName = "",
        TransportProtocol protocol = TransportProtocol.Tcp)
    {
        if (isServerMode)
        {
            StartServerView(address, port, timeoutOrHeartbeatSeconds, protocol);
            return;
        }

        StartClientView(address, port, timeoutOrHeartbeatSeconds, displayName, protocol);
    }

    public async void NavigateToConnect()
    {
        CancelRender();
        CleanupCurrentView();
        SetSidebarVisible(false);

        if (_serverSession is not null)
        {
            try
            {
                await _serverSession.DisposeAsync();
            }
            catch
            {
            }

            _serverSession = null;
        }

        if (_clientSession is not null)
        {
            try
            {
                await _clientSession.DisposeAsync();
            }
            catch
            {
            }

            _clientSession = null;
        }

        ClientsPanel.Reset();
        ContentArea.Content = new ConnectView();
    }

    public void NavigateToMain(bool isServerMode)
    {
        CleanupCurrentView();

        if (isServerMode)
        {
            SetSidebarVisible(true);
            ContentArea.Content = new MainView(isServerMode: true);
            return;
        }

        SetSidebarVisible(false);
        ContentArea.Content = new ClientView(string.Empty);
    }

    public RenderView NavigateToRender(bool isServerMode, RenderSettings settings)
    {
        var view = new RenderView(isServerMode, settings.TotalFrames);
        CleanupCurrentView();
        ContentArea.Content = view;

        if (_serverSession is null)
        {
            const string message = "Cannot render: server is not running.";
            Log(message);
            view.OnRenderFailed(message);
            return view;
        }

        foreach (ClientIdentifier client in _serverSession.Clients)
        {
            view.OnClientConnected(
                client.Id.ToString(),
                client.DisplayName,
                _serverSession.GetClientAddress(client) ?? "unknown");
        }

        BindRenderView(view, settings);
        Log($"Starting render: {settings.TotalFrames} frames @ {settings.Fps} fps, {settings.FramesPerBatch} frame(s)/batch -> {settings.OutputPath}");
        _ = StartRenderAsync(_serverSession, settings, view);

        return view;
    }

    public void CancelRender()
    {
        _serverSession?.CancelRender();
        ClearRenderBindings();
    }

    public void Log(string message)
    {
    }

    private void StartServerView(string address, int port, int timeoutSeconds, TransportProtocol protocol)
    {
        SetSidebarVisible(true);
        CleanupCurrentView();
        ClientsPanel.Reset();
        ContentArea.Content = new MainView(isServerMode: true);

        _serverSession = new ServerSession();
        _serverSession.ClientConnected += OnServerClientConnected;
        _serverSession.ClientDisconnected += OnServerClientDisconnected;

        var settings = new ServerConnectionSettings(
            address,
            port,
            protocol,
            TimeSpan.FromSeconds(timeoutSeconds));

        Log($"Starting server on {address}:{port} ({protocol}, timeout {timeoutSeconds}s)...");
        _ = StartServerAsync(_serverSession, settings);
    }

    private void StartClientView(
        string address,
        int port,
        int heartbeatSeconds,
        string displayName,
        TransportProtocol protocol)
    {
        SetSidebarVisible(false);
        CleanupCurrentView();

        var view = new ClientView($"{address}:{port}");
        ContentArea.Content = view;

        _clientSession = new ClientSession();
        _clientSession.Connected += () => Dispatcher.UIThread.Post(view.OnConnected);
        _clientSession.Disconnected += () => Dispatcher.UIThread.Post(view.OnDisconnected);
        _clientSession.FrameStarted += frameIndex =>
            Dispatcher.UIThread.Post(() => view.OnFrameStarted(frameIndex));
        _clientSession.FrameCompleted += (frameIndex, duration, result) =>
            Dispatcher.UIThread.Post(() =>
            {
                WriteableBitmap? preview = view.ShowPreview ? FractalResultBitmap.From(result) : null;
                view.OnFrameCompleted(frameIndex, duration, preview);
            });
        _clientSession.FrameFailed += frameIndex =>
            Dispatcher.UIThread.Post(() => view.OnFrameFailed(frameIndex));

        var settings = new ClientConnectionSettings(
            address,
            port,
            protocol,
            TimeSpan.FromSeconds(heartbeatSeconds));

        Log($"Connecting to {address}:{port} ({protocol}, heartbeat {heartbeatSeconds}s)...");
        _ = StartClientAsync(_clientSession, displayName, settings);
    }

    private async Task StartServerAsync(IServerSession session, ServerConnectionSettings settings)
    {
        try
        {
            await session.StartAsync(settings);
            Log("Server started. Waiting for clients...");
        }
        catch (Exception ex)
        {
            Log($"Server failed to start: {ex.Message}");
        }
    }

    private async Task StartClientAsync(IClientSession session, string displayName, ClientConnectionSettings settings)
    {
        try
        {
            await session.ConnectAsync(displayName, settings);
        }
        catch (Exception ex)
        {
            Log($"Connection failed: {ex.Message}");
        }
    }

    private async Task StartRenderAsync(IServerSession session, RenderSettings settings, RenderView view)
    {
        try
        {
            await session.StartRenderAsync(settings);
        }
        catch (Exception ex)
        {
            Log($"Render failed to start: {ex.Message}");
            view.OnRenderFailed(ex.Message);
            ClearRenderBindings();
        }
    }

    private void OnServerClientConnected(ClientIdentifier client)
    {
        Dispatcher.UIThread.Post(() =>
        {
            string address = _serverSession?.GetClientAddress(client) ?? "unknown";
            ClientsPanel.OnClientConnected(client.Id.ToString(), client.DisplayName, address);
            Log($"Client {FormatClientName(client)} joined from {address}");
        });
    }

    private void OnServerClientDisconnected(ClientIdentifier client)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ClientsPanel.OnClientDisconnected(client.Id.ToString());
            Log($"Client {FormatClientName(client)} left");
        });
    }

    private void BindRenderView(RenderView view, RenderSettings settings)
    {
        ClearRenderBindings();

        if (_serverSession is null)
        {
            return;
        }

        _renderClientConnected = client => Dispatcher.UIThread.Post(() =>
        {
            view.OnClientConnected(
                client.Id.ToString(),
                client.DisplayName,
                _serverSession?.GetClientAddress(client) ?? "unknown");
        });
        _renderClientDisconnected = client =>
            Dispatcher.UIThread.Post(() => view.OnClientDisconnected(client.Id.ToString()));
        _renderFrameDispatched = (client, frameIndex) =>
            Dispatcher.UIThread.Post(() => view.OnFrameDispatched(client.Id.ToString(), frameIndex));
        _renderFrameCompleted = (client, frameIndex, duration) =>
            Dispatcher.UIThread.Post(() => view.OnFrameCompleted(client.Id.ToString(), frameIndex, duration));
        _renderFrameFailed = (client, frameIndex) =>
            Dispatcher.UIThread.Post(() => view.OnFrameFailed(client.Id.ToString(), frameIndex));
        _renderTimingReportReady = report => Dispatcher.UIThread.Post(() => view.OnTimingReport(report));
        _renderCompleted = () => Dispatcher.UIThread.Post(() =>
        {
            view.OnRenderCompleted(settings.OutputPath);
            ClearRenderBindings();
        });
        _renderFailed = ex => Dispatcher.UIThread.Post(() =>
        {
            view.OnRenderFailed(ex.Message);
            Log($"Render failed: {ex.Message}");
            ClearRenderBindings();
        });

        _serverSession.ClientConnected += _renderClientConnected;
        _serverSession.ClientDisconnected += _renderClientDisconnected;
        _serverSession.FrameDispatched += _renderFrameDispatched;
        _serverSession.FrameCompleted += _renderFrameCompleted;
        _serverSession.FrameFailed += _renderFrameFailed;
        _serverSession.TimingReportReady += _renderTimingReportReady;
        _serverSession.RenderCompleted += _renderCompleted;
        _serverSession.RenderFailed += _renderFailed;
    }

    private void ClearRenderBindings()
    {
        if (_serverSession is null)
        {
            return;
        }

        if (_renderClientConnected is not null)
        {
            _serverSession.ClientConnected -= _renderClientConnected;
        }

        if (_renderClientDisconnected is not null)
        {
            _serverSession.ClientDisconnected -= _renderClientDisconnected;
        }

        if (_renderFrameDispatched is not null)
        {
            _serverSession.FrameDispatched -= _renderFrameDispatched;
        }

        if (_renderFrameCompleted is not null)
        {
            _serverSession.FrameCompleted -= _renderFrameCompleted;
        }

        if (_renderFrameFailed is not null)
        {
            _serverSession.FrameFailed -= _renderFrameFailed;
        }

        if (_renderTimingReportReady is not null)
        {
            _serverSession.TimingReportReady -= _renderTimingReportReady;
        }

        if (_renderCompleted is not null)
        {
            _serverSession.RenderCompleted -= _renderCompleted;
        }

        if (_renderFailed is not null)
        {
            _serverSession.RenderFailed -= _renderFailed;
        }

        _renderClientConnected = null;
        _renderClientDisconnected = null;
        _renderFrameDispatched = null;
        _renderFrameCompleted = null;
        _renderFrameFailed = null;
        _renderTimingReportReady = null;
        _renderCompleted = null;
        _renderFailed = null;
    }

    private static string FormatClientName(ClientIdentifier client)
    {
        return string.IsNullOrWhiteSpace(client.DisplayName) ? client.Id.ToString() : client.DisplayName;
    }

    private void CleanupCurrentView()
    {
        switch (ContentArea.Content)
        {
            case MainView mainView:
                mainView.Cleanup();
                break;
            case ClientView clientView:
                clientView.Cleanup();
                break;
        }
    }

    private void SetSidebarVisible(bool visible)
    {
        ClientsSidebarBorder.IsVisible = visible;
        ClientsSplitter.IsVisible = visible;
        ContentGrid.ColumnDefinitions[1].Width = visible ? new GridLength(5) : new GridLength(0);
        ContentGrid.ColumnDefinitions[2].Width = visible ? new GridLength(180) : new GridLength(0);
    }
}
