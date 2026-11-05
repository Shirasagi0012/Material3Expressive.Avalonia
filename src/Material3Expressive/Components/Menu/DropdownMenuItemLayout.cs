/* Menu.kt: MenuArrangement and selected-leading-icon AnimatedVisibility.
 * Copyright 2025-2026 The Android Open Source Project; Apache-2.0. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
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
    public static readonly StyledProperty<double> PreferredMinWidthProperty =
        AvaloniaProperty.Register<DropdownMenuSlotPresenter, double>(nameof(PreferredMinWidth));
    public double PreferredMinWidth { get => GetValue(PreferredMinWidthProperty); set => SetValue(PreferredMinWidthProperty, value); }
    static DropdownMenuSlotPresenter() => AffectsMeasure<DropdownMenuSlotPresenter>(PreferredMinWidthProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return new(Math.Min(availableSize.Width, Math.Max(PreferredMinWidth, size.Width)), size.Height);
    }

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
    public static readonly StyledProperty<IDataTemplate?> NativeSelectedIconTemplateProperty = AvaloniaProperty.Register<DropdownMenuLeadingIcon, IDataTemplate?>(nameof(NativeSelectedIconTemplate));
    public IDataTemplate? NativeSelectedIconTemplate { get => GetValue(NativeSelectedIconTemplateProperty); set => SetValue(NativeSelectedIconTemplateProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> SizeAnimationProperty = AvaloniaProperty.Register<DropdownMenuLeadingIcon, MaterialSpring?>(nameof(SizeAnimation));
    public static readonly StyledProperty<MaterialSpring?> AlphaAnimationProperty = AvaloniaProperty.Register<DropdownMenuLeadingIcon, MaterialSpring?>(nameof(AlphaAnimation));
    public MaterialSpring? SizeAnimation { get => GetValue(SizeAnimationProperty); set => SetValue(SizeAnimationProperty, value); }
    public MaterialSpring? AlphaAnimation { get => GetValue(AlphaAnimationProperty); set => SetValue(AlphaAnimationProperty, value); }
    private DropdownMenuItem? _owner;
    private MenuItem? _nativeOwner;
    private AnimationChildScope? _animationScope;
    private Transition<bool>? _transition;
    private IDisposable? _opacityBinding;
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
        Refresh();
    }
    private void Refresh()
    {
        if (Presenter is not { } presenter) return;
        if (_owner is null && _nativeOwner is null)
        { Stop(); presenter.Content = null; presenter.ContentTemplate = null; presenter.UpdateChild(); return; }
        var expressive = _owner?.IsExpressive ?? true;
        var hasLeading = _owner?.HasLeading ?? _nativeOwner!.Icon is not null;
        var hasSelected = _owner?.HasSelectedLeading ?? (_nativeOwner!.ToggleType != MenuItemToggleType.None && NativeSelectedIconTemplate is not null);
        var selected = _owner?.Selected ?? _nativeOwner!.IsChecked;
        var selectedIcon = expressive && hasSelected && selected;
        _animated = expressive && !hasLeading && hasSelected;
        // Retain the selected icon through its exit, exactly as AnimatedVisibility does.
        var retained = _transition is { } running && (running.CurrentState || running.TargetState || running.IsRunning);
        var showSelected = selectedIcon || _animated && retained;
        presenter.Content = showSelected ? _owner?.SelectedLeadingIcon : _owner is { } owner ? owner.LeadingIcon : _nativeOwner!.Icon;
        presenter.ContentTemplate = showSelected ? _owner is { } selectedOwner ? selectedOwner.SelectedLeadingIconTemplate : NativeSelectedIconTemplate : _owner?.LeadingIconTemplate;
        presenter.UpdateChild();
        var visible = hasLeading || expressive && hasSelected && selected;
        IsVisible = hasLeading || expressive && hasSelected;
        if (!_animated)
        { Stop(); _visible = visible; }
        else if (_transition is null && this.IsAttachedToVisualTree() && SizeAnimation is not null && AlphaAnimation is not null)
        {
            var scope = _animationScope = AnimationScope.Get(this).CreateChild("Menu leading icon");
            _visible = visible;
            presenter.Measure(Size.Infinity);
            _fullWidth = MathUtils.DipToPx(presenter.DesiredSize.Width, LayoutHelper.GetLayoutScale(this));
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
            _opacityBinding = Bind(OpacityProperty, new Binding(nameof(alpha.Value))
            {
                Source = alpha, Priority = BindingPriority.Animation,
                Converter = new FuncValueConverter<float, double>(value => Math.Clamp(value, 0, 1))
            });
            var transition = _transition;
            _ = scope.Launch(ct => transition.RunAsync(scope, ct));
        }
        else if (_transition is not null && _visible != visible)
        {
            _visible = visible;
            if (!_updatePending && this.IsAttachedToVisualTree() && _animationScope is { } active
                && !active.Lifetime.IsCancellationRequested)
            {
                _updatePending = true;
                // AnimatedVisibility observes selected in composition; its newly started
                // animation first receives a frame in the following broadcast batch.
                _ = active.Launch(async ct =>
                {
                    await active.NextFrameAsync(ct);
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
    private void Stop()
    {
        var transition = _transition;
        _transition = null; _width = null; _updatePending = false;
        // Adding a regular icon removes AnimatedVisibility from the upstream composition.
        // Its frame loop, queued target and subscriptions must end before static content takes over.
        _animationScope?.Dispose(); _animationScope = null;
        transition?.Dispose();
        _opacityBinding?.Dispose(); _opacityBinding = null;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _nativeOwner = TemplatedParent as MenuItem;
        if (_nativeOwner is not null) _nativeOwner.PropertyChanged += NativePropertyChanged;
        Refresh();
        AnimationScope.AddScopeChangedHandler(this, ScopeRebuilt);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_nativeOwner is not null) _nativeOwner.PropertyChanged -= NativePropertyChanged;
        AnimationScope.RemoveScopeChangedHandler(this, ScopeRebuilt);
        _nativeOwner = null;
        Stop(); base.OnDetachedFromVisualTree(e);
    }
    private void ScopeRebuilt(object? sender, AnimationScopeChangedEventArgs e)
    {
        if (e.NewScope is not { } scope || !this.IsAttachedToVisualTree() || _transition is not { } transition) return;
        _animationScope?.Dispose();
        var motion = _animationScope = scope.CreateChild("Menu leading icon");
        _updatePending = false;
        if (_width is { } width) motion.SubscribeValue(width, _ => InvalidateMeasure());
        transition.UpdateTarget(_visible);
        _ = motion.Launch(ct => transition.RunAsync(motion, ct));
    }
    private void NativePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MenuItem.IsCheckedProperty || change.Property == MenuItem.IconProperty || change.Property == MenuItem.ToggleTypeProperty)
            Refresh();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeAnimationProperty || change.Property == AlphaAnimationProperty || change.Property == NativeSelectedIconTemplateProperty) Refresh();
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
        // EnterExitTransition measures Alignment.End with LayoutDirection.Ltr even
        // in RTL. Counter the inherited mirror so the reveal stays at physical right.
        Child?.Arrange(new Rect(_animated && FlowDirection != FlowDirection.RightToLeft ? finalSize.Width - fullWidth : 0, 0,
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
