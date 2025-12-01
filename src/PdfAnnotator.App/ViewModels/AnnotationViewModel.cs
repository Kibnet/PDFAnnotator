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

public class AnnotationViewModel : ViewModelBase
{
    private readonly IPdfService _pdfService;
    private readonly IPresetService _presetService;
    private readonly ILogger<AnnotationViewModel> _logger;

    private string _pdfPath = string.Empty;
    public string PdfPath
    {
        get => _pdfPath;
        set { _pdfPath = value; RaisePropertyChanged(); }
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
            _currentPage = value;
            RaisePropertyChanged();
            _ = LoadPageAsync();
            RefreshPreview();
        }
    }

    private Bitmap? _pageBitmap;
    public Bitmap? PageBitmap
    {
        get => _pageBitmap;
        set { _pageBitmap = value; RaisePropertyChanged(); }
    }

    public ObservableCollection<AnnotationPreset> Presets { get; } = new();

    private AnnotationPreset? _selectedPreset;
    public AnnotationPreset? SelectedPreset
    {
        get => _selectedPreset;
        set { _selectedPreset = value; RaisePropertyChanged(); ApplyPreset(); }
    }

    public string? SelectedPresetName { get; set; }

    private double _textX;
    public double TextX { get => _textX; set { _textX = value; RaisePropertyChanged(); } }

    private double _textY;
    public double TextY { get => _textY; set { _textY = value; RaisePropertyChanged(); } }

    private double _previewX;
    public double PreviewX { get => _previewX; set { _previewX = value; RaisePropertyChanged(); } }

    private double _previewY;
    public double PreviewY { get => _previewY; set { _previewY = value; RaisePropertyChanged(); } }

    private double _fontSize = 12;
    public double FontSize { get => _fontSize; set { _fontSize = value; RaisePropertyChanged(); } }

    private double _angle;
    public double Angle { get => _angle; set { _angle = value; RaisePropertyChanged(); } }

    private string _colorHex = "#000000";
    public string ColorHex { get => _colorHex; set { _colorHex = value; RaisePropertyChanged(); } }

    private string _fontName = "Helvetica";
    public string FontName { get => _fontName; set { _fontName = value; RaisePropertyChanged(); } }

    public ObservableCollection<string> Fonts { get; } = new(new[] { "Helvetica", "Arial", "Times New Roman" });

    public ObservableCollection<TableRow> Rows { get; } = new();
    private string _selectedCodePreview = string.Empty;
    public string SelectedCodePreview
    {
        get => _selectedCodePreview;
        set { _selectedCodePreview = value; RaisePropertyChanged(); }
    }

    public ICommand LoadPdfCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand ReloadPresetsCommand { get; }
    public ICommand SaveAnnotatedPdfCommand { get; }

    public AnnotationViewModel(IPdfService pdfService, IPresetService presetService, ILogger<AnnotationViewModel> logger)
    {
        _pdfService = pdfService;
        _presetService = presetService;
        _logger = logger;

        LoadPdfCommand = new RelayCommand(async _ => await LoadPdfAsync());
        SavePresetCommand = new RelayCommand(async _ => await SavePresetAsync());
        ReloadPresetsCommand = new RelayCommand(async _ => await LoadPresetsAsync());
        SaveAnnotatedPdfCommand = new RelayCommand(async _ => await SaveAnnotatedAsync());
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

    public void UpdatePosition(double x, double y, double imageHeight)
    {
        PreviewX = x;
        PreviewY = y;
        TextX = x;
        TextY = imageHeight - y;
        RefreshPreview();
    }

    private async Task LoadPdfAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            _logger.LogWarning("PDF path missing for annotation");
            return;
        }

        PageCount = await _pdfService.GetPageCountAsync(PdfPath);
        CurrentPage = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        if (string.IsNullOrWhiteSpace(PdfPath) || !File.Exists(PdfPath))
        {
            return;
        }

        PageBitmap = await _pdfService.RenderPageAsync(PdfPath, CurrentPage, 150);
        RefreshPreview();
    }

    private async Task SavePresetAsync()
    {
        var preset = new AnnotationPreset
        {
            Name = SelectedPreset?.Name ?? $"Annot_{DateTime.Now:HHmmss}",
            TextX = TextX,
            TextY = TextY,
            FontSize = FontSize,
            Angle = Angle,
            ColorHex = ColorHex,
            FontName = FontName
        };

        await _presetService.SaveAnnotationPresetAsync(preset);
        await LoadPresetsAsync();
        SelectedPreset = Presets.FirstOrDefault(p => p.Name == preset.Name);
    }

    public async Task LoadPresetsAsync()
    {
        Presets.Clear();
        var presets = await _presetService.LoadAllAnnotationPresetsAsync();
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
    }

    private async Task SaveAnnotatedAsync()
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

        var output = "output/annotated.pdf";
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        await _pdfService.GenerateAnnotatedPdfAsync(PdfPath, output, Rows.ToList(), preset);
        _logger.LogInformation("Annotated PDF saved at {Path}", output);
    }

    private void RefreshPreview()
    {
        var row = Rows.FirstOrDefault(r => r.Page == CurrentPage);
        SelectedCodePreview = row?.Code ?? string.Empty;
    }
}
