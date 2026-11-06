// PullToRefresh.kt / CircularArrowProgressIndicator and ArrowValues; Apache-2.0.
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using SkiaSharp;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

internal sealed class PullToRefreshArrow : Control
{
    public static readonly StyledProperty<float> DistanceFractionProperty = AvaloniaProperty.Register<PullToRefreshArrow, float>(nameof(DistanceFraction));
    public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<PullToRefreshArrow, Color>(nameof(Color));
    public static readonly StyledProperty<MaterialSpring?> EffectsAnimationProperty = AvaloniaProperty.Register<PullToRefreshArrow, MaterialSpring?>(nameof(EffectsAnimation));
    public float DistanceFraction { get => GetValue(DistanceFractionProperty); set => SetValue(DistanceFractionProperty, value); }
    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public MaterialSpring? EffectsAnimation { get => GetValue(EffectsAnimationProperty); set => SetValue(EffectsAnimationProperty, value); }
    private AnimationChildScope? _scope;
    private Animatable<float>? _alpha;
    private bool _alphaUpdatePending;
    internal float Alpha { get; private set; }
    protected override bool BypassFlowDirectionPolicies => true;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scope = AnimationScope.Get(this).CreateChild("Pull arrow alpha");
        _alpha = new Animatable<float>(new FloatConverter(), DistanceFraction >= 1 ? 1 : .3f, .01f);
        _scope.SubscribeValue(_alpha, value => { Alpha = value; InvalidateVisual(); });
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _scope?.Dispose(); _scope = null; _alpha = null; _alphaUpdatePending = false;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DistanceFractionProperty || change.Property == EffectsAnimationProperty)
        {
            if (_scope is { } scope && _alpha is { } alpha && EffectsAnimation is { Stiffness: > 0 })
            {
                var target = DistanceFraction >= 1 ? 1f : .3f;
                if (alpha.TargetValue != target && !_alphaUpdatePending)
                {
                    _alphaUpdatePending = true;
                    _ = scope.Launch(async ct =>
                    {
                        // animateFloatAsState commits its latest target in SideEffect,
                        // after recomposition and the old animation's frame sample.
                        if (!await scope.WaitForFrameEndAsync(ct)) return;
                        _alphaUpdatePending = false;
                        var latest = DistanceFraction >= 1 ? 1f : .3f;
                        if (alpha.TargetValue != latest && EffectsAnimation is { Stiffness: > 0 } latestSpec)
                            await alpha.AnimateToAsync(scope, latest,
                                new SpringSpec<float>(latestSpec.DampingRatio, latestSpec.Stiffness, .01f), cancellationToken: ct);
                    }, "Pull arrow alpha composition");
                }
            }
            if (change.Property == DistanceFractionProperty)
                ControlAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,
                    change.GetOldValue<float>(), change.GetNewValue<float>());
        }
        InvalidateVisual();
    }
    public override void Render(DrawingContext context) => context.Custom(new ArrowDrawOperation(
        Bounds.Size, (float)LayoutHelper.GetLayoutScale(this), DistanceFraction, Color, Alpha));

    protected override AutomationPeer OnCreateAutomationPeer() => new ArrowPeer(this);
    private sealed class ArrowPeer(PullToRefreshArrow owner) : ControlAutomationPeer(owner), IRangeValueProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
        protected override object? GetProviderCore(Type providerType) => providerType == typeof(IRangeValueProvider)
            ? owner.DistanceFraction > 0 ? this : null : base.GetProviderCore(providerType);
        public bool IsReadOnly => true;
        public double Minimum => 0;
        public double Maximum => 1;
        public double Value => owner.DistanceFraction;
        public double SmallChange => double.NaN;
        public double LargeChange => double.NaN;
        public void SetValue(double value) => throw new InvalidOperationException("Pull progress is read-only.");
    }

    internal readonly record struct ArrowValues(float Rotation, float StartAngle, float EndAngle, float Scale)
    {
        public static ArrowValues From(float progress)
        {
            var adjustedPercent = Math.Max(Math.Min(1f, progress) - .4f, 0f) * 5 / 3;
            var overshootPercent = Math.Abs(progress) - 1f;
            var linearTension = Math.Clamp(overshootPercent, 0f, 2f);
            var tensionPercent = linearTension - linearTension * linearTension / 4;
            var endTrim = adjustedPercent * .8f;
            var rotation = (-.25f + .4f * adjustedPercent + tensionPercent) * .5f;
            return new(rotation, rotation * 360, (rotation + endTrim) * 360, Math.Min(1f, adjustedPercent));
        }
    }

    private sealed class ArrowDrawOperation(Size size, float density, float progress, Color color, float alpha) : ICustomDrawOperation
    {
        public Rect Bounds { get; } = new Rect(size).Inflate(2.5);
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }
        public void Render(ImmediateDrawingContext context)
        {
            using var lease = (context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
                ?? throw new NotSupportedException("Pull-to-refresh arrows require Avalonia.Skia.")).Lease();
            var canvas = lease.SkCanvas;
            var values = ArrowValues.From(progress);
            var stroke = 2.5f * density;
            var radius = 5.5f * density + stroke / 2;
            var cx = (float)size.Width * density / 2;
            var cy = (float)size.Height * density / 2;
            using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke,
                StrokeWidth = stroke, StrokeCap = SKStrokeCap.Butt, StrokeJoin = SKStrokeJoin.Miter,
                Color = new SKColor(color.R, color.G, color.B,
                    (byte)Math.Clamp(Math.Floor(color.A * alpha * lease.CurrentOpacity + .5), 0, 255)) };
            canvas.Save();
            try
            {
                canvas.Scale(1 / density);
                // Upstream intentionally passes rotation (turn fraction) as degrees here,
                // while start/end angles below are converted to degrees separately.
                canvas.RotateDegrees(values.Rotation, cx, cy);
                canvas.DrawArc(new SKRect(cx - radius, cy - radius, cx + radius, cy + radius),
                    values.StartAngle, values.EndAngle - values.StartAngle, false, paint);
                var width = 10f * density * values.Scale;
                var height = 5f * density * values.Scale;
                var x = radius + cx - width / 2;
                var y = cy - stroke;
                using var builder = new SKPathBuilder { FillType = SKPathFillType.EvenOdd };
                builder.MoveTo(x, y); builder.LineTo(x + width / 2, y + height); builder.LineTo(x + width, y);
                using var arrow = builder.Detach();
                // The subtraction is in physical pixels in the pinned source.
                canvas.RotateDegrees(values.EndAngle - stroke, cx, cy);
                canvas.DrawPath(arrow, paint);
            }
            finally { canvas.Restore(); }
        }
    }
}
