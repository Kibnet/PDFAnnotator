namespace PdfAnnotator.Core.Models;

public class ExtractionPreset : IPreset
{
    public string Name { get; set; } = string.Empty;
    public double X0 { get; set; }
    public double Y0 { get; set; }
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public TextDirection Direction { get; set; } = TextDirection.LeftToRightTopToBottom;
    public bool AddSpacesBetweenWords { get; set; } = true;
}
