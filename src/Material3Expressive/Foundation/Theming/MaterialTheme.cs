using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens.Runtime;
using MaterialColorUtilities.Avalonia;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

#pragma warning disable CS0618 // ResolvedTheme stays as this theme's snapshot until the M9 cutover; see docs/refactor/migration-plan.md.

/// <summary>
/// A whole Material theme: colour, typography, shape, motion and ripple schemes.
/// </summary>
/// <remarks>
/// This is both the mutable authoring surface and a resource provider. The immutable
/// <see cref="ResolvedTheme" /> remains its execution snapshot, while token consumers resolve
/// system colour definitions through the ordinary Avalonia resource cascade.
/// </remarks>
public partial class MaterialTheme : ResourceProvider
{
    /// <summary>The Material 3 baseline seed, so an undeclared theme still renders as Material.</summary>
    private static readonly Color BaselineSeedColor = Color.FromRgb(0x67, 0x50, 0xA4);

    private ResolvedTheme? _resolved;

    public MaterialTheme()
    {
        // Subscribe before assigning the defaults so that the initial values go through exactly
        // the same attach path as any later assignment.
        PropertyChanged += OnSelfPropertyChanged;

        ColorScheme = new TonalSpotScheme(BaselineSeedColor);
        TypographyScheme = new TypographyScheme();
        ShapeScheme = new ShapeScheme();
        MotionScheme = new MotionScheme();
        RippleTheme = new RippleThemeConfiguration();
    }

    /// <summary>Raised whenever this theme or any of its schemes changes.</summary>
    public event EventHandler? Changed;

    [GeneratedStyledProperty] public partial ColorScheme? ColorScheme { get; set; }
    [GeneratedStyledProperty] public partial TypographyScheme TypographyScheme { get; set; }
    [GeneratedStyledProperty] public partial ShapeScheme ShapeScheme { get; set; }
    [GeneratedStyledProperty] public partial MotionScheme MotionScheme { get; set; }
    [GeneratedStyledProperty] public partial RippleThemeConfiguration RippleTheme { get; set; }

    /// <summary>The current snapshot, rebuilt on demand after any change.</summary>
    public ResolvedTheme Resolved => _resolved ??= new ResolvedTheme(this);

    internal void SetResolvedSnapshot(ResolvedTheme value) => _resolved = value;

    /// <inheritdoc />
    public override bool HasResources => true;

    /// <summary>
    /// Provides theme-dependent system token definitions. Component defaults deliberately remain
    /// in the generated token graph so a local theme does not mask component overrides above it.
    /// </summary>
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is MaterialToken referenceToken &&
            ReferencePaletteTokens.TryGetMetadata(
                referenceToken,
                out var palette,
                out var tone,
                out var paletteCustomKey))
        {
            var color = Resolved.ColorScheme?.GetPaletteColor(palette, tone, paletteCustomKey)
                        ?? Colors.Transparent;
            // A provider may publish T directly; definitions are needed only for aliases and
            // recipes. Avoid allocating a LiteralTokenDefinition on every resource lookup.
            value = color;
            return true;
        }

        if (key is MaterialToken token &&
            SystemColorTokens.TryGetRole(token, out var role, out var customKey))
        {
            var isDark = MaterialColorUtilities.Avalonia.ColorScheme.IsDark(
                theme ?? ThemeVariant.Default);
            var color = Resolved.ColorScheme?.GetColor(role, isDark, customKey)
                        ?? Colors.Transparent;
            value = color;
            return true;
        }

        if (key is MaterialToken typeToken &&
            SystemTypeScaleTokens.TryGetRole(typeToken, out var typeRole, out var field))
        {
            var scale = Resolved.Typography.GetTypeScale(typeRole);
            value = field switch
            {
                SystemTypeScaleField.FontSize => scale.FontSize,
                SystemTypeScaleField.LineHeight => scale.LineHeight,
                SystemTypeScaleField.LetterSpacing => scale.LetterSpacing,
                SystemTypeScaleField.Font => scale.Font,
                SystemTypeScaleField.Weight => scale.Weight,
                _ => null
            };
            return value is not null;
        }

        value = null;
        return false;
    }

    public object ProvideValue()
    {
        return this;
    }

    private void OnSelfPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ColorSchemeProperty
            && e.Property != TypographySchemeProperty
            && e.Property != ShapeSchemeProperty
            && e.Property != MotionSchemeProperty
            && e.Property != RippleThemeProperty)
            return;

        Detach(e.OldValue);
        Attach(e.NewValue);
        Invalidate();
    }

    private void Attach(object? scheme)
    {
        switch (scheme)
        {
            // A colour scheme changes for reasons a property change does not cover — a custom
            // colour added to its collection, for instance — so it has its own event.
            case ColorScheme colorScheme:
                colorScheme.SchemeChanged += OnSchemeChanged;
                break;
            case TypographyScheme typographyScheme:
                typographyScheme.TypeScaleOverrides.CollectionChanged += OnTypeScaleOverridesChanged;
                foreach (var item in typographyScheme.TypeScaleOverrides)
                    Attach(item);
                break;
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged += OnSchemePropertyChanged;
                break;
        }
    }

    private void Detach(object? scheme)
    {
        switch (scheme)
        {
            case ColorScheme colorScheme:
                colorScheme.SchemeChanged -= OnSchemeChanged;
                break;
            case TypographyScheme typographyScheme:
                typographyScheme.TypeScaleOverrides.CollectionChanged -= OnTypeScaleOverridesChanged;
                foreach (var item in typographyScheme.TypeScaleOverrides)
                    Detach(item);
                break;
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged -= OnSchemePropertyChanged;
                break;
        }
    }

    private void OnSchemeChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    private void OnSchemePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        Invalidate();
    }

    private void OnTypeScaleOverridesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in e.OldItems ?? (System.Collections.IList)Array.Empty<object>())
            Detach(item);

        foreach (var item in e.NewItems ?? (System.Collections.IList)Array.Empty<object>())
            Attach(item);

        Invalidate();
    }

    private void Invalidate()
    {
        _resolved = null;
        RaiseResourcesChanged();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
