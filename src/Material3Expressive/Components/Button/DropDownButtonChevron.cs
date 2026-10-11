using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Components;

// DropDownButton's trailing chevron rotation. The angle is a plain styled value so the theme can
// state the destination (0 closed, 180 while :flyout-open) and a ComposeAnimation declaration can
// animate it like any other property; the change handler only maps it onto RenderTransform.
internal static class DropDownButtonChevron
{
    public static readonly AttachedProperty<double> AngleProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("Angle", typeof(DropDownButtonChevron));

    static DropDownButtonChevron()
    {
        AngleProperty.Changed.AddClassHandler<Visual>((visual, e) =>
            visual.RenderTransform = e.GetNewValue<double>() == 0 ? null : new RotateTransform(e.GetNewValue<double>()));
    }

    public static double GetAngle(Visual visual) => visual.GetValue(AngleProperty);
    public static void SetAngle(Visual visual, double value) => visual.SetValue(AngleProperty, value);
}
