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
    private readonly IPresetService _presetService;
    private readonly ILogger<ExtractionViewModel> _logger;

    public event EventHandler<List<TableRow>>? TableUpdated;
    public event EventHandler? PresetApplied;

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

    public int Dpi { get; set; } = 50;
    public int PageRotation { get; set; } = 0;  // Rotation angle: 0, 90, 180, or 270
    public Bitmap? PageBitmap { get; set; }

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

    public ExtractionPreset? SelectedPreset 
    { 
        get => _selectedPreset;
        set
        {
            _selectedPreset = value;
            if (_selectedPreset != null)
            {
                // Apply the preset values directly
                X0 = _selectedPreset.X0;
                Y0 = _selectedPreset.Y0;
                X1 = _selectedPreset.X1;
                Y1 = _selectedPreset.Y1;
                
                // Notify that the preset has been applied so the view can update the selection rectangle
                PresetApplied?.Invoke(this, EventArgs.Empty);
                
                // Automatically extract text for preview when a preset is selected
                _ = ExtractTextPreviewAsync();
            }
        }
    }
    
    private ExtractionPreset? _selectedPreset;
    
    public string? SelectedPresetName { get; set; }

    public ICommand LoadPdfCommand { get; }
    public ICommand ExtractTextCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand LoadPresetCommand { get; }
    public ICommand ReloadPresetsCommand { get; }
    public ICommand RotateLeftCommand { get; }
    public ICommand RotateRightCommand { get; }

    public ExtractionViewModel(IPdfService pdfService, IPresetService presetService, ILogger<ExtractionViewModel> logger)
    {
        _pdfService = pdfService;
        _presetService = presetService;
        _logger = logger;

        LoadPdfCommand = new RelayCommand(async _ => await LoadPdfAsync());
        ExtractTextCommand = new RelayCommand(async _ => await ExtractAsync());
        SavePresetCommand = new RelayCommand(async _ => await SavePresetAsync());
        LoadPresetCommand = new RelayCommand(async _ => await ApplyPreset());
        ReloadPresetsCommand = new RelayCommand(async _ => await LoadPresetsAsync());
        RotateLeftCommand = new RelayCommand(_ => RotateLeft());
        RotateRightCommand = new RelayCommand(_ => RotateRight());
    }

    public async Task LoadPdfAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            _logger.LogWarning("PDF path is empty or missing");
            return;
        }

        PageCount = await _pdfService.GetPageCountAsync(PdfPath);
        CurrentPage = Math.Clamp(CurrentPage, 1, PageCount);
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            return;
        }

        PageBitmap = await _pdfService.RenderPageAsync(PdfPath, CurrentPage, Dpi, PageRotation);
        
        // If a preset is already selected, notify that we need to update the selection rectangle
        // This ensures the rectangle is displayed when switching pages or loading a new PDF
        if (SelectedPreset != null)
        {
            PresetApplied?.Invoke(this, EventArgs.Empty);
            // Also extract text for preview when page changes
            _ = ExtractTextPreviewAsync();
        }
    }

    public void UpdateSelection(double startX, double startY, double endX, double endY, double imageHeight)
    {
        SelectLeft = Math.Min(startX, endX);
        SelectTop = Math.Min(startY, endY);
        SelectWidth = Math.Abs(endX - startX);
        SelectHeight = Math.Abs(endY - startY);
        X0 = Math.Min(startX, endX);
        X1 = Math.Max(startX, endX);
        // Convert to PDF coordinate system: bottom-left origin
        Y0 = imageHeight - Math.Max(startY, endY);
        Y1 = imageHeight - Math.Min(startY, endY);
        
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
                Y1 = Y1
            };

            var rows = await _pdfService.ExtractTextAsync(PdfPath, preset, PageRotation);
            
            // Combine all extracted text for the current page into a single preview
            var previewText = string.Join("\n", rows.Where(r => r.Page == CurrentPage).Select(r => r.FieldText));
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
        var preset = new ExtractionPreset
        {
            Name = SelectedPreset?.Name ?? "Current",
            X0 = X0,
            Y0 = Y0,
            X1 = X1,
            Y1 = Y1
        };

        var rows = await _pdfService.ExtractTextAsync(PdfPath, preset, PageRotation);
        TableUpdated?.Invoke(this, rows);
    }

    private async Task SavePresetAsync()
    {
        var preset = new ExtractionPreset
        {
            Name = SelectedPreset?.Name ?? $"Preset_{DateTime.Now:HHmmss}",
            X0 = X0,
            Y0 = Y0,
            X1 = X1,
            Y1 = Y1
        };
        await _presetService.SaveExtractionPresetAsync(preset);
        await LoadPresetsAsync();
        SelectedPreset = Presets.FirstOrDefault(p => p.Name == preset.Name);
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
        
        // Notify that the preset has been applied so the view can update the selection rectangle
        PresetApplied?.Invoke(this, EventArgs.Empty);
        
        // Automatically extract text for preview when a preset is applied
        _ = ExtractTextPreviewAsync();
    }

    public async Task LoadPresetsAsync()
    {
        Presets.Clear();
        var presets = await _presetService.LoadAllExtractionPresetsAsync();
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
        var preset = await _presetService.LoadExtractionPresetAsync(path);
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
    
    private void RotateLeft()
    {
        PageRotation = (PageRotation - 90 + 360) % 360;
        _ = LoadPageAsync();
    }
    
    private void RotateRight()
    {
        PageRotation = (PageRotation + 90) % 360;
        _ = LoadPageAsync();
    }
}