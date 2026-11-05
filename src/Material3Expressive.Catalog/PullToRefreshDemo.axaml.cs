using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class PullToRefreshDemo : Window
{
    private readonly PullToRefreshState _state = new(1);
    private readonly PullToRefreshScrollBehavior _scroll = new();
    private bool _ready;
    public PullToRefreshDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        Circular.State = Expressive.State = _state;
        Gesture.State = new BouncyRefreshState();
        BouncePreview.State = new BouncyRefreshState();
        Gesture.ScrollBehavior = _scroll;
        _scroll.RefreshRequested += (_, _) => Gesture.IsRefreshing = true;
        _ready = true;
    }
    private async void DistanceChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_ready) return;
        try { await _state.SnapToAsync((float)e.NewValue); }
        catch (OperationCanceledException) { }
    }
    private async void StartRefresh(object? sender, RoutedEventArgs e)
    {
        Circular.IsRefreshing = Expressive.IsRefreshing = true;
        try { await _state.AnimateToThresholdAsync(); }
        catch (OperationCanceledException) { }
    }
    private async void StopRefresh(object? sender, RoutedEventArgs e)
    {
        Circular.IsRefreshing = Expressive.IsRefreshing = false;
        try { await _state.AnimateToHiddenAsync(); }
        catch (OperationCanceledException) { }
    }
    private void Pull(object? sender, RoutedEventArgs e)
    {
        var pre = _scroll.OnPreScroll(80);
        _scroll.OnPostScroll(80 - pre); // No scroll child consumes the remainder in this sample.
    }
    private void Push(object? sender, RoutedEventArgs e)
    {
        var pre = _scroll.OnPreScroll(-40);
        _scroll.OnPostScroll(-40 - pre);
    }
    private async void Release(object? sender, RoutedEventArgs e)
    {
        try { await _scroll.OnPreFlingAsync(0); }
        catch (OperationCanceledException) { } // Refresh state can supersede release settling.
    }
    private void CompleteRefresh(object? sender, RoutedEventArgs e) => Gesture.IsRefreshing = false;
    private async void PreviewBounce(object? sender, RoutedEventArgs e)
    {
        try
        {
            await BouncePreview.State!.SnapToAsync(0);
            await BouncePreview.State.AnimateToThresholdAsync();
        }
        catch (OperationCanceledException) { }
    }
    private void ToggleTheme(object? sender, RoutedEventArgs e) =>
        RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;

    private sealed class BouncyRefreshState : PullToRefreshState
    {
        public override Task AnimateToThresholdAsync(CancellationToken cancellationToken = default) =>
            AnimateToAsync(1, new SpringSpec<float>(.2f), cancellationToken);
    }
}
