using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Material3Expressive.Components;

// Orientation selects internal candidates. Public defaults remain ordinary theme setters,
// so application styles can replace dimensions, the menu, or the template.
internal static class ScrollBarThemeDefaults
{
    public static readonly AttachedProperty<double> WidthProperty = AvaloniaProperty.RegisterAttached<ScrollBar, double>("Width", typeof(ScrollBarThemeDefaults), double.NaN);
    public static double GetWidth(ScrollBar owner) => owner.GetValue(WidthProperty);
    public static void SetWidth(ScrollBar owner, double value) => owner.SetValue(WidthProperty, value);
    public static readonly AttachedProperty<double> HeightProperty = AvaloniaProperty.RegisterAttached<ScrollBar, double>("Height", typeof(ScrollBarThemeDefaults), double.NaN);
    public static double GetHeight(ScrollBar owner) => owner.GetValue(HeightProperty);
    public static void SetHeight(ScrollBar owner, double value) => owner.SetValue(HeightProperty, value);
    public static readonly AttachedProperty<FlyoutBase?> ContextFlyoutProperty = AvaloniaProperty.RegisterAttached<ScrollBar, FlyoutBase?>("ContextFlyout", typeof(ScrollBarThemeDefaults));
    public static FlyoutBase? GetContextFlyout(ScrollBar owner) => owner.GetValue(ContextFlyoutProperty);
    public static void SetContextFlyout(ScrollBar owner, FlyoutBase? value) => owner.SetValue(ContextFlyoutProperty, value);
    public static readonly AttachedProperty<IControlTemplate?> TemplateProperty = AvaloniaProperty.RegisterAttached<ScrollBar, IControlTemplate?>("Template", typeof(ScrollBarThemeDefaults));
    public static IControlTemplate? GetTemplate(ScrollBar owner) => owner.GetValue(TemplateProperty);
    public static void SetTemplate(ScrollBar owner, IControlTemplate? value) => owner.SetValue(TemplateProperty, value);
}
