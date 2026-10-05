using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace Material3Expressive.Components;

/// <summary>Material appearance of a native ToggleButton; state, commands and input remain native.</summary>
public static class ToggleButtonAppearance
{
    static ToggleButtonAppearance()
    {
        // Preserve legacy attached-property lookup by owner/name while sharing MaterialButton's
        // property identities and storage.
        AvaloniaPropertyRegistry.Instance.Register(typeof(ToggleButtonAppearance), IconProperty);
        AvaloniaPropertyRegistry.Instance.Register(typeof(ToggleButtonAppearance), IconTemplateProperty);
    }

    public static readonly AttachedProperty<ToggleButtonColors?> ColorsProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonColors?>("Colors", typeof(ToggleButtonAppearance));
    public static ToggleButtonColors? GetColors(ToggleButton owner) => owner.GetValue(ColorsProperty);
    public static void SetColors(ToggleButton owner, ToggleButtonColors? value) => owner.SetValue(ColorsProperty, value);

    public static readonly AttachedProperty<CornerRadius> CheckedCornerRadiusProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, CornerRadius>("CheckedCornerRadius", typeof(ToggleButtonAppearance));
    public static CornerRadius GetCheckedCornerRadius(ToggleButton owner) => owner.GetValue(CheckedCornerRadiusProperty);
    public static void SetCheckedCornerRadius(ToggleButton owner, CornerRadius value) => owner.SetValue(CheckedCornerRadiusProperty, value);

    // Compatibility aliases: both names address the same attached-property identity and value.
    public static readonly AttachedProperty<object?> IconProperty = MaterialButton.IconProperty;
    public static object? GetIcon(ToggleButton owner) => owner.GetValue(IconProperty);
    public static void SetIcon(ToggleButton owner, object? value) => owner.SetValue(IconProperty, value);
    public static readonly AttachedProperty<IDataTemplate?> IconTemplateProperty = MaterialButton.IconTemplateProperty;
    public static IDataTemplate? GetIconTemplate(ToggleButton owner) => owner.GetValue(IconTemplateProperty);
    public static void SetIconTemplate(ToggleButton owner, IDataTemplate? value) => owner.SetValue(IconTemplateProperty, value);
}

/// <summary>The six slots of Compose ToggleButtonColors. Null Colors consumes dynamic theme tokens.</summary>
public sealed record ToggleButtonColors(Color ContainerColor, Color ContentColor,
    Color DisabledContainerColor, Color DisabledContentColor, Color CheckedContainerColor, Color CheckedContentColor);

// A template binding combines live tokens, native state, and an optional explicit color table.
internal sealed class ToggleButtonColorConverter : IMultiValueConverter
{
    public bool Content { get; set; }
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [ToggleButtonColors or null, bool, bool or null, Color, Color, Color])
            return AvaloniaProperty.UnsetValue;
        var colors = values[0] as ToggleButtonColors;
        var enabled = (bool)values[1]!;
        var isChecked = values[2] is true;
        var color = colors is null ? (Color)values[enabled ? isChecked ? 5 : 3 : 4]!
            : Content ? !enabled ? colors.DisabledContentColor : isChecked ? colors.CheckedContentColor : colors.ContentColor
            : !enabled ? colors.DisabledContainerColor : isChecked ? colors.CheckedContainerColor : colors.ContainerColor;
        return new SolidColorBrush(color);
    }
}
