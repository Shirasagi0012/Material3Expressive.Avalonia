using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Catalog;

public partial class AlertDialogDemo : Window
{
    public AlertDialogDemo() { VisualAnimationScope.Initialize(this); InitializeComponent(); }
    private void OnOpenAlert(object? sender, RoutedEventArgs e) => Alert.IsOpen = true;
    private void OnOpenBasic(object? sender, RoutedEventArgs e) => Basic.IsOpen = true;
    private void OnCloseBasic(object? sender, RoutedEventArgs e) => Basic.IsOpen = false;
    private void OnConfirm(object? sender, RoutedEventArgs e) { Result.Text = "Record deleted"; Alert.IsOpen = false; }
    private void OnCancel(object? sender, RoutedEventArgs e) { Result.Text = "Record kept"; Alert.IsOpen = false; }
    private void OnDismissRequested(object? sender, DismissRequestedEventArgs e)
    {
        Result.Text = $"Dismissed: {e.Reason}"; ((Dialog)sender!).IsOpen = false;
    }
}
