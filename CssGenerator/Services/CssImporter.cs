using System.Collections.Generic;
using CssGenerator.Models;
using ExCSS;

namespace CssGenerator.Services;

public static class CssImporter
{
    public static List<CssClass> Parse(string cssText)
    {
        var parser = new StylesheetParser();
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

                foreach (var property in rule.Style)
                {
                    cssClass.Properties.Add(new CssProperty
                    {
                        Name = property.Name,
                        Value = property.Value
                    });
                }

                classes.Add(cssClass);
            }
        }

        return classes;
    }
}
