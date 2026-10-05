/*
 * Translated from AndroidX ProgressIndicator.kt and internal/*WavyProgressModifiers.kt.
 * Copyright 2022, 2024 The Android Open Source Project; Apache-2.0.
 * Fixed baseline: docs/upstream-baseline.json.
 */
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using SkiaSharp;

namespace Material3Expressive.Components;

/// <summary>Shared progress, paint, automation and animation lifetime for the four indicators.</summary>
public abstract class ProgressIndicator : Control
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ProgressIndicator, double>(nameof(Progress));
    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressIndicator, bool>(nameof(IsIndeterminate));
    public bool IsIndeterminate { get => GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<ProgressIndicator, Color>(nameof(Color));
    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public static readonly StyledProperty<Color> TrackColorProperty =
        AvaloniaProperty.Register<ProgressIndicator, Color>(nameof(TrackColor));
    public Color TrackColor { get => GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }
    public static readonly StyledProperty<double> GapSizeProperty =
        AvaloniaProperty.Register<ProgressIndicator, double>(nameof(GapSize));
    public double GapSize { get => GetValue(GapSizeProperty); set => SetValue(GapSizeProperty, value); }

    // Theme-only token inputs: preferred size, not Width/Height constraints.
    public static readonly StyledProperty<double> ContainerWidthProperty =
        AvaloniaProperty.Register<ProgressIndicator, double>("ContainerWidth");
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double ContainerWidth { get => GetValue(ContainerWidthProperty); set => SetValue(ContainerWidthProperty, value); }
    public static readonly StyledProperty<double> ContainerHeightProperty =
        AvaloniaProperty.Register<ProgressIndicator, double>("ContainerHeight");
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double ContainerHeight { get => GetValue(ContainerHeightProperty); set => SetValue(ContainerHeightProperty, value); }
    public static readonly StyledProperty<double> DefaultWavelengthProperty =
        AvaloniaProperty.Register<ProgressIndicator, double>("DefaultWavelength");
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double DefaultWavelength { get => GetValue(DefaultWavelengthProperty); set => SetValue(DefaultWavelengthProperty, value); }

    VisualAnimationScope? motion, indeterminateMotion, offsetMotion, amplitudeMotion;
    InfiniteTransition? infinite;
    Animatable<float>? amplitude;
    Task? amplitudeTask;
    LinearProgressDrawingCache? linearCache;
    CircularProgressDrawingCache? circularCache;
    CircularProgressShapes? shapes;
    bool amplitudeDirty = true, preparingWave, observesAmplitude;
    float targetAmplitude, waveWidth = -1, waveHeight = -1, waveScale = -1;
    float animatedAmplitude, waveOffset, firstHead, firstTail, secondHead, secondTail;
    float globalRotation, additionalRotation, sweep = .1f;
    internal bool Circular => this is CircularProgressIndicator or CircularWavyProgressIndicator;
    internal bool Wavy => this is WavyProgressIndicator;
    internal float CoercedProgress => Math.Clamp((float)Progress, 0f, 1f);
    internal float CurrentAmplitude => animatedAmplitude;
    internal float WaveOffset => waveOffset;
    internal float[] Fractions => IsIndeterminate
        ? [firstTail, firstHead, secondTail, secondHead] : [0, CoercedProgress];
    internal float Sweep => sweep;
    internal float GlobalRotation => globalRotation;
    internal float AdditionalRotation => additionalRotation;
    internal VisualAnimationScope? AnimationScope => motion;
    float Wavelength => (float)(((WavyProgressIndicator)this).Wavelength ?? GetValue(DefaultWavelengthProperty));
    float WaveSpeed => (float)(((WavyProgressIndicator)this).WaveSpeed ?? Wavelength);

    static ProgressIndicator()
    {
        HorizontalAlignmentProperty.OverrideDefaultValue<ProgressIndicator>(HorizontalAlignment.Left);
        VerticalAlignmentProperty.OverrideDefaultValue<ProgressIndicator>(VerticalAlignment.Top);
        AffectsMeasure<ProgressIndicator>(ContainerWidthProperty, ContainerHeightProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var width = MathF.Round((float)GetValue(ContainerWidthProperty) * scale, MidpointRounding.AwayFromZero) / scale;
        var height = MathF.Round((float)GetValue(ContainerHeightProperty) * scale, MidpointRounding.AwayFromZero) / scale;
        return new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        motion = VisualAnimationScope.Create(this);
        StartIndeterminate();
        if (Wavy && !Circular) StartOffset();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        motion?.Dispose(); motion = indeterminateMotion = offsetMotion = amplitudeMotion = null;
        infinite = null; amplitude = null; amplitudeTask = null; amplitudeDirty = true;
        observesAmplitude = false; waveWidth = waveHeight = waveScale = -1;
        linearCache?.Dispose(); linearCache = null; circularCache?.Dispose(); circularCache = null;
        shapes = null;
        firstHead = firstTail = secondHead = secondTail = globalRotation = additionalRotation = waveOffset = animatedAmplitude = 0;
        sweep = .1f;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsIndeterminateProperty)
        {
            StartIndeterminate();
            // Switching overloads replaces the upstream wavy modifier node.
            offsetMotion?.Dispose(); offsetMotion = null; waveOffset = 0;
            amplitudeMotion?.Dispose(); amplitudeMotion = null;
            amplitude = null; amplitudeTask = null;
            linearCache?.Dispose(); linearCache = null;
            circularCache?.Dispose(); circularCache = null; shapes = null;
            observesAmplitude = false; waveWidth = waveHeight = waveScale = -1;
            if (Wavy && !Circular) StartOffset();
        }
        if (change.Property == ProgressProperty || change.Property == IsIndeterminateProperty ||
            change.Property == WavyProgressIndicator.AmplitudeProperty || change.Property == WavyProgressIndicator.AmplitudeSelectorProperty ||
            change.Property == GapSizeProperty || change.Property == WavyProgressIndicator.StrokeProperty ||
            change.Property == WavyProgressIndicator.TrackStrokeProperty || change.Property == WavyProgressIndicator.WavelengthProperty ||
            change.Property == WavyProgressIndicator.WaveSpeedProperty || change.Property == DefaultWavelengthProperty ||
            change.Property == WavyProgressIndicator.ActiveThicknessProperty || change.Property == WavyProgressIndicator.TrackThicknessProperty ||
            change.Property == LinearWavyProgressIndicator.StopSizeProperty)
            amplitudeDirty = true;
        if (change.Property == ProgressProperty && !IsIndeterminate)
        {
            static double SemanticValue(double value) => double.IsNaN(value) ? 0 : Math.Clamp((float)value, 0f, 1f);
            ControlAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,
                SemanticValue(change.GetOldValue<double>()), SemanticValue(change.GetNewValue<double>()));
        }
        if (Wavy && (change.Property == WavyProgressIndicator.WavelengthProperty ||
            change.Property == WavyProgressIndicator.WaveSpeedProperty || change.Property == DefaultWavelengthProperty)) StartOffset();
        InvalidateVisual();
    }

    void StartIndeterminate()
    {
        indeterminateMotion?.Dispose(); indeterminateMotion = null; infinite = null;
        firstHead = firstTail = secondHead = secondTail = globalRotation = additionalRotation = 0; sweep = .1f;
        if (!IsIndeterminate || motion is not { IsEnding: false }) return;
        var scope = indeterminateMotion = motion.CreateChild("Indeterminate progress");
        if (this is CircularWavyProgressIndicator)
        {
            // This upstream modifier uses concurrent Animatables, not InfiniteTransition.
            Animate(0, 1080, ProgressIndicatorMotion.GlobalRotation, value => globalRotation = value);
            Animate(0, 360, ProgressIndicatorMotion.Rotation, value => additionalRotation = value);
            Animate(.1f, .87f, ProgressIndicatorMotion.Sweep, value => sweep = value);
            void Animate(float initial, float target, IAnimationSpec<float> spec, Action<float> sample)
            {
                var value = scope.CreateAnimatable(initial);
                scope.SubscribeValue(value, current => { sample(current); InvalidateVisual(); });
                _ = scope.Launch(ct => value.AnimateToAsync(target, spec, cancellationToken: ct), "Circular wavy indeterminate");
            }
        }
        else
        {
            infinite = scope.CreateInfiniteTransition("Progress indicator");
            if (Circular)
            {
                Track(0, 1080, ProgressIndicatorMotion.GlobalRotation, value => globalRotation = value);
                Track(0, 360, ProgressIndicatorMotion.Rotation, value => additionalRotation = value);
                Track(.1f, .87f, ProgressIndicatorMotion.Sweep, value => sweep = value);
            }
            else
            {
                Track(0, 1, ProgressIndicatorMotion.Linear(0, 1000), value => firstHead = value);
                Track(0, 1, ProgressIndicatorMotion.Linear(250, 1000), value => firstTail = value);
                Track(0, 1, ProgressIndicatorMotion.Linear(650, 850), value => secondHead = value);
                Track(0, 1, ProgressIndicatorMotion.Linear(900, 850), value => secondTail = value);
            }
            _ = scope.Launch(ct => infinite.RunAsync(ct), "Infinite progress transition");
            void Track(float initial, float target, InfiniteRepeatableSpec<float> spec, Action<float> sample)
            {
                var value = infinite.AnimateFloat(initial, target, spec);
                scope.SubscribeValue(value, current => { sample(current); InvalidateVisual(); });
            }
        }
    }

    void StartOffset()
    {
        offsetMotion?.Dispose(); offsetMotion = null;
        if (motion is not { IsEnding: false } || !Wavy) return;
        if (Circular && animatedAmplitude <= 0) return;
        if (WaveSpeed <= 0 || Wavelength <= 0) { waveOffset = 0; return; }
        var vertexCount = Circular ? shapes?.VertexCount ?? -1 : 1;
        if (vertexCount <= 0) return;
        var duration = Math.Max(50, (int)MathF.Round(Wavelength / WaveSpeed * 1000 * vertexCount,
            MidpointRounding.AwayFromZero));
        var scope = offsetMotion = motion.CreateChild("Progress wave offset");
        var start = waveOffset;
        var value = scope.CreateAnimatable(start);
        value.UpdateBounds(Animatable<float>.AnimatableBounds<float>.Unbounded.WithLower(start).WithUpper(start + 1));
        scope.SubscribeValue(value, current => { waveOffset = current % 1; if (!preparingWave) InvalidateVisual(); });
        _ = scope.Launch(ct => value.AnimateToAsync(start + 1,
            new InfiniteRepeatableSpec<float>(new TweenSpec<float>(TimeSpan.FromMilliseconds(duration), easing: ComposeEasings.Linear)),
            cancellationToken: ct), "Progress wave offset");
    }

    void PrepareWave(float width, float height, float scale)
    {
        var wave = (WavyProgressIndicator)this;
        if (width != waveWidth || height != waveHeight || scale != waveScale)
        {
            amplitudeDirty = true; waveWidth = width; waveHeight = height; waveScale = scale;
        }
        var cacheChanged = amplitudeDirty;
        if (amplitudeDirty)
        {
            amplitudeDirty = false;
            var target = targetAmplitude = Math.Clamp((float)(wave.Amplitude ?? (IsIndeterminate ? 1 : wave.AmplitudeSelector(CoercedProgress))), 0, 1);
            if (this is CircularWavyProgressIndicator && IsIndeterminate) animatedAmplitude = target;
            else if (motion is { IsEnding: false } scope)
            {
                if (amplitude is null)
                {
                    amplitudeMotion = scope.CreateChild("Progress amplitude");
                    amplitude = amplitudeMotion.CreateAnimatable(target);
                    amplitudeMotion.SubscribeValue(amplitude, value =>
                    {
                        animatedAmplitude = value;
                        if (observesAmplitude) amplitudeDirty = true;
                        if (!preparingWave) InvalidateVisual();
                    });
                }
                // Upstream ignores a new target while the current amplitude job is running.
                if (amplitude.TargetValue != target && (amplitudeTask is null || amplitudeTask.IsCompleted))
                {
                    var value = amplitude;
                    var easing = value.Value < target ? ProgressIndicatorMotion.Standard : ProgressIndicatorMotion.Accelerate;
                    amplitudeTask = amplitudeMotion!.Launch(async ct =>
                    {
                        await value.AnimateToAsync(target, new TweenSpec<float>(TimeSpan.FromMilliseconds(500), easing: easing), cancellationToken: ct);
                        if (Circular && target == 0) { offsetMotion?.Dispose(); offsetMotion = null; }
                        // Keep the cache invalidation from the final observed value until the
                        // async job has completed, so its upstream completion gate can open.
                        if (observesAmplitude) { amplitudeDirty = true; InvalidateVisual(); }
                    }, "Progress wave amplitude");
                }
            }
        }
        if (Circular)
        {
            shapes ??= new();
            shapes.Update(width, height, Wavelength * scale, (float)wave.ActiveStroke.Width * scale,
                IsIndeterminate ? animatedAmplitude is > 0 and < 1 : amplitudeTask is not null);
            if (cacheChanged)
            {
                // The determinate Compose draw-cache observes amplitudeState through
                // startOffsetAnimation/isDrawingWave only while it has no offset job.
                observesAmplitude = !IsIndeterminate && targetAmplitude > 0 && offsetMotion is null;
                if ((IsIndeterminate ? animatedAmplitude > 0 : targetAmplitude > 0) && offsetMotion is null) StartOffset();
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var width = (float)Bounds.Width * scale; var height = (float)Bounds.Height * scale;
        if (Wavy)
        {
            // SubscribeValue publishes the initial value synchronously. It is already consumed
            // by this draw; Avalonia forbids invalidating the visual during the render pass.
            preparingWave = true;
            try { PrepareWave(width, height, scale); }
            finally { preparingWave = false; }
            var wave = (WavyProgressIndicator)this;
            if (Circular)
            {
                circularCache ??= new();
                var enableMotion = WaveSpeed > 0 && (!IsIndeterminate || animatedAmplitude > 0);
                circularCache.Update(width, height, IsIndeterminate ? sweep : CoercedProgress,
                    animatedAmplitude, animatedAmplitude > 0 && enableMotion ? waveOffset : 0,
                    Wavelength * scale, (float)GapSize * scale, wave.ActiveStroke, wave.ResolvedTrackStroke, scale, enableMotion, shapes!);
                context.Custom(new ProgressIndicatorDrawOperation(Bounds.Size, scale, Color, TrackColor, wave.ActiveStroke, wave.ResolvedTrackStroke,
                    [new(circularCache.Progress)], new(circularCache.Track), IsIndeterminate ? globalRotation + additionalRotation + 90 : 0));
            }
            else
            {
                linearCache ??= new();
                linearCache.Update(width, height, Wavelength * scale, Fractions, animatedAmplitude,
                    animatedAmplitude > 0 ? waveOffset : 0, (float)GapSize * scale, wave.ActiveStroke, wave.ResolvedTrackStroke, scale);
                using (context.PushClip(new Rect(Bounds.Size)))
                    context.Custom(new ProgressIndicatorDrawOperation(Bounds.Size, scale, Color, TrackColor, wave.ActiveStroke, wave.ResolvedTrackStroke,
                        linearCache.Progress.Select(path => new SKPath(path)).ToArray(), new(linearCache.Track),
                        FlowDirection == FlowDirection.RightToLeft ? 180 : 0,
                        IsIndeterminate ? 0 : (float)((LinearWavyProgressIndicator)this).StopSize * scale,
                        CoercedProgress, linearCache.CapWidth));
            }
        }
        else
        {
            var cap = this is LinearProgressIndicator linear ? linear.StrokeCap : ((CircularProgressIndicator)this).StrokeCap;
            var strokeWidth = Circular ? (float)((CircularProgressIndicator)this).StrokeWidth * scale : height;
            var stroke = new ProgressIndicatorStroke(strokeWidth / scale, cap);
            context.Custom(new PlainProgressDrawOperation(Bounds.Size, scale, Circular, IsIndeterminate,
                CoercedProgress, Color, TrackColor, stroke, (float)GapSize * scale,
                FlowDirection == FlowDirection.RightToLeft, Fractions, sweep, globalRotation + additionalRotation,
                this is LinearProgressIndicator { DrawStopIndicator: null } indicator ? (float)indicator.StopSize * scale : 0));
            if (this is LinearProgressIndicator { IsIndeterminate: false, DrawStopIndicator: { } draw }) draw(context, Bounds.Size);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ProgressIndicatorAutomationPeer(this);

    sealed class ProgressIndicatorAutomationPeer(ProgressIndicator owner) : ControlAutomationPeer(owner), IRangeValueProvider
    {
        protected override string GetClassNameCore() => owner.GetType().Name;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
        protected override object? GetProviderCore(Type providerType) =>
            providerType == typeof(IRangeValueProvider) ? owner.IsIndeterminate ? null : this : base.GetProviderCore(providerType);
        protected override Rect GetBoundingRectangleCore()
        {
            if (owner.Circular) return base.GetBoundingRectangleCore();
            if (TopLevel.GetTopLevel(owner) is not { } root || owner.TransformToVisual(root) is not { } transform) return default;
            return new Rect(owner.Bounds.Size).Inflate(new Thickness(0, 10)).TransformToAABB(transform);
        }
        public bool IsReadOnly => true;
        public double Minimum => 0;
        public double Maximum => 1;
        public double Value => double.IsNaN(owner.Progress) ? 0 : owner.CoercedProgress;
        public double SmallChange => double.NaN;
        public double LargeChange => double.NaN;
        public void SetValue(double value) => throw new InvalidOperationException("Progress indicators are read-only.");
    }
}
