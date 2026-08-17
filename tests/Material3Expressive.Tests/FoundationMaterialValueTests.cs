using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialDesignTokens.Avalonia;
using Xunit;
using MaterialTokenOverrides =
    Material3Expressive.Foundation.Tokens.Runtime.MaterialTokenOverrides;

namespace Material3Expressive.Tests;

/// <summary>
/// Contracts for the additive foundation Raw-to-MaterialValue path. These tests intentionally
/// resolve through real resource placement so a scheme snapshot or controller cannot satisfy them.
/// </summary>
public sealed class FoundationMaterialValueTests
{
    [Fact]
    public void SchemeMetadataDefaultsStaySparseAndMotionFactoriesPreserveTheirDensity()
    {
        var shape = new ShapeScheme();
        Assert.Equal(ShapeTokens.CornerExtraSmall, shape.ExtraSmall);
        Assert.Equal(ShapeTokens.CornerLarge, shape.Large);
        Assert.All(
            new AvaloniaProperty[]
            {
                ShapeScheme.ExtraSmallProperty,
                ShapeScheme.SmallProperty,
                ShapeScheme.MediumProperty,
                ShapeScheme.LargeProperty,
                ShapeScheme.LargeIncreasedProperty,
                ShapeScheme.ExtraLargeProperty,
                ShapeScheme.ExtraLargeIncreasedProperty,
                ShapeScheme.ExtraExtraLargeProperty,
            },
            property => Assert.False(shape.IsSet(property)));

        var typography = new TypographyScheme();
        Assert.Equal(TypefaceTokens.Brand, typography.BrandFontFamily);
        Assert.Equal(TypefaceTokens.WeightBold, typography.WeightBold);
        Assert.All(
            new AvaloniaProperty[]
            {
                TypographyScheme.BrandFontFamilyProperty,
                TypographyScheme.PlainFontFamilyProperty,
                TypographyScheme.EmphasizedBrandFontFamilyProperty,
                TypographyScheme.EmphasizedPlainFontFamilyProperty,
                TypographyScheme.WeightRegularProperty,
                TypographyScheme.WeightMediumProperty,
                TypographyScheme.WeightBoldProperty,
            },
            property => Assert.False(typography.IsSet(property)));

        var motion = new MotionScheme();
        Assert.Equal(MotionTokens.Expressive.DefaultSpatial, motion.DefaultSpatial);
        Assert.All(MotionProperties, property => Assert.False(motion.IsSet(property)));

        var expressive = MotionScheme.CreateExpressive();
        Assert.Equal(MotionTokens.Expressive.DefaultSpatial, expressive.DefaultSpatial);
        Assert.All(MotionProperties, property => Assert.True(expressive.IsSet(property)));

        var standard = MotionScheme.CreateStandard();
        Assert.Equal(MotionTokens.Standard.DefaultSpatial, standard.DefaultSpatial);
        Assert.All(MotionProperties, property => Assert.True(standard.IsSet(property)));
    }

    [AvaloniaFact]
    public void UntouchedLocalSchemesRemainSparseAndInheritAncestorRawOverrides()
    {
        var role = TypographyRole.DisplayLarge;
        var ancestorShape = RoundedCornerShape.Uniform(37);
        var ancestorMotion = new SpringSpec(0.72, 913);
        var ancestorFont = new FontFamily("Ancestor Brand");
        var ancestorRaw = new MaterialTokenOverrides();
        ancestorRaw.Set(FoundationThemeRawTokens.Shape.Large, ancestorShape);
        ancestorRaw.Set(
            FoundationThemeRawTokens.Motion.For(MotionRole.FastEffects),
            ancestorMotion);
        ancestorRaw.Set(
            FoundationThemeRawTokens.Typography.For(FontFamilyToken.Brand),
            ancestorFont);
        ancestorRaw.Set(FoundationThemeRawTokens.TypeScale.For(role).FontSize, 61d);

        var ancestorTheme = new MaterialTheme { Children = { ancestorRaw } };
        var localTheme = new MaterialTheme();
        var localScope = new Border();
        localScope.Resources.MergedDictionaries.Add(localTheme);
        var root = new Border { Child = localScope };
        root.Resources.MergedDictionaries.Add(ancestorTheme);
        ThemeTestHelper.ShowWindow(root);

        Assert.False(localTheme.ShapeScheme.IsSet(ShapeScheme.LargeProperty));
        Assert.False(localTheme.MotionScheme.IsSet(MotionScheme.FastEffectsProperty));
        Assert.False(
            localTheme.TypographyScheme.IsSet(TypographyScheme.BrandFontFamilyProperty));

        Assert.Equal(
            ancestorShape,
            Resolve(localScope, ShapeValueKeys.For(ShapeRole.CornerLarge)));
        Assert.Equal(
            ancestorMotion,
            Resolve(localScope, MotionValueKeys.For(MotionRole.FastEffects)));
        Assert.Equal(
            ancestorFont,
            Resolve(localScope, TypographyValueKeys.For(role).FontFamily));
        Assert.Equal(61d, Resolve(localScope, TypographyValueKeys.For(role).FontSize));
    }

    [AvaloniaFact]
    public void ChildRawSourcesWinOverTheCompatibilityAuthoringBridge()
    {
        var role = TypographyRole.DisplayLarge;
        var bridgeShape = RoundedCornerShape.Uniform(17);
        var childShape = RoundedCornerShape.Uniform(29);
        var bridgeMotion = new SpringSpec(0.81, 511);
        var childMotion = new SpringSpec(1, 2777);
        var bridgeFont = new FontFamily("Bridge Brand");
        var childFont = new FontFamily("Child Brand");
        var theme = new MaterialTheme();
        theme.ShapeScheme.Large = bridgeShape;
        theme.MotionScheme.FastEffects = bridgeMotion;
        theme.TypographyScheme.BrandFontFamily = bridgeFont;
        theme.TypographyScheme.TypeScaleOverrides.Add(
            new TypeScaleOverrides { Variant = role, FontSize = 21 });

        var child = new MaterialTokenOverrides();
        child.Set(FoundationThemeRawTokens.Shape.Large, childShape);
        child.Set(FoundationThemeRawTokens.Motion.For(MotionRole.FastEffects), childMotion);
        child.Set(
            FoundationThemeRawTokens.Typography.For(FontFamilyToken.Brand),
            childFont);
        child.Set(FoundationThemeRawTokens.TypeScale.For(role).FontSize, 33d);
        theme.Children.Add(child);

        var host = Mount(new Border(), theme);

        Assert.Equal(childShape, Resolve(host, ShapeValueKeys.For(ShapeRole.CornerLarge)));
        Assert.Equal(childMotion, Resolve(host, MotionValueKeys.For(MotionRole.FastEffects)));
        Assert.Equal(childFont, Resolve(host, TypographyValueKeys.For(role).FontFamily));
        Assert.Equal(33d, Resolve(host, TypographyValueKeys.For(role).FontSize));
    }

    [AvaloniaFact]
    public void DefaultEqualLocalShapeCanClaimAndReleaseAnAncestorForFreshResolution()
    {
        var ancestorShape = RoundedCornerShape.Uniform(43);
        var ancestorRaw = new MaterialTokenOverrides();
        ancestorRaw.Set(FoundationThemeRawTokens.Shape.Large, ancestorShape);
        var ancestorTheme = new MaterialTheme { Children = { ancestorRaw } };
        var localTheme = new MaterialTheme();
        var localScope = new Border();
        localScope.Resources.MergedDictionaries.Add(localTheme);
        var root = new Border { Child = localScope };
        root.Resources.MergedDictionaries.Add(ancestorTheme);
        ThemeTestHelper.ShowWindow(root);
        var key = ShapeValueKeys.For(ShapeRole.CornerLarge);

        Assert.Equal(ancestorShape, Resolve(localScope, key));

        // A default-equal SetValue changes IsSet/source presence even though Avalonia does not
        // publish an effective-value change. Fresh resolution must still observe that Raw hit.
        localTheme.ShapeScheme.Large = ShapeTokens.CornerLarge;
        Assert.True(localTheme.ShapeScheme.IsSet(ShapeScheme.LargeProperty));
        Assert.Equal(ShapeTokens.CornerLarge, Resolve(localScope, key));

        localTheme.ShapeScheme.ClearValue(ShapeScheme.LargeProperty);
        Assert.False(localTheme.ShapeScheme.IsSet(ShapeScheme.LargeProperty));
        Assert.Equal(ancestorShape, Resolve(localScope, key));
    }

    [AvaloniaFact]
    public void FoundationLeafDefaultsMatchTheGeneratedAndFoundationTables()
    {
        var host = Mount(new Border(), new MaterialTheme());

        foreach (var role in Enum.GetValues<ShapeRole>())
            Assert.Equal(ExpectedShape(role), Resolve(host, ShapeValueKeys.For(role)));

        foreach (var role in Enum.GetValues<MotionRole>())
            Assert.Equal(ExpectedMotion(role), Resolve(host, MotionValueKeys.For(role)));

        foreach (var role in Enum.GetValues<TypographyRole>())
        {
            var expected = TypeScale.For(role);
            var keys = TypographyValueKeys.For(role);
            Assert.Equal(expected.FontSize, Resolve(host, keys.FontSize));
            Assert.Equal(expected.LineHeight, Resolve(host, keys.LineHeight));
            Assert.Equal(expected.LetterSpacing, Resolve(host, keys.LetterSpacing));
            Assert.Equal(ExpectedFontFamily(expected.Font), Resolve(host, keys.FontFamily));
            Assert.Equal(ExpectedFontWeight(expected.Weight), Resolve(host, keys.FontWeight));
        }
    }

    [AvaloniaFact]
    public void ShapeEndpointsLeaveBoundsAndRtlProjectionToTheConsumer()
    {
        var host = Mount(new Border(), new MaterialTheme());
        var start = Resolve(host, ShapeValueKeys.For(ShapeRole.CornerLargeStart));
        var full = Resolve(host, ShapeValueKeys.For(ShapeRole.CornerFull));

        Assert.Equal(typeof(RoundedCornerShape), ShapeValueKeys.For(ShapeRole.CornerLargeStart).ValueType);
        Assert.NotEqual(
            start.ToCornerRadius(new Size(80, 40), isRightToLeft: false),
            start.ToCornerRadius(new Size(80, 40), isRightToLeft: true));
        Assert.NotEqual(
            full.ToCornerRadius(new Size(40, 20)),
            full.ToCornerRadius(new Size(80, 40)));
    }

    [AvaloniaFact]
    public void SchemeAndOverrideMutationsReResolveBoundLeavesAndBroadcastOnce()
    {
        var role = TypographyRole.DisplayLarge;
        var typeOverride = new TypeScaleOverrides { Variant = role, FontSize = 18 };
        var theme = new MaterialTheme();
        theme.TypographyScheme.TypeScaleOverrides.Add(typeOverride);

        var shape = Bind(ShapeValueKeys.For(ShapeRole.CornerLarge));
        var motion = Bind(MotionValueKeys.For(MotionRole.FastEffects));
        var font = Bind(TypographyValueKeys.For(role).FontFamily);
        var fontSize = Bind(TypographyValueKeys.For(role).FontSize);
        Mount(new StackPanel { Children = { shape, motion, font, fontSize } }, theme);

        var notifications = 0;
        theme.Changed += (_, _) => notifications++;

        var newShape = RoundedCornerShape.Uniform(35);
        theme.ShapeScheme.Large = newShape;
        Assert.Equal(1, notifications);
        Assert.Equal(newShape, Assert.IsType<RoundedCornerShape>(shape.Content));

        notifications = 0;
        var newMotion = new SpringSpec(0.66, 1234);
        theme.MotionScheme.FastEffects = newMotion;
        Assert.Equal(1, notifications);
        Assert.Equal(newMotion, Assert.IsType<SpringSpec>(motion.Content));

        notifications = 0;
        var newFont = new FontFamily("Mutation Brand");
        theme.TypographyScheme.BrandFontFamily = newFont;
        Assert.Equal(1, notifications);
        Assert.Equal(newFont, Assert.IsType<FontFamily>(font.Content));

        notifications = 0;
        typeOverride.FontSize = 19;
        Assert.Equal(1, notifications);
        Assert.Equal(19d, Assert.IsType<double>(fontSize.Content));
    }

    [AvaloniaFact]
    public void TypeScaleOverridesAreFieldSparseAndLaterItemsWin()
    {
        var role = TypographyRole.BodyMedium;
        var plain = new FontFamily("Earlier Plain");
        var theme = new MaterialTheme();
        theme.TypographyScheme.PlainFontFamily = plain;
        theme.TypographyScheme.WeightBold = FontWeight.Black;
        theme.TypographyScheme.TypeScaleOverrides.Add(
            new TypeScaleOverrides
            {
                Variant = role,
                FontSize = 17,
                LineHeight = 27,
                Font = FontFamilyToken.Plain,
                Weight = FontWeightToken.Bold,
            });
        theme.TypographyScheme.TypeScaleOverrides.Add(
            new TypeScaleOverrides
            {
                Variant = role,
                FontSize = 23,
                LetterSpacing = 0.75,
            });

        var host = Mount(new Border(), theme);
        var keys = TypographyValueKeys.For(role);

        Assert.Equal(23d, Resolve(host, keys.FontSize));
        Assert.Equal(27d, Resolve(host, keys.LineHeight));
        Assert.Equal(0.75d, Resolve(host, keys.LetterSpacing));
        Assert.Equal(plain, Resolve(host, keys.FontFamily));
        Assert.Equal(FontWeight.Black, Resolve(host, keys.FontWeight));
    }

    [Fact]
    public void FoundationPlansDeclareOnlyFoundationRawDependencies()
    {
        var foundationRaw = EnumerateFoundationRawTokens().ToArray();
        var keys = ShapeValueKeys.All.Cast<MaterialValueKey>()
            .Concat(MotionValueKeys.All)
            .Concat(TypographyValueKeys.All)
            .ToArray();

        Assert.NotEmpty(keys);
        foreach (var key in keys)
        {
            Assert.All(
                key.Plan.Dependencies,
                dependency => Assert.Contains(
                    foundationRaw,
                    candidate => ReferenceEquals(candidate, dependency)));
        }

        var roleToken = new MaterialToken<ShapeRole>(
            "Tests.Foundation.ShapeRole",
            TokenLayer.Component);
        var dynamicShape = ShapeValueKeys.FromRawRole(
            "Tests.Foundation.DynamicShape",
            roleToken);
        Assert.Equal(
            1 + FoundationThemeRawTokens.Shape.All.Count,
            dynamicShape.Plan.Dependencies.Count);
        Assert.Same(roleToken, dynamicShape.Plan.Dependencies[0]);
        Assert.All(
            FoundationThemeRawTokens.Shape.All,
            raw => Assert.Contains(
                dynamicShape.Plan.Dependencies,
                dependency => ReferenceEquals(dependency, raw)));
    }

    [Fact]
    public void DefaultCatalogDefinesEveryFoundationRawToken()
    {
        Assert.Equal(8, FoundationThemeRawTokens.Shape.All.Count);
        Assert.Equal(
            Enum.GetValues<MotionRole>().Length,
            FoundationThemeRawTokens.Motion.All.Count);
        Assert.Equal(
            Enum.GetValues<TypographyRole>().Length * 5,
            FoundationThemeRawTokens.TypeScale.All.Count);
        Assert.Equal(
            Enum.GetValues<FontFamilyToken>().Length + Enum.GetValues<FontWeightToken>().Length,
            FoundationThemeRawTokens.Typography.All.Count);

        var expected = EnumerateFoundationRawTokens().ToArray();
        var distinct = expected.Distinct(ReferenceEqualityComparer.Instance).ToArray();

        Assert.Equal(expected.Length, distinct.Length);
        Assert.Equal(expected.Length, FoundationThemeDefaultRawTokens.Instance.DeclaredTokens.Count);

        foreach (var token in expected)
        {
            Assert.Contains(
                FoundationThemeDefaultRawTokens.Instance.DeclaredTokens,
                candidate => ReferenceEquals(candidate, token));
            var lookup = DefaultRawTokens.Catalog.Lookup(token, ThemeVariant.Default);
            Assert.Equal(RawTokenCatalogLookupKind.Definition, lookup.Kind);
            Assert.NotNull(lookup.Definition);
            Assert.Equal(token.ValueType, lookup.Definition!.ValueType);
        }
    }

    private static ContentControl Bind(MaterialValueKey key)
    {
        var consumer = new ContentControl();
        consumer.Bind(ContentControl.ContentProperty, new DynamicResourceExtension(key));
        return consumer;
    }

    private static Border Mount(Control content, MaterialTheme theme)
    {
        var scope = new Border { Child = content };
        MaterialThemeScope.SetTheme(scope, theme);
        ThemeTestHelper.ShowWindow(scope);
        return scope;
    }

    private static T Resolve<T>(IResourceHost host, MaterialValueKey<T> key) =>
        SharedTokenTestHelper.Resolve(host, key);

    private static IReadOnlyList<AvaloniaProperty> MotionProperties { get; } =
    [
        MotionScheme.DefaultSpatialProperty,
        MotionScheme.FastSpatialProperty,
        MotionScheme.SlowSpatialProperty,
        MotionScheme.DefaultEffectsProperty,
        MotionScheme.FastEffectsProperty,
        MotionScheme.SlowEffectsProperty,
    ];

    private static IEnumerable<MaterialToken> EnumerateFoundationRawTokens()
    {
        foreach (var token in FoundationThemeRawTokens.Shape.All)
            yield return token;
        foreach (var token in FoundationThemeRawTokens.Motion.All)
            yield return token;
        foreach (var token in FoundationThemeRawTokens.TypeScale.All)
            yield return token;
        foreach (var token in FoundationThemeRawTokens.Typography.All)
            yield return token;
    }

    private static RoundedCornerShape ExpectedShape(ShapeRole role) => role switch
    {
        ShapeRole.CornerNone => ShapeTokens.CornerNone,
        ShapeRole.CornerExtraSmall => ShapeTokens.CornerExtraSmall,
        ShapeRole.CornerExtraSmallTop => ShapeTokens.CornerExtraSmall.Top(),
        ShapeRole.CornerSmall => ShapeTokens.CornerSmall,
        ShapeRole.CornerMedium => ShapeTokens.CornerMedium,
        ShapeRole.CornerLarge => ShapeTokens.CornerLarge,
        ShapeRole.CornerLargeStart => ShapeTokens.CornerLarge.Start(),
        ShapeRole.CornerLargeEnd => ShapeTokens.CornerLarge.End(),
        ShapeRole.CornerLargeTop => ShapeTokens.CornerLarge.Top(),
        ShapeRole.CornerLargeIncreased => ShapeTokens.CornerLargeIncreased,
        ShapeRole.CornerExtraLarge => ShapeTokens.CornerExtraLarge,
        ShapeRole.CornerExtraLargeIncreased => ShapeTokens.CornerExtraLargeIncreased,
        ShapeRole.CornerExtraLargeTop => ShapeTokens.CornerExtraLarge.Top(),
        ShapeRole.CornerExtraExtraLarge => ShapeTokens.CornerExtraExtraLarge,
        ShapeRole.CornerFull => ShapeTokens.CornerFull,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    private static SpringSpec ExpectedMotion(MotionRole role) => role switch
    {
        MotionRole.DefaultSpatial => MotionTokens.Expressive.DefaultSpatial,
        MotionRole.FastSpatial => MotionTokens.Expressive.FastSpatial,
        MotionRole.SlowSpatial => MotionTokens.Expressive.SlowSpatial,
        MotionRole.DefaultEffects => MotionTokens.Expressive.DefaultEffects,
        MotionRole.FastEffects => MotionTokens.Expressive.FastEffects,
        MotionRole.SlowEffects => MotionTokens.Expressive.SlowEffects,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    private static FontFamily ExpectedFontFamily(FontFamilyToken token) => token switch
    {
        FontFamilyToken.Brand => TypefaceTokens.Brand,
        FontFamilyToken.Plain => TypefaceTokens.Plain,
        FontFamilyToken.EmphasizedBrand => TypefaceTokens.EmphasizedBrand,
        FontFamilyToken.EmphasizedPlain => TypefaceTokens.EmphasizedPlain,
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, null),
    };

    private static FontWeight ExpectedFontWeight(FontWeightToken token) => token switch
    {
        FontWeightToken.Regular => TypefaceTokens.WeightRegular,
        FontWeightToken.Medium => TypefaceTokens.WeightMedium,
        FontWeightToken.Bold => TypefaceTokens.WeightBold,
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, null),
    };
}
