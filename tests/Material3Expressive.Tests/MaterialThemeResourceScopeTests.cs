using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens.Runtime;
using Material3Expressive.Tests.TestUtils;
using System.Runtime.CompilerServices;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialThemeResourceScopeTests
{
    private readonly MaterialTokenResolver _resolver = new();

    [AvaloniaFact]
    public void UndeclaredTheme_PreservesLegacyDefaultAndPublishesOnlyABaselineResource()
    {
        var consumer = new Border();
        ThemeTestHelper.ShowWindow(consumer);

        Assert.Null(MaterialThemeScope.GetTheme(Application.Current!));
        Assert.Same(ResolvedTheme.Default, MaterialThemeScope.GetResolvedTheme(consumer));
        Assert.Equal(
            ResolvedTheme.Default.ColorScheme!.GetColor(
                MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary,
                isDark: false),
            _resolver.Resolve(consumer, SystemColorTokens.Primary, ThemeVariant.Light));
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
            _resolver.Resolve(consumer, SystemColorTokens.Primary, ThemeVariant.Light));
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
            _resolver.Resolve(localConsumer, SystemColorTokens.Primary, ThemeVariant.Light));
        Assert.Equal(
            ThemeTestHelper.Expected(applicationTheme, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            _resolver.Resolve(siblingConsumer, SystemColorTokens.Primary, ThemeVariant.Light));
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
        Assert.NotSame(first, firstPlacement);

        MaterialThemeScope.SetTheme(scope, replacement);
        Assert.DoesNotContain(firstPlacement, scope.Resources.MergedDictionaries);
        var replacementPlacement = Assert.Single(scope.Resources.MergedDictionaries);
        Assert.NotSame(replacement, replacementPlacement);
        Assert.Equal(
            ThemeTestHelper.Expected(replacement, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            _resolver.Resolve(consumer, SystemColorTokens.Primary, ThemeVariant.Light));

        MaterialThemeScope.SetTheme(scope, null);
        Assert.DoesNotContain(replacementPlacement, scope.Resources.MergedDictionaries);
        Assert.Equal(
            ThemeTestHelper.Expected(applicationTheme, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            _resolver.Resolve(consumer, SystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void OneThemeInstance_CanBePlacedInMultipleScopes()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var firstConsumer = new Border();
        var secondConsumer = new Border();
        var firstScope = new Border { Child = firstConsumer };
        var secondScope = new Border { Child = secondConsumer };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { firstScope, secondScope } });

        MaterialThemeScope.SetTheme(firstScope, theme);
        MaterialThemeScope.SetTheme(secondScope, theme);

        Assert.Equal(
            _resolver.Resolve(firstConsumer, SystemColorTokens.Primary, ThemeVariant.Light),
            _resolver.Resolve(secondConsumer, SystemColorTokens.Primary, ThemeVariant.Light));

        MaterialThemeScope.SetTheme(firstScope, null);
        Assert.Equal(
            ThemeTestHelper.Expected(theme, MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Primary),
            _resolver.Resolve(secondConsumer, SystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void SharedThemeMutation_NotifiesAProxyPlacementAndChangesResolution()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var firstScope = new Border();
        var consumer = new Border();
        var secondScope = new Border { Child = consumer };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { firstScope, secondScope } });

        // The first placement owns MaterialTheme itself; the second exercises the single-owner
        // proxy path.
        MaterialThemeScope.SetTheme(firstScope, theme);
        MaterialThemeScope.SetTheme(secondScope, theme);
        var before = _resolver.Resolve(consumer, SystemColorTokens.Primary, ThemeVariant.Light);
        var notifications = 0;
        ((IResourceHost)consumer).ResourcesChanged += (_, _) => notifications++;

        ((MaterialColorUtilities.Avalonia.TonalSpotScheme)theme.ColorScheme!).Color = Colors.Green;

        Assert.True(notifications > 0);
        Assert.NotEqual(
            before,
            _resolver.Resolve(consumer, SystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void TokenOverrideAlreadyAtThemeScope_WinsOverTheThemeProvider()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var consumer = new Border();
        var scope = new Border { Child = consumer };
        var overrides = new MaterialTokenOverrides();
        overrides.Set(SystemColorTokens.Primary, Colors.HotPink);
        scope.Resources.MergedDictionaries.Add(overrides);
        ThemeTestHelper.ShowWindow(scope);

        // XAML commonly creates Resources before assigning the attached Theme property. The
        // provider placement must not let that order mask explicit token overrides at this scope.
        MaterialThemeScope.SetTheme(scope, theme);

        Assert.Equal(
            Colors.HotPink,
            _resolver.Resolve(consumer, SystemColorTokens.Primary, ThemeVariant.Light));
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
            _resolver.Resolve(popupConsumer, SystemColorTokens.Primary, ThemeVariant.Light));
    }

    [AvaloniaFact]
    public void LongLivedTheme_DoesNotKeepAnAbandonedLocalScopeAlive()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var scope = CreateAbandonedLocalScope(theme);

        Collect(scope);

        Assert.False(scope.IsAlive);
        GC.KeepAlive(theme);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAbandonedLocalScope(MaterialTheme theme)
    {
        var scope = new Border();
        MaterialThemeScope.SetTheme(scope, theme);
        return new WeakReference(scope);
    }

    private static void Collect(WeakReference reference)
    {
        for (var attempt = 0; attempt < 3 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
