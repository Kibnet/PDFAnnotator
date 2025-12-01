using PdfAnnotator.Core.Models;

namespace PdfAnnotator.App.ViewModels;

public class TableRowViewModel : ViewModelBase
{
    private int _page;
    private string _fieldText = string.Empty;
    private string _code = string.Empty;
    private bool _pageError;
    private bool _codeWarning;

    public int Page
    {
        get => _page;
        set { _page = value; RaisePropertyChanged(); }
    }

    public string FieldText
    {
        get => _fieldText;
        set { _fieldText = value; RaisePropertyChanged(); }
    }

    public string Code
    {
        get => _code;
        set
        {
            _code = value;
            CodeWarning = string.IsNullOrWhiteSpace(_code);
            RaisePropertyChanged();
        }
    }

    public bool PageError
    {
        get => _pageError;
        set { _pageError = value; RaisePropertyChanged(); }
    }

    public bool CodeWarning
    {
        get => _codeWarning;
        set { _codeWarning = value; RaisePropertyChanged(); }
    }

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
