using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialDesignTokens.Avalonia;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the ButtonGroup MaterialValue geometry profiles.</summary>
public sealed class ButtonGroupMaterialValueTests
{
    [AvaloniaFact]
    public void ProfilesHaveConsumerTypesAndOnlyDeclareConsumedRawDependencies()
    {
        Assert.Equal(typeof(ButtonGroupGeometryProfile), ButtonGroupValueKeys.StandardProfile.ValueType);
        Assert.Equal(typeof(ButtonGroupGeometryProfile), ButtonGroupValueKeys.ConnectedProfile.ValueType);

        Assert.Equal(
            [ButtonGroupSmallTokensKeys.BetweenSpace, ButtonGroupSmallTokensKeys.ContainerHeight],
            ButtonGroupValueKeys.StandardProfile.Plan.Dependencies);
        Assert.Equal(
            [
                ConnectedButtonGroupSmallTokensKeys.BetweenSpace,
                ConnectedButtonGroupSmallTokensKeys.ContainerHeight,
                ConnectedButtonGroupSmallTokensKeys.InnerCornerSize,
            ],
            ButtonGroupValueKeys.ConnectedProfile.Plan.Dependencies);

        Assert.DoesNotContain(
            ButtonGroupValueKeys.ConnectedProfile.Plan.Dependencies,
            token => token == ConnectedButtonGroupSmallTokensKeys.PressedInnerCornerSize);
        Assert.DoesNotContain(
            ButtonGroupValueKeys.ConnectedProfile.Plan.Dependencies,
            token => token == ConnectedButtonGroupSmallTokensKeys.SelectedInnerCornerCornerSizePercent);
    }

    [AvaloniaFact]
    public void StandardAndConnectedProfilesResolveTheirBaselinePolicy()
    {
        var standard = new MaterialButtonGroup { Variant = ButtonGroupVariant.Standard };
        var connected = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        var standardScope = Place(standard, new MaterialTheme());
        var connectedScope = Place(connected, new MaterialTheme());

        ThemeTestHelper.ShowWindow(
            new StackPanel { Children = { standardScope, connectedScope } });

        Assert.Equal(
            new ButtonGroupGeometryProfile(12, 40, 0, false),
            Resolve(standard, ButtonGroupValueKeys.StandardProfile));
        Assert.Equal(
            new ButtonGroupGeometryProfile(2, 40, 8, true),
            Resolve(connected, ButtonGroupValueKeys.ConnectedProfile));
        Assert.Equal(12, standard.GeometryProfile.BetweenSpace);
        Assert.False(standard.GeometryProfile.IsConnected);
        Assert.Equal(2, connected.GeometryProfile.BetweenSpace);
        Assert.True(connected.GeometryProfile.IsConnected);
    }

    [AvaloniaFact]
    public void VariantStyleSelectsAProfileWithoutMakingThePlanRequesterRelative()
    {
        var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Standard };
        var scope = Place(group, new MaterialTheme());
        ThemeTestHelper.ShowWindow(scope);

        Assert.False(group.GeometryProfile.IsConnected);
        group.Variant = ButtonGroupVariant.Connected;
        PumpLayout();

        Assert.True(group.GeometryProfile.IsConnected);
        Assert.Equal(2, group.GeometryProfile.BetweenSpace);
        Assert.Equal(
            Resolve(group, ButtonGroupValueKeys.ConnectedProfile),
            group.GeometryProfile);
    }

    [AvaloniaFact]
    public void RawOverridesAndInPlaceMutationRefreshTheSelectedProfile()
    {
        var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        var theme = new MaterialTheme();
        var scope = Place(group, theme);
        var overrides = new ConnectedButtonGroupSmallTokensOverrides
        {
            BetweenSpace = Token.Literal(6d),
            ContainerHeight = Token.Literal(52d),
            InnerCornerSize = Token.Literal(11d),
        };
        theme.Children.Add(overrides);
        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(new ButtonGroupGeometryProfile(6, 52, 11, true), group.GeometryProfile);

        overrides.BetweenSpace = Token.Literal(3d);
        overrides.ContainerHeight = Token.Literal(60d);
        overrides.InnerCornerSize = Token.Literal(13d);

        Assert.Equal(new ButtonGroupGeometryProfile(3, 60, 13, true), group.GeometryProfile);
    }

    [AvaloniaFact]
    public void FlatProfileOverrideWinsOverTheEndpointAndIsLive()
    {
        var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        var scope = Place(group, new MaterialTheme());
        var first = new ButtonGroupGeometryProfile(9, 64, 17, true);
        scope.Resources[ButtonGroupValueKeys.ConnectedProfile] = first;
        ThemeTestHelper.ShowWindow(scope);

        Assert.Same(first, group.GeometryProfile);

        var second = new ButtonGroupGeometryProfile(4, 44, 5, true);
        scope.Resources[ButtonGroupValueKeys.ConnectedProfile] = second;
        Assert.Same(second, group.GeometryProfile);
    }

    [AvaloniaFact]
    public void SeparateThemePlacementsDoNotShareRawOverrides()
    {
        var first = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        var second = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        var firstTheme = new MaterialTheme();
        var secondTheme = new MaterialTheme();
        var firstScope = Place(first, firstTheme);
        var secondScope = Place(second, secondTheme);
        firstTheme.Children.Add(new ConnectedButtonGroupSmallTokensOverrides
        {
            BetweenSpace = Token.Literal(7d),
        });
        secondTheme.Children.Add(new ConnectedButtonGroupSmallTokensOverrides
        {
            BetweenSpace = Token.Literal(3d),
        });

        ThemeTestHelper.ShowWindow(
            new StackPanel { Children = { firstScope, secondScope } });

        Assert.Equal(7, first.GeometryProfile.BetweenSpace);
        Assert.Equal(3, second.GeometryProfile.BetweenSpace);
    }

    [AvaloniaFact]
    public void InvalidLocalRawDefinitionDoesNotFallThroughToAncestorFlatProfile()
    {
        var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        var localScope = Place(group, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[ButtonGroupValueKeys.ConnectedProfile] =
            new ButtonGroupGeometryProfile(99, 99, 99, true);
        localScope.Resources[ConnectedButtonGroupSmallTokensKeys.InnerCornerSize] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            Resolve(group, ButtonGroupValueKeys.ConnectedProfile, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(ConnectedButtonGroupSmallTokensKeys.InnerCornerSize, exception.Token);
        Assert.Same(ButtonGroupValueKeys.ConnectedProfile, exception.ValueKey);
    }

    [AvaloniaFact]
    public void OrientationAndVisibleSegmentShapeRemainLateControlPolicy()
    {
        var first = new ToggleButton { Content = "A" };
        var hidden = new ToggleButton { Content = "Hidden", IsVisible = false };
        var last = new ToggleButton { Content = "C" };
        var group = new MaterialButtonGroup
        {
            Variant = ButtonGroupVariant.Connected,
            Orientation = Orientation.Vertical,
            Children = { first, hidden, last },
        };

        ThemeTestHelper.ShowWindow(group);
        PumpLayout();

        var inner = CornerSize.Absolute(8);
        var firstShape = M3Button.GetContainerShapeOverride(first);
        var lastShape = M3Button.GetContainerShapeOverride(last);
        Assert.NotNull(firstShape);
        Assert.NotNull(lastShape);
        Assert.Equal(CornerSize.Full, firstShape!.TopStart);
        Assert.Equal(CornerSize.Full, firstShape.TopEnd);
        Assert.Equal(inner, firstShape.BottomStart);
        Assert.Equal(inner, firstShape.BottomEnd);
        Assert.Equal(inner, lastShape!.TopStart);
        Assert.Equal(inner, lastShape.TopEnd);
        Assert.Equal(CornerSize.Full, lastShape.BottomStart);
        Assert.Equal(CornerSize.Full, lastShape.BottomEnd);
        Assert.Null(M3Button.GetContainerShapeOverride(hidden));

        group.Orientation = Orientation.Horizontal;
        PumpLayout();

        firstShape = M3Button.GetContainerShapeOverride(first);
        lastShape = M3Button.GetContainerShapeOverride(last);
        Assert.Equal(CornerSize.Full, firstShape!.TopStart);
        Assert.Equal(inner, firstShape.TopEnd);
        Assert.Equal(inner, lastShape!.TopStart);
        Assert.Equal(CornerSize.Full, lastShape.TopEnd);

        group.Variant = ButtonGroupVariant.Standard;
        PumpLayout();
        Assert.Null(M3Button.GetContainerShapeOverride(first));
        Assert.Null(M3Button.GetContainerShapeOverride(last));
    }

    [AvaloniaFact]
    public void ThemePlacementCanAttachDetachAndAttachAgainWithoutLegacyControllerState()
    {
        var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Connected };
        var scope = new Border();
        var theme = new MaterialTheme();
        MaterialThemeScope.SetTheme(scope, theme);

        scope.Child = group;
        ThemeTestHelper.ShowWindow(scope);
        Assert.True(group.GeometryProfile.IsConnected);

        scope.Child = null;
        scope.Child = group;
        PumpLayout();

        Assert.True(group.GeometryProfile.IsConnected);
        Assert.Equal(2, group.GeometryProfile.BetweenSpace);
    }

    [AvaloniaFact]
    public void ApplicationAndLocalProfileValuesKeepTheirAvaloniaPrecedence()
    {
        var applicationProfile = new ButtonGroupGeometryProfile(21, 70, 1, false);
        var style = new Style(selector => selector.Is<MaterialButtonGroup>());
        style.Add(new Setter(MaterialButtonGroup.GeometryProfileProperty, applicationProfile));
        Application.Current!.Styles.Add(style);

        try
        {
            // The connected variant has a more-specific ControlTheme selector for its profile.
            // Use the standard variant here so this test exercises ordinary application-style
            // precedence rather than competing with that intentional variant policy.
            var group = new MaterialButtonGroup { Variant = ButtonGroupVariant.Standard };
            var scope = Place(group, new MaterialTheme());
            ThemeTestHelper.ShowWindow(scope);
            Assert.Same(applicationProfile, group.GeometryProfile);

            var localProfile = new ButtonGroupGeometryProfile(22, 71, 2, true);
            group.GeometryProfile = localProfile;
            Assert.Same(localProfile, group.GeometryProfile);
        }
        finally
        {
            Application.Current.Styles.Remove(style);
        }
    }

    private static Border Place(MaterialButtonGroup group, MaterialTheme theme)
    {
        var scope = new Border { Child = group };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static T Resolve<T>(
        IResourceHost origin,
        MaterialValueKey<T> key,
        ThemeVariant? theme = null) => SharedTokenTestHelper.Resolve(origin, key, theme);

    private static void PumpLayout()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }
}
