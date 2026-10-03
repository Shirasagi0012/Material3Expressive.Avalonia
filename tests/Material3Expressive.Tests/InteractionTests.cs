using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

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
/// release, and the quick-tap alpha snap, driven by the packaged runtime on a manual frame clock.
/// </summary>
public class RippleAnimationTests
{
    [AvaloniaFact]
    public void AQuickTap_CompletesTheWholeFadeInBeforeFadingOut()
    {
        using var clock = new MotionTestClockScope();
        using var ripple = new RippleAnimation(new Point(8, 12), new Size(100, 40), clock.Scope);
        clock.Track(ripple.AnimateAsync());

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(20));
        Assert.InRange(ripple.Value, 0.26, 0.27);

        ripple.Finish();

        // AndroidX snaps the drawn alpha to one for a release during fade-in, but the underlying
        // radius and centre animations still get their complete 225ms.
        Assert.Equal(1, ripple.Value);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(204));
        Assert.Equal(1, ripple.Value);
        Assert.False(ripple.IsComplete);

        // At exactly 225ms the expansion completes; fade-out begins on its next frame.
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Equal(1, ripple.Value);
        Assert.True(ripple.IsRunning);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(75));
        Assert.Equal(0.5, ripple.Value, 6);
        Assert.False(ripple.IsComplete);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(75));
        Assert.Equal(0, ripple.Value);
        Assert.True(ripple.IsComplete);
    }

    [AvaloniaFact]
    public void ALongPress_HoldsAtFullExpansionUntilItIsFinished()
    {
        using var clock = new MotionTestClockScope();
        using var ripple = new RippleAnimation(new Point(8, 12), new Size(100, 40), clock.Scope);
        clock.Track(ripple.AnimateAsync());

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(225));

        Assert.Equal(1, ripple.Value);
        Assert.False(ripple.IsRunning);
        Assert.False(ripple.IsComplete);

        ripple.Finish();
        Assert.True(ripple.IsRunning);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(150));
        Assert.True(ripple.IsComplete);
    }

    [AvaloniaFact]
    public void AnUnboundedRipple_IsCentredAndHonoursItsExplicitRadius()
    {
        using var clock = new MotionTestClockScope();
        using var ripple = new RippleAnimation(
            new Point(2, 3),
            new Size(80, 40),
            clock.Scope,
            bounded: false,
            radius: 24);

        clock.Track(ripple.AnimateAsync());
        Assert.False(ripple.IsBounded);
        Assert.Equal(new Point(40, 20), ripple.Center);
        Assert.Equal(24, ripple.TargetRadius);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(100));

        Assert.Equal(new Point(40, 20), ripple.Center);
    }

    [AvaloniaFact]
    public void AutomaticTargetRadius_FollowsMaterialThreeRules()
    {
        using var clock = new MotionTestClockScope();
        // The half diagonal of an 80x60 layout is 50.
        using var bounded = new RippleAnimation(new Point(), new Size(80, 60), clock.Scope);
        using var unbounded = new RippleAnimation(new Point(), new Size(80, 60), clock.Scope, bounded: false);

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
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0.08, controller.StateLayerOpacity, 6);

        controller.Set(InteractionState.Focused, true);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(46));
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);

        // Removing an interaction below the top leaves focus in place.
        controller.Set(InteractionState.Hovered, false);
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);

        // A later hover entry now sits above focus, matching RippleNode's interactions.lastOrNull.
        controller.Set(InteractionState.Hovered, true);
        clock.SendFrame(clock.Now);
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

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0.08, controller.StateLayerOpacity, 6);

        window.MouseMove(new Point(1, 1), RawInputModifiers.None);
        clock.SendFrame(clock.Now);
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
        // Core Desktop emits FocusInteraction for pointer focus as well.
        Assert.True(controller.Interaction.HasFlag(InteractionState.Focused));
        Assert.Equal(InteractionState.Pressed, controller.LastInteraction);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(225));
        Assert.Equal(1, controller.RippleCount); // held at full expansion while pressed

        window.MouseUp(point, MouseButton.Left);
        Assert.False(controller.Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(InteractionState.Focused, controller.LastInteraction);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(200));
        Assert.Equal(0, controller.RippleCount);
        // Desktop filters mouse Focus only from the indication source, retaining hover ink.
        Assert.Equal(0.08, controller.StateLayerOpacity, 6);
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

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(46));
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);
    }

    [AvaloniaFact]
    public void KeyboardRipple_StartsAtClickableZeroUntilPointerBoundsAreObserved()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        button.Focus(NavigationMethod.Tab);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
        var ripples = (List<RippleAnimation>)typeof(MaterialInteractionController)
            .GetField("_ripples", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(controller)!;
        Assert.Equal(default, Assert.Single(ripples).Center);

        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
        var point = button.TranslatePoint(new Rect(button.Bounds.Size).Center, window)!.Value;
        window.MouseMove(point, RawInputModifiers.None);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(new Rect(surface.Bounds.Size).Center, ripples.Last().Center);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
    }

    [AvaloniaFact]
    public void KeyboardFocusIndication_RemainsUntilUnfocusAfterMouseInput()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);
        var origin = button.TranslatePoint(default, window)!.Value;
        var point = origin + new Point(12, button.Bounds.Height / 2);

        window.MouseMove(point, RawInputModifiers.None);
        Assert.True(button.Focus(NavigationMethod.Tab));
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        window.MouseMove(new Point(1, 1), RawInputModifiers.None);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));

        Assert.Equal(InteractionState.Focused, controller.LastInteraction);
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);
        window.FocusManager.Focus(null);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, controller.StateLayerOpacity, 6);
    }

    [AvaloniaFact]
    public void MouseFocusIndication_IsNotSynthesizedWhenKeyboardModeChanges()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        var controller = MaterialInteraction.GetOrCreate(button);
        var origin = button.TranslatePoint(default, window)!.Value;
        var point = origin + new Point(12, button.Bounds.Height / 2);

        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        window.MouseMove(new Point(1, 1), RawInputModifiers.None);
        // A keyboard navigation request which keeps the current focus does not emit Focus.
        Assert.True(button.Focus(NavigationMethod.Tab));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));

        Assert.Equal(InteractionState.Focused, controller.LastInteraction);
        Assert.Equal(0, controller.StateLayerOpacity, 6);
        window.FocusManager.Focus(null);
        Assert.True(button.Focus(NavigationMethod.Tab));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);
    }

    [AvaloniaFact]
    public void ProgrammaticFocus_UsesTheWindowsInputMode()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var first);
        var second = new Button { Content = "Second" };
        window.Content = null;
        Dispatcher.UIThread.RunJobs();
        window.Content = new StackPanel { Children = { first, second } };
        Dispatcher.UIThread.RunJobs();
        var controller = MaterialInteraction.GetOrCreate(second);

        Assert.True(second.Focus());
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
        Assert.Equal(0.10, controller.StateLayerOpacity, 6);

        var point = first.TranslatePoint(new Point(12, first.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.True(second.Focus());
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.True(controller.Interaction.HasFlag(InteractionState.Focused));
        Assert.Equal(0, controller.StateLayerOpacity, 6);
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
        clock.SendFrame(clock.Now);
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
        var window = IconButtonThemeFixture.ShowWindow(out var button, pressedCornerRadius: new CornerRadius(8));
        var container = button.GetVisualDescendants().OfType<MaterialSurface>().First(b => b.Name == "PART_Container");

        // AnimatedShape clamps its initial stadium to the measured half extent.
        Assert.Equal(new CornerRadius(20), container.EffectiveCornerRadius);

        var origin = button.TranslatePoint(default, window)!.Value;
        var point = origin + new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
        window.MouseDown(point, MouseButton.Left);

        // The morph starts at the clamped half-pill (20 on a 40dp button), not the raw 1000.
        Assert.Equal(20, container.EffectiveCornerRadius.TopLeft, 6);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(20, container.EffectiveCornerRadius.TopLeft, 6); // first animation frame
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.InRange(container.EffectiveCornerRadius.TopLeft, 8 + 1e-6, 20 - 1e-6);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(8, container.EffectiveCornerRadius.TopLeft, 6); // md.comp.icon-button.small.pressed-container-shape = sys.shape.small

        window.MouseUp(point, MouseButton.Left);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(20, container.EffectiveCornerRadius.TopLeft, 6);
    }

    [AvaloniaFact]
    public void CompletedShapeMorph_ReclampsWhenContainerIsResized()
    {
        using var clock = new MotionTestClockScope();
        var window = IconButtonThemeFixture.ShowWindow(out var button, pressedCornerRadius: new CornerRadius(8));
        var container = button.GetVisualDescendants().OfType<MaterialSurface>().First(b => b.Name == "PART_Container");

        var origin = button.TranslatePoint(default, window)!.Value;
        var point = origin + new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
        window.MouseDown(point, MouseButton.Left);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        window.MouseUp(point, MouseButton.Left);
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(20, container.EffectiveCornerRadius.TopLeft, 6);

        button.Width = 56;
        button.Height = 56;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Size(56, 56), container.Bounds.Size);
        Assert.Equal(28, container.EffectiveCornerRadius.TopLeft, 6);
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
    private static readonly SpringSpec Spec = MotionScheme.Expressive.DefaultEffects;

    [AvaloniaFact]
    public void ItStartsAtRestOnItsInitialShape()
    {
        using var clock = new MotionTestClockScope();
        using var morph = new AnimatedCornerRadius(Small, Spec, clock.Scope);

        Assert.False(morph.IsRunning);
        Assert.Equal(Small, morph.Current);
    }

    [AvaloniaFact]
    public void TheStadiumShape_ClampsToTheHalfPillBeforeInterpolating()
    {
        using var clock = new MotionTestClockScope();
        using var morph = new AnimatedCornerRadius(Stadium, Spec, clock.Scope) { Size = new Size(40, 40) };
        Assert.Equal(new CornerRadius(20), morph.Current);

        clock.Track(morph.AnimateToAsync(Small));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));

        var radius = morph.Current.TopLeft;
        Assert.True(radius is > 8 and < 20, $"expected an honest 20→8 morph, got {radius}");
    }

    [AvaloniaFact]
    public void ItMorphsToANewShapeAndArrives()
    {
        using var clock = new MotionTestClockScope();
        using var morph = new AnimatedCornerRadius(Stadium, Spec, clock.Scope) { Size = new Size(40, 40) };
        clock.Track(morph.AnimateToAsync(Small));

        Assert.True(morph.IsRunning);

        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromSeconds(1));

        Assert.False(morph.IsRunning);
        Assert.Equal(Small, morph.Current);
    }

    [AvaloniaFact]
    public void TheVisibleShape_DoesNotJumpWhenTheMorphIsReversed()
    {
        using var clock = new MotionTestClockScope();
        using var morph = new AnimatedCornerRadius(Stadium, Spec, clock.Scope) { Size = new Size(40, 40) };
        clock.Track(morph.AnimateToAsync(Small));

        for (var i = 0; i < 3; i++)
        {
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromSeconds(1.0 / 60));
        }

        var before = morph.Current;
        clock.Track(morph.AnimateToAsync(Stadium));
        var after = morph.Current;

        Assert.Equal(before.TopLeft, after.TopLeft, 9);
    }

    [AvaloniaFact]
    public void AReversal_KeepsItsMomentum()
    {
        using var clock = new MotionTestClockScope();
        using var morph = new AnimatedCornerRadius(Stadium, Spec, clock.Scope) { Size = new Size(40, 40) };
        clock.Track(morph.AnimateToAsync(Small));

        for (var i = 0; i < 3; i++)
        {
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromSeconds(1.0 / 60));
        }

        var radiusBeforeReversal = morph.Current.TopLeft;
        clock.Track(morph.AnimateToAsync(Stadium));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(1));

        // Flipping progress means the velocity has to flip too, or the morph would stall and
        // restart. One millisecond in, it should still be heading the way it was.
        Assert.True(morph.Current.TopLeft < radiusBeforeReversal);
        Assert.True(morph.IsRunning);
    }

    [AvaloniaFact]
    public void RepeatedInterruption_StaysWithinTheEndpointRange()
    {
        using var clock = new MotionTestClockScope();
        using var morph = new AnimatedCornerRadius(Stadium, Spec, clock.Scope) { Size = new Size(40, 40) };
        var targets = new[] { Small, Boxy, Stadium };

        for (var i = 0; i < 2000; i++)
        {
            clock.Track(morph.AnimateToAsync(targets[i % targets.Length]));
            clock.SendFrame(clock.Now);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(2));
        }

        // Freezing the visible corners mid-flight must not accumulate error: the value stays
        // inside the clamped endpoint range no matter how often the target changes.
        var current = morph.Current;
        Assert.True(double.IsFinite(current.TopLeft));
        Assert.InRange(current.TopLeft, 4 - 1e-9, 20 + 1e-9);
    }

    [AvaloniaFact]
    public void AnimatingToTheCurrentTarget_IsANoOp()
    {
        using var clock = new MotionTestClockScope();
        using var morph = new AnimatedCornerRadius(Stadium, Spec, clock.Scope) { Size = new Size(40, 40) };
        clock.Track(morph.AnimateToAsync(Small));
        clock.SendFrame(clock.Now);
        clock.AdvanceBy(TimeSpan.FromSeconds(1.0 / 60));

        var progress = morph.Progress;
        clock.Track(morph.AnimateToAsync(Small));

        Assert.Equal(progress, morph.Progress);
    }
}
