using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Xunit;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// Reflection sweep over the static MaterialTokens facade: every declared key must be answered
/// by the source under test, and literal values must match the key's TValue (TokenRef/WithAlpha/
/// SpringSpecRef derivations are only type-pinned at the end of their resolution chain, so they
/// are skipped here).
/// </summary>
internal static class TokenScan
{
    public static List<MaterialTokenKey> KeysUnder(Type type)
    {
        var keys = new List<MaterialTokenKey>();
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is MaterialTokenKey key)
                keys.Add(key);
        }

        foreach (var nested in type.GetNestedTypes())
            keys.AddRange(KeysUnder(nested));

        return keys;
    }

    public static void AssertComplete(ITokenSource source, params Type[] facadeSections)
    {
        foreach (var key in facadeSections.SelectMany(KeysUnder))
        {
            Assert.True(source.TryGetToken(key, null, out var value), $"miss: {key.DebugName}");
            if (value is TokenRef or WithAlpha or SpringSpecRef)
                continue;
            Assert.True(
                key.ValueType.IsInstanceOfType(value),
                $"{key.DebugName}: value {value.GetType().Name} does not match {key.ValueType.Name}");
        }
    }

    public static object Resolve(IResourceHost host, MaterialTokenKey key)
    {
        Assert.True(TokenResolver.TryResolve(host, key, null, out var value), $"miss: {key.DebugName}");
        return value!;
    }
}

/// <summary>
/// Dictionary-backed <see cref="ITokenSource"/> for hand-planted literal / TokenRef / WithAlpha /
/// SpringSpecRef entries. <see cref="Set"/> raises <see cref="Changed"/> so tests can drive live
/// updates.
/// </summary>
internal sealed class TestTokenSource : ITokenSource
{
    private readonly Dictionary<MaterialTokenKey, object> _values = new();

    public event EventHandler? Changed;

    public void Set(MaterialTokenKey key, object value)
    {
        _values[key] = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        _values.TryGetValue(key, out value!);
}

/// <summary>
/// An <see cref="IServiceProvider"/> that answers nothing, or — with <c>inTemplate</c> — only the
/// <see cref="IAvaloniaXamlIlControlTemplateProvider"/> marker service, emulating the XAML
/// compiler's control-template context for <c>TokenExtension.ProvideValue</c>.
/// </summary>
internal sealed class StubServiceProvider(bool inTemplate = false) : IServiceProvider
{
    private sealed class TemplateMarker : IAvaloniaXamlIlControlTemplateProvider;

    public object? GetService(Type serviceType) =>
        inTemplate && serviceType == typeof(IAvaloniaXamlIlControlTemplateProvider)
            ? new TemplateMarker()
            : null;
}
