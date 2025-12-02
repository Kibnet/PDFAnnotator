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

public partial class AnnotationView : UserControl
{
    public AnnotationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private AnnotationViewModel? Vm => DataContext as AnnotationViewModel;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm == null || AnnotImage?.Source == null)
        {
            return;
        }

        var pos = ToImageSpace(e.GetPosition(AnnotImage));
        var imageHeight = GetImagePixelSize().Height;
        Vm.UpdatePosition(pos.X, pos.Y, imageHeight);
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

    private Point ToImageSpace(Point viewPoint)
    {
        var size = GetImagePixelSize();
        var bounds = AnnotImage.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0 || size.Width <= 0 || size.Height <= 0)
        {
            return viewPoint;
        }

        var scaleX = size.Width / bounds.Width;
        var scaleY = size.Height / bounds.Height;
        return new Point(viewPoint.X * scaleX, viewPoint.Y * scaleY);
    }

    private PixelSize GetImagePixelSize()
    {
        if (AnnotImage?.Source is Bitmap bmp)
        {
            return bmp.PixelSize;
        }

        var width = AnnotImage?.Bounds.Width ?? 0;
        var height = AnnotImage?.Bounds.Height ?? 0;
        return new PixelSize((int)width, (int)height);
    }
}
