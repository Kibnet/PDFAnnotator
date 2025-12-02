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
    public Bitmap? PageBitmap { get; set; }

    public double X0 { get; set; }
    public double Y0 { get; set; }
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double SelectLeft { get; set; }
    public double SelectTop { get; set; }
    public double SelectWidth { get; set; }
    public double SelectHeight { get; set; }

    public ObservableCollection<ExtractionPreset> Presets { get; } = new();

    private ExtractionPreset? _selectedPreset;
    public ExtractionPreset? SelectedPreset
    {
        get => _selectedPreset;
        set { _selectedPreset = value; ApplyPreset(); }
    }

    public string? SelectedPresetName { get; set; }

    public ICommand LoadPdfCommand { get; }
    public ICommand ExtractTextCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand ReloadPresetsCommand { get; }

    public ExtractionViewModel(IPdfService pdfService, IPresetService presetService, ILogger<ExtractionViewModel> logger)
    {
        _pdfService = pdfService;
        _presetService = presetService;
        _logger = logger;

        LoadPdfCommand = new RelayCommand(async _ => await LoadPdfAsync());
        ExtractTextCommand = new RelayCommand(async _ => await ExtractAsync(), _ => PageCount > 0);
        SavePresetCommand = new RelayCommand(async _ => await SavePresetAsync());
        ReloadPresetsCommand = new RelayCommand(async _ => await LoadPresetsAsync());
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

        PageBitmap = await _pdfService.RenderPageAsync(PdfPath, CurrentPage, Dpi);
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

        var rows = await _pdfService.ExtractTextAsync(PdfPath, preset);
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

    private void ApplyPreset()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        X0 = SelectedPreset.X0;
        Y0 = SelectedPreset.Y0;
        X1 = SelectedPreset.X1;
        Y1 = SelectedPreset.Y1;
    }
}
