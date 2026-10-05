using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Material appearance for the native ToggleSwitch; state and activation remain native.</summary>
public static class SwitchAppearance
{
    public static readonly AttachedProperty<object?> ThumbIconProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, object?>("ThumbIcon", typeof(SwitchAppearance));
    public static object? GetThumbIcon(ToggleSwitch owner) => owner.GetValue(ThumbIconProperty);
    public static void SetThumbIcon(ToggleSwitch owner, object? value) => owner.SetValue(ThumbIconProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> ThumbIconTemplateProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, IDataTemplate?>("ThumbIconTemplate", typeof(SwitchAppearance));
    public static IDataTemplate? GetThumbIconTemplate(ToggleSwitch owner) => owner.GetValue(ThumbIconTemplateProperty);
    public static void SetThumbIconTemplate(ToggleSwitch owner, IDataTemplate? value) => owner.SetValue(ThumbIconTemplateProperty, value);

    // Keep the original C# field and accessor entry points by aliasing the canonical properties.
    public static readonly AttachedProperty<object?> ThumbContentProperty = ThumbIconProperty;
    public static object? GetThumbContent(ToggleSwitch owner) => GetThumbIcon(owner);
    public static void SetThumbContent(ToggleSwitch owner, object? value) => SetThumbIcon(owner, value);
    public static readonly AttachedProperty<IDataTemplate?> ThumbContentTemplateProperty = ThumbIconTemplateProperty;
    public static IDataTemplate? GetThumbContentTemplate(ToggleSwitch owner) => GetThumbIconTemplate(owner);
    public static void SetThumbContentTemplate(ToggleSwitch owner, IDataTemplate? value) => SetThumbIconTemplate(owner, value);

    public static readonly AttachedProperty<SwitchColors?> ColorsProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, SwitchColors?>("Colors", typeof(SwitchAppearance));
    public static SwitchColors? GetColors(ToggleSwitch owner) => owner.GetValue(ColorsProperty);
    public static void SetColors(ToggleSwitch owner, SwitchColors? value) => owner.SetValue(ColorsProperty, value);
}

/// <summary>The sixteen final paint colors of Compose SwitchColors. Null Colors uses dynamic tokens.</summary>
public sealed record SwitchColors(Color CheckedThumbColor, Color CheckedTrackColor, Color CheckedBorderColor,
    Color CheckedIconColor, Color UncheckedThumbColor, Color UncheckedTrackColor, Color UncheckedBorderColor,
    Color UncheckedIconColor, Color DisabledCheckedThumbColor, Color DisabledCheckedTrackColor,
    Color DisabledCheckedBorderColor, Color DisabledCheckedIconColor, Color DisabledUncheckedThumbColor,
    Color DisabledUncheckedTrackColor, Color DisabledUncheckedBorderColor, Color DisabledUncheckedIconColor);
