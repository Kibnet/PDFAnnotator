using System.Collections.Generic;
using System.Threading.Tasks;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public interface IPresetService<T> where T : IPreset
{
    Task SavePresetAsync(T preset);
    Task DeletePresetAsync(string presetName);
    Task RenamePresetAsync(string oldName, string newName);
    Task<List<T>> LoadAllPresetsAsync();
    Task<T?> LoadPresetAsync(string path);
}
