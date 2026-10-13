using System.Collections.Generic;

namespace CssGenerator.Models;

public class CssClass
{
    public string Name { get; set; } = string.Empty;

    public List<CssProperty> Properties { get; set; } = new();
}