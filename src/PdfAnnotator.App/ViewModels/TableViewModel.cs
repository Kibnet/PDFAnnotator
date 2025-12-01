using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using PdfAnnotator.Core.Models;
using PdfAnnotator.Core.Services;

namespace PdfAnnotator.App.ViewModels;

public class TableViewModel : ViewModelBase
{
    private readonly ICsvService _csvService;
    private readonly ILogger<TableViewModel> _logger;
    public event EventHandler<List<TableRow>>? RowsUpdated;

    public ObservableCollection<TableRowViewModel> Rows { get; } = new();

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
            vm.PropertyChanged += (_, _) => NotifyRowsUpdated();
            Rows.Add(vm);
        }
        RowsUpdated?.Invoke(this, Rows.Select(r => r.ToModel()).ToList());
    }

    private async Task SaveCsvAsync()
    {
        var path = "tables/latest.csv";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await _csvService.SaveCsvAsync(path, Rows.Select(r => r.ToModel()).ToList());
        _logger.LogInformation("CSV saved to {Path}", path);
    }

    private async Task LoadCsvAsync()
    {
        var path = "tables/latest.csv";
        var loaded = await _csvService.LoadCsvAsync(path);
        SetRows(loaded);
    }

    private void NotifyRowsUpdated()
    {
        RowsUpdated?.Invoke(this, Rows.Select(r => r.ToModel()).ToList());
    }
}
