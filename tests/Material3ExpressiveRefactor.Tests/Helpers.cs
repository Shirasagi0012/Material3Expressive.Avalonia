using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation;
using Material3ExpressiveRefactor.Foundation.Tokens;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// Dictionary-backed <see cref="ITokenSource"/> for hand-planted literal / TokenRef / WithAlpha
/// entries. <see cref="Set"/> raises <see cref="Changed"/> so tests can drive live updates.
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
