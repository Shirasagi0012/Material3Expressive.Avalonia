/*
 * Portions translated from AndroidX RadioButton.kt, Copyright 2021 The Android Open Source Project.
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0).
 * Fixed baseline: docs/upstream-baseline.json; RadioButtonImpl / RadioButtonTokens v0_117.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;
using SkiaSharp;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// Template part: paint and animation only. Native RadioButton owns selection, grouping and automation.
internal sealed class RadioButtonIndicator : Control
{
    public static readonly StyledProperty<bool?> IsCheckedProperty = AvaloniaProperty.Register<RadioButtonIndicator, bool?>(nameof(IsChecked), false);
    public bool? IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public static readonly StyledProperty<RadioButtonColors?> ColorsProperty = AvaloniaProperty.Register<RadioButtonIndicator, RadioButtonColors?>(nameof(Colors));
    public RadioButtonColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<Color> SelectedColorProperty = AvaloniaProperty.Register<RadioButtonIndicator, Color>(nameof(SelectedColor));
    public Color SelectedColor { get => GetValue(SelectedColorProperty); set => SetValue(SelectedColorProperty, value); }
    public static readonly StyledProperty<Color> UnselectedColorProperty = AvaloniaProperty.Register<RadioButtonIndicator, Color>(nameof(UnselectedColor));
    public Color UnselectedColor { get => GetValue(UnselectedColorProperty); set => SetValue(UnselectedColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledSelectedColorProperty = AvaloniaProperty.Register<RadioButtonIndicator, Color>(nameof(DisabledSelectedColor));
    public Color DisabledSelectedColor { get => GetValue(DisabledSelectedColorProperty); set => SetValue(DisabledSelectedColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledUnselectedColorProperty = AvaloniaProperty.Register<RadioButtonIndicator, Color>(nameof(DisabledUnselectedColor));
    public Color DisabledUnselectedColor { get => GetValue(DisabledUnselectedColorProperty); set => SetValue(DisabledUnselectedColorProperty, value); }

    private VisualAnimationScope? _motion;
    private Animatable<float>? _dotRadius;
    private Animatable<Color>? _color;
    private bool _pending, _colorsEnabled;
    internal float DotRadius { get; private set; }
    internal Color PaintedColor { get; private set; }
    private float RadiusTarget => IsChecked == true ? 6 : 0;
    private Color ColorTarget => (IsEffectivelyEnabled, IsChecked == true) switch
    {
        (true, true) => Colors?.SelectedColor ?? SelectedColor,
        (true, false) => Colors?.UnselectedColor ?? UnselectedColor,
        (false, true) => Colors?.DisabledSelectedColor ?? DisabledSelectedColor,
        _ => Colors?.DisabledUnselectedColor ?? DisabledUnselectedColor,
    };

    protected override Size MeasureOverride(Size availableSize) => new(20, 20);
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TemplatedParent is RadioButton owner)
        {
            var interaction = MaterialInteraction.GetOrCreate(owner);
            interaction.IsRippleBounded = false;
            interaction.RippleRadius = 20;
        }
        _motion = VisualAnimationScope.Create(this);
        _dotRadius = _motion.CreateAnimatable(RadiusTarget);
        _color = _motion.CreateAnimatable(ColorTarget);
        _colorsEnabled = IsEffectivelyEnabled;
        _motion.SubscribeValue(_dotRadius, value => { DotRadius = value; InvalidateVisual(); });
        _motion.SubscribeValue(_color, value => { PaintedColor = value; InvalidateVisual(); });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _motion?.Dispose();
        _motion = null;
        _dotRadius = null;
        _color = null;
        _pending = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty || change.Property == IsEffectivelyEnabledProperty ||
            change.Property == ColorsProperty || change.Property.PropertyType == typeof(Color)) UpdateTargets();
    }

    private MotionSpring Resolve(MaterialTokenKey<MotionSpring> key, MotionSpring fallback) =>
        TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is MotionSpring spec ? spec : fallback;

    private void UpdateTargets()
    {
        if (_motion is not { IsEnding: false } motion || _pending) return;
        _pending = true;
        // animate*AsState retargets after recomposition. Coalesce property/token changes in that frame.
        _ = motion.Launch(async ct =>
        {
            await motion.Clock.NextFrameAsync(ct);
            _pending = false;
            if (_dotRadius is not { } dot || _color is not { } color) return;
            var radiusTarget = RadiusTarget;
            if (dot.TargetValue != radiusTarget)
            {
                var spatial = Resolve(MaterialTokens.Sys.Motion.Spring.SpatialFast, MotionScheme.Expressive.FastSpatial);
                _ = motion.Launch(async token =>
                {
                    // Fixed AnimateAsState.restartAnimation cancels its previous job before animateTo.
                    await dot.StopAsync();
                    token.ThrowIfCancellationRequested();
                    // The supplied motion-scheme spring has no Dp threshold: vectorization uses 0.01,
                    // rather than animateDpAsState's unused default dpDefaultSpring (0.1 DIP).
                    await dot.AnimateToAsync(radiusTarget, new SpringSpec<float>(spatial.DampingRatio, spatial.Stiffness, 0.01f), cancellationToken: token);
                }, "RadioButton dot radius");
            }
            var colorTarget = ColorTarget;
            // Disabled removes animateColorAsState; re-enabling creates a fresh state at its target.
            var animate = IsEffectivelyEnabled && _colorsEnabled;
            _colorsEnabled = IsEffectivelyEnabled;
            if (animate ? color.TargetValue == colorTarget : !color.IsRunning && color.Value == colorTarget && color.TargetValue == colorTarget) return;
            var effects = Resolve(MaterialTokens.Sys.Motion.Spring.EffectsDefault, MotionScheme.Expressive.DefaultEffects);
            _ = motion.Launch(async token =>
            {
                await color.StopAsync();
                token.ThrowIfCancellationRequested();
                if (animate) await color.AnimateToAsync(colorTarget, new SpringSpec<Color>(effects.DampingRatio, effects.Stiffness), cancellationToken: token);
                else await color.SnapToAsync(colorTarget);
            }, "RadioButton color");
        }, "RadioButton targets");
    }

    public override void Render(DrawingContext context)
    {
        var scale = (float)Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        context.Custom(new RadioDrawOperation(new Rect(Bounds.Size), scale, PaintedColor, DotRadius));
    }

    private sealed class RadioDrawOperation(Rect bounds, float scale, Color color, float dotRadius) : ICustomDrawOperation
    {
        public Rect Bounds { get; } = bounds;
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }
        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
                ?? throw new NotSupportedException("Material RadioButton requires the Avalonia.Skia renderer.");
            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            try
            {
                // Preserve Compose's Float physical-pixel arithmetic before the DIP transform.
                canvas.Scale(1 / scale);
                var strokeWidth = 2 * scale;
                var center = 20 * scale / 2;
                using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth,
                    Color = new SKColor(color.R, color.G, color.B,
                        (byte)Math.Clamp(Math.Round(color.A * lease.CurrentOpacity, MidpointRounding.AwayFromZero), 0, 255)) };
                Circle(center - strokeWidth / 2);
                // Accepted baseline deviation: do not turn a negative painted radius into an oval.
                // Keep the spring state and timing intact; a sub-1-DIP rebound paints no dot.
                var paintedRadius = dotRadius * scale - strokeWidth / 2;
                if (paintedRadius > 0)
                {
                    paint.Style = SKPaintStyle.Fill;
                    Circle(paintedRadius);
                }
                // Preserve Skiko's drawOval path and Float arithmetic for positive radii.
                void Circle(float radius)
                {
                    var first = center - radius;
                    var second = center + radius;
                    canvas.DrawOval(new SKRect(Math.Min(first, second), Math.Min(first, second),
                        Math.Max(first, second), Math.Max(first, second)), paint);
                }
            }
            finally { canvas.Restore(); }
        }
    }
}
