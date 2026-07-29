using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
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

    [AvaloniaTheory]
    [InlineData(ButtonSize.ExtraSmall, TypographyRole.LabelLarge, 14d)]
    [InlineData(ButtonSize.Small, TypographyRole.LabelLarge, 14d)]
    [InlineData(ButtonSize.Medium, TypographyRole.TitleMedium, 16d)]
    [InlineData(ButtonSize.Large, TypographyRole.HeadlineSmall, 24d)]
    [InlineData(ButtonSize.ExtraLarge, TypographyRole.HeadlineLarge, 32d)]
    public void ChangingSize_RetunesEveryMetricTogether(
        ButtonSize size,
        TypographyRole expectedTextStyle,
        double expectedFontSize)
    {
        var button = new MaterialButton { Content = "Press" };
        ThemeTestHelper.ShowWindow(button);

        button.Size = size;

        var metrics = ButtonSizeTokens.For(size);

        Assert.Equal(metrics.ContainerHeight, button.MinHeight);
        Assert.Equal(metrics.LeadingSpace, button.Padding.Left);
        Assert.Equal(expectedTextStyle, TypographyScheme.GetTextStyle(button));
        Assert.Equal(expectedFontSize, button.FontSize);
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

        // The corners move a little further towards the square shape on every frame, and are not
        // there yet — this is the whole point of driving the shape with a spring rather than
        // assigning a CornerRadius. That it arrives is covered by AnimatedShapeTests; what is
        // being proved here is that the render clock drives the loop at all.
        var previous = pill;
        for (var frame = 0; frame < 3; frame++)
        {
            PumpFrame();
            var current = CornerRadiusOf(button).TopLeft;
            Assert.True(current < previous, $"frame {frame} did not advance the morph ({current})");
            previous = current;
        }

        // CornerMedium is where it is heading; it must not have jumped there.
        Assert.True(previous > 12, $"expected the morph to still be running, already at {previous}");
    }

    [AvaloniaFact]
    public void AHandledButtonPress_StillStartsAndEndsMaterialFeedback()
    {
        var button = new MaterialButton { Content = "Press", Width = 120, Height = 40 };
        var window = ThemeTestHelper.ShowWindow(button);
        var point = button.TranslatePoint(new Avalonia.Point(36, 20), window)
                    ?? throw new InvalidOperationException("Button was not mounted.");
        var probe = new Probe(button);

        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);

        Assert.True(button.IsPressed);
        Assert.True(probe.Interaction.Interaction.HasFlag(InteractionState.Pressed));
        probe.Interaction.Motion.SettleAll();
        Assert.True(probe.Corners.TopLeft < 20);

        window.MouseUp(point, MouseButton.Left);

        Assert.False(button.IsPressed);
        Assert.False(probe.Interaction.Interaction.HasFlag(InteractionState.Pressed));
    }

    /// <summary>
    /// Runs one frame of the headless render clock.
    /// </summary>
    /// <remarks>
    /// The forced tick reports the elapsed time of a real stopwatch and the frame loop measures
    /// real deltas, so frames have to be spaced in real time — ticking in a tight loop replays the
    /// same instant and nothing moves. The tick is then pumped through the dispatcher, because
    /// re-requesting an animation frame schedules the next render there.
    /// </remarks>
    private static void PumpFrame(int milliseconds = 16)
    {
        System.Threading.Thread.Sleep(milliseconds);
        Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
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

        public MaterialInteractionHost Interaction =>
            (MaterialInteractionHost)typeof(MaterialButton)
                .GetField(
                    "_interaction",
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(button)!;
    }
}
