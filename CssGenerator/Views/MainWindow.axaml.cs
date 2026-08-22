using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using CssGenerator.Models;
using CssGenerator.Services;

namespace CssGenerator.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType ProjectFileType = new("CssGenerator Project")
    {
        Patterns = new[] { "*.cssgen" }
    };

    private static readonly FilePickerFileType CssFileType = new("CSS File")
    {
        Patterns = new[] { "*.css" }
    };

    private CssProject _project;
    private string? _currentFilePath;

    public MainWindow()
    {
        InitializeComponent();

        _project = new CssProject();
        _project.HtmlSnippets.Add(new HtmlSnippet
        {
            Name = "snippet1",
            Html = "<div class=\"table\">\n    Hello World\n</div>"
        });

        ClassList.SelectionChanged += ClassList_SelectionChanged;
        ClassNameEditor.TextChanged += ClassNameEditor_TextChanged;
        AddPropertyButton.Click += AddProperty_Click;

        UpdateClassList();
        UpdateCss();

        UpdateSnippetList();
        SnippetSelector.SelectedIndex = 0;
    }

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        _project = new CssProject();
        _currentFilePath = null;

        ClassNameEditor.Text = string.Empty;

        UpdateClassList();
        UpdatePropertyPanel();
        UpdateCss();

        EnsureAtLeastOneSnippet();
        UpdateSnippetList();
        SnippetSelector.SelectedIndex = 0;
    }

    private async void Open_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Project",
            AllowMultiple = false,
            FileTypeFilter = new[] { ProjectFileType }
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;

        if (path is null)
        {
            return;
        }

        _project = ProjectFile.Load(path);
        _currentFilePath = path;

        ClassNameEditor.Text = string.Empty;

        UpdateClassList();
        UpdatePropertyPanel();
        UpdateCss();

        EnsureAtLeastOneSnippet();
        UpdateSnippetList();
        SnippetSelector.SelectedIndex = 0;
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        var path = _currentFilePath;

        if (path is null)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Project",
                SuggestedFileName = "project",
                DefaultExtension = "cssgen",
                FileTypeChoices = new[] { ProjectFileType }
            });

            path = file?.TryGetLocalPath();
        }

        if (path is null)
        {
            return;
        }

        ProjectFile.Save(path, _project);
        _currentFilePath = path;
    }

    private async void ExportCss_Click(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export CSS",
            SuggestedFileName = "styles",
            DefaultExtension = "css",
            FileTypeChoices = new[] { CssFileType }
        });

        var path = file?.TryGetLocalPath();

        if (path is null)
        {
            return;
        }

        File.WriteAllText(path, CssBuilder.Build(_project));
    }

    private void AddClass_Click(object? sender, RoutedEventArgs e)
    {
        var number = _project.Classes.Count + 1;

        var cssClass = new CssClass
        {
            Name = $"class{number}"
        };

        _project.Classes.Add(cssClass);

        UpdateClassList();

        ClassList.SelectedIndex = _project.Classes.IndexOf(cssClass);

        UpdateCss();
    }

    private void ClassList_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (ClassList.SelectedIndex < 0 ||
            ClassList.SelectedIndex >= _project.Classes.Count)
        {
            return;
        }

        var cssClass = _project.Classes[ClassList.SelectedIndex];

        ClassNameEditor.Text = cssClass.Name;

        UpdatePropertyPanel();
        UpdateCss();
    }

    private void ClassNameEditor_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (ClassList.SelectedIndex < 0 ||
            ClassList.SelectedIndex >= _project.Classes.Count)
        {
            return;
        }

        var cssClass = _project.Classes[ClassList.SelectedIndex];

        cssClass.Name = ClassNameEditor.Text ?? string.Empty;

        UpdateClassList();
        ClassList.SelectedIndex = _project.Classes.IndexOf(cssClass);

        UpdateCss();
    }


    private void AddProperty_Click(
    object? sender,
    RoutedEventArgs e)
    {
        if (ClassList.SelectedIndex < 0 ||
            ClassList.SelectedIndex >= _project.Classes.Count)
        {
            return;
        }

        var cssClass = _project.Classes[ClassList.SelectedIndex];

        cssClass.Properties.Add(new CssProperty());

        UpdatePropertyPanel();
        UpdateCss();
    }

    private void UpdateClassList()
    {
        _project.Classes.Sort((a, b) =>
        {
            var aIsDotted = a.Name.StartsWith(".") ? 1 : 0;
            var bIsDotted = b.Name.StartsWith(".") ? 1 : 0;

            return aIsDotted != bIsDotted
                ? aIsDotted - bIsDotted
                : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        var names = new List<string>();

        foreach (var cssClass in _project.Classes)
        {
            names.Add(cssClass.Name);
        }

        ClassList.ItemsSource = names;
    }

    private void UpdatePropertyPanel()
    {
        PropertyPanel.Children.Clear();

        if (ClassList.SelectedIndex < 0 ||
            ClassList.SelectedIndex >= _project.Classes.Count)
        {
            return;
        }

        var cssClass = _project.Classes[ClassList.SelectedIndex];

        foreach (var property in cssClass.Properties)
        {
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*,28"),
                ColumnSpacing = 6
            };

            var name = new TextBox
            {
                Text = property.Name,
                PlaceholderText = "property"
            };

            var value = new TextBox
            {
                Text = property.Value,
                PlaceholderText = "value"
            };

            var delete = new Button
            {
                Classes = { "subtle" },
                Content = "✕",
                Width = 28,
                Height = 28,
                Padding = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            name.TextChanged += (_, _) =>
            {
                property.Name = name.Text ?? string.Empty;
                UpdateCss();
            };

            value.TextChanged += (_, _) =>
            {
                property.Value = value.Text ?? string.Empty;
                UpdateCss();
            };

            delete.Click += (_, _) =>
            {
                cssClass.Properties.Remove(property);
                UpdatePropertyPanel();
                UpdateCss();
            };

            Grid.SetColumn(value, 1);
            Grid.SetColumn(delete, 2);

            row.Children.Add(name);
            row.Children.Add(value);
            row.Children.Add(delete);

            PropertyPanel.Children.Add(row);
        }
    }

    private void UpdateCss()
    {
        CssEditor.Text = CssBuilder.Build(_project);
    }

    private void AddSnippet_Click(object? sender, RoutedEventArgs e)
    {
        var number = _project.HtmlSnippets.Count + 1;

        var snippet = new HtmlSnippet
        {
            Name = $"snippet{number}"
        };

        _project.HtmlSnippets.Add(snippet);

        UpdateSnippetList();

        SnippetSelector.SelectedIndex = _project.HtmlSnippets.Count - 1;
    }

    private void DeleteSnippet_Click(object? sender, RoutedEventArgs e)
    {
        if (SnippetSelector.SelectedIndex < 0 ||
            SnippetSelector.SelectedIndex >= _project.HtmlSnippets.Count)
        {
            return;
        }

        _project.HtmlSnippets.RemoveAt(SnippetSelector.SelectedIndex);

        EnsureAtLeastOneSnippet();
        UpdateSnippetList();

        SnippetSelector.SelectedIndex = 0;
    }

    private void SnippetSelector_SelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (SnippetSelector.SelectedIndex < 0 ||
            SnippetSelector.SelectedIndex >= _project.HtmlSnippets.Count)
        {
            return;
        }

        var snippet = _project.HtmlSnippets[SnippetSelector.SelectedIndex];

        SnippetNameEditor.Text = snippet.Name;
        HtmlEditor.Text = snippet.Html;
    }

    private void SnippetNameEditor_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (SnippetSelector.SelectedIndex < 0 ||
            SnippetSelector.SelectedIndex >= _project.HtmlSnippets.Count)
        {
            return;
        }

        var snippet = _project.HtmlSnippets[SnippetSelector.SelectedIndex];

        snippet.Name = SnippetNameEditor.Text ?? string.Empty;

        UpdateSnippetList();
        SnippetSelector.SelectedIndex = _project.HtmlSnippets.IndexOf(snippet);
    }

    private void HtmlEditor_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (SnippetSelector.SelectedIndex < 0 ||
            SnippetSelector.SelectedIndex >= _project.HtmlSnippets.Count)
        {
            return;
        }

        var snippet = _project.HtmlSnippets[SnippetSelector.SelectedIndex];

        snippet.Html = HtmlEditor.Text ?? string.Empty;
    }

    private void UpdateSnippetList()
    {
        var names = new List<string>();

        foreach (var snippet in _project.HtmlSnippets)
        {
            names.Add(
                string.IsNullOrWhiteSpace(snippet.Name)
                    ? "(unnamed)"
                    : snippet.Name);
        }

        SnippetSelector.ItemsSource = names;
    }

    private void EnsureAtLeastOneSnippet()
    {
        if (_project.HtmlSnippets.Count == 0)
        {
            _project.HtmlSnippets.Add(new HtmlSnippet
            {
                Name = "snippet1"
            });
        }
    }

    private void OpenPreview_Click(object? sender, RoutedEventArgs e)
    {
        var html = $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
            {CssEditor.Text}
            </style>
            </head>
            <body>
            {HtmlEditor.Text}
            </body>
            </html>
            """;

        var path = Path.Combine(Path.GetTempPath(), "cssgenerator-preview.html");

        File.WriteAllText(path, html);

        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true
        });
    }
}