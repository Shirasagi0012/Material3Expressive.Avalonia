using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace ComposeAnimation;

/// <summary>Measures full content and applies a persistent Motion to this particular visual tree.</summary>
internal sealed class EnterExitLayout : Control, ILookaheadLayoutPolicy
{
    private readonly MatrixTransform _transform = new();
    private readonly AnimationSampleObservable<double> _opacitySamples = new(1);
    private readonly AnimationSampleObservable<Matrix> _transformSamples = new(Matrix.Identity);
    private IDisposable? _opacityContribution;
    private IDisposable? _transformContribution;
    internal EnterExitSurface Surface { get; } = new();
    internal EnterExitMotion? Motion { get; private set; }
    internal Size MeasuredContentSize { get; private set; }

    internal EnterExitLayout()
    {
        LookaheadLayout.RegisterInherited(this);
        VisualChildren.Add(Surface);
        LogicalChildren.Add(Surface);
    }

    internal void SetMotion(EnterExitMotion? motion)
    {
        if (ReferenceEquals(Motion, motion)) return;
        if (Motion is { } previous)
        {
            previous.MeasureInvalidated -= InvalidateMeasure;
            previous.ArrangeInvalidated -= InvalidateArrange;
            previous.VisualInvalidated -= ApplyVisuals;
        }

        _opacityContribution?.Dispose();
        _transformContribution?.Dispose();
        _opacityContribution = null;
        _transformContribution = null;
        Motion = motion;
        if (motion is { })
        {
            motion.MeasureInvalidated += InvalidateMeasure;
            motion.ArrangeInvalidated += InvalidateArrange;
            motion.VisualInvalidated += ApplyVisuals;

            _opacitySamples.Publish(motion.Opacity);
            _transformSamples.Publish(Matrix.Identity);

            _opacityContribution = Surface.Bind(
                OpacityProperty,
                _opacitySamples,
                BindingPriority.Animation);
            _transformContribution = _transform.Bind(
                MatrixTransform.MatrixProperty,
                _transformSamples,
                BindingPriority.Animation);
        }

        Surface.RenderTransform = motion is null ? null : _transform;
        Surface.RenderTransformOrigin = motion is null
            ? RelativePoint.Center
            : new RelativePoint(0, 0, RelativeUnit.Absolute);

        ClipToBounds = false;
        InvalidateMeasure();
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => [Surface];

    LookaheadMeasureResult ILookaheadLayoutPolicy.MeasureLayout(LookaheadMeasureContext context, LookaheadConstraints constraints)
    {
        LookaheadPlaceable content = context.Measure(Surface, constraints);
        if (context.Phase == LookaheadLayoutPhase.Target)
        {
            return context.Layout(content.DesiredSize,
                placement => placement.Arrange(content, new Rect(placement.FinalSize)));
        }

        MeasuredContentSize = content.DesiredSize;

        Motion?.ConfigureAnimations(
            MeasuredContentSize,
            targetContentSize: context.TryGetTarget(Surface, out LookaheadLayoutResult target) ? target.DesiredSize : null);

        return context.Layout(
            size: constraints.Constrain(Motion?.GetAnimatedSize(MeasuredContentSize) ?? MeasuredContentSize),
            placement => {
                Size size = Motion?.ChangesSize == true ? MeasuredContentSize : placement.FinalSize;
                Point offset = Motion?.GetContentOffset(placement.FinalSize) ?? default;
                placement.Arrange(content, new Rect(offset, size));
                ApplyVisuals();
            });
    }

    private void ApplyVisuals()
    {
        if (Motion is not { } motion) return;
        _opacitySamples.Publish(motion.Opacity);
        var origin = new Point(motion.TransformOrigin.X * MeasuredContentSize.Width,
            motion.TransformOrigin.Y * MeasuredContentSize.Height);
        _transformSamples.Publish(Matrix.CreateTranslation(-origin.X, -origin.Y)
          * Matrix.CreateScale(motion.Scale, motion.Scale)
          * Matrix.CreateTranslation(origin.X + motion.SlideOffset.X, origin.Y + motion.SlideOffset.Y));
        ClipToBounds = motion.ClipsToBounds;
    }

    protected override Size MeasureOverride(Size availableSize)
        => LookaheadPolicyRuntime.MeasureActual(this, this, LookaheadPolicyRuntime.ActualPhase(this),
            LookaheadConstraints.AtMost(availableSize));

    protected override Size ArrangeOverride(Size finalSize) => LookaheadPolicyRuntime.ArrangeActual(this, finalSize);

    protected override Size MeasureCore(Size availableSize)
        => LookaheadLayout.TryMeasureCore(this, availableSize, out Size desired) ? desired : base.MeasureCore(availableSize);

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadLayout.TryArrangeCore(this, finalRect, out Rect bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) LookaheadLayout.VisualParentChanged(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadLayout.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }
}
