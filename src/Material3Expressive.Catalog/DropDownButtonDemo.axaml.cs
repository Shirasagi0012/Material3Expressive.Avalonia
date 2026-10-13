using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class DropDownButtonDemo : Window
{
    public DropDownButtonDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }

    private void OnPick(object? sender, RoutedEventArgs e) =>
        PickedText.Text = (sender as MenuItem)?.Header?.ToString() ?? "(none)";
}
