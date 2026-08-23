using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CssGenerator.Models;

namespace CssGenerator.Services;

public static class SnippetResolver
{
    private static readonly Regex IncludePattern = new(
        @"<!--\s*include:\s*(.+?)\s*-->",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Resolve(string html, CssProject project)
    {
        return Resolve(html, project, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static string Resolve(string html, CssProject project, HashSet<string> activeNames)
    {
        return IncludePattern.Replace(html, match =>
        {
            var name = match.Groups[1].Value;

            if (!activeNames.Add(name))
            {
                return $"<!-- circular include: {name} -->";
            }

            var snippet = project.HtmlSnippets.Find(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

            var result = snippet is null
                ? $"<!-- snippet '{name}' not found -->"
                : Resolve(snippet.Html, project, activeNames);

            activeNames.Remove(name);

            return result;
        });
    }
}
