using System;
using Avalonia;
using Avalonia.Layout;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

// TabSamples.kt / FancyAnimatedIndicatorWithModifier: the leading edge moves first.
public sealed class ElasticTabIndicator : TabIndicatorLayout
{
    private AnimationChildScope? _scope;
    private Animatable<float>? _start, _end;
    private float _startTarget, _endTarget;
    private double _left, _width;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scope = AnimationScope.Get(this).CreateChild("Custom elastic tab indicator");
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _scope?.Dispose(); _scope = null; _start = _end = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override Size MeasureIndicator(Size availableSize)
    {
        if (SelectedIndex < 0 || SelectedIndex >= TabPositions.Count) return default;
        var position = TabPositions[SelectedIndex];
        if (_start is null && _scope is { } scope)
        {
            _start = new Animatable<float>(new FloatConverter(), _startTarget = (float)position.Left, .01f);
            _end = new Animatable<float>(new FloatConverter(), _endTarget = (float)position.Right, .01f);
            scope.SubscribeValue(_start, _ => InvalidateMeasure());
            scope.SubscribeValue(_end, _ => InvalidateMeasure());
        }
        if (_scope is { } motion && _start is { } start && _end is { } end)
        {
            Animate(end, (float)position.Right, ref _endTarget, true);
            Animate(start, (float)position.Left, ref _startTarget, false);

            void Animate(Animatable<float> value, float target, ref float requested, bool trailing)
            {
                if (requested == target) return;
                requested = target;
                _ = motion.Launch(async ct =>
                {
                    if (target != (trailing ? _endTarget : _startTarget)) return;
                    var stiffness = (value.TargetValue < target) == trailing ? 1000f : 50f;
                    await value.AnimateToAsync(motion, target, new SpringSpec<float>(1, stiffness, .01f), cancellationToken: ct);
                }, "Elastic indicator edge");
            }
        }
        var density = (float)LayoutHelper.GetLayoutScale(this);
        int Px(double value) => (int)MathF.Floor((float)value * density + .5f);
        var startPx = Px(_start?.Value ?? position.Left);
        var endPx = Px(_end?.Value ?? position.Right);
        _left = (startPx - (IsScrollable ? (Px(position.Width) - Px(position.ContentWidth)) / 2 : 0)) / (double)density;
        _width = (endPx - startPx) / (double)density;
        Child?.Measure(new Size(_width, availableSize.Height));
        return availableSize;
    }
    protected override Size ArrangeIndicator(Size finalSize)
    {
        Child?.Arrange(new Rect(_left, 0, _width, finalSize.Height));
        return finalSize;
    }
}
