using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DistributedFractals.Gui.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string title, string message, string confirmLabel, string? cancelLabel, bool danger = false, string windowTitle = "Warning")
    {
        InitializeComponent();
        Title                 = windowTitle;
        TitleText.Text        = title;
        MessageText.Text      = message;
        ConfirmButton.Content = confirmLabel;

        if (cancelLabel is null)
        {
            CancelButton.IsVisible = false;
            Grid.SetColumn(ConfirmButton, 0);
            Grid.SetColumnSpan(ConfirmButton, 3);
            ConfirmButton.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
            ConfirmButton.MinWidth = 100;
        }
        else
            CancelButton.Content = cancelLabel;

        if (danger)
        {
            ConfirmButton.Classes.Remove("accent");
            ConfirmButton.Classes.Add("danger");
        }
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);
    private void OnCancelClick(object? sender, RoutedEventArgs e)  => Close(false);
}
