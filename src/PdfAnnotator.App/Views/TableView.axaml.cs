using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using PdfAnnotator.App.ViewModels;

namespace PdfAnnotator.App.Views;

public partial class TableView : UserControl
{
    public TableView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private TableViewModel? Vm => DataContext as TableViewModel;

    private async void OnSaveCsvClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm == null)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        if (top == null)
        {
            return;
        }

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = GetSuggestedFileName(),
            DefaultExtension = "csv",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("CSV") { Patterns = new[] { "*.csv" } },
                FilePickerFileTypes.All
            }
        });

        var path = file?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Vm.CsvPath = path;
        await Vm.SaveCsvAsync(path);
    }

    private async void OnLoadCsvClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm == null)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        if (top == null)
        {
            return;
        }

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("CSV") { Patterns = new[] { "*.csv" } },
                FilePickerFileTypes.All
            }
        });

        var path = files?.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        Vm.CsvPath = path;
        await Vm.LoadCsvAsync(path);
    }

    private string GetSuggestedFileName()
    {
        if (!string.IsNullOrWhiteSpace(Vm?.CsvPath))
        {
            var name = Path.GetFileName(Vm.CsvPath);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return "latest.csv";
    }
}
