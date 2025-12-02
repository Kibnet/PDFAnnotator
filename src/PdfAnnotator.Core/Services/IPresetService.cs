using System.Collections.Generic;
using System.Threading.Tasks;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public interface IPresetService
{
    Task SaveExtractionPresetAsync(ExtractionPreset preset);
    Task SaveAnnotationPresetAsync(AnnotationPreset preset);
    Task<List<ExtractionPreset>> LoadAllExtractionPresetsAsync();
    Task<List<AnnotationPreset>> LoadAllAnnotationPresetsAsync();
    Task<ExtractionPreset?> LoadExtractionPresetAsync(string path);
}
