using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Models;
using Microsoft.Extensions.Logging;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;
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

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("PDF не найден", path);
            }

            try
            {
                using var pigDoc = PdfDocument.Open(path);
                var pageCount = pigDoc.NumberOfPages;
                if (page < 1 || page > pageCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(page), $"Страница {page} вне диапазона 1..{pageCount}");
                }

                var pigPage = pigDoc.GetPage(page);
                var widthPt = pigPage.Width;
                var heightPt = pigPage.Height;
                var targetWidth = Math.Clamp((int)Math.Round(widthPt / 72.0 * dpi), 1, 8000);
                var targetHeight = Math.Clamp((int)Math.Round(heightPt / 72.0 * dpi), 1, 8000);

                using var docReader = DocLib.Instance.GetDocReader(path, new PageDimensions(targetWidth, targetHeight));
                using var pageReader = docReader.GetPageReader(page - 1);
                var converter = new NaiveTransparencyRemover(255, 255, 255);
                var rawBytes = pageReader.GetImage(converter);
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();

                var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(dpi, dpi),
                    PixelFormat.Bgra8888, AlphaFormat.Unpremul);
                using (var buffer = bitmap.Lock())
                {
                    Marshal.Copy(rawBytes, 0, buffer.Address, rawBytes.Length);
                }

                lock (_cacheLock)
                {
                    _renderCache[key] = bitmap;
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка рендера PDF {Path} страница {Page} dpi {Dpi}", path, page, dpi);
                throw new InvalidOperationException($"Не удалось отрендерить PDF {Path.GetFileName(path)} страница {page} (dpi {dpi})", ex);
            }
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
            using var document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
            for (var pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                var pageNumber = pageIndex + 1;
                var page = document.Pages[pageIndex];
                using var gfx = XGraphics.FromPdfPage(page);

                var row = rows.FirstOrDefault(r => r.Page == pageNumber);
                if (row != null && !string.IsNullOrWhiteSpace(row.Code))
                {
                    var color = XColor.FromArgb(ParseColor(preset.ColorHex));
                    var font = new XFont(preset.FontName, preset.FontSize);
                    gfx.TranslateTransform(preset.TextX, page.Height - preset.TextY);
                    if (Math.Abs(preset.Angle) > 0.01)
                    {
                        gfx.RotateAtTransform(preset.Angle, new XPoint(0, 0));
                    }
                    gfx.DrawString(row.Code, font, new XSolidBrush(color), new XPoint(0, 0));
                }
            }

            document.Save(outputPdfPath);
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
