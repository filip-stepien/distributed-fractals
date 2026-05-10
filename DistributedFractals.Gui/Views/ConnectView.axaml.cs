using Avalonia.Controls;
using Avalonia.Interactivity;
using DistributedFractals.Server.Core;

namespace DistributedFractals.Gui.Views;

public partial class ConnectView : UserControl
{
    private bool _isServerMode = true;
    private bool _advancedOpen;

    public ConnectView()
    {
        InitializeComponent();
    }

    private void OnServerModeClick(object? sender, RoutedEventArgs e)
    {
        _isServerMode = true;
        ListenAddressRow.IsVisible = true;
        AddressRow.IsVisible = false;
        DisplayNameRow.IsVisible = false;
        TimeoutRow.IsVisible = true;
        HeartbeatRow.IsVisible = false;
        ServerModeButton.Classes.Set("accent", true);
        ServerModeButton.Classes.Set("muted", false);
        ClientModeButton.Classes.Set("accent", false);
        ClientModeButton.Classes.Set("muted", true);
    }

    private void OnClientModeClick(object? sender, RoutedEventArgs e)
    {
        _isServerMode = false;
        ListenAddressRow.IsVisible = false;
        AddressRow.IsVisible = true;
        DisplayNameRow.IsVisible = true;
        TimeoutRow.IsVisible = false;
        HeartbeatRow.IsVisible = true;
        ServerModeButton.Classes.Set("accent", false);
        ServerModeButton.Classes.Set("muted", true);
        ClientModeButton.Classes.Set("accent", true);
        ClientModeButton.Classes.Set("muted", false);
    }

    private void OnAdvancedToggleClick(object? sender, RoutedEventArgs e)
    {
        _advancedOpen = !_advancedOpen;
        AdvancedPanel.IsVisible = _advancedOpen;
        AdvancedArrow.Text = _advancedOpen ? "v" : ">";
    }

    private void OnConnectClick(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is not MainWindow window)
        {
            return;
        }

        if (!int.TryParse(PortInput.Text, out int port) || port <= 0 || port > 65535)
        {
            window.Log("Invalid port.");
            return;
        }

        TransportProtocol protocol = ProtocolCombo.SelectedIndex == 1
            ? TransportProtocol.Udp
            : TransportProtocol.Tcp;

        if (_isServerMode)
        {
            StartServer(window, port, protocol);
            return;
        }

        StartClient(window, port, protocol);
    }

    private void StartServer(MainWindow window, int port, TransportProtocol protocol)
    {
        string listenText = ListenAddressInput.Text?.Trim() ?? string.Empty;
        if (!System.Net.IPAddress.TryParse(listenText, out var _))
        {
            window.Log($"Invalid listen address: '{listenText}'.");
            return;
        }

        int timeoutSec = (int)(TimeoutInput.Value ?? 30m);
        window.NavigateToMain(
            isServerMode: true,
            address: listenText,
            port: port,
            timeoutOrHeartbeatSeconds: timeoutSec,
            displayName: string.Empty,
            protocol: protocol);
    }

    private void StartClient(MainWindow window, int port, TransportProtocol protocol)
    {
        string addressText = AddressInput.Text?.Trim() ?? string.Empty;
        if (!System.Net.IPAddress.TryParse(addressText, out var _))
        {
            window.Log($"Invalid server address: '{addressText}'.");
            return;
        }

        int heartbeatSec = (int)(HeartbeatInput.Value ?? 5m);
        string displayName = DisplayNameInput.Text?.Trim() ?? string.Empty;
        window.NavigateToMain(
            isServerMode: false,
            address: addressText,
            port: port,
            timeoutOrHeartbeatSeconds: heartbeatSec,
            displayName: displayName,
            protocol: protocol);
    }
}
