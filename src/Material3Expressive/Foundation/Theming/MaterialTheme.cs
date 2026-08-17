using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia;
using MaterialDesignTokens.Avalonia;
using PropertyGenerator.Avalonia;
using LegacyMaterialToken = Material3Expressive.Foundation.Tokens.Runtime.MaterialToken;
using LegacySystemTypeScaleField = Material3Expressive.Foundation.Tokens.Runtime.SystemTypeScaleField;
using LegacySystemTypeScaleTokens = Material3Expressive.Foundation.Tokens.Runtime.SystemTypeScaleTokens;
using TypeScaleRawField = Material3Expressive.Foundation.Tokens.TypeScaleRawField;

namespace Material3Expressive.Foundation;

#pragma warning disable CS0618 // ResolvedTheme stays as this theme's snapshot until the M9 cutover; see docs/refactor/migration-plan.md.

/// <summary>
/// A placement-local Material token boundary and the legacy whole-theme authoring surface.
/// </summary>
public partial class MaterialTheme : ResourceProvider
{
    private readonly AvaloniaList<IMaterialTokenSource> _children = new()
    {
        ResetBehavior = ResetBehavior.Reset,
    };
    private readonly Dictionary<IMaterialTokenSource, int> _sourceSubscriptions =
        new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<TypeScaleOverrides> _typeScaleOverrideSubscriptions =
        new(ReferenceEqualityComparer.Instance);
    private ResolvedTheme? _resolved;
    private bool _authoringSubscriptionsActive;
    private bool _legacySubscriptionsActive;

    public MaterialTheme()
    {
        PropertyChanged += OnSelfPropertyChanged;
        _children.CollectionChanged += OnChildrenChanged;
        _children.Validate = ValidateChildSourceForInsertion;

        // A placement with no colour source must remain sparse so an ancestor scheme can flow
        // through it. The MCU catalog supplies the terminal baseline only after the entire owner
        // resource chain misses.
        TypographyScheme = new TypographyScheme();
        ShapeScheme = new ShapeScheme();
        MotionScheme = new MotionScheme();
        RippleTheme = new RippleThemeConfiguration();

        // Authoring subscriptions are tied to the real resource placement. A detached theme is
        // inert with respect to external mutation callbacks; OnAddOwner restores the compatibility
        // snapshot and subscribes the sparse sources when the provider is mounted again.
    }

    /// <summary>Raised once whenever this theme's local authoring state changes.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Gets the sparse Raw-token sources in this placement. Later children have higher priority.
    /// </summary>
    [Content]
    public AvaloniaList<IMaterialTokenSource> Children => _children;

    /// <summary>
    /// Gets or sets the optional MCU colour source. It behaves as an implicit source before every
    /// item in <see cref="Children"/>.
    /// </summary>
    [GeneratedStyledProperty] public partial ColorScheme? ColorScheme { get; set; }
    [GeneratedStyledProperty] public partial TypographyScheme TypographyScheme { get; set; }
    [GeneratedStyledProperty] public partial ShapeScheme ShapeScheme { get; set; }
    [GeneratedStyledProperty] public partial MotionScheme MotionScheme { get; set; }
    [GeneratedStyledProperty] public partial RippleThemeConfiguration RippleTheme { get; set; }

    /// <summary>The current compatibility snapshot, rebuilt on demand after any change.</summary>
    [Obsolete(
        "Legacy whole-theme snapshot. Consume typed MaterialValueKey<T> resources through this " +
        "MaterialTheme placement.",
        DiagnosticId = "M3E1004")]
    public ResolvedTheme Resolved => _resolved ??= new ResolvedTheme(this);

    internal void RestoreResolvedSnapshot(ResolvedTheme snapshot) =>
        _resolved = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

    /// <inheritdoc />
    public override bool HasResources => true;

    /// <inheritdoc />
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        // Typography ControlThemes have not reached their MaterialValue cutover yet. Keep this
        // narrow legacy publication until those consumers migrate; colour identities already use
        // the MCU-owned shared Raw contract and must not re-enter the old provider path.
        if (key is LegacyMaterialToken legacyTypeToken &&
            LegacySystemTypeScaleTokens.TryGetRole(
                legacyTypeToken,
                out var typeRole,
                out var field))
        {
            var scale = Resolved.Typography.GetTypeScale(typeRole);
            value = field switch
            {
                LegacySystemTypeScaleField.FontSize => scale.FontSize,
                LegacySystemTypeScaleField.LineHeight => scale.LineHeight,
                LegacySystemTypeScaleField.LetterSpacing => scale.LetterSpacing,
                LegacySystemTypeScaleField.Font => scale.Font,
                LegacySystemTypeScaleField.Weight => scale.Weight,
                _ => null,
            };
            return value is not null;
        }

        if (key is MaterialToken raw)
            return TryGetLocalRaw(raw, theme, out value);

        if (key is MaterialValueKey flat)
        {
            if (Owner is not { } owner)
            {
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.UnownedPlacement,
                    $"Material value '{flat.Path}' cannot be resolved because its MaterialTheme " +
                    "is not attached to a resource host.",
                    valueKey: flat,
                    theme: theme);
            }

            ValidateChildSourcePlacements();
            var session = new MaterialResolutionSession(owner, theme, DefaultRawTokens.Catalog);
            value = flat.Resolve(session);
            return true;
        }

        value = null;
        return false;
    }

    public object ProvideValue() => this;

    protected override void OnAddOwner(IResourceHost owner)
    {
        // Authoring objects may have changed while this placement was detached. Rebuild the
        // compatibility snapshot lazily from their current values and restore every mutation
        // subscription before Avalonia publishes the attach notification.
        _resolved = null;
        ActivateLegacySubscriptions();
        ActivateAuthoringSubscriptions();
        try
        {
            base.OnAddOwner(owner);
        }
        catch
        {
            DeactivateAuthoringSubscriptions();
            DeactivateLegacySubscriptions();
            throw;
        }
    }

    protected override void OnRemoveOwner(IResourceHost owner)
    {
        // Sources commonly outlive a placement (for example an application-owned override
        // object). They must not retain a detached MaterialTheme through event handlers.
        DeactivateAuthoringSubscriptions();
        DeactivateLegacySubscriptions();
        // Keep the last compatibility snapshot stable across a placement move. Detached
        // authoring mutations deliberately do not publish callbacks; reattachment rebuilds the
        // snapshot from the current authoring state.
        base.OnRemoveOwner(owner);
    }

    private bool TryGetLocalRaw(
        MaterialToken token,
        ThemeVariant? queryTheme,
        out object? definition)
    {
        for (var index = _children.Count - 1; index >= 0; index--)
        {
            var source = _children[index];
            if (source is null)
                continue;

            ValidateChildSourceForInsertion(source);

            var found = source.TryGetRawToken(token, queryTheme, out definition);
            if (!found)
            {
                ValidateLocalSourceMiss(source, token, definition, queryTheme);
                continue;
            }

            ValidateLocalDefinitionHit(source, token, definition, queryTheme);
            return true;
        }

        // When the convenience scheme is also present in Children, its child position is the
        // explicit placement. Do not query or subscribe to the same mutable source twice.
        if (ColorScheme is IMaterialTokenSource colorSource &&
            !ContainsReference(_children, colorSource))
        {
            ValidateChildSourceForInsertion(colorSource);
            var found = colorSource.TryGetRawToken(token, queryTheme, out definition);
            if (found)
            {
                ValidateLocalDefinitionHit(colorSource, token, definition, queryTheme);
                return true;
            }

            ValidateLocalSourceMiss(colorSource, token, definition, queryTheme);
        }

        // Legacy schemes remain a lower-priority, sparse authoring bridge. Unset fields must miss
        // here so the outer resource chain and terminal catalog can supply the definition.
        if (TryGetCompatibilityAuthoringRaw(token, out definition))
            return true;

        definition = null;
        return false;
    }

    private bool TryGetCompatibilityAuthoringRaw(
        MaterialToken token,
        out object? definition)
    {
        if (FoundationThemeRawTokens.Shape.TryGetRole(token, out var shapeRole))
            return TryGetShapeAuthoringRaw(shapeRole, out definition);

        if (FoundationThemeRawTokens.Motion.TryGetRole(token, out var motionRole))
            return TryGetMotionAuthoringRaw(motionRole, out definition);

        if (FoundationThemeRawTokens.TypeScale.TryGet(
                token,
                out var typographyRole,
                out var typeScaleField))
        {
            return TryGetTypeScaleAuthoringRaw(typographyRole, typeScaleField, out definition);
        }

        if (FoundationThemeRawTokens.Typography.TryGet(
                token,
                out FontFamilyToken fontFamilyToken))
        {
            return TryGetTypographyFontFamilyAuthoringRaw(fontFamilyToken, out definition);
        }

        if (FoundationThemeRawTokens.Typography.TryGet(
                token,
                out FontWeightToken fontWeightToken))
        {
            return TryGetTypographyFontWeightAuthoringRaw(fontWeightToken, out definition);
        }

        definition = null;
        return false;
    }

    private bool TryGetShapeAuthoringRaw(ShapeRole role, out object? definition)
    {
        var scheme = ShapeScheme;
        switch (role)
        {
            case ShapeRole.CornerExtraSmall when scheme.IsSet(ShapeScheme.ExtraSmallProperty):
                definition = Token.Literal(scheme.ExtraSmall);
                return true;
            case ShapeRole.CornerSmall when scheme.IsSet(ShapeScheme.SmallProperty):
                definition = Token.Literal(scheme.Small);
                return true;
            case ShapeRole.CornerMedium when scheme.IsSet(ShapeScheme.MediumProperty):
                definition = Token.Literal(scheme.Medium);
                return true;
            case ShapeRole.CornerLarge when scheme.IsSet(ShapeScheme.LargeProperty):
                definition = Token.Literal(scheme.Large);
                return true;
            case ShapeRole.CornerLargeIncreased
                when scheme.IsSet(ShapeScheme.LargeIncreasedProperty):
                definition = Token.Literal(scheme.LargeIncreased);
                return true;
            case ShapeRole.CornerExtraLarge when scheme.IsSet(ShapeScheme.ExtraLargeProperty):
                definition = Token.Literal(scheme.ExtraLarge);
                return true;
            case ShapeRole.CornerExtraLargeIncreased
                when scheme.IsSet(ShapeScheme.ExtraLargeIncreasedProperty):
                definition = Token.Literal(scheme.ExtraLargeIncreased);
                return true;
            case ShapeRole.CornerExtraExtraLarge
                when scheme.IsSet(ShapeScheme.ExtraExtraLargeProperty):
                definition = Token.Literal(scheme.ExtraExtraLarge);
                return true;
            default:
                definition = null;
                return false;
        }
    }

    private bool TryGetMotionAuthoringRaw(MotionRole role, out object? definition)
    {
        var scheme = MotionScheme;
        switch (role)
        {
            case MotionRole.DefaultSpatial when scheme.IsSet(MotionScheme.DefaultSpatialProperty):
                definition = Token.Literal(scheme.DefaultSpatial);
                return true;
            case MotionRole.FastSpatial when scheme.IsSet(MotionScheme.FastSpatialProperty):
                definition = Token.Literal(scheme.FastSpatial);
                return true;
            case MotionRole.SlowSpatial when scheme.IsSet(MotionScheme.SlowSpatialProperty):
                definition = Token.Literal(scheme.SlowSpatial);
                return true;
            case MotionRole.DefaultEffects when scheme.IsSet(MotionScheme.DefaultEffectsProperty):
                definition = Token.Literal(scheme.DefaultEffects);
                return true;
            case MotionRole.FastEffects when scheme.IsSet(MotionScheme.FastEffectsProperty):
                definition = Token.Literal(scheme.FastEffects);
                return true;
            case MotionRole.SlowEffects when scheme.IsSet(MotionScheme.SlowEffectsProperty):
                definition = Token.Literal(scheme.SlowEffects);
                return true;
            default:
                definition = null;
                return false;
        }
    }

    private bool TryGetTypeScaleAuthoringRaw(
        TypographyRole role,
        TypeScaleRawField field,
        out object? definition)
    {
        var overrides = TypographyScheme.TypeScaleOverrides;
        for (var index = overrides.Count - 1; index >= 0; index--)
        {
            var item = overrides[index];
            if (item.Variant != role)
                continue;

            switch (field)
            {
                case TypeScaleRawField.FontSize
                    when item.IsSet(TypeScaleOverrides.FontSizeProperty):
                    definition = Token.Literal(item.FontSize);
                    return true;
                case TypeScaleRawField.LineHeight
                    when item.IsSet(TypeScaleOverrides.LineHeightProperty):
                    definition = Token.Literal(item.LineHeight);
                    return true;
                case TypeScaleRawField.LetterSpacing
                    when item.IsSet(TypeScaleOverrides.LetterSpacingProperty):
                    definition = Token.Literal(item.LetterSpacing);
                    return true;
                case TypeScaleRawField.Font when item.IsSet(TypeScaleOverrides.FontProperty):
                    definition = Token.Literal(item.Font);
                    return true;
                case TypeScaleRawField.Weight when item.IsSet(TypeScaleOverrides.WeightProperty):
                    definition = Token.Literal(item.Weight);
                    return true;
            }
        }

        definition = null;
        return false;
    }

    private bool TryGetTypographyFontFamilyAuthoringRaw(
        FontFamilyToken token,
        out object? definition)
    {
        var scheme = TypographyScheme;
        switch (token)
        {
            case FontFamilyToken.Brand
                when scheme.IsSet(TypographyScheme.BrandFontFamilyProperty):
                definition = Token.Literal(scheme.BrandFontFamily);
                return true;
            case FontFamilyToken.Plain
                when scheme.IsSet(TypographyScheme.PlainFontFamilyProperty):
                definition = Token.Literal(scheme.PlainFontFamily);
                return true;
            case FontFamilyToken.EmphasizedBrand
                when scheme.IsSet(TypographyScheme.EmphasizedBrandFontFamilyProperty):
                definition = Token.Literal(scheme.EmphasizedBrandFontFamily);
                return true;
            case FontFamilyToken.EmphasizedPlain
                when scheme.IsSet(TypographyScheme.EmphasizedPlainFontFamilyProperty):
                definition = Token.Literal(scheme.EmphasizedPlainFontFamily);
                return true;
            default:
                definition = null;
                return false;
        }
    }

    private bool TryGetTypographyFontWeightAuthoringRaw(
        FontWeightToken token,
        out object? definition)
    {
        var scheme = TypographyScheme;
        switch (token)
        {
            case FontWeightToken.Regular when scheme.IsSet(TypographyScheme.WeightRegularProperty):
                definition = Token.Literal(scheme.WeightRegular);
                return true;
            case FontWeightToken.Medium when scheme.IsSet(TypographyScheme.WeightMediumProperty):
                definition = Token.Literal(scheme.WeightMedium);
                return true;
            case FontWeightToken.Bold when scheme.IsSet(TypographyScheme.WeightBoldProperty):
                definition = Token.Literal(scheme.WeightBold);
                return true;
            default:
                definition = null;
                return false;
        }
    }

    private static void ValidateLocalSourceMiss(
        IMaterialTokenSource source,
        MaterialToken token,
        object? definition,
        ThemeVariant? queryTheme)
    {
        if (definition is null)
            return;

        throw MaterialResolutionFailures.Create(
            MaterialResolutionFailureKind.RecipeFailure,
            $"Material token source '{source.GetType().FullName}' returned false but also " +
            $"provided a definition for raw token '{token.Path}'.",
            token: token,
            theme: queryTheme,
            path: [new MaterialResolutionFrame(token.Path, token.Layer, token.ValueType)],
            expected: token.ValueType,
            actual: definition.GetType());
    }

    private static void ValidateLocalDefinitionHit(
        IMaterialTokenSource source,
        MaterialToken token,
        object? definition,
        ThemeVariant? queryTheme)
    {
        var path = new[] { new MaterialResolutionFrame(token.Path, token.Layer, token.ValueType) };
        if (definition is null)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.NullDefinition,
                $"Material token source '{source.GetType().FullName}' returned a null definition " +
                $"for raw token '{token.Path}'.",
                token: token,
                theme: queryTheme,
                path: path,
                expected: token.ValueType);
        }

        var actualType = definition is TokenDefinition typedDefinition
            ? typedDefinition.ValueType
            : definition.GetType();
        var valid = definition is TokenDefinition
            ? actualType == token.ValueType
            : token.ValueType.IsInstanceOfType(definition);
        if (!valid)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.WrongType,
                $"Material token source '{source.GetType().FullName}' returned " +
                $"'{actualType.FullName}' for raw token '{token.Path}', but " +
                $"'{token.ValueType.FullName}' is required.",
                token: token,
                theme: queryTheme,
                path: path,
                expected: token.ValueType,
                actual: actualType);
        }
    }

    private void OnSelfPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ColorSchemeProperty)
        {
            if (_legacySubscriptionsActive)
            {
                DetachLegacyScheme(e.OldValue);
                AttachLegacyScheme(e.NewValue);
            }
            if (_authoringSubscriptionsActive)
                UpdateSourceSubscriptions();
            Invalidate();
            return;
        }

        if (e.Property != TypographySchemeProperty
            && e.Property != ShapeSchemeProperty
            && e.Property != MotionSchemeProperty
            && e.Property != RippleThemeProperty)
            return;

        if (_legacySubscriptionsActive)
        {
            DetachLegacyScheme(e.OldValue);
            AttachLegacyScheme(e.NewValue);
            if (e.Property == TypographySchemeProperty)
            {
                if (e.OldValue is TypographyScheme oldTypography)
                    oldTypography.TypeScaleOverrides.CollectionChanged -= OnTypeScaleOverridesChanged;
                if (e.NewValue is TypographyScheme newTypography)
                    newTypography.TypeScaleOverrides.CollectionChanged += OnTypeScaleOverridesChanged;
                UpdateTypeScaleOverrideSubscriptions();
            }
        }
        Invalidate();
    }

    private void AttachLegacyScheme(object? scheme)
    {
        switch (scheme)
        {
            case TypographyScheme typographyScheme:
                typographyScheme.PropertyChanged += OnSchemePropertyChanged;
                break;
            case ColorScheme colorScheme:
                // SchemeChanged also covers CustomColors mutations, which do not surface as a
                // property change on the ColorScheme itself.
                colorScheme.SchemeChanged += OnColorSchemeChanged;
                break;
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged += OnSchemePropertyChanged;
                break;
        }
    }

    private void DetachLegacyScheme(object? scheme)
    {
        switch (scheme)
        {
            case TypographyScheme typographyScheme:
                typographyScheme.PropertyChanged -= OnSchemePropertyChanged;
                break;
            case ColorScheme colorScheme:
                colorScheme.SchemeChanged -= OnColorSchemeChanged;
                break;
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged -= OnSchemePropertyChanged;
                break;
        }
    }

    private void ActivateLegacySubscriptions()
    {
        if (_legacySubscriptionsActive)
            return;

        _legacySubscriptionsActive = true;
        AttachLegacyScheme(ColorScheme);
        AttachLegacyScheme(TypographyScheme);
        AttachLegacyScheme(ShapeScheme);
        AttachLegacyScheme(MotionScheme);
        AttachLegacyScheme(RippleTheme);
        TypographyScheme.TypeScaleOverrides.CollectionChanged += OnTypeScaleOverridesChanged;
        UpdateTypeScaleOverrideSubscriptions();
    }

    private void DeactivateLegacySubscriptions()
    {
        if (!_legacySubscriptionsActive)
            return;

        _legacySubscriptionsActive = false;
        DetachLegacyScheme(ColorScheme);
        DetachLegacyScheme(TypographyScheme);
        DetachLegacyScheme(ShapeScheme);
        DetachLegacyScheme(MotionScheme);
        DetachLegacyScheme(RippleTheme);
        TypographyScheme.TypeScaleOverrides.CollectionChanged -= OnTypeScaleOverridesChanged;

        foreach (var item in _typeScaleOverrideSubscriptions)
            item.PropertyChanged -= OnSchemePropertyChanged;
        _typeScaleOverrideSubscriptions.Clear();
    }

    private void OnColorSchemeChanged(object? sender, EventArgs e)
    {
        // Once attached, the raw-source subscription below is the authoritative notification and
        // avoids publishing the same change twice. Legacy callbacks are only active while the
        // provider is mounted.
        if (!_authoringSubscriptionsActive)
            Invalidate();
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_authoringSubscriptionsActive)
            UpdateSourceSubscriptions();
        Invalidate();
    }

    private static void ValidateChildSourceForInsertion(IMaterialTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is IResourceProvider { Owner: not null } provider)
        {
            throw new InvalidOperationException(
                $"Material token source '{source.GetType().FullName}' is already installed as an " +
                $"independent resource provider on '{provider.Owner.GetType().FullName}'. A source " +
                "instance cannot be installed independently and in MaterialTheme.Children at the same time.");
        }
    }

    private void ValidateChildSourcePlacements()
    {
        foreach (var source in _children)
            ValidateChildSourceForInsertion(source);

        if (ColorScheme is IMaterialTokenSource colorSource &&
            !ContainsReference(_children, colorSource))
        {
            ValidateChildSourceForInsertion(colorSource);
        }
    }

    private void ActivateAuthoringSubscriptions()
    {
        if (_authoringSubscriptionsActive)
            return;

        ValidateChildSourcePlacements();

        _authoringSubscriptionsActive = true;
        try
        {
            UpdateSourceSubscriptions();
        }
        catch
        {
            DeactivateAuthoringSubscriptions();
            throw;
        }
    }

    private void DeactivateAuthoringSubscriptions()
    {
        if (!_authoringSubscriptionsActive)
            return;

        _authoringSubscriptionsActive = false;
        foreach (var source in _sourceSubscriptions.Keys)
            source.Changed -= OnTokenSourceChanged;
        _sourceSubscriptions.Clear();

    }

    private void UpdateSourceSubscriptions()
    {
        var desired = new Dictionary<IMaterialTokenSource, int>(ReferenceEqualityComparer.Instance);
        if (ColorScheme is IMaterialTokenSource colorSource)
            AddReference(desired, colorSource);
        foreach (var source in _children)
        {
            if (source is not null)
                AddReference(desired, source);
        }

        foreach (var source in desired.Keys)
            ValidateChildSourceForInsertion(source);

        foreach (var source in _sourceSubscriptions.Keys.ToArray())
        {
            if (!desired.ContainsKey(source))
                source.Changed -= OnTokenSourceChanged;
        }

        foreach (var source in desired.Keys)
        {
            if (!_sourceSubscriptions.ContainsKey(source))
                source.Changed += OnTokenSourceChanged;
        }

        _sourceSubscriptions.Clear();
        foreach (var pair in desired)
            _sourceSubscriptions.Add(pair.Key, pair.Value);
    }

    private static void AddReference(
        Dictionary<IMaterialTokenSource, int> references,
        IMaterialTokenSource source)
    {
        references.TryGetValue(source, out var count);
        references[source] = count + 1;
    }

    private static bool ContainsReference(
        IEnumerable<IMaterialTokenSource> sources,
        IMaterialTokenSource candidate)
    {
        foreach (var source in sources)
        {
            if (ReferenceEquals(source, candidate))
                return true;
        }

        return false;
    }

    private void OnTokenSourceChanged(object? sender, EventArgs e)
    {
        if (sender is IMaterialTokenSource source)
            ValidateChildSourceForInsertion(source);
        Invalidate();
    }

    private void OnSchemePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => Invalidate();

    private void OnTypeScaleOverridesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateTypeScaleOverrideSubscriptions();
        Invalidate();
    }

    private void UpdateTypeScaleOverrideSubscriptions()
    {
        var desired = TypographyScheme is { } typography
            ? new HashSet<TypeScaleOverrides>(
                typography.TypeScaleOverrides,
                ReferenceEqualityComparer.Instance)
            : new HashSet<TypeScaleOverrides>(ReferenceEqualityComparer.Instance);

        foreach (var item in _typeScaleOverrideSubscriptions.ToArray())
        {
            if (!desired.Contains(item))
                item.PropertyChanged -= OnSchemePropertyChanged;
        }

        foreach (var item in desired)
        {
            if (!_typeScaleOverrideSubscriptions.Contains(item))
                item.PropertyChanged += OnSchemePropertyChanged;
        }

        _typeScaleOverrideSubscriptions.Clear();
        _typeScaleOverrideSubscriptions.UnionWith(desired);
    }

    private void Invalidate()
    {
        _resolved = null;
        RaiseResourcesChanged();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
