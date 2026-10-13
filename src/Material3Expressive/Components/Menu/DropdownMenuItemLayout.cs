/* Menu.kt: MenuArrangement and selected-leading-icon AnimatedVisibility.
 * Copyright 2025-2026 The Android Open Source Project; Apache-2.0. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// Compose Box(defaultMinSize) does not propagate its minimum to its content. Avalonia's
// ContentPresenter uses its own DesiredSize (including MinWidth) when arranging even
// non-stretched content, so small icons need their measured child size here.
internal sealed class DropdownMenuSlotPresenter : ContentPresenter
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            var size = child.DesiredSize.Constrain(finalSize);
            var x = HorizontalContentAlignment == HorizontalAlignment.Center ? (finalSize.Width - size.Width) / 2 : 0;
            child.Arrange(new Rect(new Point(x, 0), size));
        }
        return finalSize;
    }
}

internal sealed class DropdownMenuItemLayout : Panel
{
    private DropdownMenuItem? _owner;
    internal void SetOwner(DropdownMenuItem? owner) { _owner = owner; Refresh(); }
    internal void Refresh()
    {
        if (Children.Count != 3) return;
        if (Children[0] is DropdownMenuLeadingIcon icon) icon.Update(_owner);
        Children[2].IsVisible = _owner?.HasTrailing == true;
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_owner is not { } owner || Children.Count != 3) return default;
        var leading = Children[0]; var text = Children[1]; var trailing = Children[2];
        var leadingSpace = owner.HasLeading || owner.HasSelectedLeading ? owner.LeadingSpacing : 0;
        var trailingSpace = owner.HasTrailing ? owner.TrailingSpacing : 0;
        leading.Measure(availableSize);
        var remaining = Math.Max(0, availableSize.Width - leading.DesiredSize.Width - leadingSpace - trailingSpace);
        if (owner.IsExpressive)
        {
            // The fixed expressive Row has no weight: measure leading, label, then trailing.
            text.Measure(new(remaining, availableSize.Height));
            trailing.Measure(new(Math.Max(0, remaining - text.DesiredSize.Width), availableSize.Height));
        }
        else
        {
            trailing.Measure(new(remaining, availableSize.Height));
            text.Measure(new(Math.Max(0, remaining - trailing.DesiredSize.Width), availableSize.Height));
        }
        // Menu's Column.width(IntrinsicSize.Max) queries AnimatedVisibility's full
        // content width, while Row placement uses its animated width. Avalonia has
        // no intrinsic pass, so propagate that width through the popup's measure.
        var intrinsicLeading = leading is DropdownMenuLeadingIcon icon && this.FindAncestorOfType<DropdownMenuPresenter>() is not null
            ? icon.IntrinsicWidth : leading.DesiredSize.Width;
        return new(intrinsicLeading + text.DesiredSize.Width + trailing.DesiredSize.Width + leadingSpace + trailingSpace,
            Math.Max(text.DesiredSize.Height, Math.Max(leading.DesiredSize.Height, trailing.DesiredSize.Height)));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_owner is not { } owner || Children.Count != 3) return finalSize;
        var leading = Children[0]; var text = Children[1]; var trailing = Children[2];
        var leadingSpace = owner.HasLeading || owner.HasSelectedLeading ? owner.LeadingSpacing : 0;
        var trailingSpace = owner.HasTrailing ? owner.TrailingSpacing : 0;
        var labelX = leading.DesiredSize.Width + leadingSpace;
        var labelWidth = owner.IsExpressive ? text.DesiredSize.Width
            : Math.Max(0, finalSize.Width - labelX - trailing.DesiredSize.Width - trailingSpace);
        Place(leading, 0, leading.DesiredSize.Width);
        Place(text, labelX, labelWidth);
        Place(trailing, finalSize.Width - trailing.DesiredSize.Width, trailing.DesiredSize.Width);
        return finalSize;
        void Place(Control child, double x, double width) => child.Arrange(new Rect(x,
            (finalSize.Height - child.DesiredSize.Height) / 2, width, child.DesiredSize.Height));
    }
}

internal sealed class DropdownMenuLeadingIcon : Decorator
{
    public static readonly StyledProperty<MaterialSpring?> SizeAnimationProperty = AvaloniaProperty.Register<DropdownMenuLeadingIcon, MaterialSpring?>(nameof(SizeAnimation));
    public static readonly StyledProperty<MaterialSpring?> AlphaAnimationProperty = AvaloniaProperty.Register<DropdownMenuLeadingIcon, MaterialSpring?>(nameof(AlphaAnimation));
    public MaterialSpring? SizeAnimation { get => GetValue(SizeAnimationProperty); set => SetValue(SizeAnimationProperty, value); }
    public MaterialSpring? AlphaAnimation { get => GetValue(AlphaAnimationProperty); set => SetValue(AlphaAnimationProperty, value); }
    private DropdownMenuItem? _owner;
    private Transition<bool>? _transition;
    private Transition<bool>.TransitionAnimationState<float>? _width;
    private bool _visible, _animated, _updatePending;
    private double _fullWidth;
    internal double IntrinsicWidth => _animated
        ? _transition is { } transition && (transition.CurrentState || transition.TargetState || transition.IsRunning)
            ? _fullWidth / LayoutHelper.GetLayoutScale(this) : 0
        : DesiredSize.Width;
    private ContentPresenter? Presenter => Child as ContentPresenter;

    internal void Update(DropdownMenuItem? owner)
    {
        _owner = owner;
        if (Presenter is not { } presenter) return;
        if (owner is null)
        { Stop(); presenter.Content = null; presenter.ContentTemplate = null; presenter.UpdateChild(); return; }
        var selectedIcon = owner.IsExpressive && owner.HasSelectedLeading && owner.Selected;
        _animated = owner.IsExpressive && !owner.HasLeading && owner.HasSelectedLeading;
        // Retain the selected icon through its exit, exactly as AnimatedVisibility does.
        var retained = _transition is { } running && (running.CurrentState || running.TargetState || running.IsRunning);
        var showSelected = selectedIcon || _animated && retained;
        presenter.Content = showSelected ? owner.SelectedLeadingIcon : owner.LeadingIcon;
        presenter.ContentTemplate = showSelected ? owner.SelectedLeadingIconTemplate : owner.LeadingIconTemplate;
        presenter.UpdateChild();
        var visible = owner.HasLeading || owner.IsExpressive && owner.HasSelectedLeading && owner.Selected;
        IsVisible = owner.HasLeading || owner.IsExpressive && owner.HasSelectedLeading;
        if (!_animated)
        { Stop(); _visible = visible; Opacity = visible ? 1 : 0; }
        else if (_transition is null && this.IsAttachedToVisualTree() && SizeAnimation is not null && AlphaAnimation is not null)
        {
            _visible = visible;
            presenter.Measure(Size.Infinity);
            _fullWidth = MathUtils.DipToPx(presenter.DesiredSize.Width, LayoutHelper.GetLayoutScale(this));
            var scope = AnimationScope.Get(this);
            _transition = new Transition<bool>(visible, "Menu leading icon");
            _transition.PropertyChanged += (_, _) =>
            {
                if (_transition is { CurrentState: false, TargetState: false, IsRunning: false })
                { presenter.Content = null; presenter.ContentTemplate = null; presenter.UpdateChild(); }
                InvalidateIntrinsicMeasure();
            };
            _width = _transition.AnimateValue(new PixelWidthConverter(), WidthTarget,
                // MotionScheme.fastSpatialSpec<IntSize>() leaves visibilityThreshold null.
                // Its vector spring uses .01 pixels, not IntSize.VisibilityThreshold (1).
                _ => new SpringSpec<float>(SizeAnimation.Value.DampingRatio, SizeAnimation.Value.Stiffness));
            var alpha = _transition.AnimateValue(new FloatConverter(), show => show ? 1f : 0f,
                _ => new SpringSpec<float>(AlphaAnimation.Value.DampingRatio, AlphaAnimation.Value.Stiffness, .01f));
            scope.SubscribeValue(_width, _ => InvalidateMeasure());
            scope.SubscribeValue(alpha, value => Opacity = Math.Clamp(value, 0, 1));
            Opacity = visible ? 1 : 0;
            var transition = _transition;
            _ = scope.Launch(ct => transition.RunAsync(scope, ct));
        }
        else if (_transition is not null && _visible != visible)
        {
            _visible = visible;
            if (!_updatePending && this.IsAttachedToVisualTree())
            {
                _updatePending = true;
                var scope = AnimationScope.Get(this);
                // AnimatedVisibility observes selected in composition; its newly started
                // animation first receives a frame in the following broadcast batch.
                _ = scope.Launch(async ct =>
                {
                    await scope.NextFrameAsync(ct);
                    _updatePending = false;
                    // A Get scope ends at the drain, not at detach: Stop may already have run.
                    if (_transition is not { } transition) return;
                    transition.UpdateTarget(_visible);
                    InvalidateIntrinsicMeasure();
                });
            }
        }
        InvalidateMeasure();
    }
    private float WidthTarget(bool show) => show ? (float)_fullWidth : 0;
    private void InvalidateIntrinsicMeasure()
    {
        InvalidateMeasure();
        // The animation can still measure zero when its full intrinsic width changes.
        // Avalonia normally remeasures a parent only when DesiredSize changes.
        (this.GetVisualParent() as Layoutable)?.InvalidateMeasure();
    }
    private void Stop() { _transition = null; _width = null; _updatePending = false; }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); Update(_owner); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { Stop(); base.OnDetachedFromVisualTree(e); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeAnimationProperty || change.Property == AlphaAnimationProperty) Update(_owner);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        var size = Child?.DesiredSize ?? default;
        var density = LayoutHelper.GetLayoutScale(this);
        var fullWidth = MathUtils.DipToPx(size.Width, density);
        if (_fullWidth != fullWidth)
        {
            _fullWidth = fullWidth;
            if (SizeAnimation is { } spec)
                _width?.Update(WidthTarget, _ => new SpringSpec<float>(spec.DampingRatio, spec.Stiffness));
        }
        return new(_animated && _width is not null ? Math.Max(0, _width.Value) / density : _visible ? size.Width : 0, size.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var fullWidth = _fullWidth / LayoutHelper.GetLayoutScale(this);
        // expandHorizontally / shrinkHorizontally default to Alignment.End.
        Child?.Arrange(new Rect(_animated ? finalSize.Width - fullWidth : 0, 0,
            _animated ? fullWidth : finalSize.Width, finalSize.Height));
        return finalSize;
    }
    private sealed class PixelWidthConverter : ITwoWayConverter<float>
    {
        public int Dimension => 1;
        public void To(float value, Span<double> destination) => destination[0] = value;
        public float From(ReadOnlySpan<double> vector) => MathUtils.RoundToInt((float)vector[0]);
    }
}
