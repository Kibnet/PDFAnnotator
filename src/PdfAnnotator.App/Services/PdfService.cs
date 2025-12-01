using Avalonia.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Microsoft.Extensions.Logging;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfAnnotator.App.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;
    private readonly Dictionary<(string path, int page, int dpi), Bitmap> _renderCache = new();
    private readonly object _cacheLock = new();

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
    }

    public Task<int> GetPageCountAsync(string path)
    {
        return Task.Run(() =>
        {
            using var document = PdfDocument.Open(path);
            return document.NumberOfPages;
        });
    }

    public Task<Bitmap> RenderPageAsync(string path, int page, int dpi)
    {
        return Task.Run(() =>
        {
            var key = (path, page, dpi);
            lock (_cacheLock)
            {
                if (_renderCache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            using var docReader = DocLib.Instance.GetDocReader(path, new PageDimensions());
            using var pageReader = docReader.GetPageReader(page - 1);
            var rawBytes = pageReader.GetImage(dpi, dpi, new RenderingSettings());
            using var stream = new MemoryStream(rawBytes.Bytes);
            var bitmap = new Bitmap(stream);

            lock (_cacheLock)
            {
                _renderCache[key] = bitmap;
            }

            return bitmap;
        });
    }

    public Task<List<TableRow>> ExtractTextAsync(string pdfPath, ExtractionPreset preset)
    {
        return Task.Run(() =>
        {
            var result = new List<TableRow>();
            using var document = PdfDocument.Open(pdfPath);

            for (var i = 1; i <= document.NumberOfPages; i++)
            {
                var page = document.GetPage(i);
                var words = page.GetWords();
                var filtered = words
                    .Where(w => IsInside(w, preset))
                    .Select(w => w.Text)
                    .ToList();
                var text = string.Join(" ", filtered);
                result.Add(new TableRow
                {
                    Page = i,
                    FieldText = text,
                    Code = string.Empty
                });
            }

            return result;
        });
    }

    public Task GenerateAnnotatedPdfAsync(string pdfPath, string outputPdfPath, List<TableRow> rows, AnnotationPreset preset)
    {
        return Task.Run(() =>
        {
            using var reader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions());
            using var doc = PdfDocument.Open(pdfPath);

            using var output = new PdfSharpCore.Pdf.PdfDocument();
            for (var pageIndex = 0; pageIndex < doc.NumberOfPages; pageIndex++)
            {
                var pageNumber = pageIndex + 1;
                var pageReader = reader.GetPageReader(pageIndex);
                var page = output.AddPage();
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;

                var render = pageReader.GetImage(150, 150, new RenderingSettings());
                using var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
                using var xImage = PdfSharpCore.Drawing.XImage.FromStream(() => new MemoryStream(render.Bytes));
                gfx.DrawImage(xImage, 0, 0, page.Width, page.Height);

                var row = rows.FirstOrDefault(r => r.Page == pageNumber);
                if (row != null && !string.IsNullOrWhiteSpace(row.Code))
                {
                    var color = PdfSharpCore.Drawing.XColor.FromArgb(ParseColor(preset.ColorHex));
                    var font = new PdfSharpCore.Drawing.XFont(preset.FontName, preset.FontSize);
                    gfx.TranslateTransform(preset.TextX, page.Height - preset.TextY);
                    if (Math.Abs(preset.Angle) > 0.01)
                    {
                        gfx.RotateAtTransform(preset.Angle, new PdfSharpCore.Drawing.XPoint(0, 0));
                    }
                    gfx.DrawString(row.Code, font, new PdfSharpCore.Drawing.XSolidBrush(color),
                        new PdfSharpCore.Drawing.XPoint(0, 0));
                }
            }

            output.Save(outputPdfPath);
            _logger.LogInformation("Annotated PDF saved to {Output}", outputPdfPath);
        });
    }

    private static bool IsInside(Word word, ExtractionPreset preset)
    {
        var rect = word.BoundingBox;
        return rect.Left >= preset.X0 && rect.Right <= preset.X1 && rect.Bottom >= preset.Y0 && rect.Top <= preset.Y1;
    }

    private static int ParseColor(string colorHex)
    {
        if (colorHex.StartsWith("#"))
        {
            colorHex = colorHex[1..];
        }

        if (colorHex.Length == 6)
        {
            colorHex = "FF" + colorHex;
        }

        return int.Parse(colorHex, System.Globalization.NumberStyles.HexNumber);
    }
}
