using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;
using McuReferencePaletteTokens = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using SharedMaterialToken = MaterialDesignTokens.Avalonia.MaterialToken;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the MaterialAppBar MaterialValue profiles.</summary>
public sealed class AppBarMaterialValueTests
{
    [AvaloniaFact]
    public void ValueKeysExposeTypedProfilesAndExactRawDependencies()
    {
        Assert.Equal(typeof(AppBarGeometryProfile), AppBarValueKeys.SmallGeometry.ValueType);
        Assert.Equal(typeof(AppBarGeometryProfile), AppBarValueKeys.MediumGeometry.ValueType);
        Assert.Equal(typeof(AppBarGeometryProfile), AppBarValueKeys.LargeGeometry.ValueType);
        Assert.Equal(typeof(AppBarTypographyProfile), AppBarValueKeys.SmallTypography.ValueType);
        Assert.Equal(typeof(AppBarTypographyProfile), AppBarValueKeys.MediumTypography.ValueType);
        Assert.Equal(typeof(AppBarTypographyProfile), AppBarValueKeys.LargeTypography.ValueType);
        Assert.Equal(typeof(AppBarVisualColors), AppBarValueKeys.VisualColors.ValueType);

        AssertDependencies(
            AppBarValueKeys.SmallGeometry,
            AppBarSmallTokensKeys.ContainerHeight,
            AppBarSmallTokensKeys.ContainerHeight,
            AppBarSmallTokensKeys.ContainerHeight);
        AssertDependencies(
            AppBarValueKeys.MediumGeometry,
            AppBarMediumTokensKeys.ContainerHeight,
            AppBarMediumFlexibleTokensKeys.ContainerHeight,
            AppBarMediumFlexibleTokensKeys.LargeContainerHeight,
            AppBarSmallTokensKeys.ContainerHeight);
        AssertDependencies(
            AppBarValueKeys.LargeGeometry,
            AppBarLargeTokensKeys.ContainerHeight,
            AppBarLargeFlexibleTokensKeys.ContainerHeight,
            AppBarLargeFlexibleTokensKeys.LargeContainerHeight,
            AppBarSmallTokensKeys.ContainerHeight);
        AssertDependencies(
            AppBarValueKeys.VisualColors,
            AppBarTokensKeys.ContainerColor,
            AppBarTokensKeys.OnScrollContainerColor,
            AppBarTokensKeys.TitleColor,
            AppBarTokensKeys.SubtitleColor,
            AppBarTokensKeys.TrailingIconColor);
        AssertDependencies(
            AppBarValueKeys.SmallTypography,
            AppBarSmallTokensKeys.TitleFont,
            AppBarSmallTokensKeys.SubtitleFont,
            AppBarSmallTokensKeys.TitleFont,
            AppBarSmallTokensKeys.SubtitleFont);
        AssertDependencies(
            AppBarValueKeys.MediumTypography,
            AppBarMediumTokensKeys.TitleFont,
            AppBarSmallTokensKeys.SubtitleFont,
            AppBarMediumFlexibleTokensKeys.TitleFont,
            AppBarMediumFlexibleTokensKeys.SubtitleFont);
        AssertDependencies(
            AppBarValueKeys.LargeTypography,
            AppBarLargeTokensKeys.TitleFont,
            AppBarSmallTokensKeys.SubtitleFont,
            AppBarLargeFlexibleTokensKeys.TitleFont,
            AppBarLargeFlexibleTokensKeys.SubtitleFont);
    }

    [AvaloniaFact]
    public void SizeSelectorsResolveCanonicalGeometryAndTypographyProfiles()
    {
        var small = new MaterialAppBar { Size = AppBarSize.Small, Title = "Small" };
        var medium = new MaterialAppBar { Size = AppBarSize.Medium, Title = "Medium" };
        var large = new MaterialAppBar { Size = AppBarSize.Large, Title = "Large" };
        var scope = new Border
        {
            Child = new StackPanel { Children = { small, medium, large } },
        };
        MaterialThemeScope.SetTheme(scope, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(new AppBarGeometryProfile(64, 64, 64, 64, 16), small.GeometryProfile);
            Assert.Equal(new AppBarGeometryProfile(112, 112, 136, 64, 16), medium.GeometryProfile);
            Assert.Equal(new AppBarGeometryProfile(152, 120, 152, 64, 16), large.GeometryProfile);

            Assert.Equal(
                new AppBarTypographyProfile(
                    TypographyRole.TitleLarge,
                    TypographyRole.LabelMedium,
                    TypographyRole.TitleLarge,
                    TypographyRole.LabelMedium),
                small.TypographyProfile);
            Assert.Equal(
                new AppBarTypographyProfile(
                    TypographyRole.HeadlineSmall,
                    TypographyRole.LabelMedium,
                    TypographyRole.HeadlineMedium,
                    TypographyRole.LabelLarge),
                medium.TypographyProfile);
            Assert.Equal(
                new AppBarTypographyProfile(
                    TypographyRole.HeadlineMedium,
                    TypographyRole.LabelMedium,
                    TypographyRole.DisplaySmall,
                    TypographyRole.TitleMedium),
                large.TypographyProfile);

            Assert.Equal(64, small.CurrentHeight);
            Assert.Equal(112, medium.CurrentHeight);
            Assert.Equal(152, large.CurrentHeight);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(AppBarSize.Medium, false, 112, 48)]
    [InlineData(AppBarSize.Medium, true, 136, 72)]
    [InlineData(AppBarSize.Large, false, 120, 56)]
    [InlineData(AppBarSize.Large, true, 152, 88)]
    public void FlexibleVariantUsesProfileHeightsAndCollapsesToTheSmallTarget(
        AppBarSize size,
        bool hasSubtitle,
        double expandedHeight,
        double collapseRange)
    {
        var appBar = new MaterialAppBar
        {
            Size = size,
            Variant = AppBarVariant.Flexible,
            Title = "Inbox",
            Subtitle = hasSubtitle ? "12 messages" : null,
        };
        var scope = Place(appBar, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(expandedHeight, appBar.CurrentHeight);
            appBar.State.Collapse();
            Assert.Equal(64, appBar.CurrentHeight);
            Assert.Equal(collapseRange, expandedHeight - appBar.CurrentHeight);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void VisualColorsResolveLightDarkAndDefaultThemeVariants()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var appBar = new MaterialAppBar { Title = "Inbox" };
        var scope = Place(appBar, theme);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            var light = Resolve(scope, AppBarValueKeys.VisualColors, ThemeVariant.Light);
            var dark = Resolve(scope, AppBarValueKeys.VisualColors, ThemeVariant.Dark);
            var defaultVariant = Resolve(scope, AppBarValueKeys.VisualColors, ThemeVariant.Default);

            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Surface),
                ColorOf(light.ContainerColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.SurfaceContainer),
                ColorOf(light.ScrolledContainerColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface),
                ColorOf(light.TitleColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurfaceVariant),
                ColorOf(light.SubtitleColor));
            Assert.Equal(ColorOf(light.SubtitleColor), ColorOf(light.IconColor));

            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.Surface, isDark: true),
                ColorOf(dark.ContainerColor));
            Assert.Equal(
                ThemeTestHelper.Expected(theme, SysColorToken.OnSurface, isDark: true),
                ColorOf(dark.TitleColor));
            Assert.Equal(
                ColorOf(light.ContainerColor),
                ColorOf(defaultVariant.ContainerColor));

            Assert.Equal(ColorOf(light.TitleColor), ColorOf(appBar.TitleBrush));
            Assert.Equal(ColorOf(light.SubtitleColor), ColorOf(appBar.SubtitleBrush));
            Assert.Equal(ColorOf(light.IconColor), ColorOf(appBar.IconBrush));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRebindVisualColors()
    {
        var appBar = new MaterialAppBar { Title = "Inbox" };
        var scope = Place(appBar, new MaterialTheme());
        var reference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var references = new MaterialTokenOverrides();
        references.Set(reference, Colors.Orange);
        var system = new MaterialTokenOverrides();
        system.Set(McuSystemColorTokens.Surface, SharedToken.Alias(reference));
        var component = new AppBarTokensOverrides
        {
            ContainerColor = SharedToken.Alias(McuSystemColorTokens.Surface),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(Colors.Orange, ColorOf(appBar.VisualColors.ContainerColor));

            system.Set(McuSystemColorTokens.Surface, SharedToken.Literal(Colors.Purple));
            Assert.Equal(Colors.Purple, ColorOf(appBar.VisualColors.ContainerColor));

            component.ContainerColor = SharedToken.Literal(Colors.Green);
            Assert.Equal(Colors.Green, ColorOf(appBar.VisualColors.ContainerColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FlatProfilesOverrideOnlyTheSelectedPresentationEndpoint()
    {
        var appBar = new MaterialAppBar { Size = AppBarSize.Small, Title = "Inbox" };
        var scope = Place(appBar, new MaterialTheme());
        var geometry = new AppBarGeometryProfile(70, 70, 70, 50, 24);
        var typography = new AppBarTypographyProfile(
            TypographyRole.HeadlineLarge,
            TypographyRole.LabelLarge,
            TypographyRole.DisplayLarge,
            TypographyRole.TitleLarge);
        var colors = new AppBarVisualColors(
            Brush(Colors.HotPink),
            Brush(Colors.Purple),
            Brush(Colors.Green),
            Brush(Colors.Blue),
            Brush(Colors.Orange));
        scope.Resources[AppBarValueKeys.SmallGeometry] = geometry;
        scope.Resources[AppBarValueKeys.SmallTypography] = typography;
        scope.Resources[AppBarValueKeys.VisualColors] = colors;
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Same(geometry, appBar.GeometryProfile);
            Assert.Same(typography, appBar.TypographyProfile);
            Assert.Same(colors, appBar.VisualColors);
            Assert.Equal(70, appBar.CurrentHeight);
            Assert.Equal(24, appBar.Padding.Left);
            Assert.Same(colors.TitleColor, appBar.TitleBrush);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsIsolatedFromASiblingAppBar()
    {
        var local = new MaterialAppBar { Title = "Local" };
        var sibling = new MaterialAppBar { Title = "Sibling" };
        var localScope = Place(local, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, sibling } };
        localScope.Resources[AppBarTokensKeys.ContainerColor] = SharedToken.Literal(Colors.HotPink);
        var window = ThemeTestHelper.ShowWindow(root);

        try
        {
            Assert.Equal(Colors.HotPink, ColorOf(local.VisualColors.ContainerColor));
            Assert.NotEqual(Colors.HotPink, ColorOf(sibling.VisualColors.ContainerColor));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ApplicationStylesAndLocalPaddingKeepPrecedenceOverTheThemeBinding()
    {
        var style = new Style(selector => selector.Is<MaterialAppBar>());
        style.Add(new Setter(MaterialAppBar.PaddingProperty, new Thickness(7)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new MaterialAppBar { Title = "Styled" };
            var styledScope = Place(styled, new MaterialTheme());
            var styledWindow = ThemeTestHelper.ShowWindow(styledScope);
            try
            {
                Assert.Equal(new Thickness(7), styled.Padding);
            }
            finally
            {
                styledWindow.Close();
            }

            var local = new MaterialAppBar
            {
                Title = "Local",
                Padding = new Thickness(3),
            };
            var localScope = Place(local, new MaterialTheme());
            var localWindow = ThemeTestHelper.ShowWindow(localScope);
            try
            {
                Assert.Equal(new Thickness(3), local.Padding);
            }
            finally
            {
                localWindow.Close();
            }
        }
        finally
        {
            Application.Current.Styles.Remove(style);
        }
    }

    [AvaloniaFact]
    public void InvalidRawHitDoesNotFallBackToAnAncestorFlatProfile()
    {
        var appBar = new MaterialAppBar { Size = AppBarSize.Small };
        var localScope = Place(appBar, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[AppBarValueKeys.SmallGeometry] = new AppBarGeometryProfile(80, 80, 80, 60, 20);
        localScope.Resources[AppBarSmallTokensKeys.ContainerHeight] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            ThemeTestHelper.ShowWindow(root));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(AppBarSmallTokensKeys.ContainerHeight, exception.Token);
        Assert.Same(AppBarValueKeys.SmallGeometry, exception.ValueKey);
    }

    [AvaloniaFact]
    public void ScrollAndCollapseAreLatePoliciesOverStableResolvedProfiles()
    {
        var appBar = new MaterialAppBar
        {
            Size = AppBarSize.Medium,
            ScrollBehavior = AppBarScrollBehavior.EnterAlways,
            Title = "Inbox",
        };
        var scope = Place(appBar, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            var profile = appBar.VisualColors;
            var container = profile.ContainerColor;
            var scrolled = profile.ScrolledContainerColor;

            Assert.Equal(24, appBar.ApplyScrollDelta(24));
            Assert.Same(profile, appBar.VisualColors);
            Assert.Same(container, appBar.VisualColors.ContainerColor);
            Assert.Same(scrolled, appBar.VisualColors.ScrolledContainerColor);
            Assert.Equal(88, appBar.CurrentHeight);

            appBar.IsScrolled = true;
            Assert.Same(profile, appBar.VisualColors);
            Assert.Same(container, appBar.VisualColors.ContainerColor);
            Assert.Same(scrolled, appBar.VisualColors.ScrolledContainerColor);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LegacySnapshotPublicationDoesNotReplaceMaterialValueAuthority()
    {
        var appBar = new MaterialAppBar { Title = "Inbox" };
        var scope = Place(appBar, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        var overrides = new AppBarTokensOverrides
        {
            ContainerColor = SharedToken.Literal(Colors.Orange),
        };
        scope.Resources.MergedDictionaries.Add(overrides);
        var window = ThemeTestHelper.ShowWindow(scope);

        try
        {
            Assert.Equal(Colors.Orange, ColorOf(appBar.VisualColors.ContainerColor));
            MaterialThemeScope.SetResolvedTheme(
                scope,
                new ResolvedTheme(ThemeTestHelper.ThemeSeededWith(Colors.Purple)));
            Assert.Equal(Colors.Orange, ColorOf(appBar.VisualColors.ContainerColor));
        }
        finally
        {
            window.Close();
        }
    }

    private static void AssertDependencies(
        MaterialValueKey key,
        params SharedMaterialToken[] expected)
    {
        Assert.Equal(expected.Length, key.Plan.Dependencies.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Same(expected[index], key.Plan.Dependencies[index]);
    }

    private static T Resolve<T>(
        IResourceHost host,
        MaterialValueKey<T> key,
        ThemeVariant? theme = null) =>
        SharedTokenTestHelper.Resolve(host, key, theme);

    private static Border Place(MaterialAppBar appBar, MaterialTheme theme)
    {
        var scope = new Border { Child = appBar };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);

    private static Color ColorOf(IImmutableBrush brush) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
}
