using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>Contracts for the native-first Button implementation and its CLR facade.</summary>
public class ButtonTests
{
    [AvaloniaFact]
    public void NativeButton_IsMaterialFilledByDefault()
    {
        var button = new Button { Content = "Press" };
        ThemeTestHelper.ShowWindow(button);

        Assert.Equal(ButtonVariant.Filled, ButtonAssist.GetVariant(button));
        Assert.Equal(ButtonSize.Small, ButtonAssist.GetSize(button));
        Assert.Equal(ButtonSmallTokens.ContainerHeight, button.MinHeight);
        Assert.Single(button.GetVisualDescendants().OfType<MaterialButtonContainerPresenter>());
    }

    [AvaloniaFact]
    public void AttachedVariant_IsLiveAndUsesTheThemeInScope()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new Button { Content = "Press" };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);

        ThemeTestHelper.ShowWindow(host);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnPrimary),
            ThemeTestHelper.ColorOf(button.Foreground));

        ButtonAssist.SetVariant(button, ButtonVariant.Tonal);

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
    public void AttachedSize_RetunesEveryMetricTogether(
        ButtonSize size,
        TypographyRole expectedTextStyle,
        double expectedFontSize)
    {
        var button = new Button { Content = "Press" };
        ThemeTestHelper.ShowWindow(button);

        ButtonAssist.SetSize(button, size);

        var metrics = ButtonSizeTokens.For(size);
        Assert.Equal(metrics.ContainerHeight, button.MinHeight);
        Assert.Equal(metrics.LeadingSpace, button.Padding.Left);
        Assert.Equal(expectedTextStyle, TypographyScheme.GetTextStyle(button));
        Assert.Equal(expectedFontSize, button.FontSize);
    }

    [AvaloniaFact]
    public void DisabledNativeButton_UsesDisabledColours()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new Button { Content = "Press" };
        var host = new Border { Child = button };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);

        button.IsEnabled = false;

        var expected = ThemeTestHelper.Expected(theme, SysColorToken.OnSurfaceVariant);
        var actual = ThemeTestHelper.ColorOf(button.Foreground);

        Assert.Equal(expected.R, actual.R);
        Assert.Equal(expected.G, actual.G);
        Assert.Equal(expected.B, actual.B);
        Assert.True(actual.A < 0xFF);
    }

    [AvaloniaFact]
    public void NativeButton_FollowsTheThemeVariant()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var button = new Button { Content = "Press" };
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
    public void RestingShape_IsAPillResolvedAgainstActualBounds()
    {
        var button = new Button { Content = "Press", Width = 120, Height = 40 };
        ThemeTestHelper.ShowWindow(button);

        Assert.Equal(20, PresenterOf(button).CurrentCornerRadius.TopLeft, 3);
    }

    [AvaloniaFact]
    public void ChangingAttachedShape_MorphsRatherThanSnapping()
    {
        var button = new Button { Content = "Press", Width = 120, Height = 40 };
        ThemeTestHelper.ShowWindow(button);
        var presenter = PresenterOf(button);

        var pill = presenter.CurrentCornerRadius.TopLeft;
        ButtonAssist.SetIsSquare(button, true);

        var previous = pill;
        for (var frame = 0; frame < 3; frame++)
        {
            PumpFrame();
            var current = presenter.CurrentCornerRadius.TopLeft;
            Assert.True(current < previous, $"frame {frame} did not advance the morph ({current})");
            previous = current;
        }

        Assert.True(previous > 12, $"expected the morph to still be running, already at {previous}");
    }

    [AvaloniaFact]
    public void HandledNativeButtonPress_StillStartsAndEndsMaterialFeedback()
    {
        var button = new Button { Content = "Press", Width = 120, Height = 40 };
        var window = ThemeTestHelper.ShowWindow(button);
        var point = button.TranslatePoint(new Point(36, 20), window)
                    ?? throw new InvalidOperationException("Button was not mounted.");
        var presenter = PresenterOf(button);
        var interaction = Assert.IsType<MaterialInteractionController>(presenter.Interaction);

        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);

        Assert.True(button.IsPressed);
        Assert.True(interaction.Interaction.HasFlag(InteractionState.Pressed));
        interaction.Motion.SettleAll();
        Assert.True(presenter.CurrentCornerRadius.TopLeft < 20);

        window.MouseUp(point, MouseButton.Left);

        Assert.False(button.IsPressed);
        Assert.False(interaction.Interaction.HasFlag(InteractionState.Pressed));
    }

    [AvaloniaFact]
    public void Facade_AddsClrSyntaxWithoutAddingStateOrImplementation()
    {
        Assert.Same(ButtonAssist.VariantProperty, MaterialButton.VariantProperty);
        Assert.Same(ButtonAssist.SizeProperty, MaterialButton.SizeProperty);
        Assert.Same(ButtonAssist.IsSquareProperty, MaterialButton.IsSquareProperty);
        Assert.Same(
            ButtonAssist.ContainerShapeOverrideProperty,
            MaterialButton.ContainerShapeOverrideProperty);

        var facade = new MaterialButton
        {
            Content = "Press",
            Variant = ButtonVariant.Outlined,
            Size = ButtonSize.Medium
        };
        ThemeTestHelper.ShowWindow(facade);

        Assert.Equal(ButtonVariant.Outlined, ButtonAssist.GetVariant(facade));
        Assert.Equal(ButtonSize.Medium, ButtonAssist.GetSize(facade));
        Assert.Single(facade.GetVisualDescendants().OfType<MaterialButtonContainerPresenter>());
    }

    [AvaloniaFact]
    public void NamedTheme_OnlyProvidesAnOverridableVariantDefault()
    {
        Assert.True(
            Application.Current!.TryFindResource("MaterialOutlinedButtonTheme", out var resource));

        var button = new Button
        {
            Content = "Press",
            Theme = Assert.IsType<ControlTheme>(resource)
        };
        ThemeTestHelper.ShowWindow(button);

        Assert.Equal(ButtonVariant.Outlined, ButtonAssist.GetVariant(button));

        ButtonAssist.SetVariant(button, ButtonVariant.Tonal);

        Assert.Equal(ButtonVariant.Tonal, ButtonAssist.GetVariant(button));
        Assert.Equal(0, button.BorderThickness.Left);
    }

    [AvaloniaFact]
    public void NativePropertyOverrides_AreNotDestroyedByTokenChanges()
    {
        var background = Brushes.HotPink;
        var button = new Button
        {
            Content = "Press",
            Background = background,
            Padding = new Thickness(3),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
        ThemeTestHelper.ShowWindow(button);

        ButtonAssist.SetVariant(button, ButtonVariant.Tonal);
        ButtonAssist.SetSize(button, ButtonSize.Large);

        Assert.Same(background, button.Background);
        Assert.Equal(new Thickness(3), button.Padding);
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, button.HorizontalAlignment);
    }

    [AvaloniaFact]
    public void TextAndOutlinedVariants_HaveExpectedContainerContracts()
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

    private static MaterialButtonContainerPresenter PresenterOf(Button button) =>
        Assert.Single(button.GetVisualDescendants().OfType<MaterialButtonContainerPresenter>());

    private static void PumpFrame(int milliseconds = 16)
    {
        System.Threading.Thread.Sleep(milliseconds);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    }
}
