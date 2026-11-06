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
    [AvaloniaTheory]
    [InlineData("button")]
    [InlineData("card")]
    [InlineData("chip")]
    public void ElevationSnapPreservesUnderlyingBindingThroughDisableAndDetach(string kind)
    {
        using var clock = new MotionTestClockScope();
        var source = new Material3Expressive.Foundation.Interaction.MaterialInteractionSource();
        Control control = kind == "chip"
            ? new InputChip { Content = "Chip", InteractionSource = source, Elevation = new(2, 3, 4, 5, 9, 1) }
            : new Card { Content = "Card", IsClickable = true, InteractionSource = source,
                Elevation = new() { Default = 2, Pressed = 3, Focused = 4, Hovered = 5, Dragged = 9, Disabled = 1 } };
        if (kind == "button")
        {
            var button = new Button { Content = "Button" };
            MaterialButton.SetInteractionSource(button, source);
            MaterialButton.SetElevation(button, new() { Default = 2, Pressed = 9, Hovered = 5, Focused = 4, Disabled = 1 });
            control = button;
        }
        var window = DialogTests.Show(control);
        try
        {
            var surface = control.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var underlying = new Slider { Maximum = 100, Value = 13 };
            using var binding = surface.Bind(MaterialSurface.ShadowElevationProperty,
                new Avalonia.Data.Binding(nameof(Slider.Value)) { Source = underlying });
            Material3Expressive.Foundation.Interaction.MaterialInteractionEvent drag = kind == "button"
                ? new Material3Expressive.Foundation.Interaction.MaterialInteractionEvent.Press(new Point(8, 8))
                : new Material3Expressive.Foundation.Interaction.MaterialInteractionEvent.DragStart();
            source.Emit(drag); Frames(20);
            Assert.Equal(9, surface.ShadowElevation);
            control.IsEnabled = false; Frames(1);
            Assert.True(surface.IsAnimating(MaterialSurface.ShadowElevationProperty));
            Assert.Equal(9, surface.ShadowElevation);
            underlying.Value = 17;
            Frames(1); Assert.Equal(1, surface.ShadowElevation);
            window.Content = null; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.False(surface.IsAnimating(MaterialSurface.ShadowElevationProperty));
            Assert.Equal(17, surface.ShadowElevation);
            underlying.Value = 19;
            Assert.Equal(19, surface.ShadowElevation);
            Assert.Equal(0, clock.PendingFrameCount);
        }
        finally { window.Close(); }
        void Frames(int count) { for (var i = 0; i < count; i++) { Dispatcher.UIThread.RunJobs(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
    }

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
