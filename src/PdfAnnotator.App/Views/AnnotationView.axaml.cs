using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PdfAnnotator.App.ViewModels;

namespace PdfAnnotator.App.Views;

public partial class AnnotationView : UserControl
{
    public AnnotationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private AnnotationViewModel? Vm => DataContext as AnnotationViewModel;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm == null || AnnotImage?.Source == null)
        {
            return;
        }

        var pos = e.GetPosition(AnnotImage);
        Vm.UpdatePosition(pos.X, pos.Y, AnnotImage.Bounds.Height);
    }
}
