using System.Collections.Generic;
using CssGenerator.Models;
using ExCSS;

namespace CssGenerator.Services;

public static class CssImporter
{
    public static List<CssClass> Parse(string cssText)
    {
        var parser = new StylesheetParser(
            includeUnknownDeclarations: true,
            tolerateInvalidValues: true);
        var stylesheet = parser.Parse(cssText);

        var classes = new List<CssClass>();

        foreach (var rule in stylesheet.StyleRules)
        {
            foreach (var selector in rule.SelectorText.Split(','))
            {
                var name = selector.Trim();

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var cssClass = new CssClass { Name = name };

                foreach (var declaration in rule.Style.CssText.Split(';'))
                {
                    var trimmed = declaration.Trim();

                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    var colonIndex = trimmed.IndexOf(':');

                    if (colonIndex < 0)
                        continue;

                    cssClass.Properties.Add(new CssProperty
                    {
                        Name = trimmed.Substring(0, colonIndex).Trim(),
                        Value = trimmed.Substring(colonIndex + 1).Trim()
                    });
                }

                classes.Add(cssClass);
            }
        }

        return classes;
    }
}
