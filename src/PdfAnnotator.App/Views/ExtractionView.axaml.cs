using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using PdfAnnotator.App.ViewModels;
using System;
using System.Collections.Generic;
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

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (Vm != null)
        {
            Vm.PresetApplied -= OnPresetApplied;
        }
        
        base.OnDataContextChanged(e);
        
        if (Vm != null)
        {
            Vm.PresetApplied += OnPresetApplied;
        }
    }

    private ExtractionViewModel? Vm => DataContext as ExtractionViewModel;

    private void OnPresetApplied(object? sender, EventArgs e)
    {
        // When a preset is applied, update the selection rectangle to show the preset area
        UpdateSelectionFromPreset();
    }

    private void UpdateSelectionFromPreset()
    {
        if (Vm == null || PageImage?.Source == null)
        {
            return;
        }

        // Get original PDF coordinates
        var pdfX0 = Vm.X0;
        var pdfY0 = Vm.Y0;
        var pdfX1 = Vm.X1;
        var pdfY1 = Vm.Y1;
        
        // Convert from PDF coordinate system (bottom-left origin) to bitmap coordinate system (top-left origin)
        var scale = Vm.Dpi / 72.0;
        var bitmapX0 = pdfX0 * scale;
        var bitmapY0 = Vm.OriginalPageHeightPt * scale - pdfY1; // Y-axis flip
        var bitmapX1 = pdfX1 * scale;
        var bitmapY1 = Vm.OriginalPageHeightPt * scale - pdfY0;
        
        // Convert bitmap coordinates to view coordinates
        var startPoint = FromBitmapSpace(new Point(Math.Min(bitmapX0, bitmapX1), Math.Min(bitmapY0, bitmapY1)));
        var endPoint = FromBitmapSpace(new Point(Math.Max(bitmapX0, bitmapX1), Math.Max(bitmapY0, bitmapY1)));
        
        // Update the selection rectangle properties directly
        Vm.SelectLeft = startPoint.X;
        Vm.SelectTop = startPoint.Y;
        Vm.SelectWidth = endPoint.X - startPoint.X;
        Vm.SelectHeight = endPoint.Y - startPoint.Y;
    }

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
        
        Vm!.UpdateSelection(startScaled.X, startScaled.Y, scaled.X, scaled.Y);
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
    
    private Point FromBitmapSpace(Point bitmapPoint)
    {
        if (PageImage?.Source is not Bitmap bmp)
        {
            return bitmapPoint;
        }

        var bounds = PageImage.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return bitmapPoint;
        }

        var scaleX = bounds.Width / bmp.PixelSize.Width;
        var scaleY = bounds.Height / bmp.PixelSize.Height;
        return new Point(bitmapPoint.X * scaleX, bitmapPoint.Y * scaleY);
    }
}