using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace Material3Expressive.Foundation.Interaction;

// Core RootNodeOwner.skiko.kt and Indication.skiko.kt: input mode filters only
// indication FocusInteraction, while elevation observes the original source.
internal sealed class MaterialInputMode
{
    private static readonly ConditionalWeakTable<TopLevel, MaterialInputMode> Windows = new();
    internal bool IsKeyboard { get; private set; } = true;

    private MaterialInputMode(TopLevel window) => Observe(window);

    private void Observe(TopLevel window)
    {
        window.AddHandler(InputElement.PointerPressedEvent, (_, _) => IsKeyboard = false,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        window.AddHandler(InputElement.PointerReleasedEvent, (_, _) => IsKeyboard = false,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        // Core switches back through focus navigation, rather than synthesizing focus
        // indications when a key is typed into an already pointer-focused control.
        window.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Tab) IsKeyboard = true;
        }, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    // Compose keeps one input mode per window, and its popups share it. A native popup is its own
    // TopLevel: with a separate default-keyboard mode, a submenu parent that Avalonia focuses
    // programmatically when its submenu opens showed the focus layer instead of pointer hover.
    internal static MaterialInputMode? For(Control owner)
    {
        if (TopLevel.GetTopLevel(owner) is not { } window)
            return null;
        var root = window;
        while (root is PopupRoot { Parent: Visual host }
               && TopLevel.GetTopLevel(host) is { } parent && !ReferenceEquals(parent, root))
            root = parent;
        if (Windows.TryGetValue(window, out var known))
            return known;
        var mode = Windows.GetValue(root, static w => new MaterialInputMode(w));
        if (!ReferenceEquals(root, window))
        {
            mode.Observe(window);
            Windows.AddOrUpdate(window, mode);
        }
        return mode;
    }

    internal void ObserveFocusNavigation(NavigationMethod method)
    {
        if (method == NavigationMethod.Pointer) IsKeyboard = false;
        else if (method is NavigationMethod.Tab or NavigationMethod.Directional) IsKeyboard = true;
    }
}
