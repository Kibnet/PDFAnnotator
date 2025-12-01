using System.Text.Json;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public class ProjectService : IProjectService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public async Task SaveProjectAsync(PdfProject project, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(project, Options);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<PdfProject> LoadProjectAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Project file not found.", path);
        }

        var json = await File.ReadAllTextAsync(path);
        var project = JsonSerializer.Deserialize<PdfProject>(json, Options);
        if (project == null)
        {
            throw new InvalidDataException("Unable to parse project file.");
        }

        project.Rows ??= new List<TableRow>();
        return project;
    }
}
