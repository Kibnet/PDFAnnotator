using Avalonia.Media.Imaging;

namespace PdfAnnotator.App.ViewModels;

public record PageSnapshot(
    string PdfPath,
    int PageCount,
    int CurrentPage,
    double WidthPt,
    double HeightPt,
    Bitmap? Bitmap);
