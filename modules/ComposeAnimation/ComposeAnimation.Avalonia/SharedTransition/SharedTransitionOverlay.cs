using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal sealed class SharedOverlayItem
{
    internal SharedTransitionMotion Motion = null!;
    internal SharedTransitionContent Source = null!;
    internal SharedTransitionContent Target = null!;
    internal VisualBrush? SourceBrush;
    internal VisualBrush TargetBrush = null!;
    internal Rect Rect;
    internal double Fade;
    internal bool BoundsLive;
    internal bool Clip;
    internal CornerRadius ClipRadius;
    internal int Depth;
    internal double ZIndex;
    internal SharedOverlayItem? Parent;
    internal Size RestingSize;
    internal Transition<int>.TransitionAnimationState<double>? FadeAnimation;
    internal PropertyChangedEventHandler? BoundsChanged;
    internal PropertyChangedEventHandler? FadeChanged;
    internal SharedTransitionHandoff? PendingHandoff;
    internal bool ReadyToDraw = true;
}

internal sealed class SharedTransitionOverlay : Control
{
    private readonly List<SharedOverlayItem> _items = [];
    private int _brushes;
    private int _renders;
    private double _renderMs;

    public SharedTransitionOverlay() => IsHitTestVisible = false;
    internal IReadOnlyList<SharedOverlayItem> Items => _items;
    internal int BrushCount => _brushes;
    internal double MeanMs => _renders == 0 ? 0 : _renderMs / _renders;
    internal bool Active => _items.Count > 0;

    internal void ResetMetrics() { _brushes = 0; _renders = 0; _renderMs = 0; }

    internal VisualBrush CreateBrush(Visual visual)
    {
        _brushes++;
        return new VisualBrush(visual) { Stretch = Stretch.Fill };
    }

    internal static void InvalidateBrush(VisualBrush brush, Visual visual)
    {
        brush.Visual = null;
        brush.Visual = visual;
    }

    internal void Add(SharedOverlayItem item)
    {
        _items.Add(item);
        InvalidateVisual();
    }

    internal void Remove(SharedOverlayItem item)
    {
        if (_items.Remove(item)) InvalidateVisual();
    }

    internal void Clear()
    {
        _items.Clear();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_items.Count == 0) return;
        var start = Stopwatch.GetTimestamp();
        foreach (var item in _items.OrderBy(i => i.Depth).ThenBy(i => i.ZIndex))
        {
            if (!item.ReadyToDraw) continue;
            IDisposable? clip = null;
            if (item.Parent is { Clip: true } parent)
            {
                var radius = ScaleRadius(parent.ClipRadius, parent.RestingSize, parent.Rect);
                clip = context.PushClip(new RoundedRect(parent.Rect, radius.TopLeft, radius.TopRight,
                    radius.BottomRight, radius.BottomLeft));
            }
            using (clip)
            {
                if (item.Source.Kind == SharedContentKind.Bounds && item.SourceBrush is not null && item.Fade < 1)
                {
                    using var opacity = context.PushOpacity(1 - item.Fade);
                    context.DrawRectangle(item.SourceBrush, null, item.Rect);
                }
                if (item.Source.Kind == SharedContentKind.Element || item.Fade > 0)
                {
                    using var opacity = context.PushOpacity(item.Source.Kind == SharedContentKind.Element ? 1 : item.Fade);
                    context.DrawRectangle(item.TargetBrush, null, item.Rect);
                }
            }
        }
        _renderMs += Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _renders++;
    }

    private static CornerRadius ScaleRadius(CornerRadius radius, Size resting, Rect rect)
    {
        var sx = resting.Width <= 0 ? 1 : rect.Width / resting.Width;
        var sy = resting.Height <= 0 ? 1 : rect.Height / resting.Height;
        return new CornerRadius(radius.TopLeft * sx, radius.TopRight * sx, radius.BottomRight * sy, radius.BottomLeft * sy);
    }
}
