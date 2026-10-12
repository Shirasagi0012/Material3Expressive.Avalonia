using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Tests;

// Diagnostic only: counts live {m3:Token} binding expressions (MultiBindingExpressions whose
// converter is TokenBindingConverter, including ones nested in other MultiBindings) held by an
// object's value store, in every frame whether active or not, so inactive size-class setters
// count too. Reads Avalonia internals by reflection; there is no public binding enumeration.
internal static class TokenBindingCounter
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Type MultiBindingExpressionType =
        typeof(AvaloniaObject).Assembly.GetType("Avalonia.Data.Core.MultiBindingExpression", throwOnError: true)!;

    public static int Count(AvaloniaObject target)
    {
        var store = typeof(AvaloniaObject).GetMethod("GetValueStore", Any)!.Invoke(target, null)!;
        var frames = (IEnumerable)store.GetType().GetProperty("Frames", Any)!.GetValue(store)!;
        var count = 0;
        foreach (var frame in frames)
        {
            var type = frame.GetType();
            var entries = (int)type.GetProperty("EntryCount", Any)!.GetValue(frame)!;
            var getEntry = type.GetMethod("GetEntry", Any)!;
            for (var i = 0; i < entries; i++)
                count += CountExpression(getEntry.Invoke(frame, [i]));
        }
        return count;
    }

    public static int CountTree(Visual root) =>
        Count(root) + root.GetVisualDescendants().Sum(v => Count(v));

    static int CountExpression(object? entry)
    {
        if (entry is null || entry.GetType() != MultiBindingExpressionType) return 0;
        var converter = MultiBindingExpressionType.GetField("_converter", Any)!.GetValue(entry);
        var count = converter is TokenBindingConverter ? 1 : 0;
        if (MultiBindingExpressionType.GetField("_expressions", Any)!.GetValue(entry) is IEnumerable children)
            foreach (var child in children) count += CountExpression(child);
        return count;
    }
}
