using System.Collections.Generic;

namespace CssGenerator.Models;

public class CssProject
{
    public List<CssClass> Classes { get; set; } = new();

    public List<HtmlSnippet> HtmlSnippets { get; set; } = new();
}