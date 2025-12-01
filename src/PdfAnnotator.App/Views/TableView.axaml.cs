using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
}
