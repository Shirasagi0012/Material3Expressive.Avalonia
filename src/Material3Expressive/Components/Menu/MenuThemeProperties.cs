using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Material3Expressive.Components;

// Native MenuFlyout binds a local null ItemContainerTheme. A theme Setter cannot replace that
// binding. Supply defaults at Theme's style priority on descendants, so explicit item/container
// themes still win and native container generation stays intact.
internal static class MenuThemeProperties
{
    public static readonly AttachedProperty<ControlTheme?> ItemThemeProperty =
        AvaloniaProperty.RegisterAttached<Control, ControlTheme?>("ItemTheme", typeof(MenuThemeProperties), inherits: true);
    public static ControlTheme? GetItemTheme(Control owner) => owner.GetValue(ItemThemeProperty);
    public static void SetItemTheme(Control owner, ControlTheme? value) => owner.SetValue(ItemThemeProperty, value);
}
