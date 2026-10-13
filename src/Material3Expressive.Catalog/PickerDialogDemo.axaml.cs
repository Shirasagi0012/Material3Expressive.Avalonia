using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Catalog;

public partial class PickerDialogDemo : Window
{
    public PickerDialogDemo() { AnimationScope.Initialize(this); InitializeComponent(); }
    private void OnOpenDate(object? sender, RoutedEventArgs e) => DateDialog.IsOpen = true;
    private void OnOpenTime(object? sender, RoutedEventArgs e) => TimeDialog.IsOpen = true;
    private void OnCancelDate(object? sender, RoutedEventArgs e) => DateDialog.IsOpen = false;
    private void OnCancelTime(object? sender, RoutedEventArgs e) => TimeDialog.IsOpen = false;
    private void OnConfirmDate(object? sender, RoutedEventArgs e) { Result.Text = DateEditor.Text; DateDialog.IsOpen = false; }
    private void OnConfirmTime(object? sender, RoutedEventArgs e) { Result.Text = TimeEditor.Text; TimeDialog.IsOpen = false; }
    private void OnToggleHelp(object? sender, RoutedEventArgs e) => TimeHelp.IsVisible = !TimeHelp.IsVisible;
    private void OnDismissRequested(object? sender, DismissRequestedEventArgs e)
    {
        Result.Text = $"Dismissed: {e.Reason}";
        ((Dialog)sender!).IsOpen = false;
    }
}
