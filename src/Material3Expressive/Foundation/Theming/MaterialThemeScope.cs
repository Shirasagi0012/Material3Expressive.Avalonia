using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>
/// Carries a <see cref="MaterialTheme" /> into a control tree.
/// </summary>
/// <remarks>
/// Setting <see cref="ThemeProperty" /> on an element makes that element's subtree use the theme.
/// The nearest declaration wins, because <see cref="ResolvedThemeProperty" /> is an ordinary
/// inherited property — there is no fallback chain to maintain, and no per-subsystem plumbing.
/// The application-level theme reaches each <c>TopLevel</c> through
/// <see cref="MaterialThemeStyles" />, since <see cref="Application" /> is not part of any tree.
/// </remarks>
public class MaterialThemeScope : AvaloniaObject
{
    /// <summary>
    /// The theme declared by this element. Not inherited: it names the element that declares the
    /// theme, not the elements affected by it.
    /// </summary>
    public static readonly AttachedProperty<MaterialTheme?> ThemeProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, MaterialTheme?>("Theme");

    /// <summary>
    /// The snapshot in effect at this element, inherited from the nearest declaration above it.
    /// Read by controls and by the markup extensions; app authors should not assign it.
    /// </summary>
    public static readonly AttachedProperty<ResolvedTheme> ResolvedThemeProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, ResolvedTheme>(
            "ResolvedTheme",
            ResolvedTheme.Default,
            inherits: true);

    private static readonly AttachedProperty<EventHandler?> ThemeChangedHandlerProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, EventHandler?>("ThemeChangedHandler");

    private static readonly FuncValueConverter<ResolvedTheme?, ResolvedColorScheme?> ThemeToColorScheme =
        new(theme => theme?.ColorScheme);

    static MaterialThemeScope()
    {
        ThemeProperty.Changed.AddClassHandler<AvaloniaObject>(static (element, args) =>
        {
            if (args.OldValue is MaterialTheme previous && element.GetValue(ThemeChangedHandlerProperty) is { } stale)
            {
                previous.Changed -= stale;
                element.ClearValue(ThemeChangedHandlerProperty);
            }

            if (args.NewValue is not MaterialTheme theme)
            {
                element.ClearValue(ResolvedThemeProperty);
                element.ClearValue(MaterialColor.ResolvedSchemeProperty);
                return;
            }

            // An application-level theme outlives every element that scopes to it, so the
            // subscription must not root the element.
            WeakReference<AvaloniaObject> weakElement = new(element);
            EventHandler? handler = null;
            handler = (sender, _) =>
            {
                if (sender is not MaterialTheme changed)
                    return;

                if (weakElement.TryGetTarget(out var target))
                    Publish(target, changed);
                else if (handler is { })
                    changed.Changed -= handler;
            };

            theme.Changed += handler;
            element.SetValue(ThemeChangedHandlerProperty, handler);
            Publish(element, theme);
        });
    }

    public static MaterialTheme? GetTheme(AvaloniaObject element)
    {
        return element.GetValue(ThemeProperty);
    }

    public static void SetTheme(AvaloniaObject element, MaterialTheme? value)
    {
        element.SetValue(ThemeProperty, value);
    }

    /// <summary>The theme in effect at this element.</summary>
    public static ResolvedTheme GetResolvedTheme(AvaloniaObject element)
    {
        return element.GetValue(ResolvedThemeProperty);
    }

    public static void SetResolvedTheme(AvaloniaObject element, ResolvedTheme value)
    {
        element.SetValue(ResolvedThemeProperty, value);
    }

    /// <summary>
    /// The style that hands the application theme to every <c>TopLevel</c>, at style priority so
    /// that a theme declared on a window itself still wins.
    /// </summary>
    internal static Style CreateTopLevelInjectionStyle()
    {
        var style = new Style(selector => selector.Is<TopLevel>());

        // Nothing to inject from without an application: the property default already covers it.
        if (Application.Current is not { } application)
            return style;

        style.Add(new Setter(
            ResolvedThemeProperty,
            CompiledBinding.Create<AvaloniaObject, object?>(
                host => host[ResolvedThemeProperty],
                application,
                mode: BindingMode.OneWay)));

        // Keep the colour layer's own inherited property in step, so that MaterialColorUtilities'
        // markup extensions resolve inside an M3E theme without depending on M3E.
        style.Add(new Setter(
            MaterialColor.ResolvedSchemeProperty,
            CompiledBinding.Create<AvaloniaObject, object?>(
                host => host[ResolvedThemeProperty],
                application,
                ThemeToColorScheme,
                BindingMode.OneWay)));

        return style;
    }

    private static void Publish(AvaloniaObject element, MaterialTheme theme)
    {
        var resolved = theme.Resolved;
        element.SetValue(ResolvedThemeProperty, resolved);
        MaterialColor.SetResolvedScheme(element, resolved.ColorScheme);
    }
}
