using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.Tests;

public class PresetServiceTests
{
    [Fact]
    public async Task ShouldSaveAndLoadExtractionPreset()
    {
        var service = new PresetService<ExtractionPreset>("presets/extraction");
        var preset = new ExtractionPreset
        {
            Name = "TestExtract",
            X0 = 1,
            Y0 = 2,
            X1 = 3,
            Y1 = 4
        };

        await service.SavePresetAsync(preset);
        var all = await service.LoadAllPresetsAsync();
        Assert.Contains(all, p => p.Name == "TestExtract");
    }

    [Fact]
    public async Task ShouldSaveAndLoadAnnotationPreset()
    {
        var service = new PresetService<AnnotationPreset>("presets/annotation");
        var preset = new AnnotationPreset
        {
            Name = "TestAnnot",
            TextX = 10,
            TextY = 20,
            FontSize = 12,
            Angle = 0,
            ColorHex = "#000000",
            FontName = "Helvetica"
        };

        await service.SavePresetAsync(preset);
        var all = await service.LoadAllPresetsAsync();
        Assert.Contains(all, p => p.Name == "TestAnnot");
    }
}
