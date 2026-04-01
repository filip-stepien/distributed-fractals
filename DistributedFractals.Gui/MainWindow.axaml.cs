using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DistributedFractals.Gui.Views;

namespace DistributedFractals.Gui;

public partial class MainWindow : Window
{
    /// <summary>Always-visible clients panel (server mode only). Use to report connect/disconnect events.</summary>
    public readonly ClientsPanel ClientsPanel = new();

    public MainWindow()
    {
        InitializeComponent();
        ClientsSidebarContent.Content = ClientsPanel;
        ContentArea.Content = new ConnectView();
        ConsoleBorder.IsVisible = false;
    }

    public void NavigateToMain(bool isServerMode, string serverAddress = "")
    {
        SetConsoleVisible(true);

        if (isServerMode)
        {
            SetSidebarVisible(true);
            ContentArea.Content = new MainView(isServerMode);

            // TODO: remove — mock clients for UI preview
            ClientsPanel.OnClientConnected("client-1", "192.168.1.10");
            ClientsPanel.OnClientConnected("client-2", "192.168.1.11");
        }
        else
        {
            SetSidebarVisible(false);
            var view = new ClientView(serverAddress);
            ContentArea.Content = view;
            view.StartMockClient(); // TODO: replace with real TCP client
        }
    }

    public void NavigateToConnect()
    {
        SetSidebarVisible(false);
        SetConsoleVisible(false);
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

    public RenderView NavigateToRender(bool isServerMode, int totalFrames)
    {
        var view = new RenderView(isServerMode, totalFrames);
        SetConsoleVisible(true);
        ContentArea.Content = view;
        view.StartMockRender(); // TODO: replace with real scheduler when network is wired up
        return view;
    }
}
