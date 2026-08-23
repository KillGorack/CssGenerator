using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CssGenerator.Views;

public enum AppendConflictChoice
{
    Cancel,
    Skip,
    Overwrite
}

public partial class AppendConflictDialog : Window
{
    public AppendConflictDialog()
    {
        InitializeComponent();
    }

    public AppendConflictDialog(IEnumerable<string> conflictingNames) : this()
    {
        ConflictList.ItemsSource = conflictingNames;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(AppendConflictChoice.Cancel);
    }

    private void Skip_Click(object? sender, RoutedEventArgs e)
    {
        Close(AppendConflictChoice.Skip);
    }

    private void Overwrite_Click(object? sender, RoutedEventArgs e)
    {
        Close(AppendConflictChoice.Overwrite);
    }
}
