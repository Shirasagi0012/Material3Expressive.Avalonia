using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3Expressive.Foundation.Interaction;

/// <summary>The Material focus indication selected for a control and its descendants.</summary>
public enum FocusFeedback { Opacity, InsetRing }

/// <summary>Inset focus-ring dimensions in DIP, matching RippleThemeConfiguration.Focus.InsetRing.</summary>
public sealed record InsetFocusRingMetrics(double OuterStrokeInset = 0, double OuterStrokeWidth = 2,
    double InnerStrokeInset = 1, double InnerStrokeWidth = 3)
{
    public InsetFocusRingMetrics() : this(0, 2, 1, 3) { }
}

public static partial class MaterialInteraction
{
    public static readonly AttachedProperty<FocusFeedback> FocusFeedbackProperty =
        AvaloniaProperty.RegisterAttached<Control, FocusFeedback>("FocusFeedback", typeof(MaterialInteraction), inherits: true);
    public static FocusFeedback GetFocusFeedback(Control control) => control.GetValue(FocusFeedbackProperty);
    public static void SetFocusFeedback(Control control, FocusFeedback value) => control.SetValue(FocusFeedbackProperty, value);

    public static readonly AttachedProperty<InsetFocusRingMetrics> FocusRingMetricsProperty =
        AvaloniaProperty.RegisterAttached<Control, InsetFocusRingMetrics>("FocusRingMetrics", typeof(MaterialInteraction),
            new(), inherits: true, validate: value => value is not null
                && Valid(value.OuterStrokeInset) && Valid(value.OuterStrokeWidth)
                && Valid(value.InnerStrokeInset) && Valid(value.InnerStrokeWidth));
    public static InsetFocusRingMetrics GetFocusRingMetrics(Control control) => control.GetValue(FocusRingMetricsProperty);
    public static void SetFocusRingMetrics(Control control, InsetFocusRingMetrics value) => control.SetValue(FocusRingMetricsProperty, value);

    /// <summary>Null uses the current theme's Secondary color.</summary>
    public static readonly AttachedProperty<Color?> FocusRingOuterColorProperty =
        AvaloniaProperty.RegisterAttached<Control, Color?>("FocusRingOuterColor", typeof(MaterialInteraction), inherits: true);
    public static Color? GetFocusRingOuterColor(Control control) => control.GetValue(FocusRingOuterColorProperty);
    public static void SetFocusRingOuterColor(Control control, Color? value) => control.SetValue(FocusRingOuterColorProperty, value);

    /// <summary>Null uses the current theme's OnSecondary color.</summary>
    public static readonly AttachedProperty<Color?> FocusRingInnerColorProperty =
        AvaloniaProperty.RegisterAttached<Control, Color?>("FocusRingInnerColor", typeof(MaterialInteraction), inherits: true);
    public static Color? GetFocusRingInnerColor(Control control) => control.GetValue(FocusRingInnerColorProperty);
    public static void SetFocusRingInnerColor(Control control, Color? value) => control.SetValue(FocusRingInnerColorProperty, value);

    private static bool Valid(double value) => double.IsFinite(value) && value >= 0;
}
