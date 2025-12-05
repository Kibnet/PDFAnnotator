using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using PropertyChanged;
using Microsoft.Extensions.Logging;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.App.ViewModels;

[AddINotifyPropertyChangedInterface]
public class ExtractionViewModel
{
    private readonly IPdfService _pdfService;
    private readonly IPresetService<ExtractionPreset> _presetService;
    private readonly ILogger<ExtractionViewModel> _logger;
    private DirectionOption _direction;
    private bool _addSpacesBetweenWords = true;

    public event EventHandler<List<TableRow>>? TableUpdated;
    public event EventHandler? PresetApplied;
    public event EventHandler<PageSnapshot>? PageChanged;

    public string PdfPath { get; set; } = string.Empty;

    public int PageCount { get; set; }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                _ = LoadPageAsync();
            }
        }
    }

    public Bitmap? PageBitmap { get; set; }
    
    // Store original PDF page dimensions in points (72 DPI) before rotation
    public double OriginalPageWidthPt { get; set; }
    public double OriginalPageHeightPt { get; set; }

    public double X0 { get; set; }
    public double Y0 { get; set; }
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double SelectLeft { get; set; }
    public double SelectTop { get; set; }
    public double SelectWidth { get; set; }
    public double SelectHeight { get; set; }
    
    // Property for extracted text preview
    public string ExtractedTextPreview { get; set; } = string.Empty;

    public ObservableCollection<ExtractionPreset> Presets { get; } = new();
    public IReadOnlyList<DirectionOption> DirectionOptions { get; } = new List<DirectionOption>
    {
        new(TextDirection.LeftToRightTopToBottom, "Слева направо, сверху вниз"),
        new(TextDirection.RightToLeftTopToBottom, "Справа налево, сверху вниз"),
        new(TextDirection.LeftToRightBottomToTop, "Слева направо, снизу вверх"),
        new(TextDirection.RightToLeftBottomToTop, "Справа налево, снизу вверх")
    };

    public DirectionOption Direction
    {
        get => _direction;
        set
        {
            if (_direction != value && value != null)
            {
                _direction = value;
                _ = ExtractTextPreviewAsync();
            }
        }
    }

    public bool AddSpacesBetweenWords
    {
        get => _addSpacesBetweenWords;
        set
        {
            if (_addSpacesBetweenWords != value)
            {
                _addSpacesBetweenWords = value;
                _ = ExtractTextPreviewAsync();
            }
        }
    }

    public ExtractionPreset? SelectedPreset 
    { 
        get => _selectedPreset;
        set
        {
            _selectedPreset = value;
            SelectedPresetName = _selectedPreset?.Name;
            PresetName = _selectedPreset?.Name ?? string.Empty;
            if (_selectedPreset != null)
            {
                // Apply the preset values directly
                X0 = _selectedPreset.X0;
                Y0 = _selectedPreset.Y0;
                X1 = _selectedPreset.X1;
                Y1 = _selectedPreset.Y1;
                Direction = DirectionOptions.First(option => option.Value == _selectedPreset.Direction);
                AddSpacesBetweenWords = _selectedPreset.AddSpacesBetweenWords;
                
                // Notify that the preset has been applied so the view can update the selection rectangle
                PresetApplied?.Invoke(this, EventArgs.Empty);
                
                // Automatically extract text for preview when a preset is selected
                _ = ExtractTextPreviewAsync();
            }
        }
    }
    
    private ExtractionPreset? _selectedPreset;
    
    public string? SelectedPresetName { get; set; }
    public string PresetName { get; set; } = string.Empty;

    public ICommand LoadPdfCommand { get; }
    public ICommand ExtractTextCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand LoadPresetCommand { get; }
    public ICommand ReloadPresetsCommand { get; }
    public ICommand DeletePresetCommand { get; }
    public ICommand RenamePresetCommand { get; }

    public ExtractionViewModel(IPdfService pdfService, IPresetService<ExtractionPreset> presetService, ILogger<ExtractionViewModel> logger)
    {
        _pdfService = pdfService;
        _presetService = presetService;
        _logger = logger;

        LoadPdfCommand = new RelayCommand(async _ => await LoadPdfAsync());
        ExtractTextCommand = new RelayCommand(async _ => await ExtractAsync());
        SavePresetCommand = new RelayCommand(async _ => await SavePresetAsync());
        LoadPresetCommand = new RelayCommand(async _ => await ApplyPreset());
        ReloadPresetsCommand = new RelayCommand(async _ => await LoadPresetsAsync());
        DeletePresetCommand = new RelayCommand(async _ => await DeletePresetAsync());
        RenamePresetCommand = new RelayCommand(async _ => await RenamePresetAsync());
        _direction = DirectionOptions.First();
    }

    public async Task LoadPdfAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            _logger.LogWarning("PDF path is empty or missing");
            return;
        }

        var snapshot = await PageRenderService.RenderPageAsync(_pdfService, PdfPath, CurrentPage);
        if (snapshot == null)
        {
            _logger.LogWarning("Failed to render PDF page");
            return;
        }

        ApplySnapshot(snapshot);
        await PostRenderActionsAsync();
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

        ApplySnapshot(snapshot);
        await PostRenderActionsAsync();
    }

    public void UpdateSelection(double startXBitmap, double startYBitmap, double endXBitmap, double endYBitmap,
        double startXView, double startYView, double endXView, double endYView)
    {
        // Store the view coordinates for rectangle display
        SelectLeft = Math.Min(startXView, endXView);
        SelectTop = Math.Min(startYView, endYView);
        SelectWidth = Math.Abs(endXView - startXView);
        SelectHeight = Math.Abs(endYView - startYView);
        
        // Get the bounds in rotated bitmap space
        var minX = Math.Min(startXBitmap, endXBitmap);
        var maxX = Math.Max(startXBitmap, endXBitmap);
        var minY = Math.Min(startYBitmap, endYBitmap);
        var maxY = Math.Max(startYBitmap, endYBitmap);
        
        // Get current bitmap dimensions
        var bitmapWidth = PageBitmap?.PixelSize.Width ?? 0;
        var bitmapHeight = PageBitmap?.PixelSize.Height ?? 0;
        if (bitmapWidth == 0 || bitmapHeight == 0 || OriginalPageWidthPt == 0 || OriginalPageHeightPt == 0)
        {
            return;
        }

        // Convert bitmap pixels to PDF points using actual page dimensions
        var scaleX = OriginalPageWidthPt / bitmapWidth;
        var scaleY = OriginalPageHeightPt / bitmapHeight;
        var pdfMinX = minX * scaleX;
        var pdfMaxX = maxX * scaleX;
        var pdfMinY = minY * scaleY;
        var pdfMaxY = maxY * scaleY;

        // Convert to PDF coordinate system: bottom-left origin
        // Use the stored original PDF page height (in points)
        X0 = pdfMinX;
        X1 = pdfMaxX;
        Y0 = OriginalPageHeightPt - pdfMaxY;
        Y1 = OriginalPageHeightPt - pdfMinY;
        
        // Automatically extract text for preview when selection changes
        _ = ExtractTextPreviewAsync();
    }

    // Method to extract text for preview purposes
    private async Task ExtractTextPreviewAsync()
    {
        // Only extract if we have a valid PDF path and selection
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath) || 
            (X0 == 0 && Y0 == 0 && X1 == 0 && Y1 == 0))
        {
            ExtractedTextPreview = string.Empty;
            return;
        }

        try
        {
            var preset = new ExtractionPreset
            {
                Name = SelectedPreset?.Name ?? "Current",
                X0 = X0,
                Y0 = Y0,
                X1 = X1,
                Y1 = Y1,
                Direction = Direction.Value,
                AddSpacesBetweenWords = AddSpacesBetweenWords
            };

            // Extract text only from the current page for preview (optimized)
            var previewText = await _pdfService.ExtractTextFromPageAsync(PdfPath, CurrentPage, preset);
            ExtractedTextPreview = string.IsNullOrEmpty(previewText) ? "Нет текста для извлечения" : previewText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text for preview");
            ExtractedTextPreview = $"Ошибка извлечения: {ex.Message}";
        }
    }

    private async Task ExtractAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            _logger.LogWarning("PDF path is empty or missing");
            return;
        }

        var preset = new ExtractionPreset
        {
            Name = SelectedPreset?.Name ?? "Current",
            X0 = X0,
            Y0 = Y0,
            X1 = X1,
            Y1 = Y1,
            Direction = Direction.Value,
            AddSpacesBetweenWords = AddSpacesBetweenWords
        };

        try
        {
            var rows = await _pdfService.ExtractTextAsync(PdfPath, preset);
            TableUpdated?.Invoke(this, rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text for table");
        }
    }

    private async Task SavePresetAsync()
    {
        var chosenName = string.IsNullOrWhiteSpace(PresetName)
            ? SelectedPreset?.Name
            : PresetName.Trim();
        
        var preset = new ExtractionPreset
        {
            Name = string.IsNullOrWhiteSpace(chosenName) ? $"Preset_{DateTime.Now:HHmmss}" : chosenName,
            X0 = X0,
            Y0 = Y0,
            X1 = X1,
            Y1 = Y1,
            Direction = Direction.Value,
            AddSpacesBetweenWords = AddSpacesBetweenWords
        };
        await _presetService.SavePresetAsync(preset);
        await LoadPresetsAsync();
        SelectedPreset = Presets.FirstOrDefault(p => p.Name == preset.Name);
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

    private async Task ApplyPreset()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        X0 = SelectedPreset.X0;
        Y0 = SelectedPreset.Y0;
        X1 = SelectedPreset.X1;
        Y1 = SelectedPreset.Y1;
        Direction = DirectionOptions.First(option => option.Value == SelectedPreset.Direction);
        AddSpacesBetweenWords = SelectedPreset.AddSpacesBetweenWords;
        
        // Notify that the preset has been applied so the view can update the selection rectangle
        PresetApplied?.Invoke(this, EventArgs.Empty);
        
        // Automatically extract text for preview when a preset is applied
        _ = ExtractTextPreviewAsync();
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

    public async Task LoadPresetFromFileAsync(string path)
    {
        var preset = await _presetService.LoadPresetAsync(path);
        if (preset == null)
        {
            _logger.LogWarning("Failed to load extraction preset from {Path}", path);
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

    private void ApplySnapshot(PageSnapshot snapshot)
    {
        PdfPath = snapshot.PdfPath;
        PageCount = snapshot.PageCount;
        _currentPage = snapshot.CurrentPage;
        OriginalPageWidthPt = snapshot.WidthPt;
        OriginalPageHeightPt = snapshot.HeightPt;
        PageBitmap = snapshot.Bitmap;
        NotifyPageChanged();
    }

    private async Task PostRenderActionsAsync()
    {
        // If a preset is already selected, notify that we need to update the selection rectangle
        // This ensures the rectangle is displayed when switching pages or loading a new PDF
        if (SelectedPreset != null)
        {
            PresetApplied?.Invoke(this, EventArgs.Empty);
            // Also extract text for preview when page changes
            _ = ExtractTextPreviewAsync();
        }
        else
        {
            await Task.CompletedTask;
        }
    }

    private void NotifyPageChanged()
    {
        var snapshot = new PageSnapshot(
            PdfPath,
            PageCount,
            CurrentPage,
            OriginalPageWidthPt,
            OriginalPageHeightPt,
            PageBitmap);
        PageChanged?.Invoke(this, snapshot);
    }
}

public record DirectionOption(TextDirection Value, string Title);
