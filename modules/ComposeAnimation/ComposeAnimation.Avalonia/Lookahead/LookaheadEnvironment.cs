using Avalonia;
using Avalonia.Controls;

namespace ComposeAnimation;

/// <summary>Inherited domain identity. A local null cuts inheritance without affecting other properties.</summary>
internal static class LookaheadEnvironment
{
    internal static readonly AttachedProperty<LookaheadContext?> ContextProperty =
        AvaloniaProperty.RegisterAttached<Control, LookaheadContext?>(
            "Context", typeof(LookaheadEnvironment), defaultValue: null, inherits: true);

    internal static LookaheadContext? GetContext(AvaloniaObject control) => control.GetValue(ContextProperty);
    internal static void SetContext(AvaloniaObject control, LookaheadContext? context) => control.SetValue(ContextProperty, context);
}
