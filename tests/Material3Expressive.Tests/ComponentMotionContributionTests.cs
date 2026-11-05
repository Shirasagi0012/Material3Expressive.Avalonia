using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class ComponentMotionContributionTests
{
    [AvaloniaFact]
    public void ChipIconVisibilityRestoresLocalOpacityAfterTransitionRecreationAndDetach()
    {
        using var clock = new MotionTestClockScope();
        var chip = new InputChip { Content = "Message", LeadingIcon = new Border { Width = 18, Height = 18 } };
        var window = DialogTests.Show(chip);
        try
        {
            var slot = chip.GetVisualDescendants().OfType<ChipIconSlot>().Single(c => c.Name == "PART_LeadingIcon");
            slot.Opacity = .37;
            Assert.True(slot.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(1, slot.Opacity);
            chip.LeadingIcon = null;
            Frames(5);
            Assert.InRange(slot.Opacity, .001, .999);
            Frames(40);
            Assert.Equal(0, slot.Opacity);
            chip.LeadingIcon = new Border { Width = 24, Height = 24 };
            Frames(50);
            Assert.Equal(1, slot.Opacity);
            window.Content = null; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.False(slot.IsAnimating(Visual.OpacityProperty));
            Assert.Equal(.37, slot.Opacity);
            Assert.Equal(0, clock.PendingFrameCount);
        }
        finally { window.Close(); }
        void Frames(int count) { for (var i = 0; i < count; i++) { Dispatcher.UIThread.RunJobs(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
    }

    [AvaloniaFact]
    public void ToolbarElevationRestoresLocalValueAfterCollapseReversalAndDetach()
    {
        using var clock = new MotionTestClockScope();
        var toolbar = new HorizontalFloatingToolbar
        {
            Content = new Border { Width = 48, Height = 48 },
            ExpandedShadowElevation = 6, CollapsedShadowElevation = 0
        };
        var window = DialogTests.Show(toolbar);
        try
        {
            var surface = toolbar.GetVisualDescendants().OfType<MaterialSurface>().Single();
            surface.ShadowElevation = 13;
            Assert.True(surface.IsAnimating(MaterialSurface.ShadowElevationProperty));
            Assert.Equal(6, surface.ShadowElevation);
            toolbar.Expanded = false; Frames(5);
            Assert.InRange(surface.ShadowElevation, .001, 5.999);
            toolbar.Expanded = true; Frames(40);
            Assert.Equal(6, surface.ShadowElevation);
            window.Content = null; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.False(surface.IsAnimating(MaterialSurface.ShadowElevationProperty));
            Assert.Equal(13, surface.ShadowElevation);
            Assert.Equal(0, clock.PendingFrameCount);
        }
        finally { window.Close(); }
        void Frames(int count) { for (var i = 0; i < count; i++) { Dispatcher.UIThread.RunJobs(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
    }
}
