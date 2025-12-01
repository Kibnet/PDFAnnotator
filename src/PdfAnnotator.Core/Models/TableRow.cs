namespace PdfAnnotator.Core.Models;

public class TableRow
{
    public int Page { get; set; }
    public string FieldText { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
