using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class TextFieldDemo : Window
{
    public TextFieldDemo()
    {
        VisualAnimationScope.Initialize(this);
        InitializeComponent();
    }

    private void ClearSearch(object? sender, RoutedEventArgs e) => SearchInput.Clear();
    private void ToggleTheme(object? sender, RoutedEventArgs e) =>
        RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void ToggleDirection(object? sender, RoutedEventArgs e) =>
        FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
