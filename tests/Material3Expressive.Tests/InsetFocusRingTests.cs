using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class InsetFocusRingTests
{
    [AvaloniaFact]
    public void ListItemShapeRetargetRefreshesCachedRingPaletteBeforeOutlineMoves()
    {
        using var clock = new MotionTestClockScope();
        var item = new ListItem { Width = 180, InteractionMode = ListItemInteractionMode.Click,
            Content = new Border { Width = 32, Height = 16 } };
        MaterialInteraction.SetFocusFeedback(item, FocusFeedback.InsetRing);
        var window = DialogTests.Show(item);
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        (int R, int G, int B) TopPixel()
        {
            var p = item.TranslatePoint(new Point(item.Bounds.Width / 2, 0), window)!.Value;
            using var frame = window.CaptureRenderedFrame()!;
            return PixelAt(frame, (int)p.X, (int)p.Y);
        }
        try
        {
            window.Resources[Sys.Color.Secondary] = Colors.Red;
            item.Focus(NavigationMethod.Tab); Frames(60);
            Assert.Equal((255, 0, 0), TopPixel());
            window.Resources[Sys.Color.Secondary] = Colors.Blue; Frames(2);
            Assert.Equal((255, 0, 0), TopPixel()); // Palette changes alone retain the cached draw.
            var ring = Assert.Single(item.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            var corners = ring.Corners;
            window.FocusManager!.Focus(null); Frames(1);
            Assert.Equal(corners, ring.Corners); Assert.Equal(1f, ring.Progress);
            Assert.Equal((0, 0, 255), TopPixel()); // Shape progress snapped 1 -> 0 with the same visible outline.
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ListItemModeChangePreservesRippleAndTracksAnimatedOutline(bool segmented, bool startWithRing)
    {
        using var clock = new MotionTestClockScope();
        ListItem item = segmented ? new SegmentedListItem { GroupIndex = 0, GroupCount = 3 } : new ListItem();
        item.Width = 180; item.InteractionMode = ListItemInteractionMode.Click;
        item.Content = new Border { Width = 32, Height = 16, Background = Brushes.Blue };
        var source = new MaterialInteractionSource(); item.InteractionSource = source;
        MaterialInteraction.SetFocusFeedback(item, startWithRing ? FocusFeedback.InsetRing : FocusFeedback.Opacity);
        var window = DialogTests.Show(item);
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        try
        {
            var controller = MaterialInteraction.GetOrCreate(item);
            var press = new MaterialInteractionEvent.Press(new Point(90, 28));
            source.Emit(new MaterialInteractionEvent.HoverEnter()); source.Emit(press); Frames(4);
            var ring = Assert.Single(item.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            var surface = item.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(1, controller.RippleCount);
            MaterialInteraction.SetFocusFeedback(item, startWithRing ? FocusFeedback.Opacity : FocusFeedback.InsetRing);
            Frames(2);
            Assert.Same(ring, Assert.Single(item.GetVisualDescendants().OfType<InsetFocusRingPresenter>()));
            Assert.Equal(1, controller.RippleCount);
            Assert.Equal(surface.EffectiveCornerRadius, ring.Corners);
            source.Emit(new MaterialInteractionEvent.CancelPress(press)); Frames(40);
            Assert.Equal(0, controller.RippleCount);
            MaterialInteraction.SetFocusFeedback(item, FocusFeedback.InsetRing);
            Assert.True(item.Focus(NavigationMethod.Tab)); Frames(60);
            Assert.Equal(1f, ring.Progress);
            Assert.Equal(surface.EffectiveCornerRadius, ring.Corners);
            window.Content = null; Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, clock.PendingFrameCount);
            window.Content = item; window.UpdateLayout();
            Assert.True(item.Focus(NavigationMethod.Tab)); Frames(60);
            ring = Assert.Single(item.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            Assert.Equal(1f, ring.Progress);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DisabledThenUnmounted_CanFocusAfterRemount(bool useSource, bool sendFrameBeforeUnmount)
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout();
            if (useSource) MaterialInteraction.GetOrCreate(button).ConnectSource(new MaterialInteractionSource());
            button.Focus(NavigationMethod.Pointer);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            button.IsEnabled = false;
            if (sendFrameBeforeUnmount) { clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
            window.Content = null;
            button.IsEnabled = true;
            window.Content = button;
            window.UpdateLayout();
            Assert.True(button.Focus(NavigationMethod.Tab));
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            var ring = Assert.Single(button.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            Assert.Equal(1f, ring.Progress);
            window.FocusManager!.Focus(null);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(0f, ring.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NewCollector_DoesNotInheritOldFilteredFocus(bool resetExistingIndication)
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            if (resetExistingIndication) MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout();
            var controller = MaterialInteraction.GetOrCreate(button);
            var source = new MaterialInteractionSource();
            controller.ConnectSource(source);
            MaterialInputMode.For(button)!.ObserveFocusNavigation(NavigationMethod.Pointer);
            source.Emit(new MaterialInteractionEvent.Focus());
            if (resetExistingIndication) controller.ResetSourceIndication();
            else MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout();
            MaterialInputMode.For(button)!.ObserveFocusNavigation(NavigationMethod.Tab);
            var focus = new MaterialInteractionEvent.Focus();
            source.Emit(focus);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            var ring = Assert.Single(button.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            Assert.Equal(1f, ring.Progress);
            source.Emit(new MaterialInteractionEvent.Unfocus(focus));
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(0f, ring.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SelectionControlModeChange_KeepsPressRippleAndHover(bool radio, bool startWithRing)
    {
        using var clock = new MotionTestClockScope();
        Button control = radio ? new RadioButton() : new CheckBox();
        if (startWithRing) MaterialInteraction.SetFocusFeedback(control, FocusFeedback.InsetRing);
        var window = new MotionTestWindow { Width = 160, Height = 100 };
        window.Styles.Add(new MaterialTheme());
        window.Content = control; window.Show(); window.UpdateLayout();
        try
        {
            var controller = MaterialInteraction.GetOrCreate(control);
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
            window.MouseMove(center);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            var hover = controller.StateLayerOpacity;
            Assert.True(hover > 0);
            window.MouseDown(center, MouseButton.Left);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(48));
            Assert.Equal(1, controller.RippleCount);
            MaterialInteraction.SetFocusFeedback(control, startWithRing ? FocusFeedback.Opacity : FocusFeedback.InsetRing);
            Assert.Equal(1, controller.RippleCount);
            Assert.Equal(hover, controller.StateLayerOpacity);
            window.MouseUp(center, MouseButton.Left);
            clock.SendFrame(clock.Now);
            for (var frame = 0; frame < 64; frame++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(0, controller.RippleCount);
            Assert.Equal(hover, controller.StateLayerOpacity);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionControlModeChange_PreservesFocusCollectorAndProgress(bool radio)
    {
        using var clock = new MotionTestClockScope();
        Button control = radio ? new RadioButton() : new CheckBox();
        MaterialInteraction.SetFocusFeedback(control, FocusFeedback.InsetRing);
        var window = new MotionTestWindow { Width = 160, Height = 100 };
        window.Styles.Add(new MaterialTheme());
        window.Content = control; window.Show(); window.UpdateLayout();
        try
        {
            control.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            var ring = Assert.Single(control.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            Assert.Equal(1f, ring.Progress);
            MaterialInteraction.SetFocusFeedback(control, FocusFeedback.Opacity);
            MaterialInteraction.SetFocusFeedback(control, FocusFeedback.InsetRing);
            Assert.Equal(1f, ring.Progress);
            MaterialInteraction.SetFocusFeedback(control, FocusFeedback.Opacity);
            window.FocusManager!.Focus(null);
            Assert.Equal(0f, ring.Progress);
            MaterialInteraction.SetFocusFeedback(control, FocusFeedback.InsetRing);
            control.Focus(NavigationMethod.Tab);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1f, ring.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Ring_FollowsThemeResourcesAndTheCurrentAnimatedCorners()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, pressedCornerRadius: new CornerRadius(6, 12, 4, 8));
        try
        {
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout();
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var ring = surface.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single();
            button.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ButtonThemeFixture.SchemeColor(true, s => s.Secondary), ring.DefaultOuterColor);
            window.Resources[Sys.Color.Secondary] = Colors.Red;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.Red, ring.DefaultOuterColor);
            var beforePress = ring.Corners;
            var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseMove(center); window.MouseDown(center, MouseButton.Left);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(64));
            Assert.Equal(surface.EffectiveCornerRadius, ring.Corners);
            Assert.NotEqual(beforePress, ring.Corners);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    public void AsymmetricRing_CoversOrdinaryBorder_AndRoundsPhysicalStroke(double scale)
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Width = 100, Height = 50, Padding = new Thickness(0), Background = Brushes.White,
            BorderBrush = Brushes.Blue, BorderThickness = new Thickness(1), CornerRadius = new(24, 8, 0, 16) };
        MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
        MaterialInteraction.SetFocusRingOuterColor(button, Colors.Red);
        MaterialInteraction.SetFocusRingInnerColor(button, Colors.Lime);
        var window = new MotionTestWindow { Width = 200, Height = 140, Background = Brushes.White };
        window.Styles.Add(new MaterialTheme());
        window.Content = new Border { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Child = button };
        window.SetRenderScaling(scale); window.Show(); window.UpdateLayout();
        try
        {
            button.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var point = surface.TranslatePoint(default, window)!.Value;
            var x = (int)Math.Round((point.X + surface.Bounds.Width / 2) * scale);
            var y = (int)Math.Round(point.Y * scale);
            using var frame = window.CaptureRenderedFrame()!;
            var outerWidth = (int)Math.Ceiling(2 * scale);
            var innerEnd = (int)(Math.Ceiling(scale) + Math.Ceiling(3 * scale));
            for (var row = 0; row < outerWidth; row++) Assert.Equal((255, 0, 0), PixelAt(frame, x, y + row));
            for (var row = outerWidth; row < innerEnd; row++) Assert.Equal((0, 255, 0), PixelAt(frame, x, y + row));
            Assert.Equal((255, 255, 255), PixelAt(frame, x, y + innerEnd));
            Assert.Equal(new CornerRadius(24, 8, 0, 16), surface.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single().Corners);
        }
        finally { window.Close(); }
    }

    private static (int R, int G, int B) PixelAt(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var row = new byte[buffer.RowBytes];
        Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, row.Length);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (row[x * 4], row[x * 4 + 1], row[x * 4 + 2])
            : (row[x * 4 + 2], row[x * 4 + 1], row[x * 4]);
    }

    [AvaloniaFact]
    public void InheritedMode_UsesThemeAndSuppressesOnlyFocusOpacity()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            MaterialInteraction.SetFocusFeedback(window, FocusFeedback.InsetRing);
            window.UpdateLayout();
            var ring = Assert.Single(button.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            Assert.NotNull(ring.FocusingAnimation);
            Assert.NotNull(ring.UnfocusingAnimation);
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => s.Secondary), ring.DefaultOuterColor);
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => s.OnSecondary), ring.DefaultInnerColor);
            button.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(96));
            Assert.True(ring.Progress > 0);
            Assert.Equal(0, MaterialInteraction.GetOrCreate(button).StateLayerOpacity);
            clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1f, ring.Progress);
            Assert.False(ring.IsHitTestVisible);
            Assert.DoesNotContain(ring, ((Avalonia.LogicalTree.ILogical)ring.GetVisualParent()!).LogicalChildren);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PointerFocus_IsFiltered_WhileKeyboardFocusSurvivesPointerMode()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout();
            var ring = button.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single();
            button.Focus(NavigationMethod.Pointer);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(0, ring.Progress);
            window.FocusManager!.Focus(null);
            button.Focus(NavigationMethod.Tab);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1, ring.Progress);
            var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseMove(center); window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1, ring.Progress);
            // Remove the later hover before blurring: RippleNode intentionally gates ring
            // retargeting on a change of the last surviving interaction (tested in parity).
            window.MouseMove(new Point(-10, -10));
            window.FocusManager.Focus(null);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.InRange(ring.Progress, 0.001f, 0.999f);
            clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(0, ring.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RapidRefocus_PreservesProgress_AndUnmountCancelsOwnedAnimation()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button, pressedCornerRadius: new CornerRadius(6));
        try
        {
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout();
            var ring = button.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single();
            button.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromMilliseconds(48));
            var before = ring.Progress;
            window.FocusManager!.Focus(null);
            Assert.Equal(before, ring.Progress);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            before = ring.Progress;
            button.Focus(NavigationMethod.Tab);
            Assert.Equal(before, ring.Progress);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.Content = null;
            Assert.Equal(0, ring.Progress);
            clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Null(MotionTestClockScope.Error(window));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SourceFocus_IsPaired_AndRecreatedCollectorMatchesCoreOldEndBehavior()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout();
            var controller = MaterialInteraction.GetOrCreate(button);
            var ring = button.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single();
            var source = new MaterialInteractionSource(); controller.ConnectSource(source);
            var a = new MaterialInteractionEvent.Focus(); var b = new MaterialInteractionEvent.Focus();
            source.Emit(a); source.Emit(b);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            source.Emit(new MaterialInteractionEvent.Unfocus(a));
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1, ring.Progress);
            controller.ResetSourceIndication();
            Assert.Equal(0, ring.Progress);
            source.Emit(new MaterialInteractionEvent.Unfocus(b));
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(0, ring.Progress);
            // Fixed Core's per-collector counter becomes -1 on this unmatched old end.
            // The first new Focus only restores it to zero; the second is forwarded.
            // The real Compose source-old-end parity case verifies this upstream behavior.
            source.Emit(new MaterialInteractionEvent.Focus());
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(0, ring.Progress);
            source.Emit(new MaterialInteractionEvent.Focus());
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1, ring.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SourceReplacement_DiscardsDeferredEndsFromThePreviousSource()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout();
            var controller = MaterialInteraction.GetOrCreate(button);
            var ring = button.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single();
            controller.ConnectSource(new MaterialInteractionSource());
            button.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            button.IsEnabled = false; // Queues the old source's native Unfocus for the next frame.
            var replacement = new MaterialInteractionSource();
            controller.ConnectSource(replacement);
            var focus = new MaterialInteractionEvent.Focus();
            replacement.Emit(focus);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1, ring.Progress);
            replacement.Emit(new MaterialInteractionEvent.Unfocus(focus));
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            replacement.Emit(new MaterialInteractionEvent.Focus());
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1, ring.Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SwitchingMode_DoesNotReplayFocus_ButNewFocusUsesOpacity()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.InsetRing);
            window.UpdateLayout(); button.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.Opacity);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(0, MaterialInteraction.GetOrCreate(button).StateLayerOpacity);
            Assert.True(button.IsFocused);
            window.FocusManager!.Focus(null); button.Focus(NavigationMethod.Tab);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.True(MaterialInteraction.GetOrCreate(button).StateLayerOpacity > 0);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ModeOverride_RestoresOpacity_AndDoesNotMoveContent()
    {
        using var clock = new MotionTestClockScope();
        var window = ButtonThemeFixture.ShowWindow(out var button);
        try
        {
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var bounds = surface.Bounds; var childBounds = surface.Child!.Bounds;
            MaterialInteraction.SetFocusFeedback(window, FocusFeedback.InsetRing);
            window.UpdateLayout();
            Assert.Equal(bounds, surface.Bounds); Assert.Equal(childBounds, surface.Child.Bounds);
            MaterialInteraction.SetFocusFeedback(button, FocusFeedback.Opacity);
            button.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.True(MaterialInteraction.GetOrCreate(button).StateLayerOpacity > 0);
            Assert.Equal(0, button.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single().Progress);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Switch_RendersOneRingOnTrack_AndKeepsRipplePresenterOnThumb()
    {
        using var clock = new MotionTestClockScope();
        var box = new ToggleSwitch();
        MaterialInteraction.SetFocusFeedback(box, FocusFeedback.InsetRing);
        var window = new MotionTestWindow { Width = 160, Height = 100 };
        window.Styles.Add(new MaterialTheme());
        window.Content = box; window.Show(); window.UpdateLayout();
        try
        {
            var ring = Assert.Single(box.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            Assert.IsType<SwitchTrack>(ring.GetVisualParent());
            Assert.Equal(new Size(52, 32), ring.Bounds.Size);
            Assert.Equal("PART_ThumbSurface", MaterialInteraction.GetOrCreate(box).Presenter!.Name);
            box.Focus(NavigationMethod.Tab);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(1, ring.Progress);
        }
        finally { window.Close(); }
    }
}
