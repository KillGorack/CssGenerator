using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CssGenerator.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Continue_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
