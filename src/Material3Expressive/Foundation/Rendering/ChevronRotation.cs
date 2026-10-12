using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Foundation.Rendering;

// Chevron rotation shared by DropDownButton (0 closed, 180 while :flyout-open) and Expander (per
// ExpandDirection and IsExpanded). The angle is a plain styled value so a theme can state the
// destination and a ComposeAnimation declaration can animate it like any other property; the change
// handler only maps it onto RenderTransform.
internal static class ChevronRotation
{
    public static readonly AttachedProperty<double> AngleProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("Angle", typeof(ChevronRotation));

    static ChevronRotation()
    {
        AngleProperty.Changed.AddClassHandler<Visual>((visual, e) =>
            visual.RenderTransform = e.GetNewValue<double>() == 0 ? null : new RotateTransform(e.GetNewValue<double>()));
    }

    public static double GetAngle(Visual visual) => visual.GetValue(AngleProperty);
    public static void SetAngle(Visual visual, double value) => visual.SetValue(AngleProperty, value);
}
