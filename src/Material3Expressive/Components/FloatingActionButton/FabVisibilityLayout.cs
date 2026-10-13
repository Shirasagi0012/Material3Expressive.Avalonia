using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class FabVisibilityLayout : Decorator
{
    private FloatingActionButton? _owner;
    private AnimationChildScope? _attachScope;
    private Animatable<float>? _scale, _alpha;
    private bool _pending, _needsTransition;
    private readonly ScaleTransform _transform = new();
    // RenderOptions are pushed after a visual's opacity. Put the layer policy on
    // the parent of the animated visual so its fill and shadow fade as one group.
    internal float Alpha => _alpha?.Value ?? (_owner?.AnimatedVisible != false ? 1 : 0);
    internal float ScaleProgress => _scale?.Value ?? (_owner?.AnimatedVisible != false ? 1 : 0);
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<FloatingActionButton>()!;
        var scope = _attachScope = AnimationScope.Get(this).CreateChild("FAB visibility");
        var initial = _owner.AnimatedVisible ? 1f : 0f;
        _scale = new Animatable<float>(new FloatConverter(), initial, VisibilityThresholds.Float);
        _alpha = new Animatable<float>(new FloatConverter(), initial, VisibilityThresholds.Float);
        scope.SubscribeValue(_scale, _ => InvalidateArrange());
        scope.SubscribeValue(_alpha, _ => InvalidateMeasure());
        _owner.PropertyChanged += OwnerChanged;
        InvalidateMeasure();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        _attachScope?.Dispose(); _attachScope = null;
        _scale = _alpha = null; _pending = _needsTransition = false;
        base.OnDetachedFromVisualTree(e);
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FloatingActionButton.AnimatedVisibleProperty)
            _needsTransition = true;
        if (e.Property == FloatingActionButton.AnimatedVisibleProperty ||
            e.Property == FloatingActionButton.VisibilityScaleSpecProperty ||
            e.Property == FloatingActionButton.VisibilityAlphaSpecProperty)
            QueueTransition();
        InvalidateArrange();
    }
    private void QueueTransition()
    {
        if (!_needsTransition || _pending || !this.IsAttachedToVisualTree() || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested ||
            _owner?.VisibilityScaleSpec is null || _owner.VisibilityAlphaSpec is null) return;
        _pending = true;
        _ = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct); _pending = false;
            if (_scale is null || _alpha is null) return;
            // A binding can become unavailable between the request and this frame.
            // Keep the request pending; never pass null to Animatable's default spring.
            if (_owner?.VisibilityScaleSpec is not { } scaleSpec ||
                _owner.VisibilityAlphaSpec is not { } alphaSpec) return;
            var target = _owner.AnimatedVisible ? 1f : 0f;
            _needsTransition = false;
            await Task.WhenAll(_scale!.AnimateToAsync(scope, target, scaleSpec, cancellationToken: ct),
                _alpha!.AnimateToAsync(scope, target, alphaSpec, cancellationToken: ct));
        }, "FAB visibility");
    }
    protected override Size MeasureOverride(Size available)
    {
        if (Alpha == 0) return default;
        Child?.Measure(available); return Child?.DesiredSize ?? default;
    }
    protected override Size ArrangeOverride(Size final)
    {
        if (Child is null || _owner is null) return final;
        RenderOptions.SetRequiresFullOpacityHandling(this, Alpha is > 0 and < 1);
        Child.Opacity = Math.Clamp(Alpha, 0, 1);
        Child.IsHitTestVisible = Alpha != 0;
        if (Alpha != 0) Child.Arrange(new Rect(final));
        var scale = MathUtils.Lerp(_owner.HiddenScale, 1f, ScaleProgress);
        _transform.ScaleX = _transform.ScaleY = scale;
        Child.RenderTransform = _transform;
        var density = LayoutHelper.GetLayoutScale(this);
        var aligned = _owner.VisibilityAlignment.ToPixels(final);
        // Alignment.align(IntSize(1,1), nodeSize): pivot is inset by one physical pixel.
        var x = final.Width == 0 ? 0 : aligned.X / final.Width;
        var y = final.Height == 0 ? 0 : aligned.Y / final.Height;
        Child.RenderTransformOrigin = new RelativePoint(x * Math.Max(0, final.Width - 1 / density), y * Math.Max(0, final.Height - 1 / density), RelativeUnit.Absolute);
        return final;
    }
}
