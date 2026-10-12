using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Material3Expressive.Components;

// Native MenuFlyout binds a local null ItemContainerTheme. A theme Setter cannot replace that
// binding. Supply defaults on descendants instead, so explicit item/container themes still win and
// native container generation stays intact.
//
// The default is assigned imperatively, the way ItemsControl assigns ItemContainerTheme, not through
// a descendant-style Theme binding: such a style is withdrawn and re-applied whenever the item's
// popup closes and reopens, and when the template survives that round trip the re-applied theme's
// /template/ styles no longer reach it (no icon, no chevron, no trailing slot, and no hover because
// the MaterialSurface attached while MaterialInteraction.IsInteractive was unset). A missing value
// while an item is detached is therefore ignored rather than clearing Theme.
internal static class MenuThemeProperties
{
    public static readonly AttachedProperty<ControlTheme?> ItemThemeProperty =
        AvaloniaProperty.RegisterAttached<Control, ControlTheme?>("ItemTheme", typeof(MenuThemeProperties), inherits: true);

    // The theme this helper assigned, so a Theme set by the app (or by ItemsControl from a real
    // ItemContainerTheme) is never overwritten.
    private static readonly AttachedProperty<ControlTheme?> AppliedThemeProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, ControlTheme?>("AppliedTheme", typeof(MenuThemeProperties));

    static MenuThemeProperties()
    {
        ItemThemeProperty.Changed.AddClassHandler<MenuItem>((item, _) => Apply(item));
        StyledElement.ParentProperty.Changed.AddClassHandler<MenuItem>((item, _) => Apply(item));
    }

    public static ControlTheme? GetItemTheme(Control owner) => owner.GetValue(ItemThemeProperty);
    public static void SetItemTheme(Control owner, ControlTheme? value) => owner.SetValue(ItemThemeProperty, value);

    private static void Apply(MenuItem item)
    {
        // The owning ItemsControl's ItemContainerTheme wins, as in ItemsControl.PrepareItemContainer.
        var theme = (item.Parent as ItemsControl)?.ItemContainerTheme ?? GetItemTheme(item);
        if (theme is null || theme.TargetType?.IsAssignableFrom(item.StyleKey) != true) return;
        if (item.IsSet(StyledElement.ThemeProperty) && !ReferenceEquals(item.GetValue(AppliedThemeProperty), item.Theme)) return;
        if (ReferenceEquals(item.Theme, theme)) return;
        item.SetCurrentValue(StyledElement.ThemeProperty, theme);
        item.SetValue(AppliedThemeProperty, theme);
    }
}
