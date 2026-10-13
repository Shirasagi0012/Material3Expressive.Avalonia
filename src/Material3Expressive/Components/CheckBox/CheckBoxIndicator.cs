/*
 * Portions translated from AndroidX Checkbox.kt, Copyright 2021 The Android Open Source Project.
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0).
 * Fixed baseline: docs/upstream-baseline.json; isCheckboxStylingFixEnabled=false.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using SkiaSharp;

namespace Material3Expressive.Components;

// Private template part; it owns paint and animation, never changes the native CheckBox state.
internal sealed class CheckBoxIndicator : Control
{
    public static readonly StyledProperty<Foundation.Motion.SpringSpec?> ShapeAnimationProperty =
        AvaloniaProperty.Register<CheckBoxIndicator, Foundation.Motion.SpringSpec?>(nameof(ShapeAnimation));
    public Foundation.Motion.SpringSpec? ShapeAnimation { get => GetValue(ShapeAnimationProperty); set => SetValue(ShapeAnimationProperty, value); }
    public static readonly StyledProperty<Foundation.Motion.SpringSpec?> ColorAnimationProperty =
        AvaloniaProperty.Register<CheckBoxIndicator, Foundation.Motion.SpringSpec?>(nameof(ColorAnimation));
    public Foundation.Motion.SpringSpec? ColorAnimation { get => GetValue(ColorAnimationProperty); set => SetValue(ColorAnimationProperty, value); }
    public static readonly StyledProperty<Foundation.Motion.SpringSpec?> UncheckColorAnimationProperty =
        AvaloniaProperty.Register<CheckBoxIndicator, Foundation.Motion.SpringSpec?>(nameof(UncheckColorAnimation));
    public Foundation.Motion.SpringSpec? UncheckColorAnimation { get => GetValue(UncheckColorAnimationProperty); set => SetValue(UncheckColorAnimationProperty, value); }

    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<bool?> IsCheckedProperty = AvaloniaProperty.Register<CheckBoxIndicator, bool?>(nameof(IsChecked), false);
    public bool? IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public static readonly StyledProperty<CheckBoxColors?> ColorsProperty = AvaloniaProperty.Register<CheckBoxIndicator, CheckBoxColors?>(nameof(Colors));
    public CheckBoxColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<CheckBoxStroke?> CheckmarkStrokeProperty = AvaloniaProperty.Register<CheckBoxIndicator, CheckBoxStroke?>(nameof(CheckmarkStroke));
    public CheckBoxStroke? CheckmarkStroke { get => GetValue(CheckmarkStrokeProperty); set => SetValue(CheckmarkStrokeProperty, value); }
    public static readonly StyledProperty<CheckBoxStroke?> OutlineStrokeProperty = AvaloniaProperty.Register<CheckBoxIndicator, CheckBoxStroke?>(nameof(OutlineStroke));
    public CheckBoxStroke? OutlineStroke { get => GetValue(OutlineStrokeProperty); set => SetValue(OutlineStrokeProperty, value); }
    public static readonly StyledProperty<Color> CheckedCheckmarkColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(CheckedCheckmarkColor));
    public Color CheckedCheckmarkColor { get => GetValue(CheckedCheckmarkColorProperty); set => SetValue(CheckedCheckmarkColorProperty, value); }
    public static readonly StyledProperty<Color> UncheckedCheckmarkColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(UncheckedCheckmarkColor));
    public Color UncheckedCheckmarkColor { get => GetValue(UncheckedCheckmarkColorProperty); set => SetValue(UncheckedCheckmarkColorProperty, value); }
    public static readonly StyledProperty<Color> CheckedBoxColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(CheckedBoxColor));
    public Color CheckedBoxColor { get => GetValue(CheckedBoxColorProperty); set => SetValue(CheckedBoxColorProperty, value); }
    public static readonly StyledProperty<Color> UncheckedBoxColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(UncheckedBoxColor));
    public Color UncheckedBoxColor { get => GetValue(UncheckedBoxColorProperty); set => SetValue(UncheckedBoxColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledCheckedBoxColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(DisabledCheckedBoxColor));
    public Color DisabledCheckedBoxColor { get => GetValue(DisabledCheckedBoxColorProperty); set => SetValue(DisabledCheckedBoxColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledUncheckedBoxColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(DisabledUncheckedBoxColor));
    public Color DisabledUncheckedBoxColor { get => GetValue(DisabledUncheckedBoxColorProperty); set => SetValue(DisabledUncheckedBoxColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledIndeterminateBoxColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(DisabledIndeterminateBoxColor));
    public Color DisabledIndeterminateBoxColor { get => GetValue(DisabledIndeterminateBoxColorProperty); set => SetValue(DisabledIndeterminateBoxColorProperty, value); }
    public static readonly StyledProperty<Color> CheckedBorderColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(CheckedBorderColor));
    public Color CheckedBorderColor { get => GetValue(CheckedBorderColorProperty); set => SetValue(CheckedBorderColorProperty, value); }
    public static readonly StyledProperty<Color> UncheckedBorderColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(UncheckedBorderColor));
    public Color UncheckedBorderColor { get => GetValue(UncheckedBorderColorProperty); set => SetValue(UncheckedBorderColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledBorderColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(DisabledBorderColor));
    public Color DisabledBorderColor { get => GetValue(DisabledBorderColorProperty); set => SetValue(DisabledBorderColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledUncheckedBorderColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(DisabledUncheckedBorderColor));
    public Color DisabledUncheckedBorderColor { get => GetValue(DisabledUncheckedBorderColorProperty); set => SetValue(DisabledUncheckedBorderColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledIndeterminateBorderColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(DisabledIndeterminateBorderColor));
    public Color DisabledIndeterminateBorderColor { get => GetValue(DisabledIndeterminateBorderColorProperty); set => SetValue(DisabledIndeterminateBorderColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledCheckmarkColorProperty = AvaloniaProperty.Register<CheckBoxIndicator, Color>(nameof(DisabledCheckmarkColor));
    public Color DisabledCheckmarkColor { get => GetValue(DisabledCheckmarkColorProperty); set => SetValue(DisabledCheckmarkColorProperty, value); }

    private Transition<bool?>? _transition;
    private CancellationTokenSource? _transitionRun;
    private Task? _transitionTask;
    private Animatable<Color>? _checkColor, _boxColor, _borderColor;
    private bool _pending, _colorsEnabled;
    internal float DrawFraction { get; private set; }
    internal float CenterGravitation { get; private set; }
    internal Color PaintedCheckmark { get; private set; }
    internal Color PaintedBox { get; private set; }
    internal Color PaintedBorder { get; private set; }

    private CheckBoxColors Palette => Colors ?? new(CheckedCheckmarkColor, UncheckedCheckmarkColor,
        CheckedBoxColor, UncheckedBoxColor, DisabledCheckedBoxColor, DisabledUncheckedBoxColor,
        DisabledIndeterminateBoxColor, CheckedBorderColor, UncheckedBorderColor, DisabledBorderColor,
        DisabledUncheckedBorderColor, DisabledIndeterminateBorderColor, DisabledCheckmarkColor);
    // The fixed baseline ignores disabledCheckmarkColor while the styling-fix flag is false.
    private Color CheckTarget => IsChecked == false ? Palette.UncheckedCheckmarkColor : Palette.CheckedCheckmarkColor;
    private Color BoxTarget => IsEffectivelyEnabled
        ? IsChecked == false ? Palette.UncheckedBoxColor : Palette.CheckedBoxColor
        : IsChecked switch { true => Palette.DisabledCheckedBoxColor, false => Palette.DisabledUncheckedBoxColor, null => Palette.DisabledIndeterminateBoxColor };
    private Color BorderTarget => IsEffectivelyEnabled
        ? IsChecked == false ? Palette.UncheckedBorderColor : Palette.CheckedBorderColor
        : IsChecked switch { true => Palette.DisabledBorderColor, false => Palette.DisabledUncheckedBorderColor, null => Palette.DisabledIndeterminateBorderColor };

    protected override Size MeasureOverride(Size availableSize) => new(20, 20);
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TemplatedParent is CheckBox owner)
        {
            var interaction = MaterialInteraction.GetOrCreate(owner);
            interaction.IsRippleBounded = false;
            interaction.RippleRadius = 20;
        }
        EnsureShapeTransition();
        _checkColor = new Animatable<Color>(new ColorConverter(), CheckTarget, default);
        _boxColor = new Animatable<Color>(new ColorConverter(), BoxTarget, default);
        _borderColor = new Animatable<Color>(new ColorConverter(), BorderTarget, default);
        _colorsEnabled = IsEffectivelyEnabled;
        var motion = AnimationScope.Get(this);
        motion.SubscribeValue(_checkColor, value => { PaintedCheckmark = value; InvalidateVisual(); });
        motion.SubscribeValue(_boxColor, value => { PaintedBox = value; InvalidateVisual(); });
        motion.SubscribeValue(_borderColor, value => { PaintedBorder = value; InvalidateVisual(); });
    }

    private void EnsureShapeTransition()
    {
        if (_transition is not null || !this.IsAttachedToVisualTree() || ShapeAnimation is null) return;
        var motion = AnimationScope.Get(this);
        _transition = new Transition<bool?>(IsChecked, "CheckBox shape");
        var draw = _transition.AnimateFloat(state => state == false ? 0 : 1, segment =>
            segment.InitialState == false || segment.TargetState != false ? Spatial() : new SnapSpec<float>(TimeSpan.FromMilliseconds(100)));
        var gravitation = _transition.AnimateFloat(state => state is null ? 1 : 0, segment =>
            segment.InitialState == false ? new SnapSpec<float>() :
            segment.TargetState == false ? new SnapSpec<float>(TimeSpan.FromMilliseconds(100)) : Spatial());
        motion.SubscribeValue(draw, value => { DrawFraction = value; InvalidateVisual(); });
        motion.SubscribeValue(gravitation, value => { CenterGravitation = value; InvalidateVisual(); });
        _transition.PropertyChanged += TransitionChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_transition is not null) _transition.PropertyChanged -= TransitionChanged;
        _transitionRun?.Cancel();
        _transitionRun?.Dispose();
        _transitionRun = null;
        _transitionTask = null;
        _transition?.Dispose();
        _transition = null;
        _checkColor = _boxColor = _borderColor = null;
        _pending = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CheckmarkStrokeProperty || change.Property == OutlineStrokeProperty) InvalidateVisual();
        if (change.Property == ShapeAnimationProperty) EnsureShapeTransition();
        if (change.Property == ShapeAnimationProperty || change.Property == ColorAnimationProperty || change.Property == UncheckColorAnimationProperty ||
            change.Property == IsCheckedProperty || change.Property == IsEffectivelyEnabledProperty ||
            change.Property == ColorsProperty || change.Property.PropertyType == typeof(Color))
            UpdateTargets();
    }

    private IFiniteAnimationSpec<float> Spatial()
    {
        var spec = ShapeAnimation!.Value;
        return new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.01f);
    }
    private void TransitionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        // RunAsync otherwise waits for an asynchronous idle wake-up. Stop at completion and
        // start after retargeting, so the first frame is registered in the recomposition frame.
        if (args.PropertyName == nameof(Transition.IsRunning) && _transition?.IsRunning == false)
            _transitionRun?.Cancel();
    }

    private void UpdateTargets()
    {
        if (!this.IsAttachedToVisualTree() || _pending) return;
        var motion = AnimationScope.Get(this);
        _pending = true;
        // Apply targets at the corresponding recomposition frame, before the first animation frame.
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct);
            _pending = false;
            if (_checkColor is null || _boxColor is null || _borderColor is null) return;
            var targetChanged = ShapeAnimation is not null && _transition is not null && _transition.TargetState != IsChecked;
            if (ShapeAnimation is not null) _transition?.UpdateTarget(IsChecked);
            if (targetChanged && (_transitionTask is null || _transitionTask.IsCompleted))
            {
                _transitionRun?.Dispose();
                _transitionRun = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _transitionTask = motion.Launch(_ => _transition!.RunAsync(motion, _transitionRun.Token), "CheckBox transition");
            }
            var spec = IsChecked == false
                ? UncheckColorAnimation
                : ColorAnimation;
            if (spec is not { } configuredColor) return;
            var colorSpec = new SpringSpec<Color>(configuredColor.DampingRatio, configuredColor.Stiffness);
            Restart(_checkColor, CheckTarget, true);
            // Disabled removes animateColorAsState; enabling creates a fresh animation already at target.
            var animate = IsEffectivelyEnabled && _colorsEnabled;
            Restart(_boxColor, BoxTarget, animate);
            Restart(_borderColor, BorderTarget, animate);
            _colorsEnabled = IsEffectivelyEnabled;
            void Restart(Animatable<Color> track, Color target, bool animate)
            {
                if (track.TargetValue == target && track.Value == target) return;
                if (animate && track.TargetValue == target) return;
                _ = motion.Launch(async token =>
                {
                    await track.StopAsync();
                    token.ThrowIfCancellationRequested();
                    if (animate) await track.AnimateToAsync(motion, target, colorSpec, cancellationToken: token);
                    else await track.SnapToAsync(target);
                }, "CheckBox color");
            }
        }, "CheckBox targets");
    }

    public override void Render(DrawingContext context)
    {
        var scale = (float)Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        var width = MathF.Floor(2 * scale) / scale;
        context.Custom(new CheckDrawOperation(new Rect(Bounds.Size), scale, PaintedBox, PaintedBorder,
            PaintedCheckmark, DrawFraction, CenterGravitation,
            OutlineStroke ?? new CheckBoxStroke { Width = width, Cap = PenLineCap.Flat },
            CheckmarkStroke ?? new CheckBoxStroke { Width = width }));
    }

    private sealed class CheckDrawOperation(Rect bounds, float scale, Color box, Color border, Color check,
        float fraction, float gravitation, CheckBoxStroke outline, CheckBoxStroke checkmark) : ICustomDrawOperation
    {
        public Rect Bounds { get; } = bounds.Inflate(Math.Max(outline.Width, checkmark.Width));
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }
        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
                ?? throw new NotSupportedException("Material CheckBox requires the Avalonia.Skia renderer.");
            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            try
            {
                // Compose draws in Float physical pixels; preserve that order before the DIP transform.
                canvas.Scale(1 / scale);
                var size = 20 * scale;
                var radius = 2 * scale;
                using var fill = new SKPaint { IsAntialias = true, Color = Ink(box) };
                using var stroke = Paint(border, outline);
                var half = stroke.StrokeWidth / 2;
                if (box == border) canvas.DrawRoundRect(new SKRect(0, 0, size, size), radius, radius, fill);
                else
                {
                    var inset = stroke.StrokeWidth;
                    canvas.DrawRoundRect(new SKRect(inset, inset, size - inset, size - inset),
                        Math.Max(0, radius - inset), Math.Max(0, radius - inset), fill);
                    canvas.DrawRoundRect(new SKRect(half, half, size - half, size - half), radius - half, radius - half, stroke);
                }
                float Lerp(float a, float b) => (1 - gravitation) * a + gravitation * b;
                using var builder = new SKPathBuilder();
                builder.MoveTo(size * 0.2f, size * Lerp(0.5f, 0.5f));
                builder.LineTo(size * Lerp(0.4f, 0.5f), size * Lerp(0.7f, 0.5f));
                builder.LineTo(size * 0.8f, size * Lerp(0.3f, 0.5f));
                using var path = builder.Detach();
                using var measure = new SKPathMeasure(path, false);
                using var segmentBuilder = new SKPathBuilder();
                measure.GetSegment(0, measure.Length * fraction, segmentBuilder, true);
                using var segment = segmentBuilder.Detach();
                using var pen = Paint(check, checkmark);
                canvas.DrawPath(segment, pen);

                SKColor Ink(Color color) => new(color.R, color.G, color.B,
                    (byte)Math.Clamp(Math.Round(color.A * lease.CurrentOpacity, MidpointRounding.AwayFromZero), 0, 255));
                SKPaint Paint(Color color, CheckBoxStroke value)
                {
                    var paint = new SKPaint { IsAntialias = true, Color = Ink(color), Style = SKPaintStyle.Stroke,
                        StrokeWidth = (float)value.Width * scale, StrokeMiter = (float)value.MiterLimit,
                        StrokeCap = value.Cap switch { PenLineCap.Round => SKStrokeCap.Round, PenLineCap.Square => SKStrokeCap.Square, _ => SKStrokeCap.Butt },
                        StrokeJoin = value.Join switch { PenLineJoin.Round => SKStrokeJoin.Round, PenLineJoin.Bevel => SKStrokeJoin.Bevel, _ => SKStrokeJoin.Miter } };
                    if (value.Dashes is { Length: > 0 } dashes)
                    {
                        using var effect = SKPathEffect.CreateDash(dashes.Select(x => (float)x * scale).ToArray(), (float)value.DashOffset * scale);
                        paint.PathEffect = effect;
                    }
                    return paint;
                }
            }
            finally { canvas.Restore(); }
        }
    }
}
