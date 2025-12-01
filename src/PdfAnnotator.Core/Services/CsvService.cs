using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public class CsvService : ICsvService
{
    private const string Header = "page;field_text;code";
    private static readonly Regex SeparatorRegex = new(@";(?=(?:[^\""]*\""[^\""]*\"")*[^\""]*$)", RegexOptions.Compiled);

    public Task SaveCsvAsync(string path, List<TableRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Header);

        foreach (var row in rows)
        {
            builder.Append(row.Page.ToString(CultureInfo.InvariantCulture));
            builder.Append(';');
            builder.Append(Quote(row.FieldText));
            builder.Append(';');
            builder.Append(Quote(row.Code));
            builder.AppendLine();
        }

        var content = builder.ToString();
        return File.WriteAllTextAsync(path, content, Encoding.UTF8);
    }

    public async Task<List<TableRow>> LoadCsvAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("CSV file not found.", path);
        }

        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
        if (lines.Length == 0 || !string.Equals(lines[0].Trim(), Header, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Invalid CSV header. Expected: " + Header);
        }

        var result = new List<TableRow>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = SplitCsvLine(line);
            if (parts.Length != 3)
            {
                throw new InvalidDataException($"Invalid CSV line at {i + 1}: {line}");
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var page))
            {
                throw new InvalidDataException($"Invalid page value at line {i + 1}");
            }

            result.Add(new TableRow
            {
                Page = page,
                FieldText = Unquote(parts[1]),
                Code = Unquote(parts[2])
            });
        }

        return result;
    }

    private static string Quote(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string Unquote(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('"') && trimmed.EndsWith('"') && trimmed.Length >= 2)
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2).Replace("\"\"", "\"");
        }
        return trimmed;
    }

    private static string[] SplitCsvLine(string line)
    {
        return SeparatorRegex.Split(line);
    }
}
