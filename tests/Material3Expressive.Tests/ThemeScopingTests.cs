using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// The Phase 1 exit criteria: a subtree theme reaches a ControlTheme-styled control, a light/dark
/// switch re-resolves live, and replacing the application theme re-resolves live.
/// </summary>
public class ThemeScopingTests
{
    [AvaloniaFact]
    public void SubtreeTheme_ReachesAControlThemeStyledControl()
    {
        var appTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var scopedTheme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);

        using var _ = ThemeTestHelper.AppTheme(appTheme);

        var appButton = new Button();
        var scopedButton = new Button();
        var scope = new Border { Child = scopedButton };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { appButton, scope } });

        MaterialThemeScope.SetTheme(scope, scopedTheme);

        // This is the case the previous design could not express: one ControlTheme, two controls,
        // two different themes.
        Assert.Equal(
            ThemeTestHelper.Expected(appTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(appButton.Background));
        Assert.Equal(
            ThemeTestHelper.Expected(scopedTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(scopedButton.Background));
        Assert.NotEqual(
            ThemeTestHelper.ColorOf(appButton.Background),
            ThemeTestHelper.ColorOf(scopedButton.Background));
    }

    [AvaloniaFact]
    public void SubtreeTheme_ReachesAControlThatReadsTokensInCode()
    {
        var appTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var scopedTheme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);

        using var _ = ThemeTestHelper.AppTheme(appTheme);

        var appProbe = new ThemeProbe();
        var scopedProbe = new ThemeProbe();
        var scope = new Border { Child = scopedProbe };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { appProbe, scope } });

        MaterialThemeScope.SetTheme(scope, scopedTheme);

        Assert.Equal(
            ThemeTestHelper.Expected(appTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(appProbe.ContainerBrush));
        Assert.Equal(
            ThemeTestHelper.Expected(scopedTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(scopedProbe.ContainerBrush));
    }

    [AvaloniaFact]
    public void ThemeVariantSwitch_ReResolvesLive()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        using var _ = ThemeTestHelper.AppTheme(theme);

        var button = new Button();
        var probe = new ThemeProbe();
        var window = ThemeTestHelper.ShowWindow(new StackPanel { Children = { button, probe } });

        window.RequestedThemeVariant = ThemeVariant.Light;

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(button.Background));
        Assert.False(probe.ObservedIsDark);

        window.RequestedThemeVariant = ThemeVariant.Dark;

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.Primary, isDark: true),
            ThemeTestHelper.ColorOf(button.Background));
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.Primary, isDark: true),
            ThemeTestHelper.ColorOf(probe.ContainerBrush));
        Assert.True(probe.ObservedIsDark);
    }

    [AvaloniaFact]
    public void ApplicationThemeReplacement_ReResolvesLive()
    {
        var first = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var second = ThemeTestHelper.ThemeSeededWith(Colors.Green);

        using var restore = ThemeTestHelper.AppTheme(first);

        var button = new Button();
        var probe = new ThemeProbe();
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { button, probe } });

        Assert.Equal(
            ThemeTestHelper.Expected(first, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(button.Background));

        MaterialThemeScope.SetTheme(Application.Current!, second);

        Assert.Equal(
            ThemeTestHelper.Expected(second, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(button.Background));
        Assert.Equal(
            ThemeTestHelper.Expected(second, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(probe.ContainerBrush));
    }

    [AvaloniaFact]
    public void MutatingALiveScheme_ReResolves()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        using var _ = ThemeTestHelper.AppTheme(theme);

        var button = new Button();
        var probe = new ThemeProbe();
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { button, probe } });

        var before = ThemeTestHelper.ColorOf(button.Background);

        // The authoring objects stay mutable on purpose: this is what a seed-colour picker does.
        ((TonalSpotScheme)theme.ColorScheme!).Color = Colors.Green;

        Assert.NotEqual(before, ThemeTestHelper.ColorOf(button.Background));
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(button.Background));
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(probe.ContainerBrush));
    }

    [AvaloniaFact]
    public void NoThemeDeclared_StillResolvesAgainstTheDefaultTheme()
    {
        var button = new Button();
        var probe = new ThemeProbe();
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { button, probe } });

        Assert.Same(ResolvedTheme.Default, probe.ObservedTheme);
        Assert.Equal(
            ResolvedTheme.Default.ColorScheme!.GetColor(SysColorToken.Primary, isDark: false),
            ThemeTestHelper.ColorOf(button.Background));
    }

    [AvaloniaFact]
    public void SubtreeTheme_FlowsIntoAPopup()
    {
        var appTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var scopedTheme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);

        using var _ = ThemeTestHelper.AppTheme(appTheme);

        var popupProbe = new ThemeProbe();
        var anchor = new Border();
        var popup = new Popup
        {
            PlacementTarget = anchor,
            Child = popupProbe
        };
        var scope = new Panel { Children = { anchor, popup } };
        ThemeTestHelper.ShowWindow(scope);

        MaterialThemeScope.SetTheme(scope, scopedTheme);
        popup.IsOpen = true;

        // A popup root is its own TopLevel, but its inheritance parent is the popup, so the
        // subtree theme reaches it rather than the application one.
        Assert.Equal(
            ThemeTestHelper.Expected(scopedTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(popupProbe.ContainerBrush));
    }

    [AvaloniaFact]
    public void ClearingASubtreeTheme_FallsBackToTheApplicationTheme()
    {
        var appTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var scopedTheme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);

        using var _ = ThemeTestHelper.AppTheme(appTheme);

        var button = new Button();
        var scope = new Border { Child = button };
        ThemeTestHelper.ShowWindow(scope);

        MaterialThemeScope.SetTheme(scope, scopedTheme);
        Assert.Equal(
            ThemeTestHelper.Expected(scopedTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(button.Background));

        MaterialThemeScope.SetTheme(scope, null);

        Assert.Equal(
            ThemeTestHelper.Expected(appTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(button.Background));
    }

    [AvaloniaFact]
    public void ScopedTheme_AlsoDrivesTheColorLayersOwnExtensions()
    {
        var appTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var scopedTheme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);

        using var _ = ThemeTestHelper.AppTheme(appTheme);

        var target = new Border();
        var scope = new Border { Child = target };
        ThemeTestHelper.ShowWindow(scope);

        MaterialThemeScope.SetTheme(scope, scopedTheme);

        // MaterialColorUtilities knows nothing about MaterialTheme; the theme publishes its
        // resolved scheme through the colour layer's own inherited property.
        Assert.Equal(
            ThemeTestHelper.Expected(scopedTheme, SysColorToken.Primary),
            MaterialColor.GetResolvedScheme(target)!.GetColor(SysColorToken.Primary, isDark: false));
    }

    [AvaloniaFact]
    public void ReplacingContentContainingMaterialControls_DoesNotThrow()
    {
        using var _ = ThemeTestHelper.AppTheme(ThemeTestHelper.ThemeSeededWith(Colors.Red));

        // Both theme-reading paths: self-painting controls and one hosting the watcher.
        var page = new StackPanel
        {
            Children =
            {
                new ThemeProbe(),
                new MaterialLinearProgressIndicator { Value = 0.5, Width = 200 },
                new MaterialButton()
            }
        };
        var host = new ContentControl { Content = page };
        ThemeTestHelper.ShowWindow(host);

        // Detaching reverts the inherited ActualThemeVariant to its unset default — null — and
        // raises that as a change while the control is still attached. Reading it as a variant
        // threw; the whole page went down with it.
        host.Content = new Border();

        Assert.Null(page.Parent);
    }

    [AvaloniaFact]
    public void ReattachingAControl_ResolvesTheThemeAtItsNewHome()
    {
        var appTheme = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var scopedTheme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);

        using var _ = ThemeTestHelper.AppTheme(appTheme);

        var probe = new ThemeProbe();
        var host = new ContentControl { Content = probe };
        var scope = new Border();
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { host, scope } });

        MaterialThemeScope.SetTheme(scope, scopedTheme);

        Assert.Equal(
            ThemeTestHelper.Expected(appTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(probe.ContainerBrush));

        // Detaching drops the inherited theme back to the default, so the move has to leave the
        // control resolved against its new parent rather than against whatever it saw on the way
        // out.
        host.Content = null;
        scope.Child = probe;

        Assert.Equal(
            ThemeTestHelper.Expected(scopedTheme, SysColorToken.Primary),
            ThemeTestHelper.ColorOf(probe.ContainerBrush));
    }

    [AvaloniaFact]
    public void MaterialControl_ResolvesThemeBeforeDerivedAttachLogic()
    {
        var probe = new ThemeOrderingProbe();

        ThemeTestHelper.ShowWindow(probe);

        Assert.True(
            probe.Events.IndexOf("theme") < probe.Events.IndexOf("derived-attach"),
            $"Observed callback order: {string.Join(", ", probe.Events)}");
    }

    [AvaloniaFact]
    public void MaterialControl_ResolvesThemeBeforeDerivedPropertyLogic()
    {
        var probe = new ThemeOrderingProbe();
        ThemeTestHelper.ShowWindow(probe);
        probe.Events.Clear();

        MaterialThemeScope.SetTheme(probe, ThemeTestHelper.ThemeSeededWith(Colors.Blue));

        Assert.Equal(["theme", "derived-property"], probe.Events);
    }
}
