using System;
using System.IO;
using System.Threading.Tasks;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.App.ViewModels;

/// <summary>
/// Single place for PDF page rendering and dimension retrieval used by all viewmodels.
/// </summary>
public static class PageRenderService
{
    public const int RenderDpi = 100;

    public static async Task<PageSnapshot?> RenderPageAsync(
        IPdfService pdfService,
        string pdfPath,
        int pageNumber,
        int? knownPageCount = null)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            return null;
        }

        var pageCount = knownPageCount ?? await pdfService.GetPageCountAsync(pdfPath);
        var clampedPage = Math.Clamp(pageNumber, 1, pageCount);

        var dims = await pdfService.GetPageDimensionsAsync(pdfPath, clampedPage);
        var bitmap = await pdfService.RenderPageAsync(pdfPath, clampedPage, RenderDpi);

        return new PageSnapshot(
            pdfPath,
            pageCount,
            clampedPage,
            dims.width,
            dims.height,
            bitmap);
    }
}
