using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>End-to-end acceptance tests for the first resource-token-backed component.</summary>
public sealed class ButtonTokenOverrideTests
{
    [AvaloniaFact]
    public void SparseComponentOverride_RefreshesButtonWithoutAffectingSiblingOrOtherTokens()
    {
        var localButton = new Button { Content = "Local" };
        var siblingButton = new Button { Content = "Sibling" };
        var localScope = new Border { Child = localButton };
        var root = new StackPanel { Children = { localScope, siblingButton } };
        var overrides = new FilledButtonTokensOverrides
        {
            ContainerColor = SharedToken.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);
        localScope.Resources.MergedDictionaries.Add(new MaterialTheme());

        ThemeTestHelper.ShowWindow(root);

        var local = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(localButton));
        var sibling = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(siblingButton));
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(localButton.Background));
        Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(siblingButton.Background));
        Assert.Equal(
            ThemeTestHelper.ColorOf(sibling.Content),
            ThemeTestHelper.ColorOf(local.Content));

        overrides.ContainerColor = SharedToken.Literal(Colors.Green);

        local = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(localButton));
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(localButton.Background));

        overrides.ContainerColor = null;

        local = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(localButton));
        sibling = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(siblingButton));
        Assert.Equal(
            ThemeTestHelper.ColorOf(siblingButton.Background),
            ThemeTestHelper.ColorOf(localButton.Background));
    }

    [AvaloniaFact]
    public void ComponentAlias_RebindsAgainstHigherLayerOverrideInTheConsumerScope()
    {
        var button = new Button { Content = "Alias" };
        var scope = new Border { Child = button };
        var componentOverrides = new FilledButtonTokensOverrides
        {
            ContainerColor = SharedToken.Alias(McuSystemColorTokens.Secondary),
        };
        var systemOverrides = new MaterialTokenOverrides();
        systemOverrides.Set(McuSystemColorTokens.Secondary, Colors.Orange);
        scope.Resources.MergedDictionaries.Add(componentOverrides);
        scope.Resources.MergedDictionaries.Add(systemOverrides);
        scope.Resources.MergedDictionaries.Add(new MaterialTheme());

        ThemeTestHelper.ShowWindow(scope);

        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(button.Background));

        systemOverrides.Set(McuSystemColorTokens.Secondary, Colors.Purple);

        Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(button.Background));
    }

    [AvaloniaFact]
    public void NumericAndShapeComponentOverrides_AreConsumedByTheSameReadSession()
    {
        var button = new Button { Content = "Metrics" };
        var scope = new Border { Child = button };
        var overrides = new ButtonSmallTokensOverrides
        {
            ContainerHeight = SharedToken.Literal(64d),
            ContainerShapeRound = SharedToken.Literal(Foundation.ShapeRole.CornerNone),
            OutlinedOutlineWidth = SharedToken.Literal(3d),
        };
        scope.Resources.MergedDictionaries.Add(overrides);
        scope.Resources.MergedDictionaries.Add(new MaterialTheme());

        M3Button.SetVariant(button, ButtonVariant.Outlined);
        ThemeTestHelper.ShowWindow(scope);

        var shapes = SharedTokenTestHelper.Resolve(button, ButtonValueKeys.SmallShapeProfile);
        Assert.Equal(64, button.MinHeight);
        Assert.Equal(Foundation.ShapeRole.CornerNone, shapes.Container);
        Assert.Equal(new Avalonia.Thickness(3), button.BorderThickness);
    }

    [AvaloniaFact]
    public void TextButton_PreservesPrimaryCorrectionButExplicitComponentTokensStillWin()
    {
        var button = new Button { Content = "Text" };
        var scope = new Border { Child = button };
        var systemOverrides = new MaterialTokenOverrides();
        var componentOverrides = new TextButtonTokensOverrides();
        scope.Resources.MergedDictionaries.Add(systemOverrides);
        scope.Resources.MergedDictionaries.Add(componentOverrides);

        M3Button.SetVariant(button, ButtonVariant.Text);
        systemOverrides.Set(McuSystemColorTokens.Primary, Colors.Orange);
        systemOverrides.Set(McuSystemColorTokens.Secondary, Colors.Purple);
        ThemeTestHelper.ShowWindow(scope);

        var colors = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(colors.Content));
        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(colors.Icon));
        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(colors.StateLayer));

        componentOverrides.LabelColor = SharedToken.Literal(Colors.HotPink);
        componentOverrides.IconColor = SharedToken.Alias(McuSystemColorTokens.Secondary);

        colors = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(colors.Content));
        Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(colors.Icon));
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(colors.StateLayer));

        systemOverrides.Set(McuSystemColorTokens.Secondary, Colors.Green);

        colors = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(colors.Icon));
    }
}
