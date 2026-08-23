using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CssGenerator.Models;

namespace CssGenerator.Services;

public static class CssCondenser
{
    private static readonly string[] Sides = { "top", "right", "bottom", "left" };

    private static readonly Regex ZeroUnitPattern = new(
        @"^0(px|em|rem|%|pt|vh|vw|vmin|vmax|ex|ch|cm|mm|in|pc|deg|rad|grad|turn|s|ms|fr|q)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static void Condense(CssProject project)
    {
        MergeDuplicateClasses(project);

        foreach (var cssClass in project.Classes)
        {
            DedupeProperties(cssClass);
            CondenseSides(cssClass, "padding");
            CondenseSides(cssClass, "margin");
            StripZeroUnits(cssClass);
        }

        RemoveEmptyClasses(project);
    }

    private static void MergeDuplicateClasses(CssProject project)
    {
        var merged = new List<CssClass>();

        foreach (var cssClass in project.Classes)
        {
            var existing = merged.Find(c =>
                string.Equals(c.Name, cssClass.Name, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                merged.Add(cssClass);
            }
            else
            {
                existing.Properties.AddRange(cssClass.Properties);
            }
        }

        project.Classes.Clear();
        project.Classes.AddRange(merged);
    }

    private static void DedupeProperties(CssClass cssClass)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<CssProperty>();

        for (var i = cssClass.Properties.Count - 1; i >= 0; i--)
        {
            var property = cssClass.Properties[i];

            if (seen.Add(property.Name.Trim()))
            {
                kept.Add(property);
            }
        }

        kept.Reverse();

        cssClass.Properties.Clear();
        cssClass.Properties.AddRange(kept);
    }

    private static void CondenseSides(CssClass cssClass, string prefix)
    {
        var indices = new int[4];
        var values = new string[4];

        for (var i = 0; i < 4; i++)
        {
            indices[i] = cssClass.Properties.FindIndex(p =>
                string.Equals(p.Name.Trim(), $"{prefix}-{Sides[i]}", StringComparison.OrdinalIgnoreCase));

            if (indices[i] < 0)
            {
                return;
            }

            values[i] = cssClass.Properties[indices[i]].Value.Trim();
        }

        var shorthandValue = SimplifySides(values[0], values[1], values[2], values[3]);
        var firstIndex = indices.Min();

        cssClass.Properties[firstIndex] = new CssProperty
        {
            Name = prefix,
            Value = shorthandValue
        };

        foreach (var index in indices.Where(i => i != firstIndex).OrderByDescending(i => i))
        {
            cssClass.Properties.RemoveAt(index);
        }
    }

    private static string SimplifySides(string top, string right, string bottom, string left)
    {
        if (top == right && right == bottom && bottom == left)
        {
            return top;
        }

        if (top == bottom && right == left)
        {
            return $"{top} {right}";
        }

        if (right == left)
        {
            return $"{top} {right} {bottom}";
        }

        return $"{top} {right} {bottom} {left}";
    }

    private static void StripZeroUnits(CssClass cssClass)
    {
        foreach (var property in cssClass.Properties)
        {
            var tokens = property.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < tokens.Length; i++)
            {
                if (ZeroUnitPattern.IsMatch(tokens[i]))
                {
                    tokens[i] = "0";
                }
            }

            property.Value = string.Join(" ", tokens);
        }
    }

    private static void RemoveEmptyClasses(CssProject project)
    {
        project.Classes.RemoveAll(c =>
            c.Properties.Count == 0 &&
            !string.Equals(c.Name.Trim(), ":root", StringComparison.OrdinalIgnoreCase));
    }
}
