using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
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
            ContainerColor = Token.Literal(Colors.HotPink),
        };
        localScope.Resources.MergedDictionaries.Add(overrides);

        ThemeTestHelper.ShowWindow(root);

        var local = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(localButton));
        var sibling = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(siblingButton));
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(local.Container));
        Assert.NotEqual(Colors.HotPink, ThemeTestHelper.ColorOf(sibling.Container));
        Assert.Equal(
            ThemeTestHelper.ColorOf(sibling.Content),
            ThemeTestHelper.ColorOf(local.Content));

        overrides.ContainerColor = Token.Literal(Colors.Green);

        local = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(localButton));
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(local.Container));

        overrides.ContainerColor = null;

        local = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(localButton));
        sibling = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(siblingButton));
        Assert.Equal(
            ThemeTestHelper.ColorOf(sibling.Container),
            ThemeTestHelper.ColorOf(local.Container));
    }

    [AvaloniaFact]
    public void ComponentAlias_RebindsAgainstHigherLayerOverrideInTheConsumerScope()
    {
        var button = new Button { Content = "Alias" };
        var scope = new Border { Child = button };
        var componentOverrides = new FilledButtonTokensOverrides
        {
            ContainerColor = Token.Alias(SystemColorTokens.Secondary),
        };
        var systemOverrides = new MaterialTokenOverrides();
        systemOverrides.Set(SystemColorTokens.Secondary, Colors.Orange);
        scope.Resources.MergedDictionaries.Add(componentOverrides);
        scope.Resources.MergedDictionaries.Add(systemOverrides);

        ThemeTestHelper.ShowWindow(scope);

        var colors = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(colors.Container));

        systemOverrides.Set(SystemColorTokens.Secondary, Colors.Purple);

        colors = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(colors.Container));
    }

    [AvaloniaFact]
    public void NumericAndShapeComponentOverrides_AreConsumedByTheSameReadSession()
    {
        var button = new Button { Content = "Metrics" };
        var scope = new Border { Child = button };
        var overrides = new ButtonSmallTokensOverrides
        {
            ContainerHeight = Token.Literal(64d),
            ContainerShapeRound = Token.Literal(Foundation.ShapeRole.CornerNone),
            OutlinedOutlineWidth = Token.Literal(3d),
        };
        scope.Resources.MergedDictionaries.Add(overrides);

        M3Button.SetVariant(button, ButtonVariant.Outlined);
        ThemeTestHelper.ShowWindow(scope);

        var metrics = Assert.IsType<ButtonMetrics>(M3Button.GetDefaultMetrics(button));
        var shapes = Assert.IsType<ButtonShapes>(M3Button.GetDefaultShapes(button));
        Assert.Equal(64, metrics.ContainerHeight);
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
        systemOverrides.Set(SystemColorTokens.Primary, Colors.Orange);
        systemOverrides.Set(SystemColorTokens.Secondary, Colors.Purple);
        ThemeTestHelper.ShowWindow(scope);

        var colors = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(colors.Content));
        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(colors.Icon));
        Assert.Equal(Colors.Orange, ThemeTestHelper.ColorOf(colors.StateLayer));

        componentOverrides.LabelColor = Token.Literal(Colors.HotPink);
        componentOverrides.IconColor = Token.Alias(SystemColorTokens.Secondary);

        colors = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(colors.Content));
        Assert.Equal(Colors.Purple, ThemeTestHelper.ColorOf(colors.Icon));
        Assert.Equal(Colors.HotPink, ThemeTestHelper.ColorOf(colors.StateLayer));

        systemOverrides.Set(SystemColorTokens.Secondary, Colors.Green);

        colors = Assert.IsType<ButtonColors>(M3Button.GetDefaultColors(button));
        Assert.Equal(Colors.Green, ThemeTestHelper.ColorOf(colors.Icon));
    }
}
