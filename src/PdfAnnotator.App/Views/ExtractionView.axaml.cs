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
        
        // Get current bitmap dimensions (after rotation)
        var currentWidth = PageImage.Source is Bitmap bmp ? bmp.PixelSize.Width : PageImage.Bounds.Width;
        var currentHeight = PageImage.Source is Bitmap bm ? bm.PixelSize.Height : PageImage.Bounds.Height;
        
        // Get original dimensions (before rotation)
        double originalWidth = currentWidth;
        double originalHeight = currentHeight;
        if (Vm.PageRotation == 90 || Vm.PageRotation == 270)
        {
            // Swap dimensions back to original
            originalWidth = currentHeight;
            originalHeight = currentWidth;
        }
        
        // Convert from PDF coordinate system (bottom-left origin) to bitmap coordinate system (top-left origin)
        var bitmapX0 = pdfX0;
        var bitmapY0 = originalHeight - pdfY1;  // Y1 in PDF corresponds to the top in bitmap
        var bitmapX1 = pdfX1;
        var bitmapY1 = originalHeight - pdfY0;  // Y0 in PDF corresponds to the bottom in bitmap
        
        // Transform coordinates based on rotation
        double rotatedX0, rotatedY0, rotatedX1, rotatedY1;
        
        switch (Vm.PageRotation)
        {
            case 90:
                // 90° clockwise rotation
                rotatedX0 = originalHeight - bitmapY1;
                rotatedY0 = bitmapX0;
                rotatedX1 = originalHeight - bitmapY0;
                rotatedY1 = bitmapX1;
                break;
            case 180:
                // 180° rotation
                rotatedX0 = originalWidth - bitmapX1;
                rotatedY0 = originalHeight - bitmapY1;
                rotatedX1 = originalWidth - bitmapX0;
                rotatedY1 = originalHeight - bitmapY0;
                break;
            case 270:
                // 270° clockwise rotation
                rotatedX0 = bitmapY0;
                rotatedY0 = originalWidth - bitmapX1;
                rotatedX1 = bitmapY1;
                rotatedY1 = originalWidth - bitmapX0;
                break;
            default: // 0°
                rotatedX0 = bitmapX0;
                rotatedY0 = bitmapY0;
                rotatedX1 = bitmapX1;
                rotatedY1 = bitmapY1;
                break;
        }
        
        // Convert bitmap coordinates to view coordinates
        var startPoint = FromBitmapSpace(new Point(Math.Min(rotatedX0, rotatedX1), Math.Min(rotatedY0, rotatedY1)));
        var endPoint = FromBitmapSpace(new Point(Math.Max(rotatedX0, rotatedX1), Math.Max(rotatedY0, rotatedY1)));
        
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