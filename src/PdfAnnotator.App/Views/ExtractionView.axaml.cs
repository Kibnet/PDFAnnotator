using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
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
        var imageHeight = PageImage.Bounds.Height;
        Vm!.UpdateSelection(_start.X, _start.Y, pos.X, pos.Y, imageHeight);
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
}
