namespace PdfAnnotator.Core.Models;

public class AnnotationPreset
{
    public string Name { get; set; } = string.Empty;
    public double TextX { get; set; }
    public double TextY { get; set; }
    public double FontSize { get; set; }
    public double Angle { get; set; }
    public string ColorHex { get; set; } = "#000000";
    public string FontName { get; set; } = "Helvetica";
}
