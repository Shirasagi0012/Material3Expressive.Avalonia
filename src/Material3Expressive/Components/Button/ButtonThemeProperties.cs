using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

// Conditional theme choices stay separate from the native effective properties. This lets a
// regular user Style override size/state defaults, just as a local value or binding does.
// The values have no independent fallback policy; ControlThemes supply all Material defaults.
internal static class ButtonThemeProperties
{
    public static readonly AttachedProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.RegisterAttached<Button, IBrush?>("Background", typeof(ButtonThemeProperties));
    public static IBrush? GetBackground(Button owner) => owner.GetValue(BackgroundProperty);
    public static void SetBackground(Button owner, IBrush? value) => owner.SetValue(BackgroundProperty, value);

    public static readonly AttachedProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.RegisterAttached<Button, IBrush?>("Foreground", typeof(ButtonThemeProperties));
    public static IBrush? GetForeground(Button owner) => owner.GetValue(ForegroundProperty);
    public static void SetForeground(Button owner, IBrush? value) => owner.SetValue(ForegroundProperty, value);

    public static readonly AttachedProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.RegisterAttached<Button, IBrush?>("BorderBrush", typeof(ButtonThemeProperties));
    public static IBrush? GetBorderBrush(Button owner) => owner.GetValue(BorderBrushProperty);
    public static void SetBorderBrush(Button owner, IBrush? value) => owner.SetValue(BorderBrushProperty, value);

    public static readonly AttachedProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.RegisterAttached<Button, Thickness>("BorderThickness", typeof(ButtonThemeProperties));
    public static Thickness GetBorderThickness(Button owner) => owner.GetValue(BorderThicknessProperty);
    public static void SetBorderThickness(Button owner, Thickness value) => owner.SetValue(BorderThicknessProperty, value);

    public static readonly AttachedProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.RegisterAttached<Button, CornerRadius>("CornerRadius", typeof(ButtonThemeProperties));
    public static CornerRadius GetCornerRadius(Button owner) => owner.GetValue(CornerRadiusProperty);
    public static void SetCornerRadius(Button owner, CornerRadius value) => owner.SetValue(CornerRadiusProperty, value);

    public static readonly AttachedProperty<Thickness> PaddingProperty =
        AvaloniaProperty.RegisterAttached<Button, Thickness>("Padding", typeof(ButtonThemeProperties));
    public static Thickness GetPadding(Button owner) => owner.GetValue(PaddingProperty);
    public static void SetPadding(Button owner, Thickness value) => owner.SetValue(PaddingProperty, value);

    public static readonly AttachedProperty<TextStyle?> TextStyleProperty =
        AvaloniaProperty.RegisterAttached<Button, TextStyle?>("TextStyle", typeof(ButtonThemeProperties));
    public static TextStyle? GetTextStyle(Button owner) => owner.GetValue(TextStyleProperty);
    public static void SetTextStyle(Button owner, TextStyle? value) => owner.SetValue(TextStyleProperty, value);

    public static readonly AttachedProperty<double> WidthProperty =
        AvaloniaProperty.RegisterAttached<Button, double>("Width", typeof(ButtonThemeProperties), double.NaN);
    public static double GetWidth(Button owner) => owner.GetValue(WidthProperty);
    public static void SetWidth(Button owner, double value) => owner.SetValue(WidthProperty, value);

    public static readonly AttachedProperty<double> HeightProperty =
        AvaloniaProperty.RegisterAttached<Button, double>("Height", typeof(ButtonThemeProperties), double.NaN);
    public static double GetHeight(Button owner) => owner.GetValue(HeightProperty);
    public static void SetHeight(Button owner, double value) => owner.SetValue(HeightProperty, value);

    public static readonly AttachedProperty<CornerRadius?> PressedCornerRadiusProperty =
        AvaloniaProperty.RegisterAttached<Button, CornerRadius?>("PressedCornerRadius", typeof(ButtonThemeProperties));
    public static CornerRadius? GetPressedCornerRadius(Button owner) => owner.GetValue(PressedCornerRadiusProperty);
    public static void SetPressedCornerRadius(Button owner, CornerRadius? value) => owner.SetValue(PressedCornerRadiusProperty, value);

    public static readonly AttachedProperty<CornerRadius> CheckedCornerRadiusProperty =
        AvaloniaProperty.RegisterAttached<Button, CornerRadius>("CheckedCornerRadius", typeof(ButtonThemeProperties));
    public static CornerRadius GetCheckedCornerRadius(Button owner) => owner.GetValue(CheckedCornerRadiusProperty);
    public static void SetCheckedCornerRadius(Button owner, CornerRadius value) => owner.SetValue(CheckedCornerRadiusProperty, value);

}
