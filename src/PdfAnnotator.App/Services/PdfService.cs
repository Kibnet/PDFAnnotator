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
    private readonly Dictionary<(string path, int page, int dpi, int rotation), Bitmap> _renderCache = new();
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

    public Task<Bitmap> RenderPageAsync(string path, int page, int dpi, int rotation = 0)
    {
        return Task.Run(() =>
        {
            var key = (path, page, dpi, rotation);
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
                
                // Don't swap dimensions - render normally, then rotate
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
                
                // Apply rotation if needed
                Bitmap rotatedBitmap = bitmap;
                if (rotation != 0)
                {
                    rotatedBitmap = RotateBitmap(bitmap, rotation);
                }

                lock (_cacheLock)
                {
                    _renderCache[key] = rotatedBitmap;
                }

                return rotatedBitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка рендера PDF {Path} страница {Page} dpi {Dpi} rotation {Rotation}", path, page, dpi, rotation);
                throw new InvalidOperationException($"Не удалось отрендерить PDF {Path.GetFileName(path)} страница {page} (dpi {dpi}, rotation {rotation})", ex);
            }
        });
    }

    public Task<List<TableRow>> ExtractTextAsync(string pdfPath, ExtractionPreset preset, int rotation = 0)
    {
        return Task.Run(() =>
        {
            var result = new List<TableRow>();
            using var document = PdfDocument.Open(pdfPath);

            for (var i = 1; i <= document.NumberOfPages; i++)
            {
                var page = document.GetPage(i);
                var words = page.GetWords();
                
                // Transform coordinates based on rotation
                var adjustedPreset = AdjustPresetForRotation(preset, page.Width, page.Height, rotation);
                
                var filtered = words
                    .Where(w => IsInside(w, adjustedPreset))
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
    
    private static Bitmap RotateBitmap(WriteableBitmap source, int angle)
    {
        // Normalize angle to 0, 90, 180, or 270
        angle = ((angle % 360) + 360) % 360;
        if (angle == 0)
        {
            return source;
        }
        
        var sourceWidth = source.PixelSize.Width;
        var sourceHeight = source.PixelSize.Height;
        
        int targetWidth, targetHeight;
        if (angle == 90 || angle == 270)
        {
            targetWidth = sourceHeight;
            targetHeight = sourceWidth;
        }
        else // angle == 180
        {
            targetWidth = sourceWidth;
            targetHeight = sourceHeight;
        }
        
        var rotated = new WriteableBitmap(new PixelSize(targetWidth, targetHeight), source.Dpi, 
            PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        
        using (var sourceBuffer = source.Lock())
        using (var targetBuffer = rotated.Lock())
        {
            unsafe
            {
                var srcPtr = (uint*)sourceBuffer.Address;
                var dstPtr = (uint*)targetBuffer.Address;
                
                for (int y = 0; y < sourceHeight; y++)
                {
                    for (int x = 0; x < sourceWidth; x++)
                    {
                        int srcIndex = y * sourceWidth + x;
                        int dstX, dstY;
                        
                        switch (angle)
                        {
                            case 90:
                                dstX = sourceHeight - 1 - y;
                                dstY = x;
                                break;
                            case 180:
                                dstX = sourceWidth - 1 - x;
                                dstY = sourceHeight - 1 - y;
                                break;
                            case 270:
                                dstX = y;
                                dstY = sourceWidth - 1 - x;
                                break;
                            default:
                                dstX = x;
                                dstY = y;
                                break;
                        }
                        
                        int dstIndex = dstY * targetWidth + dstX;
                        dstPtr[dstIndex] = srcPtr[srcIndex];
                    }
                }
            }
        }
        
        return rotated;
    }
    
    private static ExtractionPreset AdjustPresetForRotation(ExtractionPreset preset, double pageWidth, double pageHeight, int rotation)
    {
        rotation = ((rotation % 360) + 360) % 360;
        if (rotation == 0)
        {
            return preset;
        }
        
        double x0 = preset.X0;
        double y0 = preset.Y0;
        double x1 = preset.X1;
        double y1 = preset.Y1;
        
        double newX0, newY0, newX1, newY1;
        
        switch (rotation)
        {
            case 90:
                // Rotate coordinates 90 degrees clockwise
                newX0 = y0;
                newY0 = pageWidth - x1;
                newX1 = y1;
                newY1 = pageWidth - x0;
                break;
            case 180:
                // Rotate coordinates 180 degrees
                newX0 = pageWidth - x1;
                newY0 = pageHeight - y1;
                newX1 = pageWidth - x0;
                newY1 = pageHeight - y0;
                break;
            case 270:
                // Rotate coordinates 270 degrees clockwise (90 counter-clockwise)
                newX0 = pageHeight - y1;
                newY0 = x0;
                newX1 = pageHeight - y0;
                newY1 = x1;
                break;
            default:
                return preset;
        }
        
        return new ExtractionPreset
        {
            Name = preset.Name,
            X0 = Math.Min(newX0, newX1),
            Y0 = Math.Min(newY0, newY1),
            X1 = Math.Max(newX0, newX1),
            Y1 = Math.Max(newY0, newY1)
        };
    }
}
