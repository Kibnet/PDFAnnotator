using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using PdfAnnotator.App.ViewModels;

namespace PdfAnnotator.App.Views;

public partial class AnnotationView : PdfPageViewBase
{
    public AnnotationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        var image = this.FindControl<Image>("AnnotImage");
        if (image != null)
        {
            AttachImage(image);
        }
    }

    private AnnotationViewModel? Vm => DataContext as AnnotationViewModel;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var image = Image;
        if (Vm == null || image?.Source is not Bitmap bmp)
        {
            return;
        }

        var pos = e.GetPosition(image);
        var scaled = ToBitmapSpace(pos);
        var bitmapWidth = bmp.PixelSize.Width;
        var bitmapHeight = bmp.PixelSize.Height;
        Vm.UpdatePosition(pos.X, pos.Y, scaled.X, scaled.Y, bitmapWidth, bitmapHeight);
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

    private async void OnLoadPresetClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
                new("Preset") { Patterns = new[] { "*.json" } },
                FilePickerFileTypes.All
            }
        });

        var path = files?.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await Vm.LoadPresetFromFileAsync(path);
    }

    private async void OnSaveAnnotatedPdfClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "annotated.pdf",
            DefaultExtension = "pdf",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("PDF") { Patterns = new[] { "*.pdf" } },
                FilePickerFileTypes.All
            }
        });

        var path = file?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await Vm.SaveAnnotatedPdfAsync(path);
    }
}
