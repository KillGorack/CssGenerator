using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CssGenerator.Views;

public partial class ResourcesDialog : Window
{
    public ResourcesDialog()
    {
        InitializeComponent();
    }

    public ResourcesDialog(IEnumerable<string> resources) : this()
    {
        ResourcesBox.Text = string.Join(Environment.NewLine, resources);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void Done_Click(object? sender, RoutedEventArgs e)
    {
        var lines = (ResourcesBox.Text ?? string.Empty)
            .Split('\n')
            .Select(line => line.Trim('\r', ' ', '\t'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        Close(lines);
    }
}
