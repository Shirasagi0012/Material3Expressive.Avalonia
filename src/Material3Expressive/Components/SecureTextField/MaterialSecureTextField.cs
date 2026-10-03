using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>
/// Secure entry restrictions for a native TextBox. PasswordChar and RevealPassword select its
/// native hidden/visible presentation. This does not replace the native automation or IME client.
/// </summary>
public static class MaterialSecureTextField
{
    public static readonly AttachedProperty<bool> IsSecureProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsSecure", typeof(MaterialSecureTextField));

    public static bool GetIsSecure(TextBox owner) => owner.GetValue(IsSecureProperty);
    public static void SetIsSecure(TextBox owner, bool value) => owner.SetValue(IsSecureProperty, value);

    private static readonly ConditionalWeakTable<TextBox, IDisposable?[]> Restrictions = new();

    static MaterialSecureTextField()
    {
        IsSecureProperty.Changed.AddClassHandler<TextBox>((owner, change) =>
        {
            if (change.GetNewValue<bool>())
            {
                // SecureTextField -> BasicSecureTextField: SingleLine and secure keyboard defaults.
                // Temporary priority values retain the caller's bindings when restrictions are removed.
                Restrictions.Add(owner,
                [
                    owner.SetValue(TextBox.AcceptsReturnProperty, false, BindingPriority.Animation),
                    owner.SetValue(TextBox.AcceptsTabProperty, false, BindingPriority.Animation),
                    owner.SetValue(TextBox.TextWrappingProperty, TextWrapping.NoWrap, BindingPriority.Animation),
                    owner.SetValue(TextBox.MinLinesProperty, 1, BindingPriority.Animation),
                    owner.SetValue(TextBox.MaxLinesProperty, 1, BindingPriority.Animation),
                    owner.SetValue(TextInputOptions.MultilineProperty, false, BindingPriority.Animation),
                    owner.SetValue(TextInputOptions.ContentTypeProperty, TextInputContentType.Password, BindingPriority.Animation),
                    owner.SetValue(TextInputOptions.IsSensitiveProperty, true, BindingPriority.Animation),
                    owner.SetValue(TextInputOptions.ShowSuggestionsProperty, false, BindingPriority.Animation),
                    owner.SetValue(TextInputOptions.AutoCapitalizationProperty, false, BindingPriority.Animation),
                ]);
                owner.CopyingToClipboard += BlockClipboardExport;
                owner.CuttingToClipboard += BlockClipboardExport;
                owner.AddHandler(InputElement.KeyDownEvent, BlockExportShortcut, RoutingStrategies.Tunnel);
            }
            else
            {
                owner.CopyingToClipboard -= BlockClipboardExport;
                owner.CuttingToClipboard -= BlockClipboardExport;
                owner.RemoveHandler(InputElement.KeyDownEvent, BlockExportShortcut);
                if (Restrictions.TryGetValue(owner, out var restrictions))
                {
                    Restrictions.Remove(owner);
                    foreach (var restriction in restrictions) restriction?.Dispose();
                }
            }
        });
    }

    // TextBox.Copy/Cut do not consult CanCopy/CanCut. The routed events protect direct calls,
    // context actions and IME actions too, including when RevealPassword is true.
    private static void BlockClipboardExport(object? sender, RoutedEventArgs args) => args.Handled = true;

    private static void BlockExportShortcut(object? sender, KeyEventArgs args)
    {
        if (Application.Current?.PlatformSettings?.HotkeyConfiguration is not { } keys) return;
        if (keys.Copy.Any(key => key.Matches(args)) || keys.Cut.Any(key => key.Matches(args)))
            args.Handled = true;
    }
}
