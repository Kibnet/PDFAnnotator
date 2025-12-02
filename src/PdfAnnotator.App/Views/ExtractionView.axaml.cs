using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using PdfAnnotator.App.ViewModels;
using System.Linq;

namespace PdfAnnotator.App.Views;

public partial class ExtractionView : UserControl
{
    private bool _dragging;
    private Point _start;

    public ExtractionView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        PageImage = this.Get<Image>("PageImage");
    }

    private ExtractionViewModel? Vm => DataContext as ExtractionViewModel;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (PageImage?.Source == null)
        {
            return;
        }

        _dragging = true;
        _start = e.GetPosition(PageImage);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || Vm == null || PageImage?.Source == null)
        {
            return;
        }

        var pos = e.GetPosition(PageImage);
        UpdateSelection(pos);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging || Vm == null || PageImage?.Source == null)
        {
            return;
        }

        var pos = e.GetPosition(PageImage);
        UpdateSelection(pos);
        _dragging = false;
    }

    private void UpdateSelection(Point pos)
    {
        var scaled = ToBitmapSpace(pos);
        var startScaled = ToBitmapSpace(_start);
        var imageHeight = PageImage.Source is Bitmap bmp ? bmp.PixelSize.Height : PageImage.Bounds.Height;
        Vm!.UpdateSelection(startScaled.X, startScaled.Y, scaled.X, scaled.Y, imageHeight);
    }

    private async void OnOpenPdfClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm == null)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        if (top == null)
        {
            return;
        }

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("PDF") { Patterns = new[] { "*.pdf" } },
                FilePickerFileTypes.All
            }
        });

        var path = files?.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Vm.PdfPath = path;
        await Vm.LoadPdfAsync();
    }

    private Point ToBitmapSpace(Point viewPoint)
    {
        if (PageImage?.Source is not Bitmap bmp)
        {
            return viewPoint;
        }

        var bounds = PageImage.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return viewPoint;
        }

        var scaleX = bmp.PixelSize.Width / bounds.Width;
        var scaleY = bmp.PixelSize.Height / bounds.Height;
        return new Point(viewPoint.X * scaleX, viewPoint.Y * scaleY);
    }
}
