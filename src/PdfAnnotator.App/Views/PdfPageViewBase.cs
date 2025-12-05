using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace PdfAnnotator.App.Views;

/// <summary>
/// Shared helper base for views that display PDF pages and need view/bitmap coordinate conversion.
/// </summary>
public abstract class PdfPageViewBase : UserControl
{
    protected Image? Image { get; private set; }

    protected void AttachImage(Image image)
    {
        Image = image;
    }

    protected Point ToBitmapSpace(Point viewPoint)
    {
        if (Image?.Source is not Bitmap bmp)
        {
            return viewPoint;
        }

        var bounds = Image.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return viewPoint;
        }

        var scaleX = bmp.PixelSize.Width / bounds.Width;
        var scaleY = bmp.PixelSize.Height / bounds.Height;
        return new Point(viewPoint.X * scaleX, viewPoint.Y * scaleY);
    }

    protected Point FromBitmapSpace(Point bitmapPoint)
    {
        if (Image?.Source is not Bitmap bmp)
        {
            return bitmapPoint;
        }

        var bounds = Image.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return bitmapPoint;
        }

        var scaleX = bounds.Width / bmp.PixelSize.Width;
        var scaleY = bounds.Height / bmp.PixelSize.Height;
        return new Point(bitmapPoint.X * scaleX, bitmapPoint.Y * scaleY);
    }

    protected double GetImageHeightPixels()
    {
        if (Image?.Source is Bitmap bmp)
        {
            return bmp.PixelSize.Height;
        }

        return Image?.Bounds.Height ?? 0;
    }
}
