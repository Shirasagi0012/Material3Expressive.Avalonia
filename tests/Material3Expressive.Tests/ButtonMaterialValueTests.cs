using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using McuReferencePaletteTokens = MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end contracts for the Button M4a MaterialValue endpoints.</summary>
public sealed class ButtonMaterialValueTests
{
    [AvaloniaTheory]
    [InlineData(ButtonVariant.Filled, SysColorToken.Primary, false)]
    [InlineData(ButtonVariant.Tonal, SysColorToken.SecondaryContainer, false)]
    [InlineData(ButtonVariant.Elevated, SysColorToken.SurfaceContainerLow, false)]
    [InlineData(ButtonVariant.Outlined, SysColorToken.Primary, true)]
    [InlineData(ButtonVariant.Text, SysColorToken.Primary, true)]
    public void EveryVariantOwnsEnabledAndDisabledBackgroundEndpoints(
        ButtonVariant variant,
        SysColorToken enabledRole,
        bool isTransparent)
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new Button { Content = "Value" };
        M3Button.SetVariant(button, variant);
        Mount(button, theme);

        var enabled = ThemeTestHelper.ColorOf(button.Background);
        var endpoint = ResolveBackground(button, variant, disabled: false);
        Assert.Equal(ThemeTestHelper.ColorOf(endpoint), enabled);

        if (isTransparent)
            Assert.Equal(Colors.Transparent, enabled);
        else
            Assert.Equal(
                ThemeTestHelper.Expected(theme, enabledRole),
                enabled);

        button.IsEnabled = false;

        var disabled = ThemeTestHelper.ColorOf(button.Background);
        var disabledEndpoint = ResolveBackground(button, variant, disabled: true);
        Assert.Equal(ThemeTestHelper.ColorOf(disabledEndpoint), disabled);

        if (isTransparent)
        {
            Assert.Equal(Colors.Transparent, disabled);
        }
        else
        {
            var onSurface = ThemeTestHelper.Expected(theme, SysColorToken.OnSurface);
            Assert.Equal(
                new Color(
                    (byte)Math.Round(onSurface.A * 0.1),
                    onSurface.R,
                    onSurface.G,
                    onSurface.B),
                disabled);
        }
    }

    [AvaloniaTheory]
    [InlineData(ButtonSize.ExtraSmall, 32d, 12d, 6d, 12d, 6d, TypographyRole.LabelLarge)]
    [InlineData(ButtonSize.Small, 40d, 16d, 10d, 16d, 10d, TypographyRole.LabelLarge)]
    [InlineData(ButtonSize.Medium, 56d, 24d, 16d, 24d, 16d, TypographyRole.TitleMedium)]
    [InlineData(ButtonSize.Large, 96d, 48d, 32d, 48d, 32d, TypographyRole.HeadlineSmall)]
    [InlineData(ButtonSize.ExtraLarge, 136d, 64d, 48d, 64d, 48d, TypographyRole.HeadlineLarge)]
    public void EverySizePublishesMinHeightAndPaddingValueEndpoints(
        ButtonSize size,
        double expectedHeight,
        double left,
        double top,
        double right,
        double bottom,
        TypographyRole expectedTextStyle)
    {
        var button = new Button { Content = "Size" };
        M3Button.SetSize(button, size);
        Mount(button, new MaterialTheme());

        Assert.Equal(expectedHeight, button.MinHeight);
        Assert.Equal(new Thickness(left, top, right, bottom), button.Padding);
        Assert.Equal(
            expectedHeight,
            ResolveHeight(button, size));
        Assert.Equal(
            new Thickness(left, top, right, bottom),
            ResolvePadding(button, size));
        Assert.Equal(expectedTextStyle, TypographyScheme.GetTextStyle(button));
        Assert.Equal(expectedTextStyle, ResolveTextStyle(button, size));
    }

    [AvaloniaTheory]
    [InlineData(ButtonSize.ExtraSmall, ShapeRole.CornerFull, ShapeRole.CornerSmall, ShapeRole.CornerMedium)]
    [InlineData(ButtonSize.Small, ShapeRole.CornerFull, ShapeRole.CornerSmall, ShapeRole.CornerMedium)]
    [InlineData(ButtonSize.Medium, ShapeRole.CornerFull, ShapeRole.CornerMedium, ShapeRole.CornerLarge)]
    [InlineData(ButtonSize.Large, ShapeRole.CornerFull, ShapeRole.CornerLarge, ShapeRole.CornerExtraLarge)]
    [InlineData(ButtonSize.ExtraLarge, ShapeRole.CornerFull, ShapeRole.CornerLarge, ShapeRole.CornerExtraLarge)]
    public void EverySizePublishesAnAtomicShapeProfile(
        ButtonSize size,
        ShapeRole resting,
        ShapeRole pressed,
        ShapeRole square)
    {
        var button = new Button { Content = "Shape" };
        M3Button.SetSize(button, size);
        Mount(button, new MaterialTheme());

        var profile = ResolveShapeProfile(button, size);
        Assert.Equal(new ButtonShapeProfile(resting, pressed, square), profile);
        Assert.Equal(profile, PresenterOf(button).ShapeProfile);
    }

    [AvaloniaFact]
    public void PresenterUsesSizeSquarePressedBoundsAndRtlShapePolicies()
    {
        var theme = new MaterialTheme();
        var button = new Button { Content = "Shape", Width = 120, Height = 40 };
        var window = Mount(button, theme);
        var presenter = PresenterOf(button);

        Assert.Equal(20, presenter.CurrentCornerRadius.TopLeft, 3);

        M3Button.SetIsSquare(button, true);
        Settle(presenter);
        var square = new ResolvedShapeScheme(theme.ShapeScheme!).Get(ShapeRole.CornerMedium)
            .ToCornerRadius(presenter.Bounds.Size);
        Assert.Equal(square, presenter.CurrentCornerRadius);

        M3Button.SetIsSquare(button, false);
        Settle(presenter);
        var point = button.TranslatePoint(new Point(36, 20), window)
                    ?? throw new InvalidOperationException("Button was not mounted.");
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        Assert.True(button.IsPressed);
        Settle(presenter);
        var pressed = new ResolvedShapeScheme(theme.ShapeScheme!).Get(ShapeRole.CornerSmall)
            .ToCornerRadius(presenter.Bounds.Size);
        Assert.Equal(pressed, presenter.CurrentCornerRadius);
        window.MouseUp(point, MouseButton.Left);

        var asymmetric = new RoundedCornerShape(
            CornerSize.Absolute(3),
            CornerSize.Absolute(7),
            CornerSize.Absolute(11),
            CornerSize.Absolute(13));
        M3Button.SetContainerShapeOverride(button, asymmetric);
        button.FlowDirection = FlowDirection.LeftToRight;
        Settle(presenter);
        Assert.Equal(
            asymmetric.ToCornerRadius(presenter.Bounds.Size),
            presenter.CurrentCornerRadius);

        button.FlowDirection = FlowDirection.RightToLeft;
        Settle(presenter);
        Assert.Equal(
            asymmetric.ToCornerRadius(presenter.Bounds.Size, isRightToLeft: true),
            presenter.CurrentCornerRadius);
    }

    [AvaloniaFact]
    public void ComponentSystemAndReferenceAliasesRebindAndTrackSourceMutation()
    {
        var button = new Button { Content = "Layers" };
        M3Button.SetVariant(button, ButtonVariant.Filled);
        var theme = new MaterialTheme();
        var scope = Place(button, theme);

        var firstReference = McuReferencePaletteTokens.Get(RefPaletteToken.Primary, 40);
        var secondReference = McuReferencePaletteTokens.Get(RefPaletteToken.Secondary, 80);
        var references = new MaterialTokenOverrides();
        references.Set(firstReference, Colors.Orange);
        references.Set(secondReference, Colors.Purple);

        var system = new MaterialTokenOverrides();
        system.Set(
            McuSystemColorTokens.Primary,
            SharedToken.Alias(firstReference));

        var component = new FilledButtonTokensOverrides
        {
            ContainerColor = SharedToken.Alias(McuSystemColorTokens.Primary),
        };
        scope.Resources.MergedDictionaries.Add(component);
        scope.Resources.MergedDictionaries.Add(system);
        scope.Resources.MergedDictionaries.Add(references);
        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(Colors.Orange, ColorOf(ResolveBackground(button, ButtonVariant.Filled, false)));
        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(button.Background));

        system.Set(
            McuSystemColorTokens.Primary,
            SharedToken.Alias(secondReference));
        Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(button.Background));

        references.Set(secondReference, Colors.Green);
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(button.Background));
    }

    [AvaloniaFact]
    public void LocalRawPlacementIsolatedFromSiblingAndRefreshesInPlace()
    {
        var localButton = new Button { Content = "Local" };
        var siblingButton = new Button { Content = "Sibling" };
        var localScope = Place(localButton, new MaterialTheme());
        var root = new StackPanel { Children = { localScope, siblingButton } };
        var overrides = new FilledButtonTokensOverrides
        {
            ContainerColor = SharedToken.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);
        ThemeTestHelper.ShowWindow(root);

        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(localButton.Background));
        Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(siblingButton.Background));

        overrides.ContainerColor = SharedToken.Literal(Colors.Green);
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(localButton.Background));
        Assert.NotEqual(Colors.Green, ThemeTestHelper.ColorOf(siblingButton.Background));
    }

    [AvaloniaFact]
    public void FlatValueResourceOverridesTheTypedEndpoint()
    {
        var button = new Button { Content = "Flat" };
        var scope = Place(button, new MaterialTheme());
        var flat = new ImmutableSolidColorBrush(Colors.HotPink);
        scope.Resources[ButtonValueKeys.FilledContainerBrush] = flat;
        ThemeTestHelper.ShowWindow(scope);

        Assert.Same(flat, button.Background);
        Assert.Same(flat, ResolveBackground(button, ButtonVariant.Filled, disabled: false));
    }

    [AvaloniaFact]
    public void LocalValuesAndApplicationStylesKeepPrecedenceOverMaterialValues()
    {
        var style = new Style(selector => selector.Is<Button>());
        var applicationBrush = new ImmutableSolidColorBrush(Colors.Orange);
        style.Add(new Setter(Button.BackgroundProperty, applicationBrush));
        style.Add(new Setter(Button.PaddingProperty, new Thickness(7)));
        Application.Current!.Styles.Add(style);

        try
        {
            var styled = new Button { Content = "Styled" };
            var styledScope = Place(styled, new MaterialTheme());
            ThemeTestHelper.ShowWindow(styledScope);
            Assert.Same(applicationBrush, styled.Background);
            Assert.Equal(new Thickness(7), styled.Padding);

            var localBrush = new ImmutableSolidColorBrush(Colors.HotPink);
            var local = new Button
            {
                Content = "Local",
                Background = localBrush,
                Padding = new Thickness(3),
                MinHeight = 88,
            };
            var localScope = Place(local, new MaterialTheme());
            ThemeTestHelper.ShowWindow(localScope);
            M3Button.SetVariant(local, ButtonVariant.Tonal);
            M3Button.SetSize(local, ButtonSize.ExtraLarge);

            Assert.Same(localBrush, local.Background);
            Assert.Equal(new Thickness(3), local.Padding);
            Assert.Equal(88, local.MinHeight);
        }
        finally
        {
            Application.Current.Styles.Remove(style);
        }
    }

    [AvaloniaFact]
    public void InvalidRawHitDoesNotFallBackToAncestorFlatEndpoint()
    {
        var key = ButtonValueKeys.FilledContainerBrush;
        var button = new Button { Content = "Invalid" };
        var localScope = Place(button, new MaterialTheme());
        var root = new Border { Child = localScope };
        root.Resources[key] = new ImmutableSolidColorBrush(Colors.HotPink);
        localScope.Resources[FilledButtonTokensKeys.ContainerColor] = "wrong-type";

        var exception = Assert.Throws<MaterialValueResolutionException>(() =>
            SharedTokenTestHelper.Resolve(button, key, ThemeVariant.Light));

        Assert.Equal(MaterialResolutionFailureKind.WrongType, exception.FailureKind);
        Assert.Same(FilledButtonTokensKeys.ContainerColor, exception.Token);
        Assert.Same(key, exception.ValueKey);
    }

    [AvaloniaFact]
    public void MigratedContainerRawOverrideChangesBackgroundButLeavesLegacyGhostRecordInert()
    {
        var button = new Button { Content = "Migrated" };
        var scope = Place(button, new MaterialTheme());
        var overrides = new FilledButtonTokensOverrides
        {
            ContainerColor = SharedToken.Literal(Colors.HotPink),
        };
        scope.Resources.MergedDictionaries.Add(overrides);
        ThemeTestHelper.ShowWindow(scope);

        var ghost = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(button.Background));
        Assert.Equal(Colors.Transparent, ThemeTestHelper.ColorOf(ghost.Container));

        overrides.ContainerColor = SharedToken.Literal(Colors.Green);
        ghost = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(button.Background));
        Assert.Equal(Colors.Transparent, ThemeTestHelper.ColorOf(ghost.Container));
    }

    private static Border Place(Button button, MaterialTheme theme)
    {
        var scope = new Border { Child = button };
        MaterialThemeScope.SetTheme(scope, theme);
        return scope;
    }

    private static Window Mount(Button button, MaterialTheme theme)
    {
        var scope = Place(button, theme);
        return ThemeTestHelper.ShowWindow(scope);
    }

    private static IImmutableBrush ResolveBackground(
        Button button,
        ButtonVariant variant,
        bool disabled)
    {
        var key = variant switch
        {
            ButtonVariant.Filled => disabled
                ? ButtonValueKeys.FilledDisabledContainerBrush
                : ButtonValueKeys.FilledContainerBrush,
            ButtonVariant.Tonal => disabled
                ? ButtonValueKeys.TonalDisabledContainerBrush
                : ButtonValueKeys.TonalContainerBrush,
            ButtonVariant.Elevated => disabled
                ? ButtonValueKeys.ElevatedDisabledContainerBrush
                : ButtonValueKeys.ElevatedContainerBrush,
            ButtonVariant.Outlined => disabled
                ? ButtonValueKeys.OutlinedDisabledContainerBrush
                : ButtonValueKeys.OutlinedContainerBrush,
            ButtonVariant.Text => disabled
                ? ButtonValueKeys.TextDisabledContainerBrush
                : ButtonValueKeys.TextContainerBrush,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

        if (!button.TryFindResource(key, button.ActualThemeVariant, out var value))
            throw new InvalidOperationException($"Resource '{key.Path}' was not found.");

        return Assert.IsAssignableFrom<IImmutableBrush>(value);
    }

    private static double ResolveHeight(Button button, ButtonSize size) => size switch
    {
        ButtonSize.ExtraSmall => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.ExtraSmallContainerHeight),
        ButtonSize.Small => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.SmallContainerHeight),
        ButtonSize.Medium => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.MediumContainerHeight),
        ButtonSize.Large => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.LargeContainerHeight),
        ButtonSize.ExtraLarge => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.ExtraLargeContainerHeight),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private static Thickness ResolvePadding(Button button, ButtonSize size) => size switch
    {
        ButtonSize.ExtraSmall => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.ExtraSmallContentPadding),
        ButtonSize.Small => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.SmallContentPadding),
        ButtonSize.Medium => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.MediumContentPadding),
        ButtonSize.Large => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.LargeContentPadding),
        ButtonSize.ExtraLarge => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.ExtraLargeContentPadding),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private static ButtonShapeProfile ResolveShapeProfile(Button button, ButtonSize size) => size switch
    {
        ButtonSize.ExtraSmall => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.ExtraSmallShapeProfile),
        ButtonSize.Small => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.SmallShapeProfile),
        ButtonSize.Medium => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.MediumShapeProfile),
        ButtonSize.Large => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.LargeShapeProfile),
        ButtonSize.ExtraLarge => SharedTokenTestHelper.Resolve(
            button,
            ButtonValueKeys.ExtraLargeShapeProfile),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private static TypographyRole ResolveTextStyle(Button button, ButtonSize size)
    {
        var key = size switch
        {
            ButtonSize.ExtraSmall => ButtonValueKeys.ExtraSmallTextStyle,
            ButtonSize.Small => ButtonValueKeys.SmallTextStyle,
            ButtonSize.Medium => ButtonValueKeys.MediumTextStyle,
            ButtonSize.Large => ButtonValueKeys.LargeTextStyle,
            ButtonSize.ExtraLarge => ButtonValueKeys.ExtraLargeTextStyle,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
        };

        if (!button.TryFindResource(key, button.ActualThemeVariant, out var value))
            throw new InvalidOperationException($"Resource '{key.Path}' was not found.");

        return Assert.IsType<TypographyRole>(value);
    }

    private static MaterialButtonContainerPresenter PresenterOf(Button button) =>
        Assert.Single(button.GetVisualDescendants().OfType<MaterialButtonContainerPresenter>());

    private static void Settle(MaterialButtonContainerPresenter presenter)
    {
        presenter.Interaction?.Motion.SettleAll();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }

    private static Color ColorOf(IImmutableBrush brush) =>
        Assert.IsType<ImmutableSolidColorBrush>(brush).Color;
}
