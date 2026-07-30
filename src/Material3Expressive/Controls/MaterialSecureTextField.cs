// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// SecureTextField.kt:45-201, 214-365 and BasicSecureTextField.kt:124-213,230-333.
// Avalonia's TextBox has all-or-nothing password masking. RevealLastTyped therefore falls back
// to Hidden on desktop rather than briefly exposing the entire secret.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Material3Expressive.Controls.Automation;

namespace Material3Expressive.Controls;

/// <summary>Defines how a <see cref="MaterialSecureTextField"/> obscures its text.</summary>
public enum SecureTextObfuscationMode
{
    /// <summary>Show the complete text.</summary>
    Visible,

    /// <summary>
    /// Request alpha25's brief last-character reveal. Avalonia desktop safely falls back to
    /// <see cref="Hidden"/> because its editor can only reveal the complete value.
    /// </summary>
    RevealLastTyped,

    /// <summary>Hide every character.</summary>
    Hidden,

    /// <summary>Use the desktop convention, which hides every character.</summary>
    System
}

/// <summary>
/// A single-line Material text field for passwords and other sensitive text.
/// </summary>
/// <remarks>
/// Editing, selection, IME, undo, and caret behavior remain native Avalonia <see cref="TextBox"/>
/// behavior. Cut and copy are disabled even while the value is visible; paste remains available.
/// The application restores the simple scalar <see cref="MaterialTextField.Text"/>,
/// <see cref="TextObfuscationMode"/>, and <see cref="IsPasswordVisible"/> properties directly.
/// </remarks>
public class MaterialSecureTextField : MaterialTextField
{
    public static readonly StyledProperty<SecureTextObfuscationMode> TextObfuscationModeProperty =
        AvaloniaProperty.Register<MaterialSecureTextField, SecureTextObfuscationMode>(
            nameof(TextObfuscationMode),
            SecureTextObfuscationMode.System,
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<char> TextObfuscationCharacterProperty =
        AvaloniaProperty.Register<MaterialSecureTextField, char>(
            nameof(TextObfuscationCharacter),
            '\u2022',
            validate: static value => value != '\0');

    public static readonly StyledProperty<bool> IsPasswordVisibleProperty =
        AvaloniaProperty.Register<MaterialSecureTextField, bool>(
            nameof(IsPasswordVisible),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Gets or sets the requested obfuscation behavior.</summary>
    public SecureTextObfuscationMode TextObfuscationMode
    {
        get => GetValue(TextObfuscationModeProperty);
        set => SetValue(TextObfuscationModeProperty, value);
    }

    /// <summary>Gets or sets the single character used to mask each text element.</summary>
    public char TextObfuscationCharacter
    {
        get => GetValue(TextObfuscationCharacterProperty);
        set => SetValue(TextObfuscationCharacterProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the full value is temporarily visible. This explicit reveal wins over
    /// <see cref="TextObfuscationMode"/> and is suitable for binding to an application-owned
    /// reveal toggle.
    /// </summary>
    public bool IsPasswordVisible
    {
        get => GetValue(IsPasswordVisibleProperty);
        set => SetValue(IsPasswordVisibleProperty, value);
    }

    /// <summary>Shows the complete value without enabling cut or copy.</summary>
    public void RevealPassword() => IsPasswordVisible = true;

    /// <summary>Returns the editor to its requested obfuscation mode.</summary>
    public void HidePassword() => IsPasswordVisible = false;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        ConfigureSecureEditor();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextObfuscationModeProperty
            || change.Property == TextObfuscationCharacterProperty
            || change.Property == IsPasswordVisibleProperty)
        {
            ConfigureSecureEditor();
        }
        else if (change.Property == LabelProperty && Editor is not null)
        {
            AutomationProperties.SetName(Editor, Label);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialSecureTextFieldAutomationPeer(this);

    private void ConfigureSecureEditor()
    {
        if (Editor is not { } editor)
            return;

        editor.PasswordChar = TextObfuscationCharacter;
        editor.RevealPassword = IsPasswordVisible
            || TextObfuscationMode == SecureTextObfuscationMode.Visible;
        editor.AcceptsReturn = false;
        editor.TextWrapping = TextWrapping.NoWrap;
        DragDrop.SetAllowDrop(editor, false);

        TextInputOptions.SetContentType(editor, TextInputContentType.Password);
        TextInputOptions.SetIsSensitive(editor, true);
        TextInputOptions.SetMultiline(editor, false);
        TextInputOptions.SetAutoCapitalization(editor, false);
        TextInputOptions.SetShowSuggestions(editor, false);

        // The wrapper provides the non-leaking Edit automation value. Keep the inner editor in
        // the raw view so it remains the actual keyboard focus target without duplicating it in
        // the normal control/content automation views.
        AutomationProperties.SetAccessibilityView(editor, AccessibilityView.Raw);
        AutomationProperties.SetName(editor, Label);
    }
}
