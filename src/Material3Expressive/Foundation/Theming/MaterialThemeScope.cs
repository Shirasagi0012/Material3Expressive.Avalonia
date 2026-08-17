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

/// <summary>Installs a <see cref="MaterialTheme"/> at a resource-host placement.</summary>
public class MaterialThemeScope : AvaloniaObject
{
    public static readonly AttachedProperty<MaterialTheme?> ThemeProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, MaterialTheme?>("Theme");

    [Obsolete(
        "Compatibility snapshot transport for unmigrated controls. Consume typed " +
        "MaterialValueKey<T> resources through a MaterialTheme placement; removed in the M9 " +
        "cutover. See docs/refactor/migration-plan.md.",
        DiagnosticId = "M3E1004")]
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

    private static readonly AttachedProperty<bool> ThemeRollbackGuardProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, bool>(
            "ThemeRollbackGuard");

    private static readonly FuncValueConverter<ResolvedTheme?, ResolvedColorScheme?> ThemeToColorScheme =
        new(theme => theme?.ColorScheme);

    static MaterialThemeScope()
    {
        ThemeProperty.Changed.AddClassHandler<AvaloniaObject>(static (element, args) =>
        {
            if (element.GetValue(ThemeRollbackGuardProperty))
                return;

            var previousTheme = args.OldValue as MaterialTheme;
            var previousPlacement = element.GetValue(ThemeResourcePlacementProperty);
            var previousHandler = element.GetValue(ThemeChangedHandlerProperty);
            var previousResolved = previousTheme?.Resolved;

            if (args.NewValue is not MaterialTheme theme)
            {
                var clearHandlerDetached = false;
                var clearPlacementRemovalAttempted = false;
                try
                {
                    if (previousTheme is not null && previousHandler is not null)
                    {
                        previousTheme.Changed -= previousHandler;
                        clearHandlerDetached = true;
                    }

                    // Resource removal can notify observers after the entry and Owner have already
                    // changed. Mark the operation before calling into Avalonia so a late exception
                    // restores the placement instead of leaving a half-committed clear.
                    clearPlacementRemovalAttempted = previousPlacement?.OwnsEntry == true;
                    RemoveResourcePlacement(previousPlacement);
                    element.ClearValue(ThemeResourcePlacementProperty);
                    element.ClearValue(ThemeChangedHandlerProperty);
                    element.ClearValue(ResolvedThemeProperty);
                    element.ClearValue(MaterialColor.ResolvedSchemeProperty);
                    return;
                }
                catch
                {
                    if (clearPlacementRemovalAttempted && previousPlacement is not null)
                        RestoreResourcePlacement(previousPlacement);

                    if (previousTheme is not null && previousResolved is not null)
                        previousTheme.RestoreResolvedSnapshot(previousResolved);

                    if (clearHandlerDetached && previousTheme is not null && previousHandler is not null)
                        previousTheme.Changed += previousHandler;

                    SetPlacementState(element, previousPlacement);
                    if (previousHandler is null)
                        element.ClearValue(ThemeChangedHandlerProperty);
                    else
                        element.SetValue(ThemeChangedHandlerProperty, previousHandler);

                    if (previousTheme is null)
                    {
                        element.ClearValue(ResolvedThemeProperty);
                        element.ClearValue(MaterialColor.ResolvedSchemeProperty);
                    }
                    else
                    {
                        Publish(element, previousTheme.Resolved);
                    }

                    RestoreThemeProperty(element, previousTheme);
                    throw;
                }
            }

            ThemeResourcePlacement? candidatePlacement = null;
            EventHandler? candidateHandler = null;
            var candidateHandlerAttached = false;
            var previousHandlerDetached = false;
            var previousPlacementRemovalAttempted = false;
            try
            {
                // Stage the candidate while the previous placement and compatibility state remain
                // fully live. A candidate already owned elsewhere therefore fails without first
                // tearing down the working theme.
                candidatePlacement = PrepareResourcePlacement(element, theme);
                var candidateResolved = theme.Resolved;
                candidateHandler = CreateThemeChangedHandler(element, theme);
                Publish(element, candidateResolved);

                theme.Changed += candidateHandler;
                candidateHandlerAttached = true;
                if (previousTheme is not null && previousHandler is not null)
                {
                    previousTheme.Changed -= previousHandler;
                    previousHandlerDetached = true;
                }

                // Mark the attempt before removal: Avalonia can physically remove the provider and
                // clear Owner before a synchronous ResourcesChanged observer throws.
                previousPlacementRemovalAttempted = previousPlacement?.OwnsEntry == true;
                RemoveResourcePlacement(previousPlacement);

                SetPlacementState(element, candidatePlacement);
                element.SetValue(ThemeChangedHandlerProperty, candidateHandler);
            }
            catch
            {
                if (candidateHandlerAttached && candidateHandler is not null)
                    theme.Changed -= candidateHandler;

                RemoveResourcePlacement(candidatePlacement);
                if (previousPlacementRemovalAttempted && previousPlacement is not null)
                    RestoreResourcePlacement(previousPlacement);

                if (previousTheme is not null && previousResolved is not null)
                    previousTheme.RestoreResolvedSnapshot(previousResolved);

                if (previousHandlerDetached && previousTheme is not null && previousHandler is not null)
                    previousTheme.Changed += previousHandler;

                SetPlacementState(element, previousPlacement);
                if (previousHandler is null)
                    element.ClearValue(ThemeChangedHandlerProperty);
                else
                    element.SetValue(ThemeChangedHandlerProperty, previousHandler);

                if (previousTheme is null)
                {
                    element.ClearValue(ResolvedThemeProperty);
                    element.ClearValue(MaterialColor.ResolvedSchemeProperty);
                }
                else
                {
                    Publish(element, previousTheme.Resolved);
                }

                RestoreThemeProperty(element, previousTheme);
                throw;
            }
        });
    }

    public static MaterialTheme? GetTheme(AvaloniaObject element) => element.GetValue(ThemeProperty);

    public static void SetTheme(AvaloniaObject element, MaterialTheme? value) =>
        element.SetValue(ThemeProperty, value);

    [Obsolete(
        "Compatibility snapshot transport for unmigrated controls. Consume typed " +
        "MaterialValueKey<T> resources through a MaterialTheme placement; removed in the M9 " +
        "cutover. See docs/refactor/migration-plan.md.",
        DiagnosticId = "M3E1004")]
    public static ResolvedTheme GetResolvedTheme(AvaloniaObject element) =>
        element.GetValue(ResolvedThemeProperty);

    [Obsolete(
        "Compatibility snapshot transport for unmigrated controls. Consume typed " +
        "MaterialValueKey<T> resources through a MaterialTheme placement; removed in the M9 " +
        "cutover. See docs/refactor/migration-plan.md.",
        DiagnosticId = "M3E1004")]
    public static void SetResolvedTheme(AvaloniaObject element, ResolvedTheme value) =>
        element.SetValue(ResolvedThemeProperty, value);

    internal static Style CreateTopLevelInjectionStyle()
    {
        var style = new Style(selector => selector.Is<TopLevel>());
        if (Application.Current is not { } application)
            return style;

        style.Add(new Setter(
            ResolvedThemeProperty,
            CompiledBinding.Create<AvaloniaObject, object?>(
                host => host[ResolvedThemeProperty],
                application,
                mode: BindingMode.OneWay)));

        style.Add(new Setter(
            MaterialColor.ResolvedSchemeProperty,
            CompiledBinding.Create<AvaloniaObject, object?>(
                host => host[ResolvedThemeProperty],
                application,
                ThemeToColorScheme,
                BindingMode.OneWay)));

        return style;
    }

    private static EventHandler CreateThemeChangedHandler(
        AvaloniaObject element,
        MaterialTheme theme)
    {
        WeakReference<AvaloniaObject> weakElement = new(element);
        EventHandler? handler = null;
        handler = (sender, _) =>
        {
            if (sender is not MaterialTheme changed)
                return;

            if (weakElement.TryGetTarget(out var target))
                Publish(target, changed.Resolved);
            else if (handler is not null)
                theme.Changed -= handler;
        };
        return handler;
    }

    private static void Publish(AvaloniaObject element, ResolvedTheme resolved)
    {
        element.SetValue(ResolvedThemeProperty, resolved);
        MaterialColor.SetResolvedScheme(element, resolved.ColorScheme);
    }

    private static ThemeResourcePlacement? PrepareResourcePlacement(
        AvaloniaObject element,
        MaterialTheme theme)
    {
        if (element is not IResourceHost || GetResources(element) is not { } resources)
            return null;

        // Do not claim an entry installed manually by the author. Clearing ThemeProperty must not
        // remove it later.
        if (resources.MergedDictionaries.Contains(theme))
        {
            return new ThemeResourcePlacement(
                resources,
                theme,
                resources.MergedDictionaries.IndexOf(theme),
                OwnsEntry: false);
        }

        if (theme.Owner is not null)
        {
            throw new InvalidOperationException(
                "A MaterialTheme instance already has a resource-host placement. " +
                "Create a distinct theme instance for the second scope.");
        }

        const int index = 0;
        try
        {
            // The real MaterialTheme is the boundary. ResourceProvider's single-owner contract
            // deliberately rejects trying to reuse the same authoring instance at another scope.
            resources.MergedDictionaries.Insert(index, theme);
        }
        catch
        {
            // Some collection implementations publish the insertion before OnAddOwner rejects it.
            // We established above that no matching entry pre-existed, so this only removes ours.
            if (resources.MergedDictionaries.Contains(theme))
                resources.MergedDictionaries.Remove(theme);
            throw;
        }

        return new ThemeResourcePlacement(resources, theme, index, OwnsEntry: true);
    }

    private static void RemoveResourcePlacement(ThemeResourcePlacement? placement)
    {
        if (placement?.OwnsEntry == true)
            placement.Resources.MergedDictionaries.Remove(placement.Theme);
    }

    private static void RestoreResourcePlacement(ThemeResourcePlacement placement)
    {
        if (!placement.OwnsEntry || placement.Resources.MergedDictionaries.Contains(placement.Theme))
            return;

        var index = Math.Clamp(
            placement.Index,
            0,
            placement.Resources.MergedDictionaries.Count);
        placement.Resources.MergedDictionaries.Insert(index, placement.Theme);
    }

    private static void SetPlacementState(
        AvaloniaObject element,
        ThemeResourcePlacement? placement)
    {
        if (placement is null)
            element.ClearValue(ThemeResourcePlacementProperty);
        else
            element.SetValue(ThemeResourcePlacementProperty, placement);
    }

    private static void RestoreThemeProperty(AvaloniaObject element, MaterialTheme? theme)
    {
        element.SetValue(ThemeRollbackGuardProperty, true);
        try
        {
            if (theme is null)
                element.ClearValue(ThemeProperty);
            else
                element.SetValue(ThemeProperty, theme);
        }
        finally
        {
            element.ClearValue(ThemeRollbackGuardProperty);
        }
    }

    private static IResourceDictionary? GetResources(AvaloniaObject element) => element switch
    {
        Application application => application.Resources,
        StyledElement styledElement => styledElement.Resources,
        _ => null,
    };

    private sealed record ThemeResourcePlacement(
        IResourceDictionary Resources,
        MaterialTheme Theme,
        int Index,
        bool OwnsEntry);
}
