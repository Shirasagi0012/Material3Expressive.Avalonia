using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Material decoration for a native ComboBox. Selection, text matching and keys stay native.</summary>
public static class MaterialComboBox
{
    public static readonly AttachedProperty<object?> LabelProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, object?>("Label", typeof(MaterialComboBox), null);
    public static object? GetLabel(ComboBox owner) => owner.GetValue(LabelProperty);
    public static void SetLabel(ComboBox owner, object? value) => owner.SetValue(LabelProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> LabelTemplateProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IDataTemplate?>("LabelTemplate", typeof(MaterialComboBox), null);
    public static IDataTemplate? GetLabelTemplate(ComboBox owner) => owner.GetValue(LabelTemplateProperty);
    public static void SetLabelTemplate(ComboBox owner, IDataTemplate? value) => owner.SetValue(LabelTemplateProperty, value);

    public static readonly AttachedProperty<object?> LeadingIconProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, object?>("LeadingIcon", typeof(MaterialComboBox));
    public static object? GetLeadingIcon(ComboBox owner) => owner.GetValue(LeadingIconProperty);
    public static void SetLeadingIcon(ComboBox owner, object? value) => owner.SetValue(LeadingIconProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> LeadingIconTemplateProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IDataTemplate?>("LeadingIconTemplate", typeof(MaterialComboBox));
    public static IDataTemplate? GetLeadingIconTemplate(ComboBox owner) => owner.GetValue(LeadingIconTemplateProperty);
    public static void SetLeadingIconTemplate(ComboBox owner, IDataTemplate? value) => owner.SetValue(LeadingIconTemplateProperty, value);

    public static readonly AttachedProperty<object?> SupportingTextProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, object?>("SupportingText", typeof(MaterialComboBox), null);
    public static object? GetSupportingText(ComboBox owner) => owner.GetValue(SupportingTextProperty);
    public static void SetSupportingText(ComboBox owner, object? value) => owner.SetValue(SupportingTextProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> SupportingTextTemplateProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IDataTemplate?>("SupportingTextTemplate", typeof(MaterialComboBox), null);
    public static IDataTemplate? GetSupportingTextTemplate(ComboBox owner) => owner.GetValue(SupportingTextTemplateProperty);
    public static void SetSupportingTextTemplate(ComboBox owner, IDataTemplate? value) => owner.SetValue(SupportingTextTemplateProperty, value);

    public static readonly AttachedProperty<bool> IsErrorProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("IsError", typeof(MaterialComboBox), false);
    public static bool GetIsError(ComboBox owner) => owner.GetValue(IsErrorProperty);
    public static void SetIsError(ComboBox owner, bool value) => owner.SetValue(IsErrorProperty, value);

    public static readonly AttachedProperty<string> ErrorMessageProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, string>("ErrorMessage", typeof(MaterialComboBox), "Invalid input");
    public static string GetErrorMessage(ComboBox owner) => owner.GetValue(ErrorMessageProperty);
    public static void SetErrorMessage(ComboBox owner, string value) => owner.SetValue(ErrorMessageProperty, value);

    public static readonly AttachedProperty<TextFieldLabelPosition> LabelPositionProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, TextFieldLabelPosition>("LabelPosition", typeof(MaterialComboBox), TextFieldLabelPosition.Inside);
    public static TextFieldLabelPosition GetLabelPosition(ComboBox owner) => owner.GetValue(LabelPositionProperty);
    public static void SetLabelPosition(ComboBox owner, TextFieldLabelPosition value) => owner.SetValue(LabelPositionProperty, value);

    public static readonly AttachedProperty<bool> OutlinedProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("Outlined", typeof(MaterialComboBox), false);
    public static bool GetOutlined(ComboBox owner) => owner.GetValue(OutlinedProperty);
    public static void SetOutlined(ComboBox owner, bool value) => owner.SetValue(OutlinedProperty, value);

    public static readonly AttachedProperty<MenuItemShapes?> ItemShapesProperty =
        AvaloniaProperty.RegisterAttached<ComboBoxItem, MenuItemShapes?>("ItemShapes", typeof(MaterialComboBox));
    public static MenuItemShapes? GetItemShapes(ComboBoxItem owner) => owner.GetValue(ItemShapesProperty);
    public static void SetItemShapes(ComboBoxItem owner, MenuItemShapes? value) => owner.SetValue(ItemShapesProperty, value);
}

internal static class ComboBoxThemeDefaults
{
    public static readonly AttachedProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IBrush?>("Foreground", typeof(ComboBoxThemeDefaults));
    public static IBrush? GetForeground(ComboBox owner) => owner.GetValue(ForegroundProperty);
    public static void SetForeground(ComboBox owner, IBrush? value) => owner.SetValue(ForegroundProperty, value);

    public static readonly AttachedProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IBrush?>("Background", typeof(ComboBoxThemeDefaults));
    public static IBrush? GetBackground(ComboBox owner) => owner.GetValue(BackgroundProperty);
    public static void SetBackground(ComboBox owner, IBrush? value) => owner.SetValue(BackgroundProperty, value);

    public static readonly AttachedProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IBrush?>("BorderBrush", typeof(ComboBoxThemeDefaults));
    public static IBrush? GetBorderBrush(ComboBox owner) => owner.GetValue(BorderBrushProperty);
    public static void SetBorderBrush(ComboBox owner, IBrush? value) => owner.SetValue(BorderBrushProperty, value);

    public static readonly AttachedProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, IBrush?>("PlaceholderForeground", typeof(ComboBoxThemeDefaults));
    public static IBrush? GetPlaceholderForeground(ComboBox owner) => owner.GetValue(PlaceholderForegroundProperty);
    public static void SetPlaceholderForeground(ComboBox owner, IBrush? value) => owner.SetValue(PlaceholderForegroundProperty, value);
}

// FuncDataTemplate<T> matches null for a reference type, and ContentPresenter builds that
// template when Content is null. An empty selection must not materialize a blank row.
internal sealed class ComboBoxSelectionTemplateConverter : IMultiValueConverter
{
    public static ComboBoxSelectionTemplateConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count >= 2 && values[0] is not null && values[0] != AvaloniaProperty.UnsetValue && values[1] is IDataTemplate template
            ? template : null;
}
