using System.Text;
using CssGenerator.Models;

namespace CssGenerator.Services;

public static class CssBuilder
{
    public static string Build(CssProject project)
    {
        var css = new StringBuilder();

        foreach (var cssClass in project.Classes)
        {
            if (string.IsNullOrWhiteSpace(cssClass.Name))
                continue;

            css.AppendLine($"{cssClass.Name} {{");

            foreach (var property in cssClass.Properties)
            {
                if (string.IsNullOrWhiteSpace(property.Name))
                    continue;

                css.AppendLine(
                    $"    {property.Name}: {property.Value};");
            }

            css.AppendLine("}");
            css.AppendLine();
        }

        return css.ToString();
    }
}