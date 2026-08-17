using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
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

public class RippleAnimationTests
{
    [Fact]
    public void AQuickTap_CompletesTheWholeFadeInBeforeFadingOut()
    {
        var ripple = new RippleAnimation(new Point(8, 12), new Size(100, 40));

        ripple.Tick(TimeSpan.FromMilliseconds(20));
        Assert.InRange(ripple.Value, 0.26, 0.27);

        ripple.Finish();

        // AndroidX snaps the drawn alpha to one for a release during fade-in, but the underlying
        // radius and centre animations still get their complete 225ms.
        Assert.Equal(1, ripple.Value);
        ripple.Tick(TimeSpan.FromMilliseconds(204));
        Assert.Equal(1, ripple.Value);
        Assert.False(ripple.IsComplete);

        // Cross the boundary rather than landing on it: TimeSpan.TotalSeconds is a binary
        // floating-point value, so an exact 225ms can compare a fraction below one.
        ripple.Tick(TimeSpan.FromMilliseconds(2));
        Assert.Equal(1, ripple.Value);
        Assert.True(ripple.IsRunning);

        ripple.Tick(TimeSpan.FromMilliseconds(75));
        Assert.Equal(0.5, ripple.Value, 6);
        Assert.False(ripple.IsComplete);

        ripple.Tick(TimeSpan.FromMilliseconds(75));
        Assert.Equal(0, ripple.Value);
        Assert.True(ripple.IsComplete);
    }

    [Fact]
    public void ALongPress_HoldsAtFullExpansionUntilItIsFinished()
    {
        var ripple = new RippleAnimation(new Point(8, 12), new Size(100, 40));

        ripple.Tick(TimeSpan.FromMilliseconds(225));

        Assert.Equal(1, ripple.Value);
        Assert.False(ripple.IsRunning);
        Assert.False(ripple.IsComplete);

        ripple.Finish();
        Assert.True(ripple.IsRunning);

        ripple.Tick(TimeSpan.FromMilliseconds(150));
        Assert.True(ripple.IsComplete);
    }

    [Fact]
    public void AnUnboundedRipple_IsCentredAndHonoursItsExplicitRadius()
    {
        var ripple = new RippleAnimation(
            new Point(2, 3),
            new Size(80, 40),
            bounded: false,
            radius: 24);

        Assert.False(ripple.IsBounded);
        Assert.Equal(new Point(40, 20), ripple.Center);
        Assert.Equal(24, ripple.TargetRadius);

        ripple.Tick(TimeSpan.FromMilliseconds(100));

        Assert.Equal(new Point(40, 20), ripple.Center);
    }

    [Fact]
    public void AutomaticTargetRadius_FollowsMaterialThreeRules()
    {
        // The half diagonal of an 80x60 layout is 50.
        var bounded = new RippleAnimation(new Point(), new Size(80, 60));
        var unbounded = new RippleAnimation(new Point(), new Size(80, 60), bounded: false);

        Assert.Equal(60, bounded.TargetRadius);
        Assert.Equal(50, unbounded.TargetRadius);
    }
}

public class MaterialInteractionControllerRippleTests
{
    [Fact]
    public void RippleTheme_DefaultsTheFrameworkToTheMaterialThreeInsetFocusRing()
    {
        var theme = new MaterialTheme();

        Assert.Equal(RippleFocusIndication.InsetRing, theme.Resolved.Ripple.FocusIndication);

        var before = theme.Resolved;
        theme.RippleTheme.FocusIndication = RippleFocusIndication.Opacity;
        var opacity = theme.Resolved;

        Assert.NotSame(before, opacity);
        Assert.Equal(RippleFocusIndication.Opacity, opacity.Ripple.FocusIndication);
        Assert.Equal(0, before.Ripple.OuterStrokeInset);
        Assert.Equal(2, before.Ripple.OuterStrokeWidth);
        Assert.Equal(1, before.Ripple.InnerStrokeInset);
        Assert.Equal(3, before.Ripple.InnerStrokeWidth);
    }

    [AvaloniaFact]
    public void InsetFocus_DrawsTheTwoConfiguredThemeStrokes()
    {
        var theme = new MaterialTheme
        {
            RippleTheme = RippleThemeConfiguration.CreateInsetFocusRing()
        };
        var owner = new Border { Width = 80, Height = 40 };
        MaterialThemeScope.SetTheme(owner, theme);
        var interaction = MaterialElementContext.Ensure(owner).Interaction;
        var window = ThemeTestHelper.ShowWindow(owner);

        interaction.Set(InteractionState.Focused, true);
        interaction.Motion.SettleAll();

        var drawing = new DrawingGroup();
        using (var context = drawing.Open())
            interaction.DrawLayers(context, Brushes.Black, new CornerRadius(12));

        var strokes = drawing.Children
            .OfType<GeometryDrawing>()
            .Where(item => item.Pen is not null)
            .Select(item => item.Pen!)
            .ToArray();

        Assert.Equal(2, strokes.Length);
        Assert.Equal(3, strokes[0].Thickness);
        Assert.Equal(2, strokes[1].Thickness);
        Assert.Equal(
            theme.Resolved.ColorScheme!.GetColor(SysColorToken.OnSecondary, isDark: false),
            Assert.IsAssignableFrom<ISolidColorBrush>(strokes[0].Brush).Color);
        Assert.Equal(
            theme.Resolved.ColorScheme.GetColor(SysColorToken.Secondary, isDark: false),
            Assert.IsAssignableFrom<ISolidColorBrush>(strokes[1].Brush).Color);

        window.Close();
    }

    [AvaloniaFact]
    public void TheMostRecentlyEnteredNonPressInteraction_Wins()
    {
        var owner = new Border();
        MaterialThemeScope.SetTheme(
            owner,
            new MaterialTheme { RippleTheme = RippleThemeConfiguration.CreateOpacity() });
        var interaction = MaterialElementContext.Ensure(owner).Interaction;
        var window = ThemeTestHelper.ShowWindow(owner);
        interaction.Motion.UseManualClock();

        interaction.Set(InteractionState.Hovered, true);
        interaction.Motion.AdvanceBy(TimeSpan.FromMilliseconds(15));
        Assert.Equal(StateTokens.HoverStateLayerOpacity, interaction.StateLayerOpacity, 6);

        interaction.Set(InteractionState.Focused, true);
        interaction.Motion.AdvanceBy(TimeSpan.FromMilliseconds(45));
        Assert.Equal(StateTokens.FocusStateLayerOpacity, interaction.StateLayerOpacity, 6);

        // Removing an interaction below the top leaves focus in place.
        interaction.Set(InteractionState.Hovered, false);
        Assert.Equal(StateTokens.FocusStateLayerOpacity, interaction.StateLayerOpacity, 6);

        // A later hover entry now sits above focus, matching RippleNode's interactions.lastOrNull.
        interaction.Set(InteractionState.Hovered, true);
        interaction.Motion.AdvanceBy(TimeSpan.FromMilliseconds(15));
        Assert.Equal(StateTokens.HoverStateLayerOpacity, interaction.StateLayerOpacity, 6);

        window.Close();
    }

    [Fact]
    public void APerCallSiteIndicationSwitch_CanSuppressHover()
    {
        var owner = new Border();
        var interaction = MaterialElementContext.Ensure(owner).Interaction;
        interaction.IsHoverIndicationEnabled = false;
        interaction.Motion.UseManualClock();

        interaction.Set(InteractionState.Hovered, true);
        interaction.Motion.AdvanceBy(TimeSpan.FromMilliseconds(15));

        Assert.True(interaction.Interaction.HasFlag(InteractionState.Hovered));
        Assert.Equal(0, interaction.StateLayerOpacity);
    }

    [AvaloniaFact]
    public void StockButtonTemplate_UsesTheMaterialThreeRipplePresenter()
    {
        var button = new Button { Content = "Stock", Width = 120, Height = 40 };
        MaterialThemeScope.SetTheme(
            button,
            new MaterialTheme { RippleTheme = RippleThemeConfiguration.CreateOpacity() });
        var window = ThemeTestHelper.ShowWindow(button);

        var presenter = Assert.Single(
            button.GetVisualDescendants().OfType<MaterialRipplePresenter>());
        var interaction = MaterialElementContext.Ensure(button).Interaction;

        Assert.Same(button, presenter.TemplatedParent);
        Assert.True(presenter.IsBounded);
        Assert.True(double.IsNaN(presenter.RippleRadius));

        interaction.Set(InteractionState.Focused, true);
        interaction.Motion.SettleAll();
        Assert.Equal(StateTokens.FocusStateLayerOpacity, interaction.StateLayerOpacity);

        window.Close();
    }

    [AvaloniaFact]
    public void KeyboardActivation_StartsACentredRippleAndReleasesIt()
    {
        var button = new Button { Width = 80, Height = 40 };
        var window = ThemeTestHelper.ShowWindow(button);
        var interaction = MaterialElementContext.Ensure(button).Interaction;

        button.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Space
        });

        Assert.True(interaction.Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(1, interaction.DebugSnapshot.RippleCount);
        Assert.Equal(new Point(40, 20), interaction.DebugSnapshot.LastRippleCenter);

        button.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.Space
        });

        Assert.False(interaction.Interaction.HasFlag(InteractionState.Pressed));
        window.Close();
    }

    [Theory]
    [InlineData(typeof(MaterialCheckbox))]
    [InlineData(typeof(MaterialRadioButton))]
    [InlineData(typeof(MaterialSwitch))]
    public void SelectionControls_UseTheMaterialThreeUnboundedRipple(Type controlType)
    {
        var control = (Control)Activator.CreateInstance(controlType)!;
        var interaction = MaterialElementContext.Ensure(control).Interaction;

        Assert.False(interaction.IsRippleBounded);
        Assert.NotNull(interaction.RippleRadius);
        var expectedRadius = control switch
        {
            MaterialCheckbox checkbox => checkbox.GeometryProfile.StateLayerSize / 2,
            MaterialRadioButton => RadioButtonDefaults.Metrics().StateLayerSize / 2,
            MaterialSwitch => SwitchDefaults.Metrics().StateLayerSize / 2,
            _ => throw new InvalidOperationException()
        };
        Assert.Equal(expectedRadius, interaction.RippleRadius!.Value);
    }
}
