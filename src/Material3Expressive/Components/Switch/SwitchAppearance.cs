using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Material appearance for the native ToggleSwitch; state and activation remain native.</summary>
public static class SwitchAppearance
{
    public static readonly AttachedProperty<object?> ThumbContentProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, object?>("ThumbContent", typeof(SwitchAppearance));
    public static object? GetThumbContent(ToggleSwitch owner) => owner.GetValue(ThumbContentProperty);
    public static void SetThumbContent(ToggleSwitch owner, object? value) => owner.SetValue(ThumbContentProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> ThumbContentTemplateProperty =
        AvaloniaProperty.RegisterAttached<ToggleSwitch, IDataTemplate?>("ThumbContentTemplate", typeof(SwitchAppearance));
    public static IDataTemplate? GetThumbContentTemplate(ToggleSwitch owner) => owner.GetValue(ThumbContentTemplateProperty);
    public static void SetThumbContentTemplate(ToggleSwitch owner, IDataTemplate? value) => owner.SetValue(ThumbContentTemplateProperty, value);

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
