using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace DistributedFractals.Gui.Views;

public partial class ClientsPanel : UserControl
{
    private static readonly Color GreenColor  = Color.FromRgb(0x22, 0xC5, 0x5E);
    private static readonly Color RedColor    = Color.FromRgb(0xEF, 0x44, 0x44);
    private static readonly Color TextColor   = Color.FromRgb(0xE2, 0xE8, 0xF0);
    private static readonly Color MutedColor  = Color.FromRgb(0x6B, 0x72, 0x80);
    private static readonly Color BorderColor = Color.FromRgb(0x2A, 0x2D, 0x35);
    private static readonly Color SurfaceColor = Color.FromRgb(0x1C, 0x1F, 0x26);

    private readonly Dictionary<string, (Border Row, Ellipse Dot)> _rows = new();
    private int _connectedCount;

    public ClientsPanel()
    {
        InitializeComponent();
    }

    public void OnClientConnected(string clientId, string address)
    {
        EmptyText.IsVisible = false;

        var dot = new Ellipse
        {
            Width = 7, Height = 7,
            Fill = new SolidColorBrush(GreenColor),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 3, 8, 0)
        };

        var nameText = new TextBlock
        {
            Text = clientId,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(TextColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var nameRow = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal
        };
        nameRow.Children.Add(dot);
        nameRow.Children.Add(nameText);

        var ipText = new TextBlock
        {
            Text = address,
            FontSize = 11,
            Foreground = new SolidColorBrush(MutedColor),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Avalonia.Thickness(15, 2, 0, 0)
        };

        var content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        content.Children.Add(nameRow);
        content.Children.Add(ipText);

        var row = new Border
        {
            Height = 52,
            Padding = new Avalonia.Thickness(14, 6),
            BorderBrush = new SolidColorBrush(BorderColor),
            BorderThickness = new Avalonia.Thickness(0, 0, 0, 1),
            Child = content
        };

        _rows[clientId] = (row, dot);
        ClientList.Children.Add(row);

        _connectedCount++;
        UpdateCount();
        Log($"{address} joined");
    }

    public void OnClientDisconnected(string clientId)
    {
        if (!_rows.TryGetValue(clientId, out var entry)) return;

        entry.Dot.Fill = new SolidColorBrush(RedColor);
        entry.Row.Opacity = 0.5;

        _connectedCount = Math.Max(0, _connectedCount - 1);
        UpdateCount();
        Log($"{clientId} left");
    }

    private void UpdateCount()
    {
        CountText.Text = _connectedCount.ToString();
        CountBadge.Background = _connectedCount > 0
            ? new SolidColorBrush(Color.FromArgb(0x33, 0x22, 0xC5, 0x5E))
            : new SolidColorBrush(BorderColor);
        CountText.Foreground = _connectedCount > 0
            ? new SolidColorBrush(GreenColor)
            : new SolidColorBrush(MutedColor);
    }

    private void Log(string message)
    {
        (VisualRoot as MainWindow)?.Log(message);
    }
}
