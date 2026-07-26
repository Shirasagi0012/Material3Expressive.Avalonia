using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// The Phase 4 exit criterion in miniature: one control, driven end to end by the token tables and
/// the theme in scope at it.
/// </summary>
public class ButtonTests
{
    [AvaloniaFact]
    public void ItRendersFilledByDefault()
    {
        var button = new MaterialButton { Content = "Press" };
        ThemeTestHelper.ShowWindow(button);

        Assert.Equal(ButtonVariant.Filled, button.Variant);
        Assert.Equal(ButtonSize.Small, button.Size);
        Assert.Equal(ButtonSmallTokens.ContainerHeight, button.MinHeight);
    }

    [AvaloniaFact]
    public void ItsLabelColour_ComesFromTheVariantAndTheThemeInScope()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new MaterialButton { Content = "Press" };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);

        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnPrimary),
            ThemeTestHelper.ColorOf(button.Foreground));

        button.Variant = ButtonVariant.Tonal;

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnSecondaryContainer),
            ThemeTestHelper.ColorOf(button.Foreground));
    }

    [AvaloniaFact]
    public void ChangingSize_RetunesEveryMetricTogether()
    {
        var button = new MaterialButton { Content = "Press" };
        ThemeTestHelper.ShowWindow(button);

        button.Size = ButtonSize.Large;

        Assert.Equal(ButtonLargeTokens.ContainerHeight, button.MinHeight);
        Assert.Equal(ButtonLargeTokens.LeadingSpace, button.Padding.Left);
        Assert.Equal(TypeScale.HeadlineSmall.FontSize, button.FontSize);
    }

    [AvaloniaFact]
    public void ADisabledButton_UsesItsDisabledColours()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new MaterialButton { Content = "Press" };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        button.IsEnabled = false;

        var expected = ThemeTestHelper.Expected(theme, SysColorToken.OnSurfaceVariant);
        var actual = ThemeTestHelper.ColorOf(button.Foreground);

        Assert.Equal(expected.R, actual.R);
        Assert.Equal(expected.G, actual.G);
        Assert.Equal(expected.B, actual.B);

        // Disabled colours carry their token opacity, which is what distinguishes them.
        Assert.True(actual.A < 0xFF);
    }

    [AvaloniaFact]
    public void ItFollowsTheThemeVariant()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new MaterialButton { Content = "Press" };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);
        var window = ThemeTestHelper.ShowWindow(host);

        window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        var light = ThemeTestHelper.ColorOf(button.Foreground);

        window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        var dark = ThemeTestHelper.ColorOf(button.Foreground);

        Assert.NotEqual(light, dark);
        Assert.Equal(ThemeTestHelper.Expected(theme, SysColorToken.OnPrimary, isDark: true), dark);
    }

    [AvaloniaFact]
    public void ItsRestingShape_IsAPill()
    {
        var button = new MaterialButton { Content = "Press", Width = 120, Height = 40 };
        ThemeTestHelper.ShowWindow(button);

        // CornerFull is a percentage, so a pill is genuinely half the height rather than a large
        // fixed radius that happens to look round at one size.
        Assert.Equal(20, CornerRadiusOf(button).TopLeft, 3);
    }

    [AvaloniaFact]
    public void SquaringItOff_MorphsRatherThanSnapping()
    {
        var button = new MaterialButton { Content = "Press", Width = 120, Height = 40 };
        ThemeTestHelper.ShowWindow(button);

        var pill = CornerRadiusOf(button).TopLeft;
        button.IsSquare = true;

        // A few frames in, the corners have started moving but have not arrived — this is the whole
        // point of driving the shape with a spring rather than assigning a CornerRadius.
        PumpFrames(3);
        var midway = CornerRadiusOf(button).TopLeft;
        Assert.True(midway < pill, $"expected the morph to have started, still at {midway}");
        Assert.True(midway > 12, $"expected the morph to still be running, already at {midway}");

        // The small button's square shape is CornerMedium.
        PumpFrames(40);
        Assert.Equal(12, CornerRadiusOf(button).TopLeft, 3);
    }

    /// <summary>
    /// Runs the headless render clock.
    /// </summary>
    /// <remarks>
    /// The forced tick reports the elapsed time of a real stopwatch, and the frame loop measures
    /// real deltas, so the frames have to be spaced in real time — ticking in a tight loop replays
    /// the same instant and nothing moves. Each tick is also followed by a dispatcher pump, because
    /// re-requesting an animation frame schedules the next render through the dispatcher.
    /// </remarks>
    private static void PumpFrames(int count, int millisecondsPerFrame = 16)
    {
        for (var i = 0; i < count; i++)
        {
            System.Threading.Thread.Sleep(millisecondsPerFrame);
            Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void TextAndOutlinedVariants_HaveNoContainerFill()
    {
        var filled = ButtonColorTokens.For(ButtonVariant.Filled);
        var outlined = ButtonColorTokens.For(ButtonVariant.Outlined);
        var text = ButtonColorTokens.For(ButtonVariant.Text);

        Assert.False(filled.ContainerColor.IsLiteral);
        Assert.Equal(0, outlined.ContainerColor.LiteralColor!.Value.A);
        Assert.Equal(0, text.ContainerColor.LiteralColor!.Value.A);
        Assert.True(outlined.HasOutline);
        Assert.False(text.HasOutline);
    }

    /// <summary>Reaches the protected corner radius the way a subclass would.</summary>
    private static Avalonia.CornerRadius CornerRadiusOf(MaterialButton button)
    {
        return new Probe(button).Corners;
    }

    private sealed class Probe(MaterialButton button)
    {
        public Avalonia.CornerRadius Corners =>
            (Avalonia.CornerRadius)typeof(MaterialButton)
                .GetProperty("ContainerCornerRadius", System.Reflection.BindingFlags.Instance
                                                      | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(button)!;
    }
}
