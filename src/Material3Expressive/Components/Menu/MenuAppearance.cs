using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

public enum MenuColorStyle { Standard, Vibrant }

/// <summary>Color style for a native MenuFlyout using the Material Vertical theme.</summary>
public static class MenuAppearance
{
    public static readonly AttachedProperty<MenuColorStyle> ColorStyleProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, MenuColorStyle>(
            "ColorStyle", typeof(MenuAppearance), inherits: true);
    public static MenuColorStyle GetColorStyle(AvaloniaObject owner) => owner.GetValue(ColorStyleProperty);
    public static void SetColorStyle(AvaloniaObject owner, MenuColorStyle value) => owner.SetValue(ColorStyleProperty, value);

    static MenuAppearance()
    {
        ColorStyleProperty.Changed.AddClassHandler<MenuFlyout>((flyout, _) =>
        {
            // Flyout is not in the presenter's inheritance tree. Bridge the actual native
            // presenter on each opening, including when it is reused or the theme changes.
            flyout.Opening -= Opening;
            flyout.Opening += Opening;
            Apply(flyout);
        });
    }

    private static void Opening(object? sender, EventArgs e) => Apply((MenuFlyout)sender!);
    private static void Apply(MenuFlyout flyout)
    {
        if (flyout.Popup.Child is MenuFlyoutPresenter presenter)
            presenter.SetCurrentValue(ColorStyleProperty, GetColorStyle(flyout));
    }
}
