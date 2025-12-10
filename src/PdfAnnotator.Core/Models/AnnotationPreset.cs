using System.Text.Json.Serialization;

namespace PdfAnnotator.Core.Models;

public class AnnotationPreset : IPreset
{
    public string Name { get; set; } = string.Empty;
    public double TextX { get; set; }
    public double TextY { get; set; }
    public double FontSize { get; set; }
    public double Angle { get; set; }
    
    [JsonPropertyName("color")]
    public string ColorHex { get; set; } = "#000000";
    
    [JsonPropertyName("fontName")]
    public string FontName { get; set; } = "Helvetica";
}