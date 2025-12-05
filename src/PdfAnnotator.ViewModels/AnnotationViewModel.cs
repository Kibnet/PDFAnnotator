using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;
using PropertyChanged;

namespace PdfAnnotator.App.ViewModels;

[AddINotifyPropertyChangedInterface]
public class AnnotationViewModel
{
    private readonly IPdfService _pdfService;
    private readonly IPresetService _presetService;
    private readonly ILogger<AnnotationViewModel> _logger;

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
        set { _selectedPreset = value; ApplyPreset(); }
    }

    public string? SelectedPresetName { get; set; }
    public double TextX { get; set; }
    public double TextY { get; set; }
    public double PreviewX { get; set; }
    public double PreviewY { get; set; }
    public double FontSize { get; set; } = 12;
    public double Angle { get; set; }
    public string ColorHex { get; set; } = "#000000";
    public string FontName { get; set; } = "Helvetica";
    public double OriginalPageWidthPt { get; set; }
    public double OriginalPageHeightPt { get; set; }

    public ObservableCollection<string> Fonts { get; } = new(new[] { "Helvetica", "Arial", "Times New Roman" });

    public ObservableCollection<TableRow> Rows { get; } = new();
    public string SelectedCodePreview { get; set; } = string.Empty;

    public void ApplyPageSnapshot(PageSnapshot snapshot)
    {
        PdfPath = snapshot.PdfPath;
        PageCount = snapshot.PageCount;
        _currentPage = snapshot.CurrentPage;
        OriginalPageWidthPt = snapshot.WidthPt;
        OriginalPageHeightPt = snapshot.HeightPt;
        PageBitmap = snapshot.Bitmap;
        RefreshPreview();
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

    public void UpdatePosition(double viewX, double viewY, double bitmapX, double bitmapY, double bitmapWidth, double bitmapHeight)
    {
        // preview in view coordinates for correct overlay positioning
        PreviewX = viewX;
        PreviewY = viewY;

        if (OriginalPageWidthPt > 0 && OriginalPageHeightPt > 0 && bitmapWidth > 0 && bitmapHeight > 0)
        {
            var scaleX = OriginalPageWidthPt / bitmapWidth;
            var scaleY = OriginalPageHeightPt / bitmapHeight;
            TextX = bitmapX * scaleX;
            TextY = OriginalPageHeightPt - bitmapY * scaleY;
        }
        else
        {
            // fallback to bitmap space if dimensions are unknown
            TextX = bitmapX;
            TextY = bitmapHeight - bitmapY;
        }
        RefreshPreview();
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
