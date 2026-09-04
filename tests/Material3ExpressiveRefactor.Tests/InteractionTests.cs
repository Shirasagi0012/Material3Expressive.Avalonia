using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Material3ExpressiveRefactor.Foundation.Interaction;
using Material3ExpressiveRefactor.Foundation.Motion;
using Material3ExpressiveRefactor.Foundation.Rendering;
using Xunit;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// The state-layer rule table, ported from the old suite. These are pure functions over the
/// upstream material-ripple StateLayer behavior: which interaction shows, how opaque, how fast.
/// </summary>
public class StateLayerTests
{
    [Theory]
    [InlineData(InteractionState.Hovered, InteractionState.Hovered)]
    [InlineData(InteractionState.Hovered | InteractionState.Focused, InteractionState.Focused)]
    [InlineData(InteractionState.Hovered | InteractionState.Focused | InteractionState.Pressed, InteractionState.Pressed)]
    [InlineData(InteractionState.Focused | InteractionState.Dragged, InteractionState.Dragged)]
    [InlineData(InteractionState.None, InteractionState.None)]
    public void OnlyTheTopmostInteraction_IsShown(InteractionState state, InteractionState expected)
    {
        Assert.Equal(expected, StateLayer.Topmost(state));
    }

    [Fact]
    public void TheOpacities_AreTheUpstreamStateTokens()
    {
        Assert.Equal(0.08, StateLayer.Opacity(InteractionState.Hovered));
        Assert.Equal(0.10, StateLayer.Opacity(InteractionState.Focused));
        Assert.Equal(0.10, StateLayer.Opacity(InteractionState.Pressed));
        Assert.Equal(0.16, StateLayer.Opacity(InteractionState.Dragged));
        Assert.Equal(0, StateLayer.Opacity(InteractionState.None));
    }

    [Fact]
    public void TheFades_AreAsymmetricWhereUpstreamMakesThem()
    {
        // Focus arrives slowly and leaves quickly; a drag does the opposite.
        Assert.True(StateLayer.FadeInDuration(InteractionState.Focused)
                    > StateLayer.FadeOutDuration(InteractionState.Focused));
        Assert.True(StateLayer.FadeOutDuration(InteractionState.Dragged)
                    > StateLayer.FadeInDuration(InteractionState.Dragged));

        // Hover is symmetric.
        Assert.Equal(
            StateLayer.FadeInDuration(InteractionState.Hovered),
            StateLayer.FadeOutDuration(InteractionState.Hovered));
    }
}

/// <summary>The 8-bit alpha quantization that mirrors Compose's Color.copy(alpha = ...).</summary>
public class ComposeOpacityTests
{
    [Fact]
    public void Opacity_QuantizesToTheNearestByte()
    {
        Assert.Equal(20 / 255d, MaterialInteractionRendering.ComposeOpacity(0.08), 9);
        Assert.Equal(128 / 255d, MaterialInteractionRendering.ComposeOpacity(0.5), 9);
        Assert.Equal(1d, MaterialInteractionRendering.ComposeOpacity(1));
    }

    [Fact]
    public void Opacity_ClampsOutOfRangeInput()
    {
        Assert.Equal(0, MaterialInteractionRendering.ComposeOpacity(-0.5));
        Assert.Equal(1, MaterialInteractionRendering.ComposeOpacity(1.5));
        Assert.Equal(0, MaterialInteractionRendering.ComposeOpacity(double.NaN));
        Assert.Equal(0, MaterialInteractionRendering.ComposeOpacity(double.PositiveInfinity));
    }
}

/// <summary>
/// The ripple state machine, ported from the old suite: three parallel curves, fade-out gated on
/// release, and the quick-tap alpha snap. Pure animation ticks — no UI needed.
/// </summary>
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

/// <summary>
/// The controller against a real themed Button in a headless window. The test clock scope is
/// entered before ShowWindow so the controller created at template attach runs on manual time;
/// pointer input goes through the headless window's mouse injection.
/// </summary>
public class MaterialInteractionControllerTests
{
    /// <summary>Reads one opaque pixel from a captured frame, honoring its byte order.</summary>
    private static (int R, int G, int B) PixelAt(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var row = new byte[buffer.RowBytes];
        Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, buffer.RowBytes);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (row[x * 4], row[x * 4 + 1], row[x * 4 + 2])
            : (row[x * 4 + 2], row[x * 4 + 1], row[x * 4]);
    }

    [AvaloniaFact]
    public void TheMostRecentlyEnteredNonPressInteraction_Wins()
    {
        using var clock = new MotionTestClockScope();
        ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);

        controller.Set(InteractionState.Hovered, true);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0.08, controller.StateLayerOpacity, 6);

        controller.Set(InteractionState.Focused, true);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(46));
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);

        // Removing an interaction below the top leaves focus in place.
        controller.Set(InteractionState.Hovered, false);
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);

        // A later hover entry now sits above focus, matching RippleNode's interactions.lastOrNull.
        controller.Set(InteractionState.Hovered, true);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0.08, controller.StateLayerOpacity, 6);
    }

    [AvaloniaFact]
    public void PointerHover_FadesTheStateLayerIn()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);

        var origin = button.TranslatePoint(default, window)!.Value;
        window.MouseMove(origin + new Point(button.Bounds.Width / 2, button.Bounds.Height / 2),
            RawInputModifiers.None);

        Assert.True(controller.Interaction.HasFlag(InteractionState.Hovered));

        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0.08, controller.StateLayerOpacity, 6);

        window.MouseMove(new Point(1, 1), RawInputModifiers.None);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, controller.StateLayerOpacity, 6);
    }

    [AvaloniaFact]
    public void PointerPress_RaisesARippleThatReleaseFadesOut()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);

        var origin = button.TranslatePoint(default, window)!.Value;
        var point = origin + new Point(12, button.Bounds.Height / 2);

        window.MouseDown(point, MouseButton.Left);

        Assert.True(controller.Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(1, controller.RippleCount);
        // A pointer-focus must not light the focus layer; Material shows it for keyboard focus.
        Assert.False(controller.Interaction.HasFlag(InteractionState.Focused));

        clock.AdvanceBy(TimeSpan.FromMilliseconds(225));
        Assert.Equal(1, controller.RippleCount); // held at full expansion while pressed

        window.MouseUp(point, MouseButton.Left);
        Assert.False(controller.Interaction.HasFlag(InteractionState.Pressed));

        clock.AdvanceBy(TimeSpan.FromMilliseconds(200));
        Assert.Equal(0, controller.RippleCount);
    }

    [AvaloniaFact]
    public void KeyboardActivation_StartsARippleAndReleasesIt()
    {
        using var clock = new MotionTestClockScope();
        ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);

        button.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Space,
        });

        Assert.True(controller.Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(1, controller.RippleCount);

        button.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyUpEvent,
            Key = Key.Space,
        });

        Assert.False(controller.Interaction.HasFlag(InteractionState.Pressed));
    }

    [AvaloniaFact]
    public void KeyboardFocus_ShowsTheFocusLayer()
    {
        using var clock = new MotionTestClockScope();
        ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);

        Assert.True(button.Focus(NavigationMethod.Tab));
        Assert.True(controller.Interaction.HasFlag(InteractionState.Focused));

        clock.AdvanceBy(TimeSpan.FromMilliseconds(46));
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);
    }

    [AvaloniaFact]
    public void Press_PaintsRippleInkOverTheContainer()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);

        var origin = button.TranslatePoint(default, window)!.Value;
        // Left of the label glyphs: the ink lightens the filled container, and sampling must not
        // land on the (already content-colored) text.
        var px = (int)(origin.X + 12);
        var py = (int)(origin.Y + button.Bounds.Height / 2);

        using var rest = window.CaptureRenderedFrame()!;
        var restPixel = PixelAt(rest, px, py);

        window.MouseDown(new Point(px, py), MouseButton.Left);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(100));

        Assert.Equal(1, controller.RippleCount);

        using var pressed = window.CaptureRenderedFrame()!;
        var pressedPixel = PixelAt(pressed, px, py);

        // Filled button: the ink is the (near-white) content color at pressed opacity 0.10.
        Assert.True(pressedPixel.R > restPixel.R,
            $"expected ripple ink over the container: rest {restPixel} vs pressed {pressedPixel}");
    }

    [AvaloniaFact]
    public void Press_SpringsTheContainerShapeToThePressedToken()
    {
        using var clock = new MotionTestClockScope();
        var window = IconButtonThemeFixture.ShowWindow(out var button);
        var container = button.GetVisualDescendants().OfType<MaterialSurface>().First(b => b.Name == "PART_Container");

        // No press yet: the styled stadium token passes through untouched.
        Assert.Equal(new CornerRadius(1000), container.EffectiveCornerRadius);

        var origin = button.TranslatePoint(default, window)!.Value;
        var point = origin + new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
        window.MouseDown(point, MouseButton.Left);

        // The morph starts at the clamped half-pill (20 on a 40dp button), not the raw 1000.
        Assert.Equal(20, container.EffectiveCornerRadius.TopLeft, 6);

        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.InRange(container.EffectiveCornerRadius.TopLeft, 8 + 1e-6, 20 - 1e-6);

        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(8, container.EffectiveCornerRadius.TopLeft, 6); // md.comp.icon-button.small.pressed-container-shape = sys.shape.small

        window.MouseUp(point, MouseButton.Left);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(20, container.EffectiveCornerRadius.TopLeft, 6);
    }
}

/// <summary>
/// The press shape-morph, ported from the old AnimatedShape suite with CornerRadius operands.
/// The extra pin versus the old tests: the stadium endpoint clamps to the half-pill BEFORE the
/// interpolation runs, or CornerRadius(1000) would stay visually round until the last instant.
/// </summary>
public class AnimatedCornerRadiusTests
{
    private static readonly CornerRadius Stadium = new(1000);
    private static readonly CornerRadius Small = new(8);
    private static readonly CornerRadius Boxy = new(4);
    private static readonly SpringSpec Spec = MotionSpecs.Resolve(null, MotionRole.DefaultEffects);

    [Fact]
    public void ItStartsAtRestOnItsInitialShape()
    {
        var morph = new AnimatedCornerRadius(Small, Spec);

        Assert.False(morph.IsRunning);
        Assert.Equal(Small, morph.Current);
    }

    [Fact]
    public void TheStadiumShape_ClampsToTheHalfPillBeforeInterpolating()
    {
        var morph = new AnimatedCornerRadius(Stadium, Spec) { Size = new Size(40, 40) };
        Assert.Equal(new CornerRadius(20), morph.Current);

        morph.AnimateTo(Small);
        morph.Progress.Tick(TimeSpan.FromMilliseconds(16));

        var radius = morph.Current.TopLeft;
        Assert.True(radius is > 8 and < 20, $"expected an honest 20→8 morph, got {radius}");
    }

    [Fact]
    public void ItMorphsToANewShapeAndArrives()
    {
        var morph = new AnimatedCornerRadius(Stadium, Spec) { Size = new Size(40, 40) };
        morph.AnimateTo(Small);

        Assert.True(morph.IsRunning);

        while (morph.IsRunning)
            morph.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        Assert.Equal(Small, morph.Current);
    }

    [Fact]
    public void TheVisibleShape_DoesNotJumpWhenTheMorphIsReversed()
    {
        var morph = new AnimatedCornerRadius(Stadium, Spec) { Size = new Size(40, 40) };
        morph.AnimateTo(Small);

        for (var i = 0; i < 3; i++)
            morph.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        var before = morph.Current;
        morph.AnimateTo(Stadium);
        var after = morph.Current;

        Assert.Equal(before.TopLeft, after.TopLeft, 9);
    }

    [Fact]
    public void AReversal_KeepsItsMomentum()
    {
        var morph = new AnimatedCornerRadius(Stadium, Spec) { Size = new Size(40, 40) };
        morph.AnimateTo(Small);

        for (var i = 0; i < 3; i++)
            morph.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        morph.AnimateTo(Stadium);
        morph.Progress.Tick(TimeSpan.FromMilliseconds(1));

        // Flipping progress means the velocity has to flip too, or the morph would stall and
        // restart. One millisecond in, it should still be heading the way it was.
        Assert.True(morph.Progress.Value < 1.0);
        Assert.True(morph.IsRunning);
    }

    [Fact]
    public void RepeatedInterruption_StaysWithinTheEndpointRange()
    {
        var morph = new AnimatedCornerRadius(Stadium, Spec) { Size = new Size(40, 40) };
        var targets = new[] { Small, Boxy, Stadium };

        for (var i = 0; i < 2000; i++)
        {
            morph.AnimateTo(targets[i % targets.Length]);
            morph.Progress.Tick(TimeSpan.FromMilliseconds(2));
        }

        // Freezing the visible corners mid-flight must not accumulate error: the value stays
        // inside the clamped endpoint range no matter how often the target changes.
        var current = morph.Current;
        Assert.True(double.IsFinite(current.TopLeft));
        Assert.InRange(current.TopLeft, 4 - 1e-9, 20 + 1e-9);
    }

    [Fact]
    public void AnimatingToTheCurrentTarget_IsANoOp()
    {
        var morph = new AnimatedCornerRadius(Stadium, Spec) { Size = new Size(40, 40) };
        morph.AnimateTo(Small);
        morph.Progress.Tick(TimeSpan.FromSeconds(1.0 / 60));

        var progress = morph.Progress.Value;
        morph.AnimateTo(Small);

        Assert.Equal(progress, morph.Progress.Value);
    }
}
