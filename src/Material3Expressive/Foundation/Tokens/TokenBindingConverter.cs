using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>
/// The MultiBinding converter behind <see cref="TokenExtension"/>. values[0] is the consuming
/// target (Self binding); values[1] is the DynamicResource trigger whose value is never read —
/// it exists only to re-run this conversion when resources or the theme variant change.
/// </summary>
internal sealed class TokenBindingConverter : IMultiValueConverter
{
    private readonly MaterialTokenKey _rootKey;
    private readonly object? _anchor;
    private readonly ThemeVariant? _anchorThemeVariant;

    public TokenBindingConverter(MaterialTokenKey rootKey, object? anchor, ThemeVariant? anchorThemeVariant)
    {
        _rootKey = rootKey;
        _anchor = anchor;
        _anchorThemeVariant = anchorThemeVariant;
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not AvaloniaObject target)
            return AvaloniaProperty.UnsetValue;

        var host = ResolveHost(target, out var theme);
        if (host is null)
            return AvaloniaProperty.UnsetValue;

        return TokenResolver.TryResolve(host, _rootKey, theme, out var value)
            ? ConvertForTarget(value, targetType)
            : AvaloniaProperty.UnsetValue;
    }

    /// <summary>
    /// Mirrors DynamicResourceExpression's host resolution: the styled target itself, then the
    /// anchor captured at ProvideValue (a resource host directly, or a resource provider whose
    /// owner is one). Mirrors its theme handling too: the dictionary theme variant captured at
    /// ProvideValue only overrides in the provider branch, otherwise the host's live
    /// ActualThemeVariant is used.
    /// </summary>
    private IResourceHost? ResolveHost(AvaloniaObject target, out ThemeVariant? theme)
    {
        if (target is IResourceHost targetHost)
        {
            theme = (targetHost as IThemeVariantHost)?.ActualThemeVariant;
            return targetHost;
        }

        if (_anchor is IResourceHost anchorHost)
        {
            theme = (anchorHost as IThemeVariantHost)?.ActualThemeVariant;
            return anchorHost;
        }

        if (_anchor is IResourceProvider provider)
        {
            var owner = provider.Owner;
            theme = _anchorThemeVariant ?? (owner as IThemeVariantHost)?.ActualThemeVariant;
            return owner;
        }

        theme = null;
        return null;
    }

    /// <summary>
    /// Replicates the platform's built-in ColorToBrushConverter behavior (design doc §5.7 item 4):
    /// a Color hitting an IBrush-typed property becomes an ImmutableSolidColorBrush per consumer;
    /// an IBrush value passes through.
    /// </summary>
    private static object? ConvertForTarget(object? value, Type targetType)
    {
        if (value is Color color && targetType == typeof(IBrush))
            return new ImmutableSolidColorBrush(color);
        return value;
    }
}
