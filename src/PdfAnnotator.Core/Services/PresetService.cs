using System.Text.Json;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public class PresetService : IPresetService
{
    private const string ExtractDir = "presets/extraction";
    private const string AnnotateDir = "presets/annotation";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public Task SaveExtractionPresetAsync(ExtractionPreset preset)
    {
        Directory.CreateDirectory(ExtractDir);
        var path = Path.Combine(ExtractDir, $"{preset.Name}.json");
        return File.WriteAllTextAsync(path, JsonSerializer.Serialize(preset, JsonOptions));
    }

    public Task SaveAnnotationPresetAsync(AnnotationPreset preset)
    {
        Directory.CreateDirectory(AnnotateDir);
        var path = Path.Combine(AnnotateDir, $"{preset.Name}.json");
        return File.WriteAllTextAsync(path, JsonSerializer.Serialize(preset, JsonOptions));
    }

    public Task<List<ExtractionPreset>> LoadAllExtractionPresetsAsync()
    {
        var list = LoadPresets<ExtractionPreset>(ExtractDir);
        return Task.FromResult(list);
    }

    public Task<List<AnnotationPreset>> LoadAllAnnotationPresetsAsync()
    {
        var list = LoadPresets<AnnotationPreset>(AnnotateDir);
        return Task.FromResult(list);
    }

    private static List<T> LoadPresets<T>(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return new List<T>();
        }

        var result = new List<T>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            var content = File.ReadAllText(file);
            var preset = JsonSerializer.Deserialize<T>(content);
            if (preset != null)
            {
                result.Add(preset);
            }
        }

        return result;
    }
}
