using System.Runtime.CompilerServices;
using Avalonia;

namespace ComposeAnimation;

/// <summary>The only place that reaches into non-public Avalonia members.</summary>
internal static class AvaloniaInternals
{
    // Avalonia 12.1.2: AvaloniaObject.InheritanceParent is protected internal. It is the exact object
    // inherited values come from (the logical parent, or the ContentPresenter / popup parent that
    // Avalonia substitutes), so scope lookup matches inheritance (D13). Re-check on Avalonia upgrades;
    // AvaloniaInternalsTest pins the behaviour.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_InheritanceParent")]
    private static extern AvaloniaObject? InheritanceParent(AvaloniaObject element);

    internal static AvaloniaObject? GetInheritanceParent(AvaloniaObject element) => InheritanceParent(element);
}
