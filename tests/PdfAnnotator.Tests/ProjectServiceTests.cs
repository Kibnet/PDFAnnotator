using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.Tests;

public class ProjectServiceTests
{
    [Fact]
    public async Task SaveAndLoadProject_Works()
    {
        var service = new ProjectService();
        var project = new PdfProject
        {
            Name = "Sample",
            PdfPath = "sample.pdf",
            ExtractPresetName = "ExtractPreset1",
            AnnotatePresetName = "AnnotPreset1",
            Rows = new List<TableRow>
            {
                new() { Page = 1, FieldText = "text", Code = "C1" }
            }
        };

        var path = Path.Combine(Path.GetTempPath(), "project_test.json");
        await service.SaveProjectAsync(project, path);
        var loaded = await service.LoadProjectAsync(path);

        Assert.Equal(project.Name, loaded.Name);
        Assert.Single(loaded.Rows);
        Assert.Equal("C1", loaded.Rows[0].Code);
    }
}
