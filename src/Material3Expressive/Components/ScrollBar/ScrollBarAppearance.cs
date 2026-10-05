using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Paint options for the native ScrollBar. Range, layout and input remain native.</summary>
public static class ScrollBarAppearance
{
    public static readonly AttachedProperty<double> ThicknessProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, double>("Thickness", typeof(ScrollBarAppearance),
            validate: value => double.IsFinite(value) && value >= 0);
    public static double GetThickness(ScrollBar owner) => owner.GetValue(ThicknessProperty);
    public static void SetThickness(ScrollBar owner, double value) => owner.SetValue(ThicknessProperty, value);

    public static readonly AttachedProperty<double> ThumbMinLengthProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, double>("ThumbMinLength", typeof(ScrollBarAppearance),
            validate: value => double.IsFinite(value) && value >= 0);
    public static double GetThumbMinLength(ScrollBar owner) => owner.GetValue(ThumbMinLengthProperty);
    public static void SetThumbMinLength(ScrollBar owner, double value) => owner.SetValue(ThumbMinLengthProperty, value);

    public static readonly AttachedProperty<Color> UnhoverColorProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, Color>("UnhoverColor", typeof(ScrollBarAppearance));
    public static Color GetUnhoverColor(ScrollBar owner) => owner.GetValue(UnhoverColorProperty);
    public static void SetUnhoverColor(ScrollBar owner, Color value) => owner.SetValue(UnhoverColorProperty, value);

    public static readonly AttachedProperty<Color> HoverColorProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, Color>("HoverColor", typeof(ScrollBarAppearance));
    public static Color GetHoverColor(ScrollBar owner) => owner.GetValue(HoverColorProperty);
    public static void SetHoverColor(ScrollBar owner, Color value) => owner.SetValue(HoverColorProperty, value);

    public static readonly AttachedProperty<TimeSpan> HoverDurationProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, TimeSpan>("HoverDuration", typeof(ScrollBarAppearance),
            validate: value => value >= TimeSpan.Zero);
    public static TimeSpan GetHoverDuration(ScrollBar owner) => owner.GetValue(HoverDurationProperty);
    public static void SetHoverDuration(ScrollBar owner, TimeSpan value) => owner.SetValue(HoverDurationProperty, value);
}
