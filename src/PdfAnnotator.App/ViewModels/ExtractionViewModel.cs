using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.App.ViewModels;

public class ExtractionViewModel : ViewModelBase
{
    private readonly IPdfService _pdfService;
    private readonly IPresetService _presetService;
    private readonly ILogger<ExtractionViewModel> _logger;

    public event EventHandler<List<TableRow>>? TableUpdated;

    private string _pdfPath = string.Empty;
    public string PdfPath
    {
        get => _pdfPath;
        set
        {
            if (_pdfPath != value)
            {
                _pdfPath = value;
                RaisePropertyChanged();
            }
        }
    }

    private int _pageCount;
    public int PageCount
    {
        get => _pageCount;
        set { _pageCount = value; RaisePropertyChanged(); }
    }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                RaisePropertyChanged();
                _ = LoadPageAsync();
            }
        }
    }

    private int _dpi = 150;
    public int Dpi
    {
        get => _dpi;
        set { _dpi = value; RaisePropertyChanged(); }
    }

    private Bitmap? _pageBitmap;
    public Bitmap? PageBitmap
    {
        get => _pageBitmap;
        set { _pageBitmap = value; RaisePropertyChanged(); }
    }

    private double _x0;
    private double _y0;
    private double _x1;
    private double _y1;
    private double _selectLeft;
    private double _selectTop;
    private double _selectWidth;
    private double _selectHeight;

    public double X0 { get => _x0; set { _x0 = value; RaisePropertyChanged(); } }
    public double Y0 { get => _y0; set { _y0 = value; RaisePropertyChanged(); } }
    public double X1 { get => _x1; set { _x1 = value; RaisePropertyChanged(); } }
    public double Y1 { get => _y1; set { _y1 = value; RaisePropertyChanged(); } }
    public double SelectLeft { get => _selectLeft; set { _selectLeft = value; RaisePropertyChanged(); } }
    public double SelectTop { get => _selectTop; set { _selectTop = value; RaisePropertyChanged(); } }
    public double SelectWidth { get => _selectWidth; set { _selectWidth = value; RaisePropertyChanged(); } }
    public double SelectHeight { get => _selectHeight; set { _selectHeight = value; RaisePropertyChanged(); } }

    public ObservableCollection<ExtractionPreset> Presets { get; } = new();

    private ExtractionPreset? _selectedPreset;
    public ExtractionPreset? SelectedPreset
    {
        get => _selectedPreset;
        set { _selectedPreset = value; RaisePropertyChanged(); ApplyPreset(); }
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

    private async Task LoadPdfAsync()
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
