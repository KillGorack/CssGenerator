using System.IO;
using System.Text.Json;
using CssGenerator.Models;

namespace CssGenerator.Services;

public static class ProjectFile
{
    public static void Save(string filename, CssProject project)
    {
        var json = JsonSerializer.Serialize(
            project,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(filename, json);
    }

    public static CssProject Load(string filename)
    {
        var json = File.ReadAllText(filename);

        return JsonSerializer.Deserialize<CssProject>(json)
               ?? new CssProject();
    }
}