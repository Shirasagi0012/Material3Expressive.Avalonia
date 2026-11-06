/*
 * Translated from AndroidX Material3 Scrollbar.kt, Copyright 2026 The Android Open Source Project.
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0).
 * Fixed AndroidX / Core baseline: docs/upstream-baseline.json.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

/// <summary>A visual-only scroll indicator. Place over the viewport and bind its three scroll metrics, in DIP.</summary>
public sealed class NonInteractiveScrollBar : Control
{
    public static readonly StyledProperty<double> ContentSizeProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, double>(nameof(ContentSize));
    public static readonly StyledProperty<double> ViewportSizeProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, double>(nameof(ViewportSize));
    public static readonly StyledProperty<double> ScrollOffsetProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, double>(nameof(ScrollOffset));
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, Orientation>(nameof(Orientation), Orientation.Vertical);
    public static readonly StyledProperty<Color> ThumbColorProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, Color>(nameof(ThumbColor));
    public static readonly StyledProperty<Color> TrackColorProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, Color>(nameof(TrackColor));
    public static readonly StyledProperty<double> ThicknessProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, double>(nameof(Thickness));
    public static readonly StyledProperty<double> ThumbMinLengthProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, double>(nameof(ThumbMinLength));
    public static readonly StyledProperty<double> ThumbMaxLengthFractionProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, double>(nameof(ThumbMaxLengthFraction), validate: value => value >= 0 && value <= 1);
    public static readonly StyledProperty<bool> IsFadeEnabledProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, bool>(nameof(IsFadeEnabled), true);
    public static readonly StyledProperty<TimeSpan> FadeDurationProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, TimeSpan>(nameof(FadeDuration));
    public static readonly StyledProperty<TimeSpan> FadeDelayProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, TimeSpan>(nameof(FadeDelay));
    public static readonly StyledProperty<double> MainAxisTrackInsetProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, double>(nameof(MainAxisTrackInset));
    public static readonly StyledProperty<double> CrossAxisTrackInsetProperty = AvaloniaProperty.Register<NonInteractiveScrollBar, double>(nameof(CrossAxisTrackInset));

    public double ContentSize { get => GetValue(ContentSizeProperty); set => SetValue(ContentSizeProperty, value); }
    public double ViewportSize { get => GetValue(ViewportSizeProperty); set => SetValue(ViewportSizeProperty, value); }
    public double ScrollOffset { get => GetValue(ScrollOffsetProperty); set => SetValue(ScrollOffsetProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public Color ThumbColor { get => GetValue(ThumbColorProperty); set => SetValue(ThumbColorProperty, value); }
    public Color TrackColor { get => GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
    public double ThumbMinLength { get => GetValue(ThumbMinLengthProperty); set => SetValue(ThumbMinLengthProperty, value); }
    public double ThumbMaxLengthFraction { get => GetValue(ThumbMaxLengthFractionProperty); set => SetValue(ThumbMaxLengthFractionProperty, value); }
    public bool IsFadeEnabled { get => GetValue(IsFadeEnabledProperty); set => SetValue(IsFadeEnabledProperty, value); }
    public TimeSpan FadeDuration { get => GetValue(FadeDurationProperty); set => SetValue(FadeDurationProperty, value); }
    public TimeSpan FadeDelay { get => GetValue(FadeDelayProperty); set => SetValue(FadeDelayProperty, value); }
    public double MainAxisTrackInset { get => GetValue(MainAxisTrackInsetProperty); set => SetValue(MainAxisTrackInsetProperty, value); }
    public double CrossAxisTrackInset { get => GetValue(CrossAxisTrackInsetProperty); set => SetValue(CrossAxisTrackInsetProperty, value); }

    protected override bool BypassFlowDirectionPolicies => true;
    private AnimationChildScope? _scope;
    private Animatable<float>? _alpha;
    private TaskCompletionSource _scrollChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _fadeCancellation;
    private Task? _fadeJob;
    private int _lastOffset;
    private bool _drawing;
    internal float PaintedAlpha => IsFadeEnabled ? _alpha?.Value ?? 0 : 1;
    internal Rect? PaintedThumb { get; private set; }
    internal Rect? PaintedTrack { get; private set; }

    static NonInteractiveScrollBar()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<NonInteractiveScrollBar>(false);
        AffectsRender<NonInteractiveScrollBar>(ContentSizeProperty, ViewportSizeProperty, ScrollOffsetProperty, FlowDirectionProperty);
    }

    protected override Size MeasureOverride(Size availableSize) => new(
        double.IsFinite(availableSize.Width) ? availableSize.Width : 0,
        double.IsFinite(availableSize.Height) ? availableSize.Height : 0);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachMotion();
    }

    private void AttachMotion()
    {
        _lastOffset = MathUtils.DipToPx((float)ScrollOffset, (float)LayoutHelper.GetLayoutScale(this));
        _alpha ??= new Animatable<float>(new FloatConverter(), IsFadeEnabled ? 0 : 1, VisibilityThresholds.Float);
        _scope = AnimationScope.Get(this).CreateChild("Non-interactive scrollbar");
        _scope.SubscribeValue(_alpha, _ => { if (!_drawing) InvalidateVisual(); });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachMotion();
        base.OnDetachedFromVisualTree(e);
    }

    private void DetachMotion()
    {
        _fadeCancellation?.Cancel(); _fadeCancellation?.Dispose(); _fadeCancellation = null;
        _scope?.Dispose(); _scope = null;
        _scrollChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _fadeJob = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != OrientationProperty && change.Property != ThumbColorProperty && change.Property != TrackColorProperty &&
            change.Property != ThicknessProperty && change.Property != ThumbMinLengthProperty && change.Property != ThumbMaxLengthFractionProperty &&
            change.Property != IsFadeEnabledProperty && change.Property != FadeDurationProperty && change.Property != FadeDelayProperty &&
            change.Property != MainAxisTrackInsetProperty && change.Property != CrossAxisTrackInsetProperty) return;
        // NonInteractiveScrollbarNodeWrapper.update undelegates the old node, including its alpha.
        DetachMotion(); _alpha = null;
        if (this.IsAttachedToVisualTree()) AttachMotion();
        InvalidateVisual();
    }

    private void ObserveScroll(int offset)
    {
        if (!IsFadeEnabled || offset == _lastOffset || _scope is not { } scope || _alpha is not { } alpha) return;
        _lastOffset = offset;
        _scrollChanged.TrySetResult();
        if (alpha.TargetValue != 0 && _fadeJob is { IsCompleted: false }) return;
        _fadeCancellation?.Cancel(); _fadeCancellation?.Dispose();
        var cancellation = _fadeCancellation = CancellationTokenSource.CreateLinkedTokenSource(scope.Lifetime);
        var duration = FadeDuration;
        var delay = FadeDelay;
        _fadeJob = scope.Launch(async _ =>
        {
            var ct = cancellation.Token;
            if (!await alpha.SnapToAsync(1, ct)) return;
            while (true)
            {
                _scrollChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
                if (delay <= TimeSpan.Zero) break;
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var timeout = scope.DelayAsync(delay, wait.Token);
                var next = scope.WaitForSignalAsync(_scrollChanged.Task, wait.Token);
                var completed = await Task.WhenAny(timeout, next);
                wait.Cancel();
                if (ct.IsCancellationRequested) return;
                if (completed == timeout) break;
                if (!await next) return;
            }
            await alpha.AnimateToAsync(scope, 0, new TweenSpec<float>(duration), cancellationToken: ct);
        }, "Scrollbar idle and fade");
    }

    public override void Render(DrawingContext context)
    {
        PaintedThumb = PaintedTrack = null;
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        // ScrollIndicatorState exposes integer physical pixels. Bindings accept native Avalonia DIP.
        var content = (float)MathUtils.DipToPx((float)ContentSize, scale);
        var viewport = (float)MathUtils.DipToPx((float)ViewportSize, scale);
        if (content <= viewport || viewport == 0 || content == 0) return;
        var offset = MathUtils.DipToPx((float)ScrollOffset, scale);
        _drawing = true;
        try { ObserveScroll(offset); }
        finally { _drawing = false; }
        var alpha = PaintedAlpha;
        var thumb = Fade(ThumbColor, alpha);
        var track = Fade(TrackColor, alpha);
        if (thumb.A == 0 && track.A == 0) return;
        var width = (float)Bounds.Width * scale;
        var height = (float)Bounds.Height * scale;
        var mainInset = (float)MainAxisTrackInset * scale;
        var crossInset = (float)CrossAxisTrackInset * scale;
        var vertical = Orientation == Orientation.Vertical;
        var trackLength = (vertical ? height : width) - 2 * mainInset;
        var minLength = (float)ThumbMinLength * scale;
        if (trackLength <= 0 || trackLength < minLength) return;
        var thickness = (float)Thickness * scale;
        // Preserve coerceIn's invalid-range failure when maxLength < minLength.
        var thumbLength = Math.Clamp(trackLength * (viewport / content), minLength, trackLength * (float)ThumbMaxLengthFraction);
        var thumbOffset = content - viewport > 0 ? (offset / (content - viewport)) * (trackLength - thumbLength) : 0;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var cross = vertical ? (rtl ? crossInset : width - crossInset - thickness) : height - crossInset - thickness;
        var start = !vertical && rtl ? width - mainInset - thumbLength - thumbOffset : mainInset + thumbOffset;
        var trackRect = Rect(mainInset, trackLength);
        var thumbRect = Rect(start, thumbLength);
        if (track.A > 0) { PaintedTrack = trackRect; context.DrawRectangle(new SolidColorBrush(track), null, trackRect); }
        if (thumb.A > 0) { PaintedThumb = thumbRect; context.DrawRectangle(new SolidColorBrush(thumb), null, thumbRect, thickness / (2 * scale), thickness / (2 * scale)); }

        Rect Rect(float position, float length) => vertical
            ? new Rect(cross / scale, position / scale, thickness / scale, length / scale)
            : new Rect(position / scale, cross / scale, length / scale, thickness / scale);
        static Color Fade(Color color, float alpha) => alpha == 1 ? color : Color.FromArgb(
            (byte)(color.A * alpha + .5f), color.R, color.G, color.B);
    }
}

// Color.copy(alpha = ThumbOpacity) replaces the role's alpha, unlike WithAlpha's multiplication.
internal static class NonInteractiveScrollBarColorConverter
{
    public static readonly Avalonia.Data.Converters.FuncMultiValueConverter<object, object> Instance = new(values =>
    {
        var inputs = values.ToArray();
        return inputs is [Color color, double opacity]
            ? Color.FromArgb((byte)(Math.Clamp((float)opacity, 0, 1) * 255 + .5f), color.R, color.G, color.B)
            : AvaloniaProperty.UnsetValue;
    });
}
