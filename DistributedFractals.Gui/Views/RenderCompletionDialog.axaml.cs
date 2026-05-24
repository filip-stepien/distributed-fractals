using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DistributedFractals.Gui.Views;

public sealed record RenderCompletionClientReport(
    string Name,
    TimeSpan? AverageBatchRender,
    TimeSpan? AverageBatchCommunication);

public sealed record RenderCompletionReport(
    string OutputPath,
    TimeSpan? RenderElapsed,
    TimeSpan? AverageBatchRender,
    TimeSpan? AverageBatchCommunication,
    IReadOnlyList<RenderCompletionClientReport> Clients);

public partial class RenderCompletionDialog : Window
{
    private readonly RenderCompletionReport _report;

    public RenderCompletionDialog(RenderCompletionReport report)
    {
        InitializeComponent();
        _report = report;

        OutputPathText.Text = report.OutputPath;
        RenderTimeText.Text = FormatDuration(report.RenderElapsed);
        AverageRenderText.Text = FormatDuration(report.AverageBatchRender);
        AverageCommunicationText.Text = FormatDuration(report.AverageBatchCommunication);

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
            ColumnDefinitions = new ColumnDefinitions("*,32,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Margin = new Thickness(16, 8, 16, 14)
        };

        TextBlock nameText = BuildText(client.Name, fontSize: 12, fontWeight: FontWeight.SemiBold);
        Grid.SetColumnSpan(nameText, 2);
        row.Children.Add(nameText);

        AddStat(row, 0, "Avg render", FormatDuration(client.AverageBatchRender));
        AddStat(row, 2, "Avg comm", FormatDuration(client.AverageBatchCommunication));

        return row;
    }

    private static void AddStat(Grid row, int column, string label, string value)
    {
        var stat = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(0, 10, 0, 0)
        };

        stat.Children.Add(BuildText(label, fontSize: 10, foreground: Color.FromRgb(0x6B, 0x72, 0x80)));
        stat.Children.Add(BuildText(value, fontSize: 13, fontWeight: FontWeight.SemiBold));

        Grid.SetRow(stat, 1);
        Grid.SetColumn(stat, column);
        row.Children.Add(stat);
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
        Avalonia.Layout.HorizontalAlignment horizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
        Color? foreground = null)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight ?? FontWeight.Normal,
            Foreground = new SolidColorBrush(foreground ?? Color.FromRgb(0xE2, 0xE8, 0xF0)),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = horizontalAlignment,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnExportCsvClick(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Export render summary",
            SuggestedFileName = "render-summary.csv",
            FileTypeChoices = [
                new Avalonia.Platform.Storage.FilePickerFileType("CSV") { Patterns = ["*.csv"] }
            ]
        });

        if (file is null)
        {
            return;
        }

        await using Stream stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteAsync(BuildTotalsCsv(_report));
    }

    private static string BuildTotalsCsv(RenderCompletionReport report)
    {
        var rows = new List<string>
        {
            CsvLine("metric", "value_ms"),
            CsvLine(
                "total_time",
                FormatMilliseconds(report.RenderElapsed)),
            CsvLine(
                "avg_render",
                FormatMilliseconds(report.AverageBatchRender)),
            CsvLine(
                "avg_comm",
                FormatMilliseconds(report.AverageBatchCommunication))
        };

        return string.Join(Environment.NewLine, rows) + Environment.NewLine;
    }

    private static string CsvLine(params string[] values)
    {
        return string.Join(",", values.Select(EscapeCsv));
    }

    private static string EscapeCsv(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string FormatMilliseconds(TimeSpan? duration)
    {
        return duration?.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatDuration(TimeSpan? duration)
    {
        if (duration is null)
        {
            return "-";
        }

        double milliseconds = duration.Value.TotalMilliseconds;
        if (milliseconds < 1000)
        {
            return $"{milliseconds:F0} ms";
        }

        if (duration.Value.TotalMinutes < 1)
        {
            return $"{duration.Value.TotalSeconds:F2} s";
        }

        return duration.Value.ToString(@"m\:ss\.f");
    }
}
