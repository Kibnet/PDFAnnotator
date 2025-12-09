using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;
using PropertyChanged;

namespace PdfAnnotator.App.ViewModels;

[AddINotifyPropertyChangedInterface]
public class AnnotationViewModel
{
    private readonly IPdfService _pdfService;
    private readonly IPresetService<AnnotationPreset> _presetService;
    private readonly ILogger<AnnotationViewModel> _logger;
    private const string DefaultInsertText = "Тестовый текст";
    private const double PositionEpsilon = 0.01;

    public string PdfPath { get; set; } = string.Empty;
    public int PageCount { get; set; }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            _currentPage = value;
            _ = LoadPageAsync();
            RefreshPreview();
        }
    }

    public Bitmap? PageBitmap { get; set; }

    public ObservableCollection<AnnotationPreset> Presets { get; } = new();

    private AnnotationPreset? _selectedPreset;
    public AnnotationPreset? SelectedPreset
    {
        get => _selectedPreset;
        set { _selectedPreset = value; SelectedPresetName = _selectedPreset?.Name; PresetName = _selectedPreset?.Name ?? string.Empty; ApplyPreset(); }
    }

    public string? SelectedPresetName { get; set; }
    public string PresetName { get; set; } = string.Empty;

    private double _textX;
    public double TextX 
    { 
        get => _textX;
        set
        {
            _textX = value;
            if (!_suppressPreviewPositionUpdate)
            {
                UpdatePreviewPosition();
            }
            RefreshPreview();
            _ = RenderCurrentPageAsync();
        }
    }
    
    private double _textY;
    public double TextY 
    { 
        get => _textY;
        set
        {
            _textY = value;
            if (!_suppressPreviewPositionUpdate)
            {
                UpdatePreviewPosition();
            }
            RefreshPreview();
            _ = RenderCurrentPageAsync();
        }
    }
    
    public double PreviewX { get; set; }
    public double PreviewY { get; set; }
    private double _fontSize = 12;
    public double FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value;
            RefreshPreview();
            _ = RenderCurrentPageAsync();
        }
    }
    private double _angle;
    public double Angle
    {
        get => _angle;
        set
        {
            _angle = value;
            RefreshPreview();
            _ = RenderCurrentPageAsync();
        }
    }
    
    public bool OpenFileAfterSaving
    {
        get => _openFileAfterSaving;
        set => _openFileAfterSaving = value;
    }
    
    private string _colorHex = "#000000";
    private Color _selectedColor = Colors.Black;
    private bool _isSyncingColor;
    private string _insertText = DefaultInsertText;
    private bool _isSyncingInsertText;
    private bool _isRendering;
    private bool _renderRequested;
    private bool _suppressPreviewPositionUpdate;
    private bool _openFileAfterSaving = true;

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            if (_colorHex == value)
            {
                return;
            }

            _colorHex = value;

            if (!_isSyncingColor && TryParseColor(value, out var parsed))
            {
                _isSyncingColor = true;
                SelectedColor = parsed;
                _isSyncingColor = false;
            }

            _ = RenderCurrentPageAsync();
        }
    }

    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (_selectedColor == value)
            {
                return;
            }

            _selectedColor = value;

            if (!_isSyncingColor)
            {
                _isSyncingColor = true;
                ColorHex = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
                _isSyncingColor = false;
            }
        }
    }

    public string InsertText
    {
        get => _insertText;
        set
        {
            var normalized = value ?? string.Empty;
            if (_insertText == normalized)
            {
                return;
            }

            _insertText = normalized;

            if (!_isSyncingInsertText)
            {
                SyncInsertTextToRow();
            }

            SelectedCodePreview = _insertText;
        }
    }

    private string _fontName = "Arial";
    public string FontName
    {
        get => _fontName;
        set
        {
            _fontName = value;
            RefreshPreview();
            _ = RenderCurrentPageAsync();
        }
    }
    public double OriginalPageWidthPt { get; set; }
    public double OriginalPageHeightPt { get; set; }

    public ObservableCollection<string> Fonts { get; } = new(new[] { "Arial", "Helvetica", "Times New Roman" });

    public ObservableCollection<TableRow> Rows { get; } = new();
    public string SelectedCodePreview { get; set; } = DefaultInsertText;

    public void ApplyPageSnapshot(PageSnapshot snapshot)
    {
        PdfPath = snapshot.PdfPath;
        PageCount = snapshot.PageCount;
        _currentPage = snapshot.CurrentPage;
        OriginalPageWidthPt = snapshot.WidthPt;
        OriginalPageHeightPt = snapshot.HeightPt;
        PageBitmap = snapshot.Bitmap;
        UpdatePreviewPosition();
        RefreshPreview();
    }

    public ICommand LoadPdfCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand LoadPresetCommand { get; }
    public ICommand ReloadPresetsCommand { get; }
    public ICommand DeletePresetCommand { get; }
    public ICommand RenamePresetCommand { get; }
    public ICommand RenderPageCommand { get; }
    // SaveAnnotatedPdfCommand removed since we're handling save in the view

    public AnnotationViewModel(IPdfService pdfService, IPresetService<AnnotationPreset> presetService, ILogger<AnnotationViewModel> logger)
    {
        _pdfService = pdfService;
        _presetService = presetService;
        _logger = logger;

        LoadPdfCommand = new RelayCommand(async _ => await LoadPdfAsync());
        SavePresetCommand = new RelayCommand(async _ => await SavePresetAsync());
        LoadPresetCommand = new RelayCommand(async _ => await ApplyPresetCommand());
        ReloadPresetsCommand = new RelayCommand(async _ => await LoadPresetsAsync());
        DeletePresetCommand = new RelayCommand(async _ => await DeletePresetAsync());
        RenamePresetCommand = new RelayCommand(async _ => await RenamePresetAsync());
        RenderPageCommand = new RelayCommand(async _ => await RenderCurrentPageAsync());
        // SaveAnnotatedPdfCommand initialization removed
    }

    public void SetRows(IEnumerable<TableRow> rows)
    {
        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(row);
        }
        RefreshPreview();
    }

    public bool UpdatePosition(double viewX, double viewY, double bitmapX, double bitmapY, double bitmapWidth, double bitmapHeight)
    {
        // preview in view coordinates for correct overlay positioning
        PreviewX = viewX;
        PreviewY = viewY;

        double newTextX;
        double newTextY;

        if (OriginalPageWidthPt > 0 && OriginalPageHeightPt > 0 && bitmapWidth > 0 && bitmapHeight > 0)
        {
            var scaleX = OriginalPageWidthPt / bitmapWidth;
            var scaleY = OriginalPageHeightPt / bitmapHeight;
            newTextX = bitmapX * scaleX;
            newTextY = OriginalPageHeightPt - bitmapY * scaleY;
        }
        else
        {
            // fallback to bitmap space if dimensions are unknown
            newTextX = bitmapX;
            newTextY = bitmapHeight - bitmapY;
        }

        var hasPositionChanged = Math.Abs(_textX - newTextX) > PositionEpsilon
                                 || Math.Abs(_textY - newTextY) > PositionEpsilon;

        _suppressPreviewPositionUpdate = true;
        TextX = newTextX;
        TextY = newTextY;
        _suppressPreviewPositionUpdate = false;
        RefreshPreview();

        return hasPositionChanged;
    }

    public async Task LoadPdfAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            _logger.LogWarning("PDF path missing for annotation");
            return;
        }

        var snapshot = await PageRenderService.RenderPageAsync(_pdfService, PdfPath, CurrentPage);
        if (snapshot == null)
        {
            _logger.LogWarning("Failed to render annotation PDF");
            return;
        }

        ApplyPageSnapshot(snapshot);
    }

    private async Task LoadPageAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            return;
        }

        var snapshot = await PageRenderService.RenderPageAsync(_pdfService, PdfPath, CurrentPage, PageCount);
        if (snapshot == null)
        {
            return;
        }

        ApplyPageSnapshot(snapshot);
    }

    private async Task SavePresetAsync()
    {
        var chosenName = string.IsNullOrWhiteSpace(PresetName)
            ? SelectedPreset?.Name
            : PresetName.Trim();
        
        var preset = new AnnotationPreset
        {
            Name = string.IsNullOrWhiteSpace(chosenName) ? $"Annot_{DateTime.Now:HHmmss}" : chosenName,
            TextX = TextX,
            TextY = TextY,
            FontSize = FontSize,
            Angle = Angle,
            ColorHex = ColorHex,
            FontName = FontName
        };

        await _presetService.SavePresetAsync(preset);
        await LoadPresetsAsync();
        SelectedPreset = Presets.FirstOrDefault(p => p.Name == preset.Name);
    }

    public async Task LoadPresetsAsync()
    {
        Presets.Clear();
        var presets = await _presetService.LoadAllPresetsAsync();
        foreach (var preset in presets)
        {
            Presets.Add(preset);
        }

        if (!string.IsNullOrWhiteSpace(SelectedPresetName))
        {
            SelectedPreset = Presets.FirstOrDefault(p => p.Name == SelectedPresetName);
        }
    }

    private void ApplyPreset()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        TextX = SelectedPreset.TextX;
        TextY = SelectedPreset.TextY;
        FontSize = SelectedPreset.FontSize;
        Angle = SelectedPreset.Angle;
        ColorHex = SelectedPreset.ColorHex;
        FontName = SelectedPreset.FontName;
        
        // Update preview position based on the new text coordinates
        UpdatePreviewPosition();
        
        RefreshPreview();
        _ = RenderCurrentPageAsync();
    }

    // New method that will be called from the view after getting the save path from user
    public async Task SaveAnnotatedPdfAsync(string outputPath)
    {
        var preset = new AnnotationPreset
        {
            Name = SelectedPreset?.Name ?? "Current",
            TextX = TextX,
            TextY = TextY,
            FontSize = FontSize,
            Angle = Angle,
            ColorHex = ColorHex,
            FontName = FontName
        };

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await _pdfService.GenerateAnnotatedPdfAsync(PdfPath, outputPath, Rows.ToList(), preset);
        _logger.LogInformation("Annotated PDF saved at {Path}", outputPath);
        
        // Open file after saving if option is enabled
        if (OpenFileAfterSaving && File.Exists(outputPath))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(outputPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open PDF file at {Path}", outputPath);
            }
        }
    }

    private void RefreshPreview()
    {
        var row = Rows.FirstOrDefault(r => r.Page == CurrentPage);
        var text = !string.IsNullOrWhiteSpace(row?.Code) ? row!.Code : DefaultInsertText;

        _isSyncingInsertText = true;
        InsertText = text;
        _isSyncingInsertText = false;

        SelectedCodePreview = InsertText;
    }
    
    private async Task DeletePresetAsync()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        var nameToDelete = SelectedPreset.Name;
        await _presetService.DeletePresetAsync(nameToDelete);
        var existing = Presets.FirstOrDefault(p => p.Name == nameToDelete);
        if (existing != null)
        {
            Presets.Remove(existing);
        }

        SelectedPreset = null;
        PresetName = string.Empty;
    }

    private async Task RenamePresetAsync()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        var newName = PresetName?.Trim();
        if (string.IsNullOrWhiteSpace(newName) || newName == SelectedPreset.Name)
        {
            return;
        }

        await _presetService.RenamePresetAsync(SelectedPreset.Name, newName);
        SelectedPresetName = newName;
        await LoadPresetsAsync();
        SelectedPreset = Presets.FirstOrDefault(p => p.Name == newName);
    }

    private async Task ApplyPresetCommand()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        TextX = SelectedPreset.TextX;
        TextY = SelectedPreset.TextY;
        FontSize = SelectedPreset.FontSize;
        Angle = SelectedPreset.Angle;
        ColorHex = SelectedPreset.ColorHex;
        FontName = SelectedPreset.FontName;
        
        // Update preview position based on the new text coordinates
        UpdatePreviewPosition();
        
        RefreshPreview();
        _ = RenderCurrentPageAsync();
    }
    
    /// <summary>
    /// Updates the preview position (PreviewX/PreviewY) based on the current TextX/TextY coordinates
    /// and the page dimensions. This converts from PDF coordinate system to view coordinate system.
    /// </summary>
    private void UpdatePreviewPosition()
    {
        if (PageBitmap == null || OriginalPageWidthPt <= 0 || OriginalPageHeightPt <= 0)
        {
            return;
        }
        
        var bitmapWidth = PageBitmap.PixelSize.Width;
        var bitmapHeight = PageBitmap.PixelSize.Height;
        
        if (bitmapWidth <= 0 || bitmapHeight <= 0)
        {
            return;
        }
        
        // Convert from PDF coordinate system (bottom-left origin) to bitmap coordinate system (top-left origin)
        var scaleX = bitmapWidth / OriginalPageWidthPt;
        var scaleY = bitmapHeight / OriginalPageHeightPt;
        var bitmapX = TextX * scaleX;
        var bitmapY = bitmapHeight - TextY * scaleY; // Y-axis flip
        
        // For preview, we need view coordinates, not bitmap coordinates
        // We'll use a simple approximation here - in a real implementation, 
        // this would need to account for the Viewbox scaling
        PreviewX = bitmapX;
        PreviewY = bitmapY;
    }

    public async Task LoadPresetFromFileAsync(string path)
    {
        var preset = await _presetService.LoadPresetAsync(path);
        if (preset == null)
        {
            _logger.LogWarning("Failed to load annotation preset from {Path}", path);
            return;
        }

        var existing = Presets.FirstOrDefault(p => p.Name == preset.Name);
        if (existing != null)
        {
            Presets.Remove(existing);
        }

        Presets.Add(preset);
        SelectedPreset = preset;
    }

    private void SyncInsertTextToRow()
    {
        var row = Rows.FirstOrDefault(r => r.Page == CurrentPage);
        if (row != null)
        {
            row.Code = _insertText;
        }
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            color = default;
            return false;
        }

        if (Color.TryParse(value, out color))
        {
            return true;
        }

        var normalized = value.StartsWith("#", StringComparison.Ordinal) ? value : $"#{value}";
        return Color.TryParse(normalized, out color);
    }

    public async Task RenderCurrentPageAsync()
    {
        _renderRequested = true;
        if (_isRendering)
        {
            return;
        }

        while (_renderRequested)
        {
            _renderRequested = false;
            _isRendering = true;
            try
            {
                await RenderAnnotatedPageAsync();
            }
            finally
            {
                _isRendering = false;
            }
        }
    }

    /// <summary>
    /// Renders the current page with annotations applied and updates the preview
    /// </summary>
    private async Task RenderAnnotatedPageAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            return;
        }

        try
        {
            // Get the current preset
            var preset = new AnnotationPreset
            {
                Name = SelectedPreset?.Name ?? "Current",
                TextX = TextX,
                TextY = TextY,
                FontSize = FontSize,
                Angle = Angle,
                ColorHex = ColorHex,
                FontName = FontName
            };

            // Get the current row for this page
            var currentRow = Rows.FirstOrDefault(r => r.Page == CurrentPage);

            // Render the annotated page directly
            var annotatedBitmap = await _pdfService.RenderAnnotatedPageAsync(PdfPath, CurrentPage, currentRow, preset, PageRenderService.RenderDpi);
            
            if (annotatedBitmap != null)
            {
                // Update the bitmap with the annotated version
                PageBitmap = annotatedBitmap;
                RefreshPreview();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render annotated page");
        }
    }
}
