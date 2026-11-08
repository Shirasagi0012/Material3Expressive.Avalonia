using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;
using Split = Material3Expressive.Components.SplitButton;

namespace Material3Expressive.Tests;

public class SplitButtonShapeTests
{
    [AvaloniaTheory]
    [InlineData("Filled")]
    [InlineData("Tonal")]
    [InlineData("Elevated")]
    [InlineData("Outlined")]
    public void IndependentSlotsMorphAndNativeToggleStillPublishesBeforeClick(string variant)
    {
        using var clock = new MotionTestClockScope();
        var leading = new Button { Content = new Border { Width = 32, Height = 16 } };
        var trailing = new ToggleButton { Content = new Border { Width = 22, Height = 22 } };
        foreach (var button in new Button[] { leading, trailing })
        {
            MaterialButton.SetShape(button, new CutCornerShape(16));
            MaterialButton.SetPressedShape(button, new CutCornerShape(2));
        }
        ToggleButtonAppearance.SetCheckedShape(trailing, new CutCornerShape(10));
        var window = DialogTests.Show(new Split { LeadingButton = leading, TrailingButton = trailing });
        try
        {
            leading.Theme = (ControlTheme)window.FindResource($"Material{variant}SplitButtonLeadingTheme")!;
            trailing.Theme = (ControlTheme)window.FindResource($"Material{variant}SplitButtonTrailingTheme")!;
            window.UpdateLayout(); Advance(2);
            var front = Surface(leading); var back = Surface(trailing);
            Assert.Equal(16, Cut(front)); Assert.Equal(16, Cut(back));
            bool? atClick = null; trailing.Click += (_, _) => atClick = trailing.IsChecked;
            trailing.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Advance(4);
            Assert.InRange(Cut(back), 2.01, 15.99); Assert.Equal(16, Cut(front));
            Advance(40); Assert.Equal(2, Cut(back));
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Advance(40);
            Assert.True(atClick); Assert.Equal(10, Cut(back));
            MaterialButton.SetPressedShape(trailing, null); Advance(2);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Advance(40);
            Assert.Equal(16, Cut(back)); // null pressed falls back to rest, not checked
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Advance(40);
            MaterialButton.SetShape(trailing, null); Advance(40);
            Assert.Null(back.ContainerGeometry);
            Assert.NotEqual(default, back.EffectiveCornerRadius);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
        void Advance(int frames) { for (var i = 0; i < frames; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
    }

    [AvaloniaFact]
    public void InactiveShapeChangesPreserveTheSelectionAnimationAndLiveCornersRetarget()
    {
        using var clock = new MotionTestClockScope();
        var trailing = new ToggleButton { Content = new Border { Width = 22, Height = 22 } };
        var rest = new CutCornerShape(18); var selected = new CutCornerShape(2);
        MaterialButton.SetShape(trailing, rest); ToggleButtonAppearance.SetCheckedShape(trailing, selected);
        var window = DialogTests.Show(new Split { LeadingButton = new Button { Content = "Save" }, TrailingButton = trailing });
        try
        {
            trailing.Theme = (ControlTheme)window.FindResource("MaterialFilledSplitButtonTrailingTheme")!;
            window.UpdateLayout(); Advance(2);
            var surface = Surface(trailing); trailing.IsChecked = true; Advance(4);
            var before = Cut(surface); Assert.InRange(before, 2.01, 17.99);
            rest.TopStart = new(12, RelativeUnit.Absolute); Advance(1);
            Assert.InRange(Cut(surface), 2.01, before); // no key(shapes) reset in Split
            Advance(40); Assert.Equal(2, Cut(surface));
            selected.TopStart = new(8, RelativeUnit.Absolute); Advance(4);
            Assert.InRange(Cut(surface), 2.01, 7.99); Advance(40); Assert.Equal(8, Cut(surface));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
        void Advance(int frames) { for (var i = 0; i < frames; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
    }

    private static MaterialSurface Surface(Button button) => button.GetVisualDescendants().OfType<MaterialSurface>().Single();
    private static double Cut(MaterialSurface surface) => ((PathGeometry)surface.ContainerGeometry!).Figures![0].StartPoint.Y;
}
