using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DistributedFractals.Gui.Views;

public partial class ConnectView : UserControl
{
    private bool _isServerMode = true;
    private bool _advancedOpen = false;

    public ConnectView()
    {
        InitializeComponent();
    }

    private void OnServerModeClick(object? sender, RoutedEventArgs e)
    {
        _isServerMode = true;
        AddressRow.IsVisible  = false;
        TimeoutRow.IsVisible   = true;
        HeartbeatRow.IsVisible = false;
        ServerModeButton.Classes.Set("accent", true);
        ServerModeButton.Classes.Set("muted",  false);
        ClientModeButton.Classes.Set("accent", false);
        ClientModeButton.Classes.Set("muted",  true);
    }

    private void OnClientModeClick(object? sender, RoutedEventArgs e)
    {
        _isServerMode = false;
        AddressRow.IsVisible   = true;
        TimeoutRow.IsVisible   = false;
        HeartbeatRow.IsVisible = true;
        ServerModeButton.Classes.Set("accent", false);
        ServerModeButton.Classes.Set("muted",  true);
        ClientModeButton.Classes.Set("accent", true);
        ClientModeButton.Classes.Set("muted",  false);
    }

    private void OnAdvancedToggleClick(object? sender, RoutedEventArgs e)
    {
        _advancedOpen = !_advancedOpen;
        AdvancedPanel.IsVisible = _advancedOpen;
        AdvancedArrow.Text      = _advancedOpen ? "▼" : "▶";
    }

    private void OnConnectClick(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is not MainWindow window) return;
        string address = $"{AddressInput.Text}:{PortInput.Text}";
        window.NavigateToMain(_isServerMode, address);
    }
}
