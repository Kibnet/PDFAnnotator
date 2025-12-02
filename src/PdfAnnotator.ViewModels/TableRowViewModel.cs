using PdfAnnotator.Core.Models;
using PropertyChanged;

namespace PdfAnnotator.App.ViewModels;

[AddINotifyPropertyChangedInterface]
public class TableRowViewModel
{
    private string _code = string.Empty;

    public int Page { get; set; }

    public string FieldText { get; set; } = string.Empty;

    public string Code
    {
        get => _code;
        set
        {
            _code = value;
            CodeWarning = string.IsNullOrWhiteSpace(_code);
        }
    }

    public bool PageError { get; set; }

    public bool CodeWarning { get; set; }

    public static TableRowViewModel FromModel(TableRow row)
    {
        return new TableRowViewModel
        {
            Page = row.Page,
            FieldText = row.FieldText,
            Code = row.Code,
            CodeWarning = string.IsNullOrWhiteSpace(row.Code)
        };
    }

    public TableRow ToModel() => new()
    {
        Page = Page,
        FieldText = FieldText,
        Code = Code
    };
}
