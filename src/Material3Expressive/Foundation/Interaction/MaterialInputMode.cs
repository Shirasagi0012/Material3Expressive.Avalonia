using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Material3Expressive.Foundation.Interaction;

// Core RootNodeOwner.skiko.kt and Indication.skiko.kt: input mode filters only
// indication FocusInteraction, while elevation observes the original source.
internal sealed class MaterialInputMode
{
    private static readonly ConditionalWeakTable<TopLevel, MaterialInputMode> Windows = new();
    internal bool IsKeyboard { get; private set; } = true;

    private MaterialInputMode(TopLevel window)
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

    internal static MaterialInputMode? For(Control owner) => TopLevel.GetTopLevel(owner) is { } window
        ? Windows.GetValue(window, static w => new MaterialInputMode(w)) : null;

    internal void ObserveFocusNavigation(NavigationMethod method)
    {
        if (method == NavigationMethod.Pointer) IsKeyboard = false;
        else if (method is NavigationMethod.Tab or NavigationMethod.Directional) IsKeyboard = true;
    }
}
