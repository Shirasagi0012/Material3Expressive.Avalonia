using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// SegmentedButtonContentMeasurePolicy: reserve IconSize + 8 even when the icon is absent.
internal sealed class SegmentedButtonContentLayout : Panel
{
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<SegmentedButtonContentLayout, double>(nameof(IconSize));
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public static readonly StyledProperty<double> IconSpacingProperty = AvaloniaProperty.Register<SegmentedButtonContentLayout, double>(nameof(IconSpacing));
    public double IconSpacing { get => GetValue(IconSpacingProperty); set => SetValue(IconSpacingProperty, value); }
    public static readonly StyledProperty<double> ContentMinWidthProperty = AvaloniaProperty.Register<SegmentedButtonContentLayout, double>(nameof(ContentMinWidth));
    public double ContentMinWidth { get => GetValue(ContentMinWidthProperty); set => SetValue(ContentMinWidthProperty, value); }
    public static readonly StyledProperty<double> MinimumInteractiveSizeProperty = AvaloniaProperty.Register<SegmentedButtonContentLayout, double>(nameof(MinimumInteractiveSize));
    public double MinimumInteractiveSize { get => GetValue(MinimumInteractiveSizeProperty); set => SetValue(MinimumInteractiveSizeProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> OffsetAnimationProperty = AvaloniaProperty.Register<SegmentedButtonContentLayout, MaterialSpring?>(nameof(OffsetAnimation));
    public MaterialSpring? OffsetAnimation { get => GetValue(OffsetAnimationProperty); set => SetValue(OffsetAnimationProperty, value); }
    static SegmentedButtonContentLayout() => AffectsMeasure<SegmentedButtonContentLayout>(IconSizeProperty, IconSpacingProperty, ContentMinWidthProperty, MinimumInteractiveSizeProperty, OffsetAnimationProperty);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconSizeProperty || change.Property == IconSpacingProperty
            || change.Property == ContentMinWidthProperty || change.Property == MinimumInteractiveSizeProperty)
            (TemplatedParent as SegmentedButton)?.Row?.InvalidateItemsLayout();
    }

    private VisualAnimationScope? _motion;
    private Animatable<int>? _offset;
    private int? _initialOffset;
    private Size _icon, _label;
    private int _reservation;
    private int _contentWidth;
    private int _targetOffset;
    private bool _commitPending;
    internal void Stop() { _motion?.Dispose(); _motion = null; _offset = null; _initialOffset = null; _commitPending = false; }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { Stop(); base.OnDetachedFromVisualTree(e); }
    internal Size IntrinsicSize(double height, Thickness padding)
    {
        if (Children.Count != 2) return default;
        if (Children[1] is ContentPresenter presenter) presenter.ApplyTemplate();
        Children[1].Measure(new Size(double.PositiveInfinity, Math.Max(0, height - padding.Top - padding.Bottom)));
        // MultiContentMeasurePolicy's default intrinsic replays measure. The defaultMinSize
        // and interactive modifiers supply minima independently of the icon's visibility.
        Children[0].Measure(new Size(double.PositiveInfinity, Children[1].DesiredSize.Height));
        return new Size(Math.Max(ContentMinWidth, Math.Max(IconSize, Children[0].DesiredSize.Width) + IconSpacing + Children[1].DesiredSize.Width + padding.Left + padding.Right),
            Math.Max(MinimumInteractiveSize, Children[1].DesiredSize.Height + padding.Top + padding.Bottom));
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        var scale = LayoutHelper.GetLayoutScale(this);
        // Layout.height(IntrinsicSize.Min) derives height from content, then measures
        // both slots with that maximum. An 18dp icon can therefore be 16dp tall.
        Children[1].Measure(availableSize);
        var constrained = availableSize.WithHeight(Children[1].DesiredSize.Height);
        Children[0].Measure(constrained); Children[1].Measure(constrained);
        _icon = Children[0].DesiredSize; _label = Children[1].DesiredSize;
        var iconSize = MathUtils.DipToPx(IconSize, scale);
        _reservation = iconSize + MathUtils.DipToPx(IconSpacing, scale);
        var offset = _icon.Width == 0 ? -_reservation / 2 : 0;
        if (_initialOffset is null) _initialOffset = offset;
        else if (this.IsAttachedToVisualTree())
        {
            if (_motion is null)
            {
                _motion = VisualAnimationScope.Create(this);
                _offset = _motion.CreateAnimatable(_initialOffset.Value);
                _motion.SubscribeValue(_offset, _ => InvalidateArrange());
            }
            _targetOffset = offset;
            if (_offset!.TargetValue != offset && !_commitPending)
            {
                _commitPending = true;
                var motion = _motion; var anim = _offset;
                var defer = offset == 0;
                // Compose launches from measure after the current layout/recomposition.
                // Commit on the shared frame before Animatable requests its first sample.
                _ = motion.Launch(async ct =>
                {
                    if (defer) await motion.Clock.NextFrameAsync(ct);
                    _commitPending = false;
                    if (OffsetAnimation is not { } spring) return;
                    await anim.AnimateToAsync(_targetOffset,
                        new SpringSpec<int>(spring.DampingRatio, spring.Stiffness, 1), cancellationToken: ct);
                }, "Segmented label offset");
            }
        }
        _contentWidth = Math.Max(iconSize, MathUtils.DipToPx(_icon.Width, scale)) + MathUtils.DipToPx(IconSpacing, scale) + MathUtils.DipToPx(_label.Width, scale);
        return new Size(_contentWidth / scale, _label.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        var extra = finalSize.Width * scale - _contentWidth;
        var x = extra >= 0 ? MathUtils.RoundToInt((float)extra / 2) : (int)extra / 2;
        Children[0].Arrange(new Rect(x / scale, Math.Truncate((finalSize.Height - _icon.Height) * scale / 2) / scale, _icon.Width, _icon.Height));
        Children[1].Arrange(new Rect((x + _reservation + (_offset?.Value ?? _initialOffset ?? 0)) / scale,
            Math.Truncate((finalSize.Height - _label.Height) * scale / 2) / scale, _label.Width, _label.Height));
        return finalSize;
    }
}
