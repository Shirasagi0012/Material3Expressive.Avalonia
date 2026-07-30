// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// SecureTextField.kt:45-201,214-365 and BasicSecureTextField.kt:124-213,230-333.

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialSecureTextFieldTests
{
    [AvaloniaFact]
    public void DefaultEditorIsSingleLineSensitiveAndFullyObfuscatedOnDesktop()
    {
        var field = new MaterialSecureTextField
        {
            Label = "Password",
            Text = "secret"
        };
        ThemeTestHelper.ShowWindow(field);
        var editor = EditorOf(field);

        Assert.Equal('\u2022', editor.PasswordChar);
        Assert.False(editor.RevealPassword);
        Assert.False(editor.AcceptsReturn);
        Assert.Equal(TextInputContentType.Password, TextInputOptions.GetContentType(editor));
        Assert.True(TextInputOptions.GetIsSensitive(editor));
        Assert.False(TextInputOptions.GetMultiline(editor));
        Assert.False(TextInputOptions.GetAutoCapitalization(editor));
        Assert.False(TextInputOptions.GetShowSuggestions(editor));
        Assert.False(DragDrop.GetAllowDrop(editor));

        editor.SelectionStart = 0;
        editor.SelectionEnd = editor.Text?.Length ?? 0;
        Assert.False(editor.CanCopy);
        Assert.False(editor.CanCut);
        Assert.True(editor.CanPaste);
    }

    [AvaloniaFact]
    public void VisibleAndExplicitRevealModesUpdateTheNativeEditor()
    {
        var field = new MaterialSecureTextField { Text = "secret" };
        ThemeTestHelper.ShowWindow(field);
        var editor = EditorOf(field);

        field.TextObfuscationMode = SecureTextObfuscationMode.Visible;
        Assert.True(editor.RevealPassword);

        field.TextObfuscationMode = SecureTextObfuscationMode.Hidden;
        Assert.False(editor.RevealPassword);

        field.TextObfuscationMode = SecureTextObfuscationMode.RevealLastTyped;
        Assert.False(editor.RevealPassword);

        field.RevealPassword();
        Assert.True(field.IsPasswordVisible);
        Assert.True(editor.RevealPassword);

        field.HidePassword();
        Assert.False(field.IsPasswordVisible);
        Assert.False(editor.RevealPassword);

        field.TextObfuscationCharacter = '*';
        Assert.Equal('*', editor.PasswordChar);
        Assert.Throws<ArgumentException>(() => field.TextObfuscationCharacter = '\0');
    }

    [AvaloniaFact]
    public void TextAndRevealScalarsRestoreAndRemainTwoWayWithTheEditor()
    {
        var field = new MaterialSecureTextField
        {
            Text = "restored",
            IsPasswordVisible = true,
            TextObfuscationMode = SecureTextObfuscationMode.Hidden
        };
        ThemeTestHelper.ShowWindow(field);
        var editor = EditorOf(field);

        Assert.Equal("restored", editor.Text);
        Assert.True(editor.RevealPassword);

        editor.Text = "edited";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("edited", field.Text);
    }

    [AvaloniaFact]
    public void ValidationChromeAndKeyboardFocusRemainOnTheNativeEditor()
    {
        var field = new MaterialSecureTextField
        {
            Label = "Password",
            IsError = true,
            SupportingText = "Too short"
        };
        ThemeTestHelper.ShowWindow(field);
        var editor = EditorOf(field);
        var supporting = Assert.Single(
            field.GetVisualDescendants().OfType<TextBlock>(),
            item => item.Name == "PART_Supporting");

        Assert.True(supporting.IsVisible);
        Assert.Equal("Too short", supporting.Text);
        Assert.True(editor.Focus());
        Assert.True(editor.IsFocused);
        Assert.False(field.IsFocused);
    }

    [AvaloniaFact]
    public void AutomationNamesThePasswordEditWithoutExposingItsValue()
    {
        var field = new MaterialSecureTextField
        {
            Label = "Account password",
            Text = "secret"
        };
        ThemeTestHelper.ShowWindow(field);

        var peer = ControlAutomationPeer.CreatePeerForElement(field);
        var provider = Assert.IsAssignableFrom<IValueProvider>(
            peer.GetProvider<IValueProvider>());
        AutomationPropertyChangedEventArgs? statusChange = null;
        peer.PropertyChanged += (_, args) =>
        {
            if (args.Property == AutomationElementIdentifiers.ItemStatusProperty)
                statusChange = args;
        };

        Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
        Assert.Equal("Account password", peer.GetName());
        Assert.Equal(string.Empty, provider.Value);
        Assert.False(provider.IsReadOnly);

        provider.SetValue("replacement");
        Assert.Equal("replacement", field.Text);
        Assert.Equal(string.Empty, provider.Value);

        field.SupportingText = "Too short";
        field.IsError = true;
        Assert.Equal("Too short", peer.GetHelpText());
        Assert.Equal("Invalid", peer.GetItemStatus());
        Assert.NotNull(statusChange);
        Assert.Null(statusChange.OldValue);
        Assert.Equal("Invalid", statusChange.NewValue);

        field.IsEnabled = false;
        Assert.True(provider.IsReadOnly);
        Assert.Throws<ElementNotEnabledException>(() => provider.SetValue("blocked"));
    }

    private static TextBox EditorOf(MaterialSecureTextField field) =>
        Assert.Single(field.GetVisualDescendants().OfType<TextBox>());
}
