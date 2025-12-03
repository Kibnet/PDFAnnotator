using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;
using System.ComponentModel;

namespace PdfAnnotator.App.ViewModels;

[AddINotifyPropertyChangedInterface]
public class TableViewModel
{
    private readonly ICsvService _csvService;
    private readonly ILogger<TableViewModel> _logger;
    private const string DefaultCsvPath = "tables/latest.csv";
    public event EventHandler<List<TableRow>>? RowsUpdated;

    public ObservableCollection<TableRowViewModel> Rows { get; } = new();
    public string CsvPath { get; set; } = DefaultCsvPath;
    public string StatusMessage { get; set; } = string.Empty;

    public ICommand SaveCsvCommand { get; }
    public ICommand LoadCsvCommand { get; }

    public TableViewModel(ICsvService csvService, ILogger<TableViewModel> logger)
    {
        _csvService = csvService;
        _logger = logger;
        SaveCsvCommand = new RelayCommand(async _ => await SaveCsvAsync());
        LoadCsvCommand = new RelayCommand(async _ => await LoadCsvAsync());
    }

    public void SetRows(IEnumerable<TableRow> rows)
    {
        Rows.Clear();
        foreach (var row in rows)
        {
            var vm = TableRowViewModel.FromModel(row);
            if (vm is INotifyPropertyChanged intf)
        {
                intf.PropertyChanged += (_, _) => NotifyRowsUpdated();
                Rows.Add(vm);
            }
        }

        StatusMessage = $"Rows in table: {Rows.Count}";
        RowsUpdated?.Invoke(this, Rows.Select(r => r.ToModel()).ToList());
    }

    public async Task SaveCsvAsync(string? path = null)
    {
        var targetPath = GetTargetPath(path);
        try
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await _csvService.SaveCsvAsync(targetPath, Rows.Select(r => r.ToModel()).ToList());
            CsvPath = targetPath;
            StatusMessage = $"CSV saved to {targetPath}";
            _logger.LogInformation("CSV saved to {Path}", targetPath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            _logger.LogError(ex, "Failed to save CSV to {Path}", targetPath);
        }
    }

    public async Task LoadCsvAsync(string? path = null)
    {
        var targetPath = GetTargetPath(path);
        if (!File.Exists(targetPath))
        {
            StatusMessage = $"CSV not found: {targetPath}";
            _logger.LogWarning("CSV file not found: {Path}", targetPath);
            return;
        }

        try
        {
            var loaded = await _csvService.LoadCsvAsync(targetPath);
            CsvPath = targetPath;
            SetRows(loaded);
            StatusMessage = $"CSV loaded from {targetPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
            _logger.LogError(ex, "Failed to load CSV from {Path}", targetPath);
        }
    }

    private void NotifyRowsUpdated()
    {
        RowsUpdated?.Invoke(this, Rows.Select(r => r.ToModel()).ToList());
    }

    private string GetTargetPath(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (!string.IsNullOrWhiteSpace(CsvPath))
        {
            return CsvPath;
        }

        return DefaultCsvPath;
    }
}
