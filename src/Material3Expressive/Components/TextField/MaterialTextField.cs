using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
namespace Material3Expressive.Components;

/// <summary>Material decoration for a native TextBox. Editing, selection and IME remain native.</summary>
public static class MaterialTextField
{
    public static readonly AttachedProperty<object?> LabelProperty =
        AvaloniaProperty.RegisterAttached<TextBox, object?>("Label", typeof(MaterialTextField), null);
    public static object? GetLabel(TextBox owner) => owner.GetValue(LabelProperty);
    public static void SetLabel(TextBox owner, object? value) => owner.SetValue(LabelProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> LabelTemplateProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IDataTemplate?>("LabelTemplate", typeof(MaterialTextField), null);
    public static IDataTemplate? GetLabelTemplate(TextBox owner) => owner.GetValue(LabelTemplateProperty);
    public static void SetLabelTemplate(TextBox owner, IDataTemplate? value) => owner.SetValue(LabelTemplateProperty, value);

    public static readonly AttachedProperty<object?> PlaceholderProperty =
        AvaloniaProperty.RegisterAttached<TextBox, object?>("Placeholder", typeof(MaterialTextField), null);
    public static object? GetPlaceholder(TextBox owner) => owner.GetValue(PlaceholderProperty);
    public static void SetPlaceholder(TextBox owner, object? value) => owner.SetValue(PlaceholderProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> PlaceholderTemplateProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IDataTemplate?>("PlaceholderTemplate", typeof(MaterialTextField), null);
    public static IDataTemplate? GetPlaceholderTemplate(TextBox owner) => owner.GetValue(PlaceholderTemplateProperty);
    public static void SetPlaceholderTemplate(TextBox owner, IDataTemplate? value) => owner.SetValue(PlaceholderTemplateProperty, value);

    public static readonly AttachedProperty<object?> SupportingTextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, object?>("SupportingText", typeof(MaterialTextField), null);
    public static object? GetSupportingText(TextBox owner) => owner.GetValue(SupportingTextProperty);
    public static void SetSupportingText(TextBox owner, object? value) => owner.SetValue(SupportingTextProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> SupportingTextTemplateProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IDataTemplate?>("SupportingTextTemplate", typeof(MaterialTextField), null);
    public static IDataTemplate? GetSupportingTextTemplate(TextBox owner) => owner.GetValue(SupportingTextTemplateProperty);
    public static void SetSupportingTextTemplate(TextBox owner, IDataTemplate? value) => owner.SetValue(SupportingTextTemplateProperty, value);

    public static readonly AttachedProperty<object?> PrefixProperty =
        AvaloniaProperty.RegisterAttached<TextBox, object?>("Prefix", typeof(MaterialTextField), null);
    public static object? GetPrefix(TextBox owner) => owner.GetValue(PrefixProperty);
    public static void SetPrefix(TextBox owner, object? value) => owner.SetValue(PrefixProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> PrefixTemplateProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IDataTemplate?>("PrefixTemplate", typeof(MaterialTextField), null);
    public static IDataTemplate? GetPrefixTemplate(TextBox owner) => owner.GetValue(PrefixTemplateProperty);
    public static void SetPrefixTemplate(TextBox owner, IDataTemplate? value) => owner.SetValue(PrefixTemplateProperty, value);

    public static readonly AttachedProperty<object?> SuffixProperty =
        AvaloniaProperty.RegisterAttached<TextBox, object?>("Suffix", typeof(MaterialTextField), null);
    public static object? GetSuffix(TextBox owner) => owner.GetValue(SuffixProperty);
    public static void SetSuffix(TextBox owner, object? value) => owner.SetValue(SuffixProperty, value);

    public static readonly AttachedProperty<IDataTemplate?> SuffixTemplateProperty =
        AvaloniaProperty.RegisterAttached<TextBox, IDataTemplate?>("SuffixTemplate", typeof(MaterialTextField), null);
    public static IDataTemplate? GetSuffixTemplate(TextBox owner) => owner.GetValue(SuffixTemplateProperty);
    public static void SetSuffixTemplate(TextBox owner, IDataTemplate? value) => owner.SetValue(SuffixTemplateProperty, value);

    public static readonly AttachedProperty<bool> IsErrorProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsError", typeof(MaterialTextField), false);
    public static bool GetIsError(TextBox owner) => owner.GetValue(IsErrorProperty);
    public static void SetIsError(TextBox owner, bool value) => owner.SetValue(IsErrorProperty, value);

    public static readonly AttachedProperty<string> ErrorMessageProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string>("ErrorMessage", typeof(MaterialTextField), "Invalid input");
    public static string GetErrorMessage(TextBox owner) => owner.GetValue(ErrorMessageProperty);
    public static void SetErrorMessage(TextBox owner, string value) => owner.SetValue(ErrorMessageProperty, value);

    public static readonly AttachedProperty<TextFieldLabelPosition> LabelPositionProperty =
        AvaloniaProperty.RegisterAttached<TextBox, TextFieldLabelPosition>("LabelPosition", typeof(MaterialTextField), TextFieldLabelPosition.Inside);
    public static TextFieldLabelPosition GetLabelPosition(TextBox owner) => owner.GetValue(LabelPositionProperty);
    public static void SetLabelPosition(TextBox owner, TextFieldLabelPosition value) => owner.SetValue(LabelPositionProperty, value);

    public static readonly AttachedProperty<bool> AlwaysMinimizeLabelProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("AlwaysMinimizeLabel", typeof(MaterialTextField), false);
    public static bool GetAlwaysMinimizeLabel(TextBox owner) => owner.GetValue(AlwaysMinimizeLabelProperty);
    public static void SetAlwaysMinimizeLabel(TextBox owner, bool value) => owner.SetValue(AlwaysMinimizeLabelProperty, value);

    public static readonly AttachedProperty<TextFieldLabelAlignment> ExpandedLabelAlignmentProperty =
        AvaloniaProperty.RegisterAttached<TextBox, TextFieldLabelAlignment>("ExpandedLabelAlignment", typeof(MaterialTextField), TextFieldLabelAlignment.Start);
    public static TextFieldLabelAlignment GetExpandedLabelAlignment(TextBox owner) => owner.GetValue(ExpandedLabelAlignmentProperty);
    public static void SetExpandedLabelAlignment(TextBox owner, TextFieldLabelAlignment value) => owner.SetValue(ExpandedLabelAlignmentProperty, value);

    public static readonly AttachedProperty<TextFieldLabelAlignment> MinimizedLabelAlignmentProperty =
        AvaloniaProperty.RegisterAttached<TextBox, TextFieldLabelAlignment>("MinimizedLabelAlignment", typeof(MaterialTextField), TextFieldLabelAlignment.Start);
    public static TextFieldLabelAlignment GetMinimizedLabelAlignment(TextBox owner) => owner.GetValue(MinimizedLabelAlignmentProperty);
    public static void SetMinimizedLabelAlignment(TextBox owner, TextFieldLabelAlignment value) => owner.SetValue(MinimizedLabelAlignmentProperty, value);

    public static readonly AttachedProperty<bool> OutlinedProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("Outlined", typeof(MaterialTextField), false);
    public static bool GetOutlined(TextBox owner) => owner.GetValue(OutlinedProperty);
    public static void SetOutlined(TextBox owner, bool value) => owner.SetValue(OutlinedProperty, value);

}

public enum TextFieldLabelPosition { Inside, Cutout, Above }
/// <summary>Logical horizontal alignment; Start and End follow FlowDirection.</summary>
public enum TextFieldLabelAlignment { Start, Center, End }
