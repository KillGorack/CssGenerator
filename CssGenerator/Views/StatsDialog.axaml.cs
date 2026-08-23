using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CssGenerator.Models;

namespace CssGenerator.Views;

public partial class StatsDialog : Window
{
    public StatsDialog()
    {
        InitializeComponent();
    }

    public StatsDialog(CssProject project) : this()
    {
        ClassCountText.Text = project.Classes.Count.ToString();
        PropertyCountText.Text = project.Classes.Sum(c => c.Properties.Count).ToString();
        SnippetCountText.Text = project.HtmlSnippets.Count.ToString();
        ResourceCountText.Text = project.Resources.Count.ToString();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
