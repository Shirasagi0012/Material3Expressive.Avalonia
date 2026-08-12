using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation;

#pragma warning disable CS0618 // ResolvedTheme plumbing lives here until the M9 cutover; see docs/refactor/migration-plan.md.

/// <summary>
/// Carries a <see cref="MaterialTheme" /> into a control tree.
/// </summary>
/// <remarks>
/// Setting <see cref="ThemeProperty" /> on an element makes that element's subtree use the theme.
/// The theme is installed into that element's resource chain for token lookup, while the inherited
/// <see cref="ResolvedThemeProperty" /> remains as a compatibility transport for existing controls.
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
    [Obsolete(
        "Compatibility transport for unmigrated controls. Consume MaterialToken values through " +
        "TokenReader/MaterialTokenResolver instead; removed in the M9 cutover. See " +
        "docs/refactor/migration-plan.md.")]
    public static readonly AttachedProperty<ResolvedTheme> ResolvedThemeProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, ResolvedTheme>(
            "ResolvedTheme",
            ResolvedTheme.Default,
            inherits: true);

    private static readonly AttachedProperty<EventHandler?> ThemeChangedHandlerProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, EventHandler?>("ThemeChangedHandler");

    private static readonly AttachedProperty<ThemeResourcePlacement?> ThemeResourcePlacementProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, ThemeResourcePlacement?>(
            "ThemeResourcePlacement");

    private static readonly AttachedProperty<MaterialTheme?> BaselineResourceProviderProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, Application, MaterialTheme?>(
            "BaselineResourceProvider");

    private static readonly FuncValueConverter<ResolvedTheme?, ResolvedColorScheme?> ThemeToColorScheme =
        new(theme => theme?.ColorScheme);

    static MaterialThemeScope()
    {
        ThemeProperty.Changed.AddClassHandler<AvaloniaObject>(static (element, args) =>
        {
            RemoveResourceProvider(element);

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
            InstallResourceProvider(element, theme);
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
    [Obsolete(
        "Compatibility transport for unmigrated controls. Consume MaterialToken values through " +
        "TokenReader/MaterialTokenResolver instead; removed in the M9 cutover. See " +
        "docs/refactor/migration-plan.md.")]
    public static ResolvedTheme GetResolvedTheme(AvaloniaObject element)
    {
        return element.GetValue(ResolvedThemeProperty);
    }

    [Obsolete(
        "Compatibility transport for unmigrated controls. Consume MaterialToken values through " +
        "TokenReader/MaterialTokenResolver instead; removed in the M9 cutover. See " +
        "docs/refactor/migration-plan.md.")]
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

    /// <summary>
    /// Installs the resource counterpart of <see cref="ResolvedTheme.Default" /> without
    /// declaring an application theme or changing the legacy inherited-property contract.
    /// </summary>
    internal static void InstallBaselineResources(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (application.GetValue(BaselineResourceProviderProperty) is not null)
            return;

        var resources = application.Resources;
        var baseline = new MaterialTheme();
        baseline.SetResolvedSnapshot(ResolvedTheme.Default);
        resources.MergedDictionaries.Insert(0, baseline);
        application.SetValue(BaselineResourceProviderProperty, baseline);
    }

    private static void Publish(AvaloniaObject element, MaterialTheme theme)
    {
        var resolved = theme.Resolved;
        element.SetValue(ResolvedThemeProperty, resolved);
        MaterialColor.SetResolvedScheme(element, resolved.ColorScheme);
    }

    private static void InstallResourceProvider(AvaloniaObject element, MaterialTheme theme)
    {
        if (element is not IResourceHost host || GetResources(element) is not { } resources)
            return;

        // Do not claim an entry that the application author installed independently. The theme is
        // already effective at this host, and clearing ThemeProperty must not remove user state.
        if (resources.MergedDictionaries.Contains(theme))
        {
            element.SetValue(
                ThemeResourcePlacementProperty,
                new ThemeResourcePlacement(resources, theme, OwnsEntry: false));
            return;
        }

        IResourceProvider provider;
        if (element is Application && theme.Owner is null)
        {
            // Application lives for the duration of the process, so letting the theme own that
            // resource host does not extend the lifetime of a transient object graph.
            provider = theme;
        }
        else
        {
            // ResourceProvider.Owner is a strong reference. Always proxy a StyledElement scope so
            // a long-lived theme authoring object cannot keep a detached control tree alive.
            provider = new MaterialThemeResourcePlacement(theme);
        }

        // Merged dictionaries use last-wins lookup. Keep the broad MaterialTheme fallback before
        // every sparse provider authored at the same scope. At Application scope it must still
        // follow the implicit baseline theme, so an explicitly declared theme wins that fallback.
        var index = 0;
        if (element is Application application &&
            application.GetValue(BaselineResourceProviderProperty) is { } baseline)
        {
            var baselineIndex = resources.MergedDictionaries.IndexOf(baseline);
            if (baselineIndex >= 0)
                index = baselineIndex + 1;
        }

        resources.MergedDictionaries.Insert(index, provider);
        element.SetValue(
            ThemeResourcePlacementProperty,
            new ThemeResourcePlacement(resources, provider, OwnsEntry: true));
    }

    private static void RemoveResourceProvider(AvaloniaObject element)
    {
        if (element.GetValue(ThemeResourcePlacementProperty) is not { } placement)
            return;

        element.ClearValue(ThemeResourcePlacementProperty);
        if (placement.OwnsEntry)
            placement.Resources.MergedDictionaries.Remove(placement.Provider);
        if (placement.Provider is MaterialThemeResourcePlacement proxy)
            proxy.Dispose();
    }

    private static IResourceDictionary? GetResources(AvaloniaObject element) => element switch
    {
        Application application => application.Resources,
        StyledElement styledElement => styledElement.Resources,
        _ => null,
    };

    private sealed record ThemeResourcePlacement(
        IResourceDictionary Resources,
        IResourceProvider Provider,
        bool OwnsEntry);

    private sealed class MaterialThemeResourcePlacement : ResourceProvider, IDisposable
    {
        private MaterialTheme? _theme;
        private EventHandler? _changedHandler;

        public MaterialThemeResourcePlacement(MaterialTheme theme)
        {
            _theme = theme;

            // The source theme often belongs to application state and can substantially outlive a
            // local scope. It must not strongly retain this proxy (whose ResourceProvider.Owner in
            // turn strongly retains the scope). Keep only a weak reverse edge and unsubscribe the
            // dormant handler on the next notification if the proxy was collected.
            var weakPlacement = new WeakReference<MaterialThemeResourcePlacement>(this);
            EventHandler? handler = null;
            handler = (sender, _) =>
            {
                if (weakPlacement.TryGetTarget(out var placement))
                {
                    placement.RaiseResourcesChanged();
                }
                else if (sender is MaterialTheme source && handler is not null)
                {
                    source.Changed -= handler;
                }
            };

            _changedHandler = handler;
            theme.Changed += handler;
        }

        public override bool HasResources => _theme?.HasResources == true;

        public override bool TryGetResource(
            object key,
            ThemeVariant? theme,
            out object? value)
        {
            if (_theme is { } source)
                return source.TryGetResource(key, theme, out value);

            value = null;
            return false;
        }

        public void Dispose()
        {
            var handler = Interlocked.Exchange(ref _changedHandler, null);
            if (Interlocked.Exchange(ref _theme, null) is { } theme && handler is not null)
                theme.Changed -= handler;
        }
    }
}
