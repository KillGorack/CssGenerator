# CssGenerator

A small Avalonia (.NET 9) desktop app for hand-building a set of CSS
classes and previewing them against real HTML.

## What it does

- **CSS classes** — add/rename/delete classes and their properties in
  the left panel. The generated CSS updates live as you type.
- **HTML snippets** — the HTML panel holds multiple named snippets per
  project (not just one blob), so you can keep a few markup examples
  around and switch between them.
- **Live-ish preview** — "Open in Browser" writes the current HTML +
  CSS to a temp file and opens it in your default browser for a true
  full-fidelity render (the app itself doesn't embed a renderer).
- **Projects** — New/Open/Save work against a `.cssgen` file, which is
  just the project's classes and HTML snippets serialized as JSON.
- **Export CSS** — writes the generated stylesheet out as a plain
  `.css` file, separate from the project file.

## Project layout

```
CssGenerator/
  Models/     CssProject, CssClass, CssProperty, HtmlSnippet
  Services/   CssBuilder (CSS text generation), ProjectFile (.cssgen
              load/save), CssPropertyNames (autocomplete data)
  Views/      MainWindow.axaml(.cs) — the whole UI is one window,
              wired up in code-behind rather than full MVVM
```

## Running it

```
cd CssGenerator
dotnet run
```

<img width="1483" height="753" alt="image" src="https://github.com/user-attachments/assets/63430a68-76df-4d81-b8c3-fec31eae526d" />


