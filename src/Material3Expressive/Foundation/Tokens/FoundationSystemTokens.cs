using Avalonia.Media;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>Canonical Raw identities for the Material system shape scale.</summary>
public static class SystemShapeTokens
{
    public static MaterialToken<RoundedCornerShape> CornerNone { get; } =
        new("System.Shape.CornerNone", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerExtraSmall { get; } =
        new("System.Shape.CornerExtraSmall", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerSmall { get; } =
        new("System.Shape.CornerSmall", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerMedium { get; } =
        new("System.Shape.CornerMedium", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerLarge { get; } =
        new("System.Shape.CornerLarge", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerLargeIncreased { get; } =
        new("System.Shape.CornerLargeIncreased", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerExtraLarge { get; } =
        new("System.Shape.CornerExtraLarge", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerExtraLargeIncreased { get; } =
        new("System.Shape.CornerExtraLargeIncreased", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerExtraExtraLarge { get; } =
        new("System.Shape.CornerExtraExtraLarge", TokenLayer.System);

    public static MaterialToken<RoundedCornerShape> CornerFull { get; } =
        new("System.Shape.CornerFull", TokenLayer.System);

    internal static IReadOnlyCollection<MaterialToken> All { get; } =
        Array.AsReadOnly<MaterialToken>(
        [
            CornerNone,
            CornerExtraSmall,
            CornerSmall,
            CornerMedium,
            CornerLarge,
            CornerLargeIncreased,
            CornerExtraLarge,
            CornerExtraLargeIncreased,
            CornerExtraExtraLarge,
            CornerFull,
        ]);

    internal static MaterialToken<RoundedCornerShape> ForProjection(ShapeRole role) => role switch
    {
        ShapeRole.CornerNone => CornerNone,
        ShapeRole.CornerExtraSmall or ShapeRole.CornerExtraSmallTop => CornerExtraSmall,
        ShapeRole.CornerSmall => CornerSmall,
        ShapeRole.CornerMedium => CornerMedium,
        ShapeRole.CornerLarge or
        ShapeRole.CornerLargeStart or
        ShapeRole.CornerLargeEnd or
        ShapeRole.CornerLargeTop => CornerLarge,
        ShapeRole.CornerLargeIncreased => CornerLargeIncreased,
        ShapeRole.CornerExtraLarge or ShapeRole.CornerExtraLargeTop => CornerExtraLarge,
        ShapeRole.CornerExtraLargeIncreased => CornerExtraLargeIncreased,
        ShapeRole.CornerExtraExtraLarge => CornerExtraExtraLarge,
        ShapeRole.CornerFull => CornerFull,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown shape role."),
    };
}

/// <summary>Canonical Raw identities for the Material system motion roles.</summary>
public static class SystemMotionTokens
{
    public static MaterialToken<SpringSpec> DefaultSpatial { get; } =
        new("System.Motion.DefaultSpatial", TokenLayer.System);

    public static MaterialToken<SpringSpec> FastSpatial { get; } =
        new("System.Motion.FastSpatial", TokenLayer.System);

    public static MaterialToken<SpringSpec> SlowSpatial { get; } =
        new("System.Motion.SlowSpatial", TokenLayer.System);

    public static MaterialToken<SpringSpec> DefaultEffects { get; } =
        new("System.Motion.DefaultEffects", TokenLayer.System);

    public static MaterialToken<SpringSpec> FastEffects { get; } =
        new("System.Motion.FastEffects", TokenLayer.System);

    public static MaterialToken<SpringSpec> SlowEffects { get; } =
        new("System.Motion.SlowEffects", TokenLayer.System);

    internal static IReadOnlyCollection<MaterialToken> All { get; } =
        Array.AsReadOnly<MaterialToken>(
        [
            DefaultSpatial,
            FastSpatial,
            SlowSpatial,
            DefaultEffects,
            FastEffects,
            SlowEffects,
        ]);

    public static MaterialToken<SpringSpec> For(MotionRole role) => role switch
    {
        MotionRole.DefaultSpatial => DefaultSpatial,
        MotionRole.FastSpatial => FastSpatial,
        MotionRole.SlowSpatial => SlowSpatial,
        MotionRole.DefaultEffects => DefaultEffects,
        MotionRole.FastEffects => FastEffects,
        MotionRole.SlowEffects => SlowEffects,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown motion role."),
    };
}

/// <summary>Canonical Raw identities for Material system typefaces and weights.</summary>
public static class SystemTypographyTokens
{
    public static MaterialToken<FontFamily> BrandFontFamily { get; } =
        new("System.Typography.FontFamily.Brand", TokenLayer.System);

    public static MaterialToken<FontFamily> PlainFontFamily { get; } =
        new("System.Typography.FontFamily.Plain", TokenLayer.System);

    public static MaterialToken<FontFamily> EmphasizedBrandFontFamily { get; } =
        new("System.Typography.FontFamily.EmphasizedBrand", TokenLayer.System);

    public static MaterialToken<FontFamily> EmphasizedPlainFontFamily { get; } =
        new("System.Typography.FontFamily.EmphasizedPlain", TokenLayer.System);

    public static MaterialToken<FontWeight> WeightRegular { get; } =
        new("System.Typography.FontWeight.Regular", TokenLayer.System);

    public static MaterialToken<FontWeight> WeightMedium { get; } =
        new("System.Typography.FontWeight.Medium", TokenLayer.System);

    public static MaterialToken<FontWeight> WeightBold { get; } =
        new("System.Typography.FontWeight.Bold", TokenLayer.System);

    internal static IReadOnlyCollection<MaterialToken> All { get; } =
        Array.AsReadOnly<MaterialToken>(
        [
            BrandFontFamily,
            PlainFontFamily,
            EmphasizedBrandFontFamily,
            EmphasizedPlainFontFamily,
            WeightRegular,
            WeightMedium,
            WeightBold,
        ]);

    public static MaterialToken<FontFamily> For(FontFamilyToken token) => token switch
    {
        FontFamilyToken.Brand => BrandFontFamily,
        FontFamilyToken.Plain => PlainFontFamily,
        FontFamilyToken.EmphasizedBrand => EmphasizedBrandFontFamily,
        FontFamilyToken.EmphasizedPlain => EmphasizedPlainFontFamily,
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown font-family token."),
    };

    public static MaterialToken<FontWeight> For(FontWeightToken token) => token switch
    {
        FontWeightToken.Regular => WeightRegular,
        FontWeightToken.Medium => WeightMedium,
        FontWeightToken.Bold => WeightBold,
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown font-weight token."),
    };
}

/// <summary>Canonical Raw identities for every Material type-scale role.</summary>
public static class SystemTypeScaleTokens
{
    private static readonly IReadOnlyDictionary<TypographyRole, SystemTypeScaleTokenSet> Sets =
        CreateSets();

    internal static IReadOnlyCollection<MaterialToken> All { get; } =
        Array.AsReadOnly(Sets.Values.SelectMany(static set => set.All).ToArray());

    public static SystemTypeScaleTokenSet For(TypographyRole role)
    {
        if (Sets.TryGetValue(role, out var tokens))
            return tokens;

        throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown typography role.");
    }

    private static IReadOnlyDictionary<TypographyRole, SystemTypeScaleTokenSet> CreateSets()
    {
        var sets = new Dictionary<TypographyRole, SystemTypeScaleTokenSet>();
        foreach (var role in Enum.GetValues<TypographyRole>())
            sets.Add(role, new SystemTypeScaleTokenSet(role));

        return sets;
    }
}

/// <summary>The five canonical Raw identities belonging to one Material type-scale role.</summary>
public sealed class SystemTypeScaleTokenSet
{
    internal SystemTypeScaleTokenSet(TypographyRole role)
    {
        var prefix = $"System.TypeScale.{role}";
        FontFamily = new MaterialToken<FontFamily>($"{prefix}.FontFamily", TokenLayer.System);
        FontWeight = new MaterialToken<FontWeight>($"{prefix}.FontWeight", TokenLayer.System);
        FontSize = new MaterialToken<double>($"{prefix}.FontSize", TokenLayer.System);
        LineHeight = new MaterialToken<double>($"{prefix}.LineHeight", TokenLayer.System);
        LetterSpacing = new MaterialToken<double>($"{prefix}.LetterSpacing", TokenLayer.System);
        All = Array.AsReadOnly<MaterialToken>(
            [FontFamily, FontWeight, FontSize, LineHeight, LetterSpacing]);
    }

    public MaterialToken<FontFamily> FontFamily { get; }

    public MaterialToken<FontWeight> FontWeight { get; }

    public MaterialToken<double> FontSize { get; }

    public MaterialToken<double> LineHeight { get; }

    public MaterialToken<double> LetterSpacing { get; }

    internal IReadOnlyCollection<MaterialToken> All { get; }
}

/// <summary>Immutable baseline definitions for the M3E-owned Foundation Raw domains.</summary>
internal sealed class FoundationDefaultRawTokens : IMaterialTokenCatalogContribution
{
    private static readonly IReadOnlyList<MaterialTokenCatalogRequirement> NoRequirements =
        Array.Empty<MaterialTokenCatalogRequirement>();

    private readonly IReadOnlyDictionary<MaterialToken, TokenDefinition> _definitions;
    private readonly IReadOnlyCollection<MaterialToken> _declaredTokens;

    private FoundationDefaultRawTokens()
    {
        var definitions = new Dictionary<MaterialToken, TokenDefinition>(
            ReferenceEqualityComparer.Instance);

        Add(definitions, SystemShapeTokens.CornerNone, ShapeTokens.CornerNone);
        Add(definitions, SystemShapeTokens.CornerExtraSmall, ShapeTokens.CornerExtraSmall);
        Add(definitions, SystemShapeTokens.CornerSmall, ShapeTokens.CornerSmall);
        Add(definitions, SystemShapeTokens.CornerMedium, ShapeTokens.CornerMedium);
        Add(definitions, SystemShapeTokens.CornerLarge, ShapeTokens.CornerLarge);
        Add(definitions, SystemShapeTokens.CornerLargeIncreased, ShapeTokens.CornerLargeIncreased);
        Add(definitions, SystemShapeTokens.CornerExtraLarge, ShapeTokens.CornerExtraLarge);
        Add(
            definitions,
            SystemShapeTokens.CornerExtraLargeIncreased,
            ShapeTokens.CornerExtraLargeIncreased);
        Add(definitions, SystemShapeTokens.CornerExtraExtraLarge, ShapeTokens.CornerExtraExtraLarge);
        Add(definitions, SystemShapeTokens.CornerFull, ShapeTokens.CornerFull);

        Add(definitions, SystemMotionTokens.DefaultSpatial, MotionTokens.Expressive.DefaultSpatial);
        Add(definitions, SystemMotionTokens.FastSpatial, MotionTokens.Expressive.FastSpatial);
        Add(definitions, SystemMotionTokens.SlowSpatial, MotionTokens.Expressive.SlowSpatial);
        Add(definitions, SystemMotionTokens.DefaultEffects, MotionTokens.Expressive.DefaultEffects);
        Add(definitions, SystemMotionTokens.FastEffects, MotionTokens.Expressive.FastEffects);
        Add(definitions, SystemMotionTokens.SlowEffects, MotionTokens.Expressive.SlowEffects);

        Add(definitions, SystemTypographyTokens.BrandFontFamily, TypefaceTokens.Brand);
        Add(definitions, SystemTypographyTokens.PlainFontFamily, TypefaceTokens.Plain);
        Add(
            definitions,
            SystemTypographyTokens.EmphasizedBrandFontFamily,
            TypefaceTokens.EmphasizedBrand);
        Add(
            definitions,
            SystemTypographyTokens.EmphasizedPlainFontFamily,
            TypefaceTokens.EmphasizedPlain);
        Add(definitions, SystemTypographyTokens.WeightRegular, TypefaceTokens.WeightRegular);
        Add(definitions, SystemTypographyTokens.WeightMedium, TypefaceTokens.WeightMedium);
        Add(definitions, SystemTypographyTokens.WeightBold, TypefaceTokens.WeightBold);

        foreach (var role in Enum.GetValues<TypographyRole>())
        {
            var tokens = SystemTypeScaleTokens.For(role);
            var values = global::Material3Expressive.Foundation.TypeScale.For(role);
            AddAlias(definitions, tokens.FontFamily, SystemTypographyTokens.For(values.Font));
            AddAlias(definitions, tokens.FontWeight, SystemTypographyTokens.For(values.Weight));
            Add(definitions, tokens.FontSize, values.FontSize);
            Add(definitions, tokens.LineHeight, values.LineHeight);
            Add(definitions, tokens.LetterSpacing, values.LetterSpacing);
        }

        _definitions = definitions;
        _declaredTokens = Array.AsReadOnly(definitions.Keys.ToArray());
    }

    internal static FoundationDefaultRawTokens Instance { get; } = new();

    public string Id => "Material3Expressive.FoundationDefaultRawTokens";

    public int SchemaVersion => 1;

    public IReadOnlyList<MaterialTokenCatalogRequirement> Requirements => NoRequirements;

    public IReadOnlyCollection<MaterialToken> DeclaredTokens => _declaredTokens;

    public bool Owns(MaterialToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _definitions.ContainsKey(token);
    }

    public MaterialTokenMissingPolicy GetMissingPolicy(MaterialToken token)
    {
        if (!Owns(token))
            throw new ArgumentException("The token is not owned by this contribution.", nameof(token));

        return MaterialTokenMissingPolicy.CatalogRequired;
    }

    public bool TryGetDefinition(
        MaterialToken token,
        Avalonia.Styling.ThemeVariant? queryTheme,
        out TokenDefinition? definition)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _definitions.TryGetValue(token, out definition);
    }

    private static void Add<T>(
        IDictionary<MaterialToken, TokenDefinition> definitions,
        MaterialToken<T> token,
        T value) =>
        definitions.Add(token, Token.Literal(value));

    private static void AddAlias<T>(
        IDictionary<MaterialToken, TokenDefinition> definitions,
        MaterialToken<T> token,
        MaterialToken<T> target) =>
        definitions.Add(token, Token.Alias(target));
}
