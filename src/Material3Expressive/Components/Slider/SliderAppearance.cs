using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

/// <summary>Material paint options for the native Avalonia Slider. Values and input remain native.</summary>
public static class SliderAppearance
{
    public static readonly AttachedProperty<SliderColors?> ColorsProperty =
        AvaloniaProperty.RegisterAttached<Slider, SliderColors?>("Colors", typeof(SliderAppearance), null);
    public static SliderColors? GetColors(Slider owner) => owner.GetValue(ColorsProperty);
    public static void SetColors(Slider owner, SliderColors? value) => owner.SetValue(ColorsProperty, value);

    public static readonly AttachedProperty<bool> IsCenteredProperty =
        AvaloniaProperty.RegisterAttached<Slider, bool>("IsCentered", typeof(SliderAppearance), false);
    public static bool GetIsCentered(Slider owner) => owner.GetValue(IsCenteredProperty);
    public static void SetIsCentered(Slider owner, bool value) => owner.SetValue(IsCenteredProperty, value);

    public static readonly AttachedProperty<double> TrackHeightProperty =
        AvaloniaProperty.RegisterAttached<Slider, double>("TrackHeight", typeof(SliderAppearance));
    public static double GetTrackHeight(Slider owner) => owner.GetValue(TrackHeightProperty);
    public static void SetTrackHeight(Slider owner, double value) => owner.SetValue(TrackHeightProperty, value);

    public static readonly AttachedProperty<double> TrackCornerSizeProperty =
        AvaloniaProperty.RegisterAttached<Slider, double>("TrackCornerSize", typeof(SliderAppearance), double.NaN);
    public static double GetTrackCornerSize(Slider owner) => owner.GetValue(TrackCornerSizeProperty);
    public static void SetTrackCornerSize(Slider owner, double value) => owner.SetValue(TrackCornerSizeProperty, value);

    public static readonly AttachedProperty<double> TrackInsideCornerSizeProperty =
        AvaloniaProperty.RegisterAttached<Slider, double>("TrackInsideCornerSize", typeof(SliderAppearance));
    public static double GetTrackInsideCornerSize(Slider owner) => owner.GetValue(TrackInsideCornerSizeProperty);
    public static void SetTrackInsideCornerSize(Slider owner, double value) => owner.SetValue(TrackInsideCornerSizeProperty, value);

    public static readonly AttachedProperty<double> ThumbTrackGapSizeProperty =
        AvaloniaProperty.RegisterAttached<Slider, double>("ThumbTrackGapSize", typeof(SliderAppearance));
    public static double GetThumbTrackGapSize(Slider owner) => owner.GetValue(ThumbTrackGapSizeProperty);
    public static void SetThumbTrackGapSize(Slider owner, double value) => owner.SetValue(ThumbTrackGapSizeProperty, value);

    public static readonly AttachedProperty<bool> EnableCornerShrinkingProperty =
        AvaloniaProperty.RegisterAttached<Slider, bool>("EnableCornerShrinking", typeof(SliderAppearance), false);
    public static bool GetEnableCornerShrinking(Slider owner) => owner.GetValue(EnableCornerShrinkingProperty);
    public static void SetEnableCornerShrinking(Slider owner, bool value) => owner.SetValue(EnableCornerShrinkingProperty, value);

    public static readonly AttachedProperty<double> TickSizeProperty =
        AvaloniaProperty.RegisterAttached<Slider, double>("TickSize", typeof(SliderAppearance));
    public static double GetTickSize(Slider owner) => owner.GetValue(TickSizeProperty);
    public static void SetTickSize(Slider owner, double value) => owner.SetValue(TickSizeProperty, value);

    public static readonly AttachedProperty<double> StopIndicatorSizeProperty =
        AvaloniaProperty.RegisterAttached<Slider, double>("StopIndicatorSize", typeof(SliderAppearance));
    public static double GetStopIndicatorSize(Slider owner) => owner.GetValue(StopIndicatorSizeProperty);
    public static void SetStopIndicatorSize(Slider owner, double value) => owner.SetValue(StopIndicatorSizeProperty, value);

    public static readonly AttachedProperty<bool> ShowStopIndicatorProperty =
        AvaloniaProperty.RegisterAttached<Slider, bool>("ShowStopIndicator", typeof(SliderAppearance), true);
    public static bool GetShowStopIndicator(Slider owner) => owner.GetValue(ShowStopIndicatorProperty);
    public static void SetShowStopIndicator(Slider owner, bool value) => owner.SetValue(ShowStopIndicatorProperty, value);

    internal static float Fraction(Slider owner) => owner.Maximum == owner.Minimum ? 0
        : (float)Math.Clamp((owner.Value - owner.Minimum) / (owner.Maximum - owner.Minimum), 0, 1);

    // These are paint coordinates only. Native Slider still resolves all snapping,
    // including irregular Ticks and its own tie rule.
    internal static float[] TickFractions(Slider owner)
    {
        var range = owner.Maximum - owner.Minimum;
        if (range <= 0) return [];
        if (owner.Ticks is { Count: > 0 } ticks)
            return ticks.Where(double.IsFinite).Where(v => v >= owner.Minimum && v <= owner.Maximum)
                .Select(v => (float)((v - owner.Minimum) / range)).Distinct().Order().ToArray();
        var frequency = owner.TickFrequency;
        if (!double.IsFinite(frequency) || frequency <= 0) return [];
        return Enumerable.Range(0, (int)Math.Min(int.MaxValue - 1, Math.Ceiling(range / frequency)))
            .Select(i => (float)(i * frequency / range)).Append(1).Distinct().ToArray();
    }
}
