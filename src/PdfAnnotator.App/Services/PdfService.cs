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
    
    // Cache for opened PDF document
    private string? _cachedPdfPath;
    private PdfDocument? _cachedDocument;
    private readonly object _documentCacheLock = new();

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
    }

    public Task<int> GetPageCountAsync(string path)
    {
        return Task.Run(() =>
        {
            var document = GetOrOpenDocument(path);
            return document.NumberOfPages;
        });
    }
    
    public Task<(double width, double height)> GetPageDimensionsAsync(string path, int page)
    {
        return Task.Run(() =>
        {
            var document = GetOrOpenDocument(path);
            if (page < 1 || page > document.NumberOfPages)
            {
                throw new ArgumentOutOfRangeException(nameof(page));
            }
            var pdfPage = document.GetPage(page);
            return (pdfPage.Width, pdfPage.Height);
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
                
                using var docReader = DocLib.Instance.GetDocReader(path, new PageDimensions(2));
                using var pageReader = docReader.GetPageReader(page - 1);
                var converter = new NaiveTransparencyRemover(255, 255, 255);
                var rawBytes = pageReader.GetImage(converter);
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();

                var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96,96),
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
            var document = GetOrOpenDocument(pdfPath);

            for (var i = 1; i <= document.NumberOfPages; i++)
            {
                var text = ExtractTextFromPage(document, i, preset);
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

    public Task<string> ExtractTextFromPageAsync(string pdfPath, int pageNumber, ExtractionPreset preset)
    {
        return Task.Run(() =>
        {
            var document = GetOrOpenDocument(pdfPath);
            return ExtractTextFromPage(document, pageNumber, preset);
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

    public Task<Bitmap> RenderAnnotatedPageAsync(string pdfPath, int page, TableRow? row, AnnotationPreset preset, int dpi = 100)
    {
        return Task.Run(() =>
        {
            // Create a temporary file for the annotated page
            var tempPath = Path.GetTempFileName();
            try
            {
                using var document = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Modify);
                
                // Check if the page number is valid
                if (page < 1 || page > document.PageCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(page), $"Page {page} is out of range. Document has {document.PageCount} pages.");
                }
                
                // Work with just the specified page
                var pageIndex = page - 1; // Convert to 0-based index
                var pdfPage = document.Pages[pageIndex];
                using var gfx = XGraphics.FromPdfPage(pdfPage);

                // Apply annotation if we have a row with text
                if (row != null && !string.IsNullOrWhiteSpace(row.Code))
                {
                    var color = XColor.FromArgb(ParseColor(preset.ColorHex));
                    var font = new XFont(preset.FontName, preset.FontSize);
                    gfx.TranslateTransform(preset.TextX, pdfPage.Height - preset.TextY);
                    if (Math.Abs(preset.Angle) > 0.01)
                    {
                        gfx.RotateAtTransform(preset.Angle, new XPoint(0, 0));
                    }
                    gfx.DrawString(row.Code, font, new XSolidBrush(color), new XPoint(0, 0));
                }

                // Save the modified document to temporary file
                document.Save(tempPath);

                // Render the annotated page
                return RenderPageAsync(tempPath, 1, dpi).Result;
            }
            finally
            {
                // Clean up temporary file
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        });
    }

    private static bool IsInside(Word word, ExtractionPreset preset)
    {
        var rect = word.BoundingBox;
        return rect.Left >= preset.X0 && rect.Right <= preset.X1 && rect.Bottom >= preset.Y0 && rect.Top <= preset.Y1;
    }

    /// <summary>
    /// Gets or opens a PDF document from cache. Document is cached in memory until a different path is requested.
    /// </summary>
    private PdfDocument GetOrOpenDocument(string path)
    {
        lock (_documentCacheLock)
        {
            // If we have a cached document for a different path, dispose it
            if (_cachedDocument != null && _cachedPdfPath != path)
            {
                _cachedDocument.Dispose();
                _cachedDocument = null;
                _cachedPdfPath = null;
            }

            // Open new document if needed
            if (_cachedDocument == null)
            {
                _cachedDocument = PdfDocument.Open(path);
                _cachedPdfPath = path;
            }

            return _cachedDocument;
        }
    }

    /// <summary>
    /// Extracts text from a single page of an already-opened PDF document.
    /// </summary>
    private static string ExtractTextFromPage(PdfDocument document, int pageNumber, ExtractionPreset preset)
    {
        var page = document.GetPage(pageNumber);
        var words = page.GetWords();
        
        var filtered = OrderWords(words, preset.Direction)
            .Where(w => IsInside(w, preset))
            .Select(w => w.Text)
            .ToList();

        var separator = preset.AddSpacesBetweenWords ? " " : string.Empty;
        return string.Join(separator, filtered);
    }

    private static IEnumerable<Word> OrderWords(IEnumerable<Word> words, TextDirection direction)
    {
        // Group words into lines first based on vertical position
        // Use a tolerance for vertical alignment (words within 5 points are considered on same line)
        const double lineTolerance = 5.0;
        
        var wordList = words.ToList();
        if (!wordList.Any()) return wordList;
        
        // Group words by approximate line (based on vertical position)
        var lines = new List<List<Word>>();
        var sortedWords = direction switch
        {
            TextDirection.LeftToRightTopToBottom or TextDirection.RightToLeftTopToBottom =>
                // For top-to-bottom: start from highest Y (top of page)
                wordList.OrderByDescending(w => w.BoundingBox.Top).ToList(),
            TextDirection.LeftToRightBottomToTop or TextDirection.RightToLeftBottomToTop =>
                // For bottom-to-top: start from lowest Y (bottom of page)
                wordList.OrderBy(w => w.BoundingBox.Bottom).ToList(),
            _ => wordList
        };
        
        foreach (var word in sortedWords)
        {
            // Find if word belongs to an existing line
            var line = lines.FirstOrDefault(l => 
            {
                var lineY = l.First().BoundingBox.Bottom;
                var wordY = word.BoundingBox.Bottom;
                return Math.Abs(lineY - wordY) <= lineTolerance;
            });
            
            if (line != null)
            {
                line.Add(word);
            }
            else
            {
                lines.Add(new List<Word> { word });
            }
        }
        
        // Order words within each line based on horizontal direction
        var orderedLines = lines.Select(line =>
        {
            return direction switch
            {
                TextDirection.LeftToRightTopToBottom or TextDirection.LeftToRightBottomToTop =>
                    line.OrderBy(w => w.BoundingBox.Left),
                TextDirection.RightToLeftTopToBottom or TextDirection.RightToLeftBottomToTop =>
                    line.OrderByDescending(w => w.BoundingBox.Right),
                _ => line.AsEnumerable()
            };
        });
        
        // Flatten back to single sequence
        return orderedLines.SelectMany(line => line);
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
