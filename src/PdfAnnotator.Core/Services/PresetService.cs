using System.Text.Json;
using System.Text.Json.Serialization;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public class PresetService<T> : IPresetService<T> where T : IPreset
{
    private readonly string _presetDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PresetService(string presetDirectory)
    {
        _presetDirectory = presetDirectory;
    }

    public Task SavePresetAsync(T preset)
    {
        Directory.CreateDirectory(_presetDirectory);
        var path = Path.Combine(_presetDirectory, $"{preset.Name}.json");
        return File.WriteAllTextAsync(path, JsonSerializer.Serialize(preset, JsonOptions));
    }

    public Task DeletePresetAsync(string presetName)
    {
        var path = Path.Combine(_presetDirectory, $"{presetName}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public async Task RenamePresetAsync(string oldName, string newName)
    {
        Directory.CreateDirectory(_presetDirectory);
        var oldPath = Path.Combine(_presetDirectory, $"{oldName}.json");
        var newPath = Path.Combine(_presetDirectory, $"{newName}.json");

        if (!File.Exists(oldPath))
        {
            return;
        }

        // Update the preset content to reflect the new name
        try
        {
            var content = await File.ReadAllTextAsync(oldPath);
            var preset = JsonSerializer.Deserialize<T>(content);
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

    public Task<List<T>> LoadAllPresetsAsync()
    {
        var list = LoadPresets();
        return Task.FromResult(list);
    }

    public Task<T?> LoadPresetAsync(string path)
    {
        if (!File.Exists(path))
        {
            return Task.FromResult<T?>(default);
        }

        try
        {
            var content = File.ReadAllText(path);
            var preset = JsonSerializer.Deserialize<T>(content, JsonOptions);
            return Task.FromResult(preset);
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    private List<T> LoadPresets()
    {
        if (!Directory.Exists(_presetDirectory))
        {
            return new List<T>();
        }

        var result = new List<T>();
        foreach (var file in Directory.GetFiles(_presetDirectory, "*.json"))
        {
            var content = File.ReadAllText(file);
            var preset = JsonSerializer.Deserialize<T>(content, JsonOptions);
            if (preset != null)
            {
                result.Add(preset);
            }
        }

        return result;
    }
}
