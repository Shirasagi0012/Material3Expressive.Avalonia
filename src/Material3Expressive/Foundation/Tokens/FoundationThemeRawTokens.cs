using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>
/// Internal Raw identities which bridge the mutable foundation authoring schemes into the shared
/// Material token graph.
/// </summary>
/// <remarks>
/// These descriptors deliberately remain internal until the generated token domain has a stable
/// public authoring contract for shape, motion, and record-shaped system token families.
/// </remarks>
internal static class FoundationThemeRawTokens
{
    internal static class Shape
    {
        internal static MaterialToken<RoundedCornerShape> ExtraSmall { get; } =
            new("System.Shape.CornerExtraSmall", TokenLayer.System);

        internal static MaterialToken<RoundedCornerShape> Small { get; } =
            new("System.Shape.CornerSmall", TokenLayer.System);

        internal static MaterialToken<RoundedCornerShape> Medium { get; } =
            new("System.Shape.CornerMedium", TokenLayer.System);

        internal static MaterialToken<RoundedCornerShape> Large { get; } =
            new("System.Shape.CornerLarge", TokenLayer.System);

        internal static MaterialToken<RoundedCornerShape> LargeIncreased { get; } =
            new("System.Shape.CornerLargeIncreased", TokenLayer.System);

        internal static MaterialToken<RoundedCornerShape> ExtraLarge { get; } =
            new("System.Shape.CornerExtraLarge", TokenLayer.System);

        internal static MaterialToken<RoundedCornerShape> ExtraLargeIncreased { get; } =
            new("System.Shape.CornerExtraLargeIncreased", TokenLayer.System);

        internal static MaterialToken<RoundedCornerShape> ExtraExtraLarge { get; } =
            new("System.Shape.CornerExtraExtraLarge", TokenLayer.System);

        internal static IReadOnlyCollection<MaterialToken> All { get; } =
            Array.AsReadOnly<MaterialToken>(
            [
                ExtraSmall,
                Small,
                Medium,
                Large,
                LargeIncreased,
                ExtraLarge,
                ExtraLargeIncreased,
                ExtraExtraLarge,
            ]);

        /// <summary>Gets the configurable Raw slot for a base role.</summary>
        internal static bool TryFor(
            ShapeRole role,
            [NotNullWhen(true)] out MaterialToken<RoundedCornerShape>? token)
        {
            token = role switch
            {
                ShapeRole.CornerExtraSmall => ExtraSmall,
                ShapeRole.CornerSmall => Small,
                ShapeRole.CornerMedium => Medium,
                ShapeRole.CornerLarge => Large,
                ShapeRole.CornerLargeIncreased => LargeIncreased,
                ShapeRole.CornerExtraLarge => ExtraLarge,
                ShapeRole.CornerExtraLargeIncreased => ExtraLargeIncreased,
                ShapeRole.CornerExtraExtraLarge => ExtraExtraLarge,
                _ => null,
            };

            return token is not null;
        }

        /// <summary>Identifies the configurable base role represented by a canonical Raw token.</summary>
        internal static bool TryGetRole(MaterialToken token, out ShapeRole role)
        {
            if (ReferenceEquals(token, ExtraSmall))
                role = ShapeRole.CornerExtraSmall;
            else if (ReferenceEquals(token, Small))
                role = ShapeRole.CornerSmall;
            else if (ReferenceEquals(token, Medium))
                role = ShapeRole.CornerMedium;
            else if (ReferenceEquals(token, Large))
                role = ShapeRole.CornerLarge;
            else if (ReferenceEquals(token, LargeIncreased))
                role = ShapeRole.CornerLargeIncreased;
            else if (ReferenceEquals(token, ExtraLarge))
                role = ShapeRole.CornerExtraLarge;
            else if (ReferenceEquals(token, ExtraLargeIncreased))
                role = ShapeRole.CornerExtraLargeIncreased;
            else if (ReferenceEquals(token, ExtraExtraLarge))
                role = ShapeRole.CornerExtraExtraLarge;
            else
            {
                role = default;
                return false;
            }

            return true;
        }
    }

    internal static class Motion
    {
        internal static MaterialToken<SpringSpec> DefaultSpatial { get; } =
            new("System.Motion.DefaultSpatial", TokenLayer.System);

        internal static MaterialToken<SpringSpec> FastSpatial { get; } =
            new("System.Motion.FastSpatial", TokenLayer.System);

        internal static MaterialToken<SpringSpec> SlowSpatial { get; } =
            new("System.Motion.SlowSpatial", TokenLayer.System);

        internal static MaterialToken<SpringSpec> DefaultEffects { get; } =
            new("System.Motion.DefaultEffects", TokenLayer.System);

        internal static MaterialToken<SpringSpec> FastEffects { get; } =
            new("System.Motion.FastEffects", TokenLayer.System);

        internal static MaterialToken<SpringSpec> SlowEffects { get; } =
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

        internal static MaterialToken<SpringSpec> For(MotionRole role) => role switch
        {
            MotionRole.DefaultSpatial => DefaultSpatial,
            MotionRole.FastSpatial => FastSpatial,
            MotionRole.SlowSpatial => SlowSpatial,
            MotionRole.DefaultEffects => DefaultEffects,
            MotionRole.FastEffects => FastEffects,
            MotionRole.SlowEffects => SlowEffects,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown motion role."),
        };

        internal static bool TryGetRole(MaterialToken token, out MotionRole role)
        {
            if (ReferenceEquals(token, DefaultSpatial))
                role = MotionRole.DefaultSpatial;
            else if (ReferenceEquals(token, FastSpatial))
                role = MotionRole.FastSpatial;
            else if (ReferenceEquals(token, SlowSpatial))
                role = MotionRole.SlowSpatial;
            else if (ReferenceEquals(token, DefaultEffects))
                role = MotionRole.DefaultEffects;
            else if (ReferenceEquals(token, FastEffects))
                role = MotionRole.FastEffects;
            else if (ReferenceEquals(token, SlowEffects))
                role = MotionRole.SlowEffects;
            else
            {
                role = default;
                return false;
            }

            return true;
        }
    }

    internal static class TypeScale
    {
        private static readonly IReadOnlyDictionary<TypographyRole, TypeScaleRawTokenSet> Sets =
            CreateSets();

        private static readonly IReadOnlyDictionary<MaterialToken, TypeScaleRawTokenIdentity> ByToken =
            CreateReverseLookup(Sets);

        internal static IReadOnlyCollection<MaterialToken> All { get; } =
            Array.AsReadOnly(ByToken.Keys.ToArray());

        internal static TypeScaleRawTokenSet For(TypographyRole role)
        {
            if (Sets.TryGetValue(role, out var tokens))
                return tokens;

            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown typography role.");
        }

        internal static bool TryGet(
            MaterialToken token,
            out TypographyRole role,
            out TypeScaleRawField field)
        {
            if (token is not null && ByToken.TryGetValue(token, out var identity))
            {
                role = identity.Role;
                field = identity.Field;
                return true;
            }

            role = default;
            field = default;
            return false;
        }

        private static IReadOnlyDictionary<TypographyRole, TypeScaleRawTokenSet> CreateSets()
        {
            var sets = new Dictionary<TypographyRole, TypeScaleRawTokenSet>();
            foreach (var role in Enum.GetValues<TypographyRole>())
                sets.Add(role, new TypeScaleRawTokenSet(role));

            return sets;
        }

        private static IReadOnlyDictionary<MaterialToken, TypeScaleRawTokenIdentity> CreateReverseLookup(
            IReadOnlyDictionary<TypographyRole, TypeScaleRawTokenSet> sets)
        {
            var result = new Dictionary<MaterialToken, TypeScaleRawTokenIdentity>(
                ReferenceEqualityComparer.Instance);

            foreach (var pair in sets)
            {
                result.Add(
                    pair.Value.FontSize,
                    new TypeScaleRawTokenIdentity(pair.Key, TypeScaleRawField.FontSize));
                result.Add(
                    pair.Value.LineHeight,
                    new TypeScaleRawTokenIdentity(pair.Key, TypeScaleRawField.LineHeight));
                result.Add(
                    pair.Value.LetterSpacing,
                    new TypeScaleRawTokenIdentity(pair.Key, TypeScaleRawField.LetterSpacing));
                result.Add(
                    pair.Value.Font,
                    new TypeScaleRawTokenIdentity(pair.Key, TypeScaleRawField.Font));
                result.Add(
                    pair.Value.Weight,
                    new TypeScaleRawTokenIdentity(pair.Key, TypeScaleRawField.Weight));
            }

            return result;
        }
    }

    internal static class Typography
    {
        internal static MaterialToken<FontFamily> BrandFontFamily { get; } =
            new("System.Typography.FontFamily.Brand", TokenLayer.System);

        internal static MaterialToken<FontFamily> PlainFontFamily { get; } =
            new("System.Typography.FontFamily.Plain", TokenLayer.System);

        internal static MaterialToken<FontFamily> EmphasizedBrandFontFamily { get; } =
            new("System.Typography.FontFamily.EmphasizedBrand", TokenLayer.System);

        internal static MaterialToken<FontFamily> EmphasizedPlainFontFamily { get; } =
            new("System.Typography.FontFamily.EmphasizedPlain", TokenLayer.System);

        internal static MaterialToken<FontWeight> WeightRegular { get; } =
            new("System.Typography.FontWeight.Regular", TokenLayer.System);

        internal static MaterialToken<FontWeight> WeightMedium { get; } =
            new("System.Typography.FontWeight.Medium", TokenLayer.System);

        internal static MaterialToken<FontWeight> WeightBold { get; } =
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

        internal static MaterialToken<FontFamily> For(FontFamilyToken token) => token switch
        {
            FontFamilyToken.Brand => BrandFontFamily,
            FontFamilyToken.Plain => PlainFontFamily,
            FontFamilyToken.EmphasizedBrand => EmphasizedBrandFontFamily,
            FontFamilyToken.EmphasizedPlain => EmphasizedPlainFontFamily,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown font-family token."),
        };

        internal static MaterialToken<FontWeight> For(FontWeightToken token) => token switch
        {
            FontWeightToken.Regular => WeightRegular,
            FontWeightToken.Medium => WeightMedium,
            FontWeightToken.Bold => WeightBold,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown font-weight token."),
        };

        internal static bool TryGet(MaterialToken token, out FontFamilyToken family)
        {
            if (ReferenceEquals(token, BrandFontFamily))
                family = FontFamilyToken.Brand;
            else if (ReferenceEquals(token, PlainFontFamily))
                family = FontFamilyToken.Plain;
            else if (ReferenceEquals(token, EmphasizedBrandFontFamily))
                family = FontFamilyToken.EmphasizedBrand;
            else if (ReferenceEquals(token, EmphasizedPlainFontFamily))
                family = FontFamilyToken.EmphasizedPlain;
            else
            {
                family = default;
                return false;
            }

            return true;
        }

        internal static bool TryGet(MaterialToken token, out FontWeightToken weight)
        {
            if (ReferenceEquals(token, WeightRegular))
                weight = FontWeightToken.Regular;
            else if (ReferenceEquals(token, WeightMedium))
                weight = FontWeightToken.Medium;
            else if (ReferenceEquals(token, WeightBold))
                weight = FontWeightToken.Bold;
            else
            {
                weight = default;
                return false;
            }

            return true;
        }
    }
}

internal enum TypeScaleRawField
{
    FontSize,
    LineHeight,
    LetterSpacing,
    Font,
    Weight,
}

internal sealed class TypeScaleRawTokenSet
{
    internal TypeScaleRawTokenSet(TypographyRole role)
    {
        var prefix = $"System.TypeScale.{role}";
        FontSize = new MaterialToken<double>($"{prefix}.FontSize", TokenLayer.System);
        LineHeight = new MaterialToken<double>($"{prefix}.LineHeight", TokenLayer.System);
        LetterSpacing = new MaterialToken<double>($"{prefix}.LetterSpacing", TokenLayer.System);
        Font = new MaterialToken<FontFamilyToken>($"{prefix}.Font", TokenLayer.System);
        Weight = new MaterialToken<FontWeightToken>($"{prefix}.Weight", TokenLayer.System);
    }

    internal MaterialToken<double> FontSize { get; }

    internal MaterialToken<double> LineHeight { get; }

    internal MaterialToken<double> LetterSpacing { get; }

    internal MaterialToken<FontFamilyToken> Font { get; }

    internal MaterialToken<FontWeightToken> Weight { get; }
}

internal readonly record struct TypeScaleRawTokenIdentity(
    TypographyRole Role,
    TypeScaleRawField Field);

/// <summary>Immutable baseline definitions for the internal foundation theme Raw families.</summary>
internal sealed class FoundationThemeDefaultRawTokens : IMaterialTokenCatalogContribution
{
    private static readonly IReadOnlyList<MaterialTokenCatalogRequirement> NoRequirements =
        Array.Empty<MaterialTokenCatalogRequirement>();

    private readonly IReadOnlyDictionary<MaterialToken, TokenDefinition> _definitions;
    private readonly IReadOnlyCollection<MaterialToken> _declaredTokens;

    private FoundationThemeDefaultRawTokens()
    {
        var definitions = new Dictionary<MaterialToken, TokenDefinition>(
            ReferenceEqualityComparer.Instance);

        Add(definitions, FoundationThemeRawTokens.Shape.ExtraSmall, ShapeTokens.CornerExtraSmall);
        Add(definitions, FoundationThemeRawTokens.Shape.Small, ShapeTokens.CornerSmall);
        Add(definitions, FoundationThemeRawTokens.Shape.Medium, ShapeTokens.CornerMedium);
        Add(definitions, FoundationThemeRawTokens.Shape.Large, ShapeTokens.CornerLarge);
        Add(
            definitions,
            FoundationThemeRawTokens.Shape.LargeIncreased,
            ShapeTokens.CornerLargeIncreased);
        Add(definitions, FoundationThemeRawTokens.Shape.ExtraLarge, ShapeTokens.CornerExtraLarge);
        Add(
            definitions,
            FoundationThemeRawTokens.Shape.ExtraLargeIncreased,
            ShapeTokens.CornerExtraLargeIncreased);
        Add(
            definitions,
            FoundationThemeRawTokens.Shape.ExtraExtraLarge,
            ShapeTokens.CornerExtraExtraLarge);

        Add(
            definitions,
            FoundationThemeRawTokens.Motion.DefaultSpatial,
            MotionTokens.Expressive.DefaultSpatial);
        Add(
            definitions,
            FoundationThemeRawTokens.Motion.FastSpatial,
            MotionTokens.Expressive.FastSpatial);
        Add(
            definitions,
            FoundationThemeRawTokens.Motion.SlowSpatial,
            MotionTokens.Expressive.SlowSpatial);
        Add(
            definitions,
            FoundationThemeRawTokens.Motion.DefaultEffects,
            MotionTokens.Expressive.DefaultEffects);
        Add(
            definitions,
            FoundationThemeRawTokens.Motion.FastEffects,
            MotionTokens.Expressive.FastEffects);
        Add(
            definitions,
            FoundationThemeRawTokens.Motion.SlowEffects,
            MotionTokens.Expressive.SlowEffects);

        Add(
            definitions,
            FoundationThemeRawTokens.Typography.BrandFontFamily,
            TypefaceTokens.Brand);
        Add(
            definitions,
            FoundationThemeRawTokens.Typography.PlainFontFamily,
            TypefaceTokens.Plain);
        Add(
            definitions,
            FoundationThemeRawTokens.Typography.EmphasizedBrandFontFamily,
            TypefaceTokens.EmphasizedBrand);
        Add(
            definitions,
            FoundationThemeRawTokens.Typography.EmphasizedPlainFontFamily,
            TypefaceTokens.EmphasizedPlain);
        Add(
            definitions,
            FoundationThemeRawTokens.Typography.WeightRegular,
            TypefaceTokens.WeightRegular);
        Add(
            definitions,
            FoundationThemeRawTokens.Typography.WeightMedium,
            TypefaceTokens.WeightMedium);
        Add(
            definitions,
            FoundationThemeRawTokens.Typography.WeightBold,
            TypefaceTokens.WeightBold);

        foreach (var role in Enum.GetValues<TypographyRole>())
        {
            var tokens = FoundationThemeRawTokens.TypeScale.For(role);
            var values = global::Material3Expressive.Foundation.TypeScale.For(role);
            Add(definitions, tokens.FontSize, values.FontSize);
            Add(definitions, tokens.LineHeight, values.LineHeight);
            Add(definitions, tokens.LetterSpacing, values.LetterSpacing);
            Add(definitions, tokens.Font, values.Font);
            Add(definitions, tokens.Weight, values.Weight);
        }

        _definitions = definitions;
        _declaredTokens = Array.AsReadOnly(definitions.Keys.ToArray());
    }

    internal static FoundationThemeDefaultRawTokens Instance { get; } = new();

    public string Id => "Material3Expressive.FoundationThemeDefaultRawTokens";

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
        T value)
    {
        definitions.Add(token, Token.Literal(value));
    }
}
