using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Styling;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// Builds the bindings behind the markup extensions.
/// </summary>
/// <remarks>
/// Every binding is rooted at <c>$self</c> so that it resolves against the element it is attached
/// to. That is the whole fix for the scoping bug these extensions used to have: a
/// <c>ControlTheme</c> setter is parsed once but instantiated per control, so the binding must
/// describe "the theme in scope at whatever you are attached to" rather than naming a source
/// object at parse time.
/// <para>
/// No application-level fallback is needed here, unlike in the colour layer:
/// <see cref="MaterialThemeScope.ResolvedThemeProperty" /> has a real default, so an element that
/// inherits nothing still resolves against <see cref="ResolvedTheme.Default" />.
/// </para>
/// </remarks>
internal static class ThemeBinding
{
    /// <summary>A value that depends only on the theme in scope.</summary>
    public static BindingBase FromTheme<T>(Func<ResolvedTheme, T> select, T fallback)
    {
        return new CompiledBinding(SelfPath(MaterialThemeScope.ResolvedThemeProperty))
        {
            Mode = BindingMode.OneWay,
            FallbackValue = fallback,
            Converter = new FuncValueConverter<ResolvedTheme?, object?>(
                theme => theme is { } ? select(theme) : fallback)
        };
    }

    /// <summary>
    /// A value that depends on the theme <i>and</i> the element's theme variant — every colour, in
    /// other words. Both inputs are read from the target, so both stay live.
    /// </summary>
    public static BindingBase FromThemeVariant<T>(Func<ResolvedTheme, bool, T> select, T fallback)
    {
        return new MultiBinding
        {
            Mode = BindingMode.OneWay,
            FallbackValue = (object?)fallback ?? AvaloniaProperty.UnsetValue,
            Bindings =
            {
                new CompiledBinding(SelfPath(MaterialThemeScope.ResolvedThemeProperty)) { Mode = BindingMode.OneWay },
                new CompiledBinding(SelfPath(ThemeVariantScope.ActualThemeVariantProperty)) { Mode = BindingMode.OneWay }
            },
            Converter = new FuncMultiValueConverter<object?, object?>(values =>
            {
                if (Value<ResolvedTheme>(values, 0) is not { } theme)
                    return fallback;

                var isDark = ColorScheme.IsDark(Value<ThemeVariant>(values, 1) ?? ThemeVariant.Default);
                return select(theme, isDark);
            })
        };
    }

    private static T? Value<T>(IReadOnlyList<object?> values, int index) where T : class
    {
        return index < values.Count ? values[index] as T : null;
    }

    private static CompiledBindingPath SelfPath(AvaloniaProperty property)
    {
        return new CompiledBindingPathBuilder()
            .Self()
            .Property(property, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
            .Build();
    }
}
