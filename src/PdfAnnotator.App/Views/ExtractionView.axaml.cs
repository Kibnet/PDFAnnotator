using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using PdfAnnotator.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfAnnotator.App.Views;

public partial class ExtractionView : PdfPageViewBase
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
        PageImage = this.FindControl<Image>("PageImage");
        if (PageImage != null)
        {
            AttachImage(PageImage);
        }
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
        if (Vm == null || PageImage?.Source is not Bitmap bmp)
        {
            return;
        }

        // Get original PDF coordinates
        var pdfX0 = Vm.X0;
        var pdfY0 = Vm.Y0;
        var pdfX1 = Vm.X1;
        var pdfY1 = Vm.Y1;

        var bitmapWidth = bmp.PixelSize.Width;
        var bitmapHeight = bmp.PixelSize.Height;
        if (bitmapWidth == 0 || bitmapHeight == 0 || Vm.OriginalPageWidthPt == 0 || Vm.OriginalPageHeightPt == 0)
        {
            return;
        }

        // Convert from PDF coordinate system (bottom-left origin) to bitmap coordinate system (top-left origin)
        var scaleX = bitmapWidth / Vm.OriginalPageWidthPt;
        var scaleY = bitmapHeight / Vm.OriginalPageHeightPt;
        var bitmapX0 = pdfX0 * scaleX;
        var bitmapY0 = bitmapHeight - pdfY1 * scaleY; // Y-axis flip
        var bitmapX1 = pdfX1 * scaleX;
        var bitmapY1 = bitmapHeight - pdfY0 * scaleY;
        
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

        Vm!.UpdateSelection(startScaled.X, startScaled.Y, scaled.X, scaled.Y,
            _start.X, _start.Y, pos.X, pos.Y);
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
