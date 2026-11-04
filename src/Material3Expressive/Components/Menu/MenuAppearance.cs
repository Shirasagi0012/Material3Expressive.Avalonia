using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Material3Expressive.Components;

public enum MenuColorStyle { Standard, Vibrant }

/// <summary>Color style and content slots for native menus using the Material Vertical theme.</summary>
public static class MenuAppearance
{
    public static readonly AttachedProperty<object?> SupportingTextProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, object?>("SupportingText", typeof(MenuAppearance));
    public static object? GetSupportingText(MenuItem owner) => owner.GetValue(SupportingTextProperty);
    public static void SetSupportingText(MenuItem owner, object? value) => owner.SetValue(SupportingTextProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> SupportingTextTemplateProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, IDataTemplate?>("SupportingTextTemplate", typeof(MenuAppearance));
    public static IDataTemplate? GetSupportingTextTemplate(MenuItem owner) => owner.GetValue(SupportingTextTemplateProperty);
    public static void SetSupportingTextTemplate(MenuItem owner, IDataTemplate? value) => owner.SetValue(SupportingTextTemplateProperty, value);

    public static readonly AttachedProperty<object?> TrailingContentProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, object?>("TrailingContent", typeof(MenuAppearance));
    public static object? GetTrailingContent(MenuItem owner) => owner.GetValue(TrailingContentProperty);
    public static void SetTrailingContent(MenuItem owner, object? value) => owner.SetValue(TrailingContentProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> TrailingContentTemplateProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, IDataTemplate?>("TrailingContentTemplate", typeof(MenuAppearance));
    public static IDataTemplate? GetTrailingContentTemplate(MenuItem owner) => owner.GetValue(TrailingContentTemplateProperty);
    public static void SetTrailingContentTemplate(MenuItem owner, IDataTemplate? value) => owner.SetValue(TrailingContentTemplateProperty, value);

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
