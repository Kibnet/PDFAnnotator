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

    public Task DeleteExtractionPresetAsync(string presetName)
    {
        var path = Path.Combine(ExtractDir, $"{presetName}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public async Task RenameExtractionPresetAsync(string oldName, string newName)
    {
        Directory.CreateDirectory(ExtractDir);
        var oldPath = Path.Combine(ExtractDir, $"{oldName}.json");
        var newPath = Path.Combine(ExtractDir, $"{newName}.json");

        if (!File.Exists(oldPath))
        {
            return;
        }

        // Update the preset content to reflect the new name
        try
        {
            var content = await File.ReadAllTextAsync(oldPath);
            var preset = JsonSerializer.Deserialize<ExtractionPreset>(content);
            if (preset != null)
            {
                preset.Name = newName;
                var updatedContent = JsonSerializer.Serialize(preset, JsonOptions);
                await File.WriteAllTextAsync(newPath, updatedContent);
                if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(oldPath);
                }
            }
        }
        catch
        {
            // Swallow exceptions to keep behavior consistent with existing service style
        }
    }

    public Task<List<ExtractionPreset>> LoadAllExtractionPresetsAsync()
    {
        var list = LoadPresets<ExtractionPreset>(ExtractDir);
        return Task.FromResult(list);
    }

    public Task<ExtractionPreset?> LoadExtractionPresetAsync(string path)
    {
        if (!File.Exists(path))
        {
            return Task.FromResult<ExtractionPreset?>(null);
        }

        try
        {
            var content = File.ReadAllText(path);
            var preset = JsonSerializer.Deserialize<ExtractionPreset>(content);
            return Task.FromResult(preset);
        }
        catch
        {
            return Task.FromResult<ExtractionPreset?>(null);
        }
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
