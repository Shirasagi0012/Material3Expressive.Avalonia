// Executable reference: Compose Multiplatform Material3 1.5.0-alpha22.
// Implementation source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 SecureTextField.kt.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

internal static class SecureTextFieldParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Create(
            "secure-textfield/filled/hidden/light",
            TextFieldVariant.Filled,
            SecureTextObfuscationMode.Hidden),
        Create(
            "secure-textfield/outlined/visible/light",
            TextFieldVariant.Outlined,
            SecureTextObfuscationMode.Visible)
    ];

    private static VisualCase Create(
        string id,
        TextFieldVariant variant,
        SecureTextObfuscationMode obfuscationMode)
    {
        var height = variant == TextFieldVariant.Outlined ? 64 : 56;
        return new VisualCase(
            id,
            new PixelSize(280, 96),
            VisualTheme.Light,
            _ => new Grid
            {
                Children =
                {
                    new MaterialSecureTextField
                    {
                        Name = "component",
                        Variant = variant,
                        Label = "Password",
                        Text = "Secret",
                        TextObfuscationMode = obfuscationMode,
                        Width = 240,
                        Height = height,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            },
            RenderScaling: 1,
            Prepare: context =>
            {
                var editor = context.Require<TextBox>("PART_TextBox");
                editor.CaretBlinkInterval = TimeSpan.Zero;
                if (editor.IsFocused)
                {
                    throw new InvalidOperationException(
                        $"'{id}' editor unexpectedly acquired focus.");
                }
                context.SettleAnimations();
            });
    }
}
