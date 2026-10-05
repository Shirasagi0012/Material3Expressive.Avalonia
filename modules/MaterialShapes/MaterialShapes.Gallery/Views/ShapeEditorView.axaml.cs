using Avalonia.Controls;
using Avalonia.Interactivity;
using MaterialShapes.Gallery.Controls;

namespace MaterialShapes.Gallery.Views;

public partial class ShapeEditorView : UserControl
{
    public ShapeEditorView()
    {
        InitializeComponent();
    }

    private void OnResetViewClicked(object? sender, RoutedEventArgs e)
    {
        PanZoomPresenter.Reset();
    }
}
