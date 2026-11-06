using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class ElevationSourceLifetimeTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedFocusRing_WithComponentSource_FollowsNativeFocus(bool chip)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        Control control = chip ? new InputChip { InteractionSource = source, Content = "Chip" }
            : new Card { IsClickable = true, InteractionSource = source, Content = "Card" };
        MaterialInteraction.SetFocusFeedback(control, FocusFeedback.InsetRing);
        var window = DialogTests.Show(control);
        try
        {
            Assert.True(control.Focus(NavigationMethod.Tab));
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            var ring = Assert.Single(control.GetVisualDescendants().OfType<InsetFocusRingPresenter>());
            Assert.Equal(1f, ring.Progress);
            window.FocusManager!.Focus(null);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromSeconds(1));
            Assert.Equal(0f, ring.Progress);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TemplateReplacementBeforeSnapPresentation_DoesNotWriteOldSurface(bool chip)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        TemplatedControl control = chip
            ? new InputChip { InteractionSource = source, Elevation = new(2, 3, 4, 5, 9, 1), Content = new Border { Width = 64, Height = 24 } }
            : new Card { IsClickable = true, InteractionSource = source,
                Elevation = new() { Default = 2, Pressed = 3, Focused = 4, Hovered = 5, Dragged = 9, Disabled = 1 },
                Content = new Border { Width = 64, Height = 24 } };
        var window = DialogTests.Show(new Grid { Children = { control } });
        MaterialSurface Surface() => control.GetVisualDescendants().OfType<MaterialSurface>().Single();
        void Frame() => clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        try
        {
            var drag = new MaterialInteractionEvent.DragStart(); source.Emit(drag); Frame(); Frame();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(200));
            Assert.Equal(9, Surface().ShadowElevation);
            Surface().ShadowElevation = 13; // Underlying value must survive the pending snap.
            control.IsEnabled = false; Frame();
            var oldSurface = Surface(); Assert.Equal(9, oldSurface.ShadowElevation);
            var template = control.Template;
            control.Template = null; window.UpdateLayout(); control.Template = template; window.UpdateLayout();
            Assert.NotSame(oldSurface, Surface()); Assert.Equal(1, Surface().ShadowElevation);
            Frame(); Assert.Equal(13, oldSurface.ShadowElevation);
            Assert.False(oldSurface.IsAnimating(MaterialSurface.ShadowElevationProperty));
            control.IsEnabled = true; Frame(); Frame(); clock.AdvanceBy(TimeSpan.FromMilliseconds(200));
            Assert.Equal(9, Surface().ShadowElevation);
            control.Template = null; window.UpdateLayout(); control.Template = template; window.UpdateLayout();
            source.Emit(new MaterialInteractionEvent.DragStop(drag)); Frame(); Frame();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
            Assert.InRange(Surface().ShadowElevation, 2.01, 8.99); // Template replacement retains exit history.
            clock.AdvanceBy(TimeSpan.FromMilliseconds(200)); Assert.Equal(2, Surface().ShadowElevation);
            // Closing with a pending snap presentation must cancel that frame waiter as well.
            control.IsEnabled = false; Frame();
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
}
