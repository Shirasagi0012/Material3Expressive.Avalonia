using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class SwipeToDismissDemo : Window
{
    public SwipeToDismissDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
    }
    private void OnDismissed(object? sender, SwipeDismissedEventArgs e) => Status.Text = $"Completed: {e.Direction}. Reset restores this item.";
    private async void OnDismissStart(object? sender, RoutedEventArgs e) => await Run(Swipe.State.DismissAsync(SwipeToDismissBoxValue.StartToEnd));
    private async void OnDismissEnd(object? sender, RoutedEventArgs e) => await Run(Swipe.State.DismissAsync(SwipeToDismissBoxValue.EndToStart));
    private async void OnReset(object? sender, RoutedEventArgs e) => await Run(Swipe.State.ResetAsync());
    private async Task Run(Task action)
    {
        try { await action; Status.Text = $"Settled: {Swipe.State.SettledValue}"; }
        catch (OperationCanceledException) { /* A new gesture, action or window close interrupts it. */ }
    }
    private void OnDirectionChanged(object? sender, RoutedEventArgs e) =>
        Swipe.FlowDirection = sender is CheckBox { IsChecked: true } ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
