using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Motion;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// FloatingActionButton.kt: classic AnimatedVisibility versus expressive updateTransition.
// The icon and label presenters remain ordinary XAML content slots.
internal sealed class ExtendedFabLayout : Panel
{
    public static readonly StyledProperty<double> MinimumSizeProperty = AvaloniaProperty.Register<ExtendedFabLayout, double>(nameof(MinimumSize), 56);
    public static readonly StyledProperty<double> LeadingSpaceProperty = AvaloniaProperty.Register<ExtendedFabLayout, double>(nameof(LeadingSpace), 20);
    public static readonly StyledProperty<double> TrailingSpaceProperty = AvaloniaProperty.Register<ExtendedFabLayout, double>(nameof(TrailingSpace), 20);
    public static readonly StyledProperty<double> IconLabelSpaceProperty = AvaloniaProperty.Register<ExtendedFabLayout, double>(nameof(IconLabelSpace), 12);
    public double MinimumSize { get => GetValue(MinimumSizeProperty); set => SetValue(MinimumSizeProperty, value); }
    public double LeadingSpace { get => GetValue(LeadingSpaceProperty); set => SetValue(LeadingSpaceProperty, value); }
    public double TrailingSpace { get => GetValue(TrailingSpaceProperty); set => SetValue(TrailingSpaceProperty, value); }
    public double IconLabelSpace { get => GetValue(IconLabelSpaceProperty); set => SetValue(IconLabelSpaceProperty, value); }
    private ExtendedFloatingActionButton? _owner;
    private VisualAnimationScope? _scope;
    private Animatable<float>? _width, _alpha;
    private Animatable<float>? _classicWidth;
    private int _labelExtentPx;
    private bool _fullyCollapsed;
    private bool _classicCurrentExpanded;
    private bool _pending;
    private double _iconWidth, _labelWidth, _rowWidth, _rowHeight;
    internal float WidthProgress => _width?.Value ?? (_owner?.Expanded != false ? 1 : 0);
    internal float LabelAlpha => _alpha?.Value ?? (_owner?.Expanded != false ? 1 : 0);
    static ExtendedFabLayout() => AffectsMeasure<ExtendedFabLayout>(MinimumSizeProperty, LeadingSpaceProperty, TrailingSpaceProperty, IconLabelSpaceProperty);
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<ExtendedFloatingActionButton>()!;
        _scope = VisualAnimationScope.Create(this);
        var initial = _owner.Expanded ? 1f : 0f;
        _fullyCollapsed = initial == 0;
        _classicCurrentExpanded = _owner.Expanded;
        _width = _scope.CreateAnimatable(initial);
        _alpha = _scope.CreateAnimatable(initial);
        _scope.SubscribeValue(_width, _ => InvalidateMeasure());
        _scope.SubscribeValue(_alpha, _ => { InvalidateMeasure(); InvalidateVisual(); });
        _owner.PropertyChanged += OwnerChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        _scope?.Dispose(); _scope = null; _width = _alpha = _classicWidth = null; _pending = false;
        base.OnDetachedFromVisualTree(e);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ExtendedFloatingActionButton.ExpandedProperty && _scope is { IsEnding: false } scope)
        {
            _fullyCollapsed = false;
            InvalidateMeasure();
            if (_pending) return;
            _pending = true;
            _ = scope.Launch(async ct =>
            {
                await scope.Clock.NextFrameAsync(ct);
                _pending = false;
                var expanded = _owner!.Expanded;
                var classic = _owner.Size == FloatingActionButtonSize.Default;
                // AnimatedVisibility's Visible -> Visible interrupted segment falls back
                // to DefaultSizeAnimationSpec/DefaultAlphaSpring, not its enter spec.
                var returningToVisible = classic && expanded && _classicCurrentExpanded && _classicWidth?.IsRunning == true;
                var widthSpec = FabMotion.Spring(this, classic && !expanded ? Sys.Motion.Spring.SpatialDefault : Sys.Motion.Spring.SpatialFast,
                    classic && !expanded ? MotionScheme.Expressive.DefaultSpatial : MotionScheme.Expressive.FastSpatial);
                var alphaSpec = FabMotion.Spring(this, classic && expanded ? Sys.Motion.Spring.EffectsDefault : Sys.Motion.Spring.EffectsFast,
                    classic && expanded ? MotionScheme.Expressive.DefaultEffects : MotionScheme.Expressive.FastEffects);
                var widthAnimation = classic && _classicWidth is not null
                    ? _classicWidth.AnimateToAsync(expanded ? _labelExtentPx : 0,
                        returningToVisible ? new SpringSpec<float>(stiffness: 400, visibilityThreshold: 1)
                            : FabMotion.Spring(this, expanded ? Sys.Motion.Spring.SpatialFast : Sys.Motion.Spring.SpatialDefault,
                                expanded ? MotionScheme.Expressive.FastSpatial : MotionScheme.Expressive.DefaultSpatial, 1), cancellationToken: ct)
                    : _width!.AnimateToAsync(expanded ? 1 : 0, widthSpec, cancellationToken: ct);
                await Task.WhenAll(widthAnimation,
                    _alpha!.AnimateToAsync(expanded ? 1 : 0, returningToVisible ? new SpringSpec<float>(stiffness: 400, visibilityThreshold: 0.01f) : alphaSpec, cancellationToken: ct));
                _classicCurrentExpanded = expanded;
                _fullyCollapsed = !_owner.Expanded && (classic ? _classicWidth?.Value == 0 : _width!.Value == 0);
                InvalidateMeasure();
            }, "Extended FAB transition");
        }
        else if (e.Property == FloatingActionButton.IconProperty || e.Property == FloatingActionButton.IconTemplateProperty || e.Property == FloatingActionButton.SizeProperty) InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        if (Children.Count < 2) return default;
        var icon = Children[0]; var label = Children[1];
        var hasIcon = _owner?.HasIcon == true;
        icon.IsVisible = hasIcon;
        label.IsVisible = !hasIcon || !_fullyCollapsed;
        var classic = _owner?.Size == FloatingActionButtonSize.Default;
        var expanded = _owner?.Expanded != false;
        var start = classic && hasIcon ? (expanded ? 16 : 0) : LeadingSpace;
        var end = classic && hasIcon ? (expanded ? 20 : 0) : TrailingSpace;
        var gap = _owner?.Size switch { FloatingActionButtonSize.Medium => 12, FloatingActionButtonSize.Large => 16, _ => IconLabelSpace };
        if (label is FabLabelClip clip) clip.Gap = hasIcon ? gap : 0;
        icon.Measure(new Size(Math.Max(0, available.Width - start - end), available.Height));
        _iconWidth = hasIcon ? icon.DesiredSize.Width : 0;
        label.Measure(new Size(Math.Max(0, available.Width - start - end - _iconWidth), available.Height));
        _labelWidth = label.DesiredSize.Width;
        _rowHeight = Math.Max(MinimumSize, Math.Max(icon.DesiredSize.Height, label.DesiredSize.Height));
        var scale = LayoutHelper.GetLayoutScale(this);
        var labelExtent = _labelWidth;
        _labelExtentPx = MathUtils.DipToPx(labelExtent, scale);
        if (classic && hasIcon && _classicWidth is null && _scope is { } scope)
        {
            _classicWidth = scope.CreateAnimatable(expanded ? (float)_labelExtentPx : 0, new ClassicWidthConverter(), 1);
            scope.SubscribeValue(_classicWidth, _ => InvalidateMeasure());
        }
        if (!hasIcon)
            _rowWidth = Math.Max(classic ? 80 : MinimumSize, start + end + _labelWidth);
        else if (classic)
            _rowWidth = Math.Max(expanded ? 80 : 56, start + end + _iconWidth + MathUtils.PxToDip(MathUtils.RoundToInt(Math.Max(0, _classicWidth?.Value ?? 0)), scale));
        else
        {
            var expandedWidth = Math.Max(MinimumSize, start + end + _iconWidth + labelExtent);
            _rowWidth = MathUtils.PxToDip(MathUtils.LerpRoundToInt(MathUtils.DipToPx(MinimumSize, scale), MathUtils.DipToPx(expandedWidth, scale), WidthProgress), scale);
        }
        return new Size(Math.Min(_rowWidth, available.Width), Math.Min(_rowHeight, available.Height));
    }
    protected override Size ArrangeOverride(Size final)
    {
        if (Children.Count < 2) return final;
        var hasIcon = _owner?.HasIcon == true;
        var classic = _owner?.Size == FloatingActionButtonSize.Default;
        var expanded = _owner?.Expanded != false;
        var gap = _owner?.Size switch { FloatingActionButtonSize.Medium => 12, FloatingActionButtonSize.Large => 16, _ => IconLabelSpace };
        var labelWidth = classic && hasIcon ? MathUtils.PxToDip(MathUtils.RoundToInt(Math.Max(0, _classicWidth?.Value ?? 0)), LayoutHelper.GetLayoutScale(this)) : _labelWidth;
        var x = classic && hasIcon ? (expanded ? 16 : (final.Width - _iconWidth - labelWidth) / 2) : LeadingSpace;
        if (!hasIcon) x = (final.Width - _labelWidth) / 2;
        var density = LayoutHelper.GetLayoutScale(this);
        x = MathUtils.RoundToInt(x * density) / density;
        // Avalonia mirrors the visual subtree for FlowDirection; use logical positions.
        Children[0].Arrange(new Rect(x, (final.Height - Children[0].DesiredSize.Height) / 2, _iconWidth, Children[0].DesiredSize.Height));
        var labelX = hasIcon ? x + _iconWidth : x;
        Children[1].Opacity = hasIcon ? Math.Clamp(LabelAlpha, 0, 1) : 1;
        Children[1].Arrange(new Rect(labelX, (final.Height - Children[1].DesiredSize.Height) / 2, labelWidth, Children[1].DesiredSize.Height));
        return final;
    }

    // IntSize.VectorConverter rounds the visible width and uses that rounded value
    // as the initial value of an interrupted Transition; velocity stays in vector space.
    private sealed class ClassicWidthConverter : ITwoWayConverter<float>
    {
        public int Dimension => 1;
        public void To(float value, Span<double> destination) => destination[0] = value;
        public float From(ReadOnlySpan<double> vector) => Math.Max(0, MathUtils.RoundToInt((float)vector[0]));
    }
}

// AnimatedVisibility clips its complete spacer + label row to the animated width.
internal sealed class FabLabelClip : Decorator
{
    internal double Gap { get; set; }
    private double _contentWidth;
    public FabLabelClip() => ClipToBounds = true;
    protected override Size MeasureOverride(Size available)
    {
        Child?.Measure(new Size(Math.Max(0, available.Width - Gap), available.Height));
        var size = Child?.DesiredSize ?? default; _contentWidth = size.Width;
        return new Size(size.Width + Gap, size.Height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        Child?.Arrange(new Rect(Gap, 0, _contentWidth, final.Height));
        return final;
    }
}
