using System;
using System.Collections.Generic;
using System.IO;
using CssGenerator.Models;
using Microsoft.Data.Sqlite;

namespace CssGenerator.Services;

public static class WebPartRepository
{
    private const string FileName = "webparts.db";

    public static List<WebPart> LoadAll()
    {
        var parts = new List<WebPart>();
        var path = ResolveDatabasePath();

        if (path is null)
        {
            return parts;
        }

        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT ID, Part_Name, Part_HTML FROM web_part_data ORDER BY Part_Name";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            parts.Add(new WebPart
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Html = reader.IsDBNull(2) ? string.Empty : reader.GetValue(2).ToString() ?? string.Empty
            });
        }

        return parts;
    }

    private static string? ResolveDatabasePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        for (var i = 0; i < 6 && directory is not null; i++)
        {
            var candidate = Path.Combine(directory.FullName, FileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
