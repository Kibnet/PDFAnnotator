using System.Collections.Generic;

namespace PdfAnnotator.Core.Models;

public class PdfProject
{
    public string Name { get; set; } = string.Empty;
    public string PdfPath { get; set; } = string.Empty;
    public string ExtractPresetName { get; set; } = string.Empty;
    public string AnnotatePresetName { get; set; } = string.Empty;
    public List<TableRow> Rows { get; set; } = new();
}
