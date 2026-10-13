using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class SecureTextFieldDemo : Window
{
    public SecureTextFieldDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }

    private void RevealFilled(object? sender, RoutedEventArgs e) =>
        FilledPassword.SetCurrentValue(TextBox.RevealPasswordProperty, !FilledPassword.RevealPassword);
    private void RevealOutlined(object? sender, RoutedEventArgs e) =>
        OutlinedPassword.SetCurrentValue(TextBox.RevealPasswordProperty, !OutlinedPassword.RevealPassword);
    private void ToggleTheme(object? sender, RoutedEventArgs e) =>
        RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    private void ToggleDirection(object? sender, RoutedEventArgs e) =>
        FlowDirection = FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
