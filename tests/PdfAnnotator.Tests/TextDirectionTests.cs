using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.Tests;

public class TextDirectionTests
{
    [Fact]
    public async Task ExtractionPresetShouldPersistTextDirection()
    {
        var service = new PresetService<ExtractionPreset>("presets/extraction");
        var preset = new ExtractionPreset
        {
            Name = "DirectionTest",
            X0 = 100,
            Y0 = 100,
            X1 = 300,
            Y1 = 200,
            Direction = TextDirection.RightToLeftTopToBottom
        };

        await service.SavePresetAsync(preset);
        var loaded = await service.LoadAllPresetsAsync();
        var found = loaded.FirstOrDefault(p => p.Name == "DirectionTest");
        
        Assert.NotNull(found);
        Assert.Equal(TextDirection.RightToLeftTopToBottom, found.Direction);
    }

    [Theory]
    [InlineData(TextDirection.LeftToRightTopToBottom)]
    [InlineData(TextDirection.RightToLeftTopToBottom)]
    [InlineData(TextDirection.LeftToRightBottomToTop)]
    [InlineData(TextDirection.RightToLeftBottomToTop)]
    public async Task AllTextDirectionsShouldSerializeCorrectly(TextDirection direction)
    {
        var service = new PresetService<ExtractionPreset>("presets/extraction");
        var preset = new ExtractionPreset
        {
            Name = $"Direction_{direction}",
            X0 = 10,
            Y0 = 20,
            X1 = 30,
            Y1 = 40,
            Direction = direction
        };

        await service.SavePresetAsync(preset);
        var loaded = await service.LoadAllPresetsAsync();
        var found = loaded.FirstOrDefault(p => p.Name == preset.Name);
        
        Assert.NotNull(found);
        Assert.Equal(direction, found.Direction);
    }

    [Fact]
    public async Task PresetWithoutDirectionShouldDefaultToLeftToRightTopToBottom()
    {
        // Create a JSON file without Direction property to simulate old presets
        var presetPath = Path.Combine("presets", "extraction", "OldFormat.json");
        Directory.CreateDirectory(Path.GetDirectoryName(presetPath)!);
        
        var json = @"{
  ""Name"": ""OldFormat"",
  ""X0"": 50,
  ""Y0"": 60,
  ""X1"": 70,
  ""Y1"": 80
}";
        await File.WriteAllTextAsync(presetPath, json);

        var service = new PresetService<ExtractionPreset>("presets/extraction");
        var loaded = await service.LoadPresetAsync(presetPath);
        
        Assert.NotNull(loaded);
        Assert.Equal(TextDirection.LeftToRightTopToBottom, loaded.Direction);
    }

    [Fact]
    public void TextDirectionEnumShouldHaveFourValues()
    {
        var values = Enum.GetValues<TextDirection>();
        Assert.Equal(4, values.Length);
        Assert.Contains(TextDirection.LeftToRightTopToBottom, values);
        Assert.Contains(TextDirection.RightToLeftTopToBottom, values);
        Assert.Contains(TextDirection.LeftToRightBottomToTop, values);
        Assert.Contains(TextDirection.RightToLeftBottomToTop, values);
    }
}
