using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Parameters of Compose HorizontalDivider / VerticalDivider on a native Separator.</summary>
public static class MaterialDivider
{
    public static readonly AttachedProperty<Orientation> OrientationProperty =
        AvaloniaProperty.RegisterAttached<Separator, Orientation>("Orientation", typeof(MaterialDivider), Orientation.Horizontal);
    public static Orientation GetOrientation(Separator owner) => owner.GetValue(OrientationProperty);
    public static void SetOrientation(Separator owner, Orientation value) => owner.SetValue(OrientationProperty, value);

    /// <summary>DIP stroke width. Zero is a device hairline and keeps zero layout thickness.</summary>
    public static readonly AttachedProperty<double> ThicknessProperty =
        AvaloniaProperty.RegisterAttached<Separator, double>("Thickness", typeof(MaterialDivider));
    public static double GetThickness(Separator owner) => owner.GetValue(ThicknessProperty);
    public static void SetThickness(Separator owner, double value) => owner.SetValue(ThicknessProperty, value);

    public static readonly AttachedProperty<Color> ColorProperty =
        AvaloniaProperty.RegisterAttached<Separator, Color>("Color", typeof(MaterialDivider));
    public static Color GetColor(Separator owner) => owner.GetValue(ColorProperty);
    public static void SetColor(Separator owner, Color value) => owner.SetValue(ColorProperty, value);
}
