using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
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

        var pos = e.GetPosition(AnnotImage);
        Vm.UpdatePosition(pos.X, pos.Y, AnnotImage.Bounds.Height);
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
