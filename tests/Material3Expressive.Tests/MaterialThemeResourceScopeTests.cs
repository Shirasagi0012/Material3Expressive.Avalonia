using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using McuSystemColorTokens = MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialThemeResourceScopeTests
{
    [AvaloniaFact]
    public void UndeclaredTheme_PreservesLegacyDefaultAndPublishesOnlyABaselineResource()
    {
        var consumer = new Border();
        consumer.Resources.MergedDictionaries.Add(new MaterialTheme());
        ThemeTestHelper.ShowWindow(consumer);

        Assert.Null(MaterialThemeScope.GetTheme(Application.Current!));
        Assert.Same(ResolvedTheme.Default, MaterialThemeScope.GetResolvedTheme(consumer));
        Assert.Equal(
            ResolvedTheme.Default.ColorScheme!.GetColor(
                MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary,
                isDark: false),
            SharedTokenTestHelper.Resolve(consumer, McuSystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void ApplicationTheme_IsVisibleThroughTheGlobalResourceChain()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        using var restore = ThemeTestHelper.AppTheme(theme);
        var consumer = new Border();
        ThemeTestHelper.ShowWindow(consumer);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            SharedTokenTestHelper.Resolve(consumer, McuSystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void LocalTheme_WinsForConsumersInItsSubtreeOnly()
    {
        var applicationTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var localTheme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        using var restore = ThemeTestHelper.AppTheme(applicationTheme);

        var localConsumer = new Border();
        var siblingConsumer = new Border();
        var scope = new Border { Child = localConsumer };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { scope, siblingConsumer } });
        MaterialThemeScope.SetTheme(scope, localTheme);

        Assert.Equal(
            ThemeTestHelper.Expected(localTheme, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            SharedTokenTestHelper.Resolve(localConsumer, McuSystemColorTokens.Primary, ThemeVariant.Light));
        Assert.Equal(
            ThemeTestHelper.Expected(applicationTheme, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            SharedTokenTestHelper.Resolve(siblingConsumer, McuSystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void ReplacingAndClearingTheme_RemovesItsOldProvider()
    {
        var applicationTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var first = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var replacement = ThemeTestHelper.ThemeSeededWith(Colors.Green);
        using var restore = ThemeTestHelper.AppTheme(applicationTheme);

        var consumer = new Border();
        var scope = new Border { Child = consumer };
        ThemeTestHelper.ShowWindow(scope);

        MaterialThemeScope.SetTheme(scope, first);
        var firstPlacement = Assert.Single(scope.Resources.MergedDictionaries);
        Assert.Same(first, firstPlacement);

        MaterialThemeScope.SetTheme(scope, replacement);
        Assert.DoesNotContain(firstPlacement, scope.Resources.MergedDictionaries);
        var replacementPlacement = Assert.Single(scope.Resources.MergedDictionaries);
        Assert.Same(replacement, replacementPlacement);
        Assert.Equal(
            ThemeTestHelper.Expected(replacement, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            SharedTokenTestHelper.Resolve(consumer, McuSystemColorTokens.Primary, ThemeVariant.Light));

        MaterialThemeScope.SetTheme(scope, null);
        Assert.DoesNotContain(replacementPlacement, scope.Resources.MergedDictionaries);
        Assert.Equal(
            ThemeTestHelper.Expected(applicationTheme, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            SharedTokenTestHelper.Resolve(consumer, McuSystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void OneThemeInstance_CannotBePlacedInMultipleScopes()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var firstScope = new Border();
        var secondScope = new Border();
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { firstScope, secondScope } });

        MaterialThemeScope.SetTheme(firstScope, theme);
        Assert.Throws<InvalidOperationException>(() => MaterialThemeScope.SetTheme(secondScope, theme));

        Assert.Same(theme, Assert.Single(firstScope.Resources.MergedDictionaries));
        Assert.DoesNotContain(theme, secondScope.Resources.MergedDictionaries);
    }

    [AvaloniaFact]
    public void ThemeMutation_NotifiesItsRealPlacementAndChangesResolution()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var consumer = new Border();
        var scope = new Border { Child = consumer };
        ThemeTestHelper.ShowWindow(scope);

        MaterialThemeScope.SetTheme(scope, theme);
        var before = SharedTokenTestHelper.Resolve(consumer, McuSystemColorTokens.Primary, ThemeVariant.Light);
        var notifications = 0;
        ((IResourceHost)consumer).ResourcesChanged += (_, _) => notifications++;

        ((MaterialColorUtilities.Avalonia.TonalSpotScheme)theme.ColorScheme!).Color = Colors.Green;

        Assert.True(notifications > 0);
        Assert.NotEqual(
            before,
            SharedTokenTestHelper.Resolve(consumer, McuSystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void TokenOverrideAlreadyAtThemeScope_WinsOverTheThemeProvider()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var consumer = new Border();
        var scope = new Border { Child = consumer };
        var overrides = new MaterialTokenOverrides();
        overrides.Set(McuSystemColorTokens.Primary, Colors.HotPink);
        scope.Resources.MergedDictionaries.Add(overrides);
        ThemeTestHelper.ShowWindow(scope);

        // XAML commonly creates Resources before assigning the attached Theme property. The
        // provider placement must not let that order mask explicit token overrides at this scope.
        MaterialThemeScope.SetTheme(scope, theme);

        Assert.Equal(
            Colors.HotPink,
            SharedTokenTestHelper.Resolve(consumer, McuSystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void SubtreeThemeResource_FlowsIntoPopupContent()
    {
        var applicationTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var scopedTheme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        using var restore = ThemeTestHelper.AppTheme(applicationTheme);
        var anchor = new Border();
        var popupConsumer = new Border();
        var popup = new Popup
        {
            PlacementTarget = anchor,
            Child = popupConsumer,
        };
        var scope = new Panel { Children = { anchor, popup } };
        ThemeTestHelper.ShowWindow(scope);
        MaterialThemeScope.SetTheme(scope, scopedTheme);

        popup.IsOpen = true;

        Assert.Equal(
            ThemeTestHelper.Expected(
                scopedTheme,
                MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            SharedTokenTestHelper.Resolve(popupConsumer, McuSystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void ThemeOwnerIdentifiesItsPlacementUntilDetached()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var scope = new Border();
        MaterialThemeScope.SetTheme(scope, theme);

        Assert.Same(scope, theme.Owner);
    }
}
