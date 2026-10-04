using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// SegmentedButtonContentMeasurePolicy: reserve IconSize + 8 even when the icon is absent.
internal sealed class SegmentedButtonContentLayout : Panel
{
    private VisualAnimationScope? _motion;
    private Animatable<int>? _offset;
    private int? _initialOffset;
    private Size _icon, _label;
    private int _reservation;
    private int _contentWidth;
    private int _targetOffset;
    private bool _commitPending;
    internal SegmentedButton Owner { get; set; } = null!;
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
        return new Size(Math.Max(58, Math.Max(Owner.Resolve(Comp.SegmentedButton.IconSize), Children[0].DesiredSize.Width) + 8 + Children[1].DesiredSize.Width + padding.Left + padding.Right),
            Math.Max(48, Children[1].DesiredSize.Height + padding.Top + padding.Bottom));
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
        var iconSize = MathUtils.DipToPx(Owner.Resolve(Comp.SegmentedButton.IconSize), scale);
        _reservation = iconSize + MathUtils.DipToPx(8, scale);
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
                    var spring = Owner.Resolve(Sys.Motion.Spring.SpatialFast);
                    await anim.AnimateToAsync(_targetOffset,
                        new SpringSpec<int>(spring.DampingRatio, spring.Stiffness, 1), cancellationToken: ct);
                }, "Segmented label offset");
            }
        }
        _contentWidth = Math.Max(iconSize, MathUtils.DipToPx(_icon.Width, scale)) + MathUtils.DipToPx(8, scale) + MathUtils.DipToPx(_label.Width, scale);
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
