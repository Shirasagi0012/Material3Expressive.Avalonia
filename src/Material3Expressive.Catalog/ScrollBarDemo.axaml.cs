using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ComposeAnimation;
using SmoothScroll.Avalonia.Controls;
using SmoothScroll.Avalonia.Interaction;

namespace Material3Expressive.Catalog;

public partial class ScrollBarDemo : Window
{
    public ScrollBarDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        CanvasViewer.GestureBindings[new ScrollGesture(ScrollInputGesture.MouseLeftDrag)] = ScrollGestureAction.Pan;
        CanvasViewer.ZoomChanged += OnCanvasZoomChanged;
        Opened += (_, _) => CanvasViewer.Offset = new Vector(
            Math.Max(0, (CanvasViewer.Extent.Width - CanvasViewer.Viewport.Width) / 2),
            Math.Max(0, (CanvasViewer.Extent.Height - CanvasViewer.Viewport.Height) / 2));
    }

    private void OnCanvasZoomChanged(object? sender, ZoomChangedEventArgs e) =>
        ZoomStatusText.Text = $"Zoom: {CanvasViewer.ZoomFactor:0.00}x — 拖拽画布或 Ctrl + 滚轮缩放";

    private void OnZoomOutClick(object? sender, RoutedEventArgs e) =>
        CanvasViewer.ZoomBy(Math.Max(CanvasViewer.MinZoomFactor - CanvasViewer.ZoomFactor, -0.25), centerPoint: null);

    private void OnZoomResetClick(object? sender, RoutedEventArgs e) =>
        CanvasViewer.ZoomTo(1, centerPoint: null);

    private void OnZoomInClick(object? sender, RoutedEventArgs e) =>
        CanvasViewer.ZoomBy(Math.Min(CanvasViewer.MaxZoomFactor - CanvasViewer.ZoomFactor, 0.25), centerPoint: null);
}
