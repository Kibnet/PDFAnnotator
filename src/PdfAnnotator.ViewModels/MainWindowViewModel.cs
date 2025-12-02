using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.App.ViewModels;

public enum AppMode
{
    Extraction,
    Table,
    Annotation
}

[AddINotifyPropertyChangedInterface]
public class MainWindowViewModel
{
    private readonly ICsvService _csvService;
    private readonly IPresetService _presetService;
    private readonly IProjectService _projectService;
    private readonly ILogger<MainWindowViewModel> _logger;

    public ExtractionViewModel Extraction { get; }
    public TableViewModel Table { get; }
    public AnnotationViewModel Annotation { get; }

    public AppMode Mode { get; set; } = AppMode.Extraction;

    public ICommand GoToTableCommand { get; }
    public ICommand GoToAnnotationCommand { get; }
    public ICommand GoToExtractionCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand LoadProjectCommand { get; }

    private PdfProject _currentProject = new() { Name = "New Project" };

    public MainWindowViewModel(
        ICsvService csvService,
        IPresetService presetService,
        IProjectService projectService,
        ExtractionViewModel extraction,
        TableViewModel table,
        AnnotationViewModel annotation,
        ILogger<MainWindowViewModel> logger)
    {
        _csvService = csvService;
        _presetService = presetService;
        _projectService = projectService;
        Extraction = extraction;
        Table = table;
        Annotation = annotation;
        _logger = logger;

        Extraction.TableUpdated += OnExtractionTableUpdated;
        Table.RowsUpdated += OnTableUpdated;

        GoToTableCommand = new RelayCommand(_ => Mode = AppMode.Table);
        GoToAnnotationCommand = new RelayCommand(_ =>
        {
            SyncTableToAnnotation();
            Mode = AppMode.Annotation;
        });
        GoToExtractionCommand = new RelayCommand(_ => Mode = AppMode.Extraction);
        SaveProjectCommand = new RelayCommand(async _ => await SaveProjectAsync());
        LoadProjectCommand = new RelayCommand(async _ => await LoadProjectAsync());

        _ = Extraction.LoadPresetsAsync();
        _ = Annotation.LoadPresetsAsync();
    }

    private void OnExtractionTableUpdated(object? sender, List<TableRow> rows)
    {
        Table.SetRows(rows);
        Mode = AppMode.Table;
    }

    private void OnTableUpdated(object? sender, List<TableRow> rows)
    {
        Annotation.SetRows(rows);
    }

    private void SyncTableToAnnotation()
    {
        Annotation.SetRows(Table.Rows.Select(r => r.ToModel()).ToList());
    }

    public async Task SaveProjectAsync()
    {
        _currentProject.Rows = Table.Rows.Select(r => r.ToModel()).ToList();
        _currentProject.ExtractPresetName = Extraction.SelectedPreset?.Name ?? string.Empty;
        _currentProject.AnnotatePresetName = Annotation.SelectedPreset?.Name ?? string.Empty;
        var path = Path.Combine("projects", $"{_currentProject.Name}.json");
        await _projectService.SaveProjectAsync(_currentProject, path);
        _logger.LogInformation("Project saved to {Path}", path);
    }

    public async Task LoadProjectAsync()
    {
        var path = Path.Combine("projects", $"{_currentProject.Name}.json");
        if (!File.Exists(path))
        {
            _logger.LogWarning("Project file not found: {Path}", path);
            return;
        }

        var project = await _projectService.LoadProjectAsync(path);
        _currentProject = project;
        Table.SetRows(project.Rows);
        Extraction.SelectedPresetName = project.ExtractPresetName;
        Annotation.SelectedPresetName = project.AnnotatePresetName;
        Mode = AppMode.Table;
    }
}
