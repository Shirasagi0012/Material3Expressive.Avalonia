// SelectableDropdownMenuItem leading-icon AnimatedVisibility, and the single-group popup host.
// Copyright 2025-2026 The Android Open Source Project; Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation;
using ComposeAnimation.Runtime;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

internal sealed class ComboBoxItemLayout : Panel
{
    private const double Gap = 8;
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        var icon = Children[0];
        var text = Children[1];
        icon.Measure(availableSize);
        text.Measure(new Size(Math.Max(0, availableSize.Width - icon.DesiredSize.Width - Gap), availableSize.Height));
        return new Size(icon.DesiredSize.Width + Gap + text.DesiredSize.Width,
            Math.Max(icon.DesiredSize.Height, text.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var icon = Children[0];
        var text = Children[1];
        icon.Arrange(new Rect(0, (finalSize.Height - icon.DesiredSize.Height) / 2, icon.DesiredSize.Width, icon.DesiredSize.Height));
        var x = icon.DesiredSize.Width + Gap;
        text.Arrange(new Rect(x, (finalSize.Height - text.DesiredSize.Height) / 2, Math.Max(0, finalSize.Width - x), text.DesiredSize.Height));
        return finalSize;
    }
}

internal sealed class ComboBoxSelectionIcon : Decorator
{
    public static readonly StyledProperty<MaterialSpring?> SizeAnimationProperty = AvaloniaProperty.Register<ComboBoxSelectionIcon, MaterialSpring?>(nameof(SizeAnimation));
    public MaterialSpring? SizeAnimation { get => GetValue(SizeAnimationProperty); set => SetValue(SizeAnimationProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> AlphaAnimationProperty = AvaloniaProperty.Register<ComboBoxSelectionIcon, MaterialSpring?>(nameof(AlphaAnimation));
    public MaterialSpring? AlphaAnimation { get => GetValue(AlphaAnimationProperty); set => SetValue(AlphaAnimationProperty, value); }

    private ComboBoxItem? _owner;
    private AnimationChildScope? _attachScope;
    private Transition<bool>? _transition;
    private Transition<bool>.TransitionAnimationState<float>? _width;
    private bool _visible, _updatePending;
    private double _fullWidth;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = TemplatedParent as ComboBoxItem;
        if (_owner != null) _owner.PropertyChanged += OwnerChanged;
        _attachScope = AnimationScope.Get(this).CreateChild("ComboBox selection icon");
        Ensure();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner != null) _owner.PropertyChanged -= OwnerChanged;
        Stop();
        _attachScope?.Dispose();
        _attachScope = null;
        _owner = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeAnimationProperty || change.Property == AlphaAnimationProperty) Ensure();
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ComboBoxItem.IsSelectedProperty) Update(false);
    }

    private void Ensure()
    {
        if (_owner == null || !this.IsAttachedToVisualTree() || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested || SizeAnimation is null || AlphaAnimation is null || _transition != null) return;
        Child?.Measure(Size.Infinity);
        _fullWidth = MathUtils.DipToPx(Child?.DesiredSize.Width ?? 0, LayoutHelper.GetLayoutScale(this));
        _visible = _owner.IsSelected;
        _transition = new Transition<bool>(_visible, "ComboBox selection icon");
        _width = _transition.AnimateValue(new PixelWidthConverter(), WidthTarget,
            _ => new SpringSpec<float>(SizeAnimation.Value.DampingRatio, SizeAnimation.Value.Stiffness));
        var alpha = _transition.AnimateValue(new FloatConverter(), show => show ? 1f : 0f,
            _ => new SpringSpec<float>(AlphaAnimation.Value.DampingRatio, AlphaAnimation.Value.Stiffness, .01f));
        scope.SubscribeValue(_width, _ => InvalidateMeasure());
        scope.SubscribeValue(alpha, value => Opacity = Math.Clamp(value, 0, 1));
        Opacity = _visible ? 1 : 0;
        var transition = _transition;
        _ = scope.Launch(ct => transition.RunAsync(scope, ct));
        InvalidateMeasure();
    }

    private void Update(bool creating)
    {
        if (_owner == null || _transition == null || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested) { Ensure(); return; }
        var visible = _owner.IsSelected;
        if (_visible == visible || creating) return;
        _visible = visible;
        if (_updatePending) return;
        _updatePending = true;
        _ = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct);
            _updatePending = false;
            // A Get scope ends at the drain, not at detach: Stop may already have run.
            if (_transition is not { } transition) return;
            transition.UpdateTarget(_visible);
            InvalidateMeasure();
            (this.GetVisualParent() as Layoutable)?.InvalidateMeasure();
        });
    }

    private float WidthTarget(bool show) => show ? (float)_fullWidth : 0;
    private void Stop() { _transition = null; _width = null; _updatePending = false; }

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
        return new Size(_width is not null ? Math.Max(0, _width.Value) / density : _visible ? size.Width : 0, size.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var fullWidth = _fullWidth / LayoutHelper.GetLayoutScale(this);
        Child?.Arrange(new Rect(finalSize.Width - fullWidth, 0, fullWidth, finalSize.Height));
        return finalSize;
    }

    private sealed class PixelWidthConverter : ITwoWayConverter<float>
    {
        public int Dimension => 1;
        public void To(float value, Span<double> destination) => destination[0] = value;
        public float From(ReadOnlySpan<double> vector) => MathUtils.RoundToInt((float)vector[0]);
    }
}

internal sealed class ComboBoxPopupChrome : Panel
{
    private ComboBox? _owner;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = TemplatedParent as ComboBox;
        if (_owner == null) return;
        var popup = _owner.GetTemplateDescendants().OfType<Popup>().FirstOrDefault(p => p.Name == "PART_Popup");
        if (popup != null && !popup.Styles.OfType<Style>().Any(style => style.Setters.OfType<Setter>().Any(setter => setter.Property == Visual.ClipToBoundsProperty)))
        {
            popup.Styles.Add(new Style(s => s.OfType<OverlayPopupHost>()) { Setters = { new Setter(Visual.ClipToBoundsProperty, false) } });
            popup.Styles.Add(new Style(s => s.OfType<OverlayPopupHost>().Template().OfType<LayoutTransformControl>())
                { Setters = { new Setter(Visual.ClipToBoundsProperty, false) } });
        }
        _owner.ContainerPrepared += OnPrepared;
        _owner.ContainerIndexChanged += OnIndex;
        _owner.PropertyChanged += OwnerChanged;
        AssignAll();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner != null)
        {
            _owner.ContainerPrepared -= OnPrepared;
            _owner.ContainerIndexChanged -= OnIndex;
            _owner.PropertyChanged -= OwnerChanged;
        }
        _owner = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        // The panel raises this before the container is visible to ContainerFromIndex.
        Assign(e.Container, e.Index);
        AssignAll();
    }

    private void OnIndex(object? sender, ContainerIndexChangedEventArgs e)
    {
        Assign(e.Container, e.NewIndex);
        AssignAll();
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ItemsControl.ItemCountProperty || e.Property == ComboBox.IsDropDownOpenProperty) AssignAll();
    }

    private void Assign(Control? container, int index)
    {
        if (_owner != null && index >= 0 && container is ComboBoxItem item)
            MaterialComboBox.SetItemShapes(item, MenuDefaults.ItemShape(index, _owner.ItemCount));
    }

    private void AssignAll()
    {
        if (_owner == null) return;
        var count = _owner.ItemCount;
        for (var i = 0; i < count; i++)
            Assign(_owner.ContainerFromIndex(i), i);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size();
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            size = new Size(Math.Max(size.Width, child.DesiredSize.Width), Math.Max(size.Height, child.DesiredSize.Height));
        }
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children) child.Arrange(new Rect(finalSize));
        return finalSize;
    }
}
