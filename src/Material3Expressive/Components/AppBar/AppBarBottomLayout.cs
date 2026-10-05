/* BottomAppBar layout translated from fixed AndroidX AppBar.kt. Apache License 2.0. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

internal sealed class AppBarBottomChrome : Decorator
{
    internal BottomAppBar Owner => (BottomAppBar)TemplatedParent!;

    private BottomAppBarState? _state;
    private AppBarDirectDrag? _drag;
    private double _fullHeight;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ObserveState();
        _drag = new AppBarDirectDrag(
            this,
            () => !Owner.TouchExplorationEnabled && Owner.ScrollBehavior is { IsPinned: false } behavior
                ? behavior.State
                : null,
            (velocity, clock, token) => Owner.ScrollBehavior!.SettleAsync(velocity, clock, token),
            deltaSign: -1);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_state is { })
            _state.PropertyChanged -= StateChanged;
        _state = null;
        _drag?.Dispose();
        _drag = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateMeasure();

    private void ObserveState()
    {
        if (_state is { })
            _state.PropertyChanged -= StateChanged;
        _state = Owner.ScrollBehavior?.State;
        if (_state is { })
            _state.PropertyChanged += StateChanged;
    }

    internal void OwnerChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == BottomAppBar.ScrollBehaviorProperty)
            ObserveState();
        if (Child is MaterialSurface { Child: AppBarBottomRow row })
            row.InvalidateMeasure();
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = Owner;
        var scale = LayoutHelper.GetLayoutScale(this);
        if (owner.ScrollBehavior is DefaultBottomAppBarScrollBehavior defaults)
            defaults.AttachDefaults(owner);
        Child!.Measure(availableSize);
        _fullHeight = Child.DesiredSize.Height;
        var height = MathUtils.DipToPx((float)_fullHeight, (float)scale);
        if (_state is { } && !owner.TouchExplorationEnabled)
            _state.HeightOffsetLimit = -height;
        var offset = owner.TouchExplorationEnabled ? 0 : _state?.HeightOffset ?? 0;
        return new Size(Child.DesiredSize.Width, Math.Max(0, MathUtils.RoundToInt(height + offset)) / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // AppBar.kt's layout modifier shrinks the external size, placing the original
        // full-height Surface at (0,0). A floating bar may paint beyond its layout box;
        // a Scaffold docks that box to the window edge, which supplies the clipping.
        Child!.Arrange(new Rect(0, 0, finalSize.Width, _fullHeight));
        return finalSize;
    }
}

internal sealed class AppBarBottomRow : Panel
{
    private BottomAppBar Owner => (BottomAppBar)TemplatedParent!;
    private Control Actions => Children[0];

    private ContentPresenter Fab => (ContentPresenter)Children[1];
    private ContentPresenter Custom => (ContentPresenter)Children[2];

    private int _left, _right, _top, _bottom, _contentWidth, _contentHeight, _fabWidth, _fabHeight;
    private Thickness Padding => Owner.ContentPadding;

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = Owner;
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        if (!double.IsFinite(availableSize.Width))
            throw new InvalidOperationException("BottomAppBar requires bounded width.");
        var consumed = owner.GetValue(MaterialWindowInsets.ConsumedPaddingProperty);
        var insets = owner.WindowInsets;
        _left = Math.Max(0, Px(insets.Left) - Px(consumed.Left));
        _right = Math.Max(0, Px(insets.Right) - Px(consumed.Right));
        _top = Math.Max(0, Px(insets.Top) - Px(consumed.Top));
        _bottom = Math.Max(0, Px(insets.Bottom) - Px(consumed.Bottom));
        var width = Math.Max(0, Px(availableSize.Width) - _left - _right);
        // Both upstream variants use windowInsetsPadding().height().padding(): padding
        // occupies the requested row height, while window insets surround that row.
        var fullContentHeight = double.IsFinite(availableSize.Height)
            ? Math.Min(
                Px(owner.EffectiveExpandedHeight),
                Math.Max(0, Px(availableSize.Height) - _top - _bottom))
            : Px(owner.EffectiveExpandedHeight);
        _contentHeight = Math.Max(0, fullContentHeight - Px(Padding.Top) - Px(Padding.Bottom));
        _contentWidth = Math.Max(0, width - Px(Padding.Left) - Px(Padding.Right));
        var custom = owner.HasCustomContent;
        Actions.IsVisible = Fab.IsVisible = !custom;
        Custom.IsVisible = custom;
        Fab.Content = custom ? null : owner.FloatingActionButton;
        Fab.ContentTemplate = custom ? null : owner.FloatingActionButtonTemplate;
        Custom.Content = custom ? owner.CustomContent : null;
        Custom.ContentTemplate = custom ? owner.CustomContentTemplate : null;
        if (custom)
            Custom.Measure(new Size(_contentWidth / scale, _contentHeight / scale));
        else
        {
            Fab.Measure(new Size(
                Math.Max(0, _contentWidth - Px(12)) / scale,
                Math.Max(0, _contentHeight - Px(8)) / scale));
            _fabWidth = Px(Fab.DesiredSize.Width);
            _fabHeight = Px(Fab.DesiredSize.Height);
            var hasFab = Fab.Content is { } || Fab.ContentTemplate is { };
            if (!hasFab)
                _fabWidth = _fabHeight = 0;
            Actions.Measure(
                new Size(Math.Max(0, _contentWidth - (hasFab ? _fabWidth + Px(12) : 0)) / scale, _contentHeight / scale));
        }

        return new Size(availableSize.Width, (_top + fullContentHeight + _bottom) / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var owner = Owner;
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        var x = (FlowDirection == FlowDirection.RightToLeft ? _right : _left) + Px(Padding.Left);
        var y = _top + Px(Padding.Top);
        if (owner.HasCustomContent)
            Custom.Arrange(new Rect(x / scale, y / scale, _contentWidth / scale, _contentHeight / scale));
        else
        {
            var hasFab = Fab.Content is { } || Fab.ContentTemplate is { };
            var actionWidth = Math.Max(0, _contentWidth - (hasFab ? _fabWidth + Px(12) : 0));
            Actions.Arrange(new Rect(x / scale, y / scale, actionWidth / scale, _contentHeight / scale));
            if (hasFab)
                Fab.Arrange(new Rect((x + actionWidth) / scale, (y + Px(8)) / scale, _fabWidth / scale, _fabHeight / scale));
        }

        return finalSize;
    }
}

internal sealed class AppBarActionsPanel : StackPanel
{
    public AppBarActionsPanel() => Orientation = Orientation.Horizontal;

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = this.GetVisualAncestors()
            .OfType<BottomAppBar>()
            .FirstOrDefault();
        var scale = LayoutHelper.GetLayoutScale(this);
        var spacing = owner?.HorizontalArrangement == AppBarArrangement.SpacedBy ? MathUtils.DipToPx(owner.Spacing, scale) : 0;
        var maximum = double.IsFinite(availableSize.Width) ? MathUtils.DipToPx(availableSize.Width, scale) : int.MaxValue;
        var occupied = 0;
        var height = 0;
        var lastSpace = 0;
        // RowColumnMeasurePolicy: each unweighted child receives the remaining width,
        // including arrangement spacing, rather than StackPanel's unbounded width.
        foreach (var child in Children)
        {
            var remaining = maximum - occupied;
            child.Measure(
                new Size(
                    maximum == int.MaxValue ? double.PositiveInfinity : Math.Max(0, remaining) / scale,
                    availableSize.Height));
            var width = MathUtils.DipToPx(child.DesiredSize.Width, scale);
            lastSpace = Math.Min(spacing, Math.Max(0, remaining - width));
            occupied += width + lastSpace;
            height = Math.Max(height, MathUtils.DipToPx(child.DesiredSize.Height, scale));
        }

        return new Size(Math.Max(0, occupied - lastSpace) / scale, height / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var owner = this.GetVisualAncestors()
            .OfType<BottomAppBar>()
            .FirstOrDefault();
        var arrangement = owner?.HorizontalArrangement ?? AppBarArrangement.End;
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        var size = Px(finalSize.Width);
        var widths = Children.Select(child => Px(child.DesiredSize.Width)).ToArray();
        var free = size - widths.Sum();
        var count = Children.Count;
        var gap = arrangement switch
        {
            AppBarArrangement.SpaceBetween => count > 1 ? free / (float)(count - 1) : 0,
            AppBarArrangement.SpaceAround => count > 0 ? free / (float)count : 0,
            AppBarArrangement.SpaceEvenly => free / (float)(count + 1),
            AppBarArrangement.SpacedBy => Px(owner!.Spacing),
            _ => 0,
        };
        var current = arrangement switch
        {
            AppBarArrangement.Center => free / 2f,
            AppBarArrangement.End => free,
            AppBarArrangement.SpaceAround => gap / 2,
            AppBarArrangement.SpaceEvenly => gap,
            _ => 0,
        };
        for (var i = 0; i < count; i++)
        {
            if (arrangement == AppBarArrangement.SpacedBy)
                current = Math.Min(current, size - widths[i]);
            var height = Px(Children[i].DesiredSize.Height);
            Children[i].Arrange(
                new Rect(
                    MathUtils.RoundToInt(current) / scale,
                    MathUtils.RoundToInt((Px(finalSize.Height) - height) / 2f) / scale,
                    widths[i] / scale,
                    height / scale));
            current += widths[i] + (arrangement == AppBarArrangement.SpacedBy ? Math.Min(gap, size - current - widths[i]) : gap);
        }

        return finalSize;
    }
}
