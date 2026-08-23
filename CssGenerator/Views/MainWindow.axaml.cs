using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
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

    private string? CurrentFilePath
    {
        get => _currentFilePath;
        set
        {
            _currentFilePath = value;
            Title = value is null
                ? "CssGenerator"
                : $"{Path.GetFileName(value)} — CssGenerator";
        }
    }

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
        HtmlEditor.AddHandler(InputElement.KeyDownEvent, HtmlEditor_KeyDown, RoutingStrategies.Tunnel);
        KeyDown += MainWindow_KeyDown;

        UpdateClassList();
        UpdateCss();

        UpdateSnippetList();
        SnippetSelector.SelectedIndex = 0;
    }

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        _project = new CssProject();
        CurrentFilePath = null;

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
        CurrentFilePath = path;

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
        var path = CurrentFilePath ?? await PromptSaveProjectPathAsync();

        if (path is null)
        {
            return;
        }

        ProjectFile.Save(path, _project);
        CurrentFilePath = path;
    }

    private async void SaveAs_Click(object? sender, RoutedEventArgs e)
    {
        var path = await PromptSaveProjectPathAsync();

        if (path is null)
        {
            return;
        }

        ProjectFile.Save(path, _project);
        CurrentFilePath = path;
    }

    private async Task<string?> PromptSaveProjectPathAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Project",
            SuggestedFileName = "project",
            DefaultExtension = "cssgen",
            FileTypeChoices = new[] { ProjectFileType }
        });

        return file?.TryGetLocalPath();
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

    private async void ExportMinifiedCss_Click(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Minified CSS",
            SuggestedFileName = "styles.min",
            DefaultExtension = "css",
            FileTypeChoices = new[] { CssFileType }
        });

        var path = file?.TryGetLocalPath();

        if (path is null)
        {
            return;
        }

        File.WriteAllText(path, CssBuilder.BuildMinified(_project));
    }

    private async void ImportCss_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import CSS",
            AllowMultiple = false,
            FileTypeFilter = new[] { CssFileType }
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;

        if (path is null)
        {
            return;
        }

        var dialog = new ConfirmDialog(
            "Import CSS",
            "This action will remove the classes currently in your session. Are you sure?");

        var confirmed = await dialog.ShowDialog<bool>(this);

        if (!confirmed)
        {
            return;
        }

        var classes = CssImporter.Parse(File.ReadAllText(path));

        _project.Classes.Clear();
        _project.Classes.AddRange(classes);

        ClassNameEditor.Text = string.Empty;

        UpdateClassList();
        UpdatePropertyPanel();
        UpdateCss();
    }

    private async void Resources_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new ResourcesDialog(_project.Resources);

        var result = await dialog.ShowDialog<List<string>?>(this);

        if (result is not null)
        {
            _project.Resources = result;
        }
    }

    private void Stats_Click(object? sender, RoutedEventArgs e)
    {
        new StatsDialog(_project).ShowDialog(this);
    }

    private void ClassSearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        var query = ClassSearchBox.Text;

        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        var index = _project.Classes.FindIndex(c =>
            c.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            ClassList.SelectedIndex = index;
        }
    }

    private void DeleteClass_Click(object? sender, RoutedEventArgs e)
    {
        if (ClassList.SelectedIndex < 0 ||
            ClassList.SelectedIndex >= _project.Classes.Count)
        {
            return;
        }

        _project.Classes.RemoveAt(ClassList.SelectedIndex);

        ClassNameEditor.Text = string.Empty;

        UpdateClassList();
        UpdatePropertyPanel();
        UpdateCss();
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
                ColumnDefinitions = new ColumnDefinitions("*,*,24,28"),
                ColumnSpacing = 6
            };

            var name = new AutoCompleteBox
            {
                Text = property.Name,
                PlaceholderText = "property",
                ItemsSource = CssPropertyNames.All,
                FilterMode = AutoCompleteFilterMode.Contains,
                MinimumPrefixLength = 1
            };

            var value = new TextBox
            {
                Text = property.Value,
                PlaceholderText = "value"
            };

            var swatch = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush(Color.Parse("#E3E6EA")),
                BorderThickness = new Thickness(1),
                IsVisible = false
            };

            if (Color.TryParse(property.Value, out var initialColor))
            {
                swatch.Background = new SolidColorBrush(initialColor);
                swatch.IsVisible = true;
            }

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

                if (Color.TryParse(property.Value, out var color))
                {
                    swatch.Background = new SolidColorBrush(color);
                    swatch.IsVisible = true;
                }
                else
                {
                    swatch.IsVisible = false;
                }

                UpdateCss();
            };

            delete.Click += (_, _) =>
            {
                cssClass.Properties.Remove(property);
                UpdatePropertyPanel();
                UpdateCss();
            };

            Grid.SetColumn(value, 1);
            Grid.SetColumn(swatch, 2);
            Grid.SetColumn(delete, 3);

            row.Children.Add(name);
            row.Children.Add(value);
            row.Children.Add(swatch);
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

        SnippetSelector.SelectedIndex = _project.HtmlSnippets.IndexOf(snippet);
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

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.S when shift:
                SaveAs_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;

            case Key.S:
                Save_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;

            case Key.N:
                New_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;

            case Key.O:
                Open_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    private void HtmlEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Tab)
        {
            return;
        }

        e.Handled = true;

        var text = HtmlEditor.Text ?? string.Empty;
        var start = Math.Min(HtmlEditor.SelectionStart, HtmlEditor.SelectionEnd);
        var end = Math.Max(HtmlEditor.SelectionStart, HtmlEditor.SelectionEnd);
        var outdent = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (start == end && !outdent)
        {
            HtmlEditor.Text = text.Insert(start, "    ");
            HtmlEditor.SelectionStart = HtmlEditor.SelectionEnd = start + 4;
            return;
        }

        var lineStart = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;

        var lineEndSearchFrom = end > start ? end - 1 : end;
        var nextNewline = text.IndexOf('\n', lineEndSearchFrom);
        var lineEnd = nextNewline < 0 ? text.Length : nextNewline;

        var block = text.Substring(lineStart, lineEnd - lineStart);
        var lines = block.Split('\n');
        var newLines = new string[lines.Length];
        var firstLineRemoved = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            if (outdent)
            {
                var removeCount = 0;

                while (removeCount < 4 &&
                       removeCount < lines[i].Length &&
                       lines[i][removeCount] == ' ')
                {
                    removeCount++;
                }

                newLines[i] = lines[i].Substring(removeCount);

                if (i == 0)
                {
                    firstLineRemoved = removeCount;
                }
            }
            else
            {
                newLines[i] = "    " + lines[i];
            }
        }

        var newBlock = string.Join("\n", newLines);
        var lengthDelta = newBlock.Length - block.Length;

        HtmlEditor.Text = text.Substring(0, lineStart) + newBlock + text.Substring(lineEnd);

        if (outdent)
        {
            HtmlEditor.SelectionStart = Math.Max(lineStart, start - firstLineRemoved);
            HtmlEditor.SelectionEnd = Math.Max(HtmlEditor.SelectionStart, end + lengthDelta);
        }
        else
        {
            HtmlEditor.SelectionStart = start + 4;
            HtmlEditor.SelectionEnd = end + lengthDelta;
        }
    }

    private void UpdateSnippetList()
    {
        _project.HtmlSnippets.Sort((a, b) =>
            string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

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

    private static readonly string PreviewPath =
        Path.Combine(Path.GetTempPath(), "cssgenerator-preview.html");

    private string BuildPreviewHtml()
    {
        var resources = string.Join(Environment.NewLine, _project.Resources);
        var body = SnippetResolver.Resolve(HtmlEditor.Text ?? string.Empty, _project);

        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <meta http-equiv="refresh" content="1">
            {resources}
            <style>
            {CssEditor.Text}
            </style>
            </head>
            <body>
            {body}
            </body>
            </html>
            """;
    }

    private void OpenPreview_Click(object? sender, RoutedEventArgs e)
    {
        File.WriteAllText(PreviewPath, BuildPreviewHtml());

        Process.Start(new ProcessStartInfo(PreviewPath)
        {
            UseShellExecute = true
        });
    }

    private void PushPreview_Click(object? sender, RoutedEventArgs e)
    {
        File.WriteAllText(PreviewPath, BuildPreviewHtml());
    }
}