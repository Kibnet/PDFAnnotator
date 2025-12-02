using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using PdfAnnotator.Core.Models;

namespace PdfAnnotator.Core.Services;

public interface IPdfService
{
    Task<int> GetPageCountAsync(string path);
    Task<Bitmap> RenderPageAsync(string path, int page, int dpi, int rotation = 0);
    Task<List<TableRow>> ExtractTextAsync(string pdfPath, ExtractionPreset preset, int rotation = 0);
    Task GenerateAnnotatedPdfAsync(string pdfPath, string outputPdfPath, List<TableRow> rows, AnnotationPreset preset);
}
