using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DistributedFractals.Gui.Views;

public sealed record RenderCompletionClientReport(
    string Name,
    string Status,
    int FramesDone,
    int FailedFrames);

public sealed record RenderCompletionReport(
    string OutputPath,
    int FramesDone,
    int TotalFrames,
    int FailedFrames,
    IReadOnlyList<RenderCompletionClientReport> Clients);

public partial class RenderCompletionDialog : Window
{
    public RenderCompletionDialog(RenderCompletionReport report)
    {
        InitializeComponent();

        OutputPathText.Text = report.OutputPath;
        FramesText.Text = $"{report.FramesDone} / {report.TotalFrames}";
        ClientsText.Text = report.Clients.Count.ToString();
        FailedText.Text = report.FailedFrames.ToString();

        if (report.Clients.Count == 0)
        {
            ClientRowsPanel.Children.Add(BuildEmptyRow());
            return;
        }

        foreach (RenderCompletionClientReport client in report.Clients)
        {
            ClientRowsPanel.Children.Add(BuildClientRow(client));
        }
    }

    private static Control BuildClientRow(RenderCompletionClientReport client)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,92,82,82"),
            Margin = new Thickness(12, 0, 12, 0),
            MinHeight = 34
        };

        row.Children.Add(BuildText(client.Name, horizontalAlignment: Avalonia.Layout.HorizontalAlignment.Left));

        TextBlock statusText = BuildText(client.Status, fontSize: 11, fontWeight: FontWeight.SemiBold);
        Grid.SetColumn(statusText, 1);
        row.Children.Add(statusText);

        TextBlock framesText = BuildText(client.FramesDone.ToString(), horizontalAlignment: Avalonia.Layout.HorizontalAlignment.Right);
        Grid.SetColumn(framesText, 2);
        row.Children.Add(framesText);

        TextBlock failedText = BuildText(client.FailedFrames.ToString(), horizontalAlignment: Avalonia.Layout.HorizontalAlignment.Right);
        Grid.SetColumn(failedText, 3);
        row.Children.Add(failedText);

        return row;
    }

    private static Control BuildEmptyRow()
    {
        return new TextBlock
        {
            Text = "No clients were connected.",
            Margin = new Thickness(12, 0, 12, 10),
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
        };
    }

    private static TextBlock BuildText(
        string text,
        double fontSize = 12,
        FontWeight? fontWeight = null,
        Avalonia.Layout.HorizontalAlignment horizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight ?? FontWeight.Normal,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = horizontalAlignment,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
