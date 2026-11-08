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
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class RoundedShapeTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void StaticCornersKeepUnitsOnMutationResizeAndReattachment(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var shape = new RoundedCornerShape { TopStart = new(.25, RelativeUnit.Relative), TopEnd = new(8, RelativeUnit.Absolute) };
        var surface = new MaterialSurface { Width = 120, Height = 64, Shape = shape,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = DialogTests.Show(surface);
        try
        {
            Assert.Equal(new CornerRadius(16, 8, 0, 0), surface.EffectiveCornerRadius);
            Assert.Null(surface.ContainerGeometry);
            surface.Height = 80; window.UpdateLayout(); Assert.Equal(20, surface.EffectiveCornerRadius.TopLeft);
            shape.TopStart = new(.5, RelativeUnit.Relative); Assert.Equal(40, surface.EffectiveCornerRadius.TopLeft);
            window.Content = null; shape.TopStart = new(12, RelativeUnit.Absolute);
            window.Content = surface; window.UpdateLayout(); Assert.Equal(12, surface.EffectiveCornerRadius.TopLeft);
            Assert.Equal(0, clock.PendingFrameCount);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("Filled")] [InlineData("Outlined")] [InlineData("FilledIcon")]
    public void MixedButtonShapesKeepTheCapturedFamilyUntilTheHalfwaySwitch(string variant)
    {
        using var clock = new MotionTestClockScope();
        Button button = variant.Contains("Icon") ? new IconButton() : new Button();
        button.Width = 120; button.Height = 64;
        MaterialButton.SetShape(button, new CutCornerShape(24));
        MaterialButton.SetPressedShape(button, new RoundedCornerShape(new RelativeScalar(.25, RelativeUnit.Relative)));
        var window = DialogTests.Show(button);
        void Frames(int count) { for (var i = 0; i < count; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
        try
        {
            button.Theme = (ControlTheme)window.FindResource($"Material{variant}ButtonTheme")!;
            window.UpdateLayout(); Frames(2);
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            button.Focus(NavigationMethod.Tab); window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Frames(1); Assert.NotNull(surface.ContainerGeometry); Assert.Equal(default, surface.EffectiveCornerRadius);
            Frames(40); Assert.Null(surface.ContainerGeometry); Assert.Equal(new CornerRadius(16), surface.EffectiveCornerRadius);
            button.Height = 48; window.UpdateLayout(); Assert.Equal(new CornerRadius(12), surface.EffectiveCornerRadius);
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Frames(1); Assert.Null(surface.ContainerGeometry);
            Frames(40); Assert.NotNull(surface.ContainerGeometry); Assert.Equal(default, surface.EffectiveCornerRadius);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("list")] [InlineData("menu")] [InlineData("group")]
    public void ExistingStateOwnersAnimateRoundedPercentagesAndMixedFamilies(string kind)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var round = new RoundedCornerShape(new RelativeScalar(.25, RelativeUnit.Relative));
        Control control = kind switch
        {
            "list" => new ListItem { Shape = new CutCornerShape(4), SelectedShape = round, InteractionMode = ListItemInteractionMode.MultipleSelection },
            "menu" => new SelectableDropdownMenuItem { Shape = new CutCornerShape(4), SelectedShape = round },
            _ => new DropdownMenuGroup { Shape = new CutCornerShape(4), InactiveShape = round, InteractionSource = source }
        };
        control.Width = 180; control.Height = 64;
        var window = DialogTests.Show(control);
        void Frames(int count) { for (var i = 0; i < count; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
        try
        {
            var surface = control.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.NotNull(surface.ContainerGeometry);
            switch (control)
            {
                case ListItem list: list.IsChecked = true; break;
                case SelectableDropdownMenuItem menu: menu.IsSelected = true; break;
                default:
                    var hover = new MaterialInteractionEvent.HoverEnter(); source.Emit(hover); Frames(40);
                    source.Emit(new MaterialInteractionEvent.HoverExit(hover)); break;
            }
            Frames(1); Assert.NotNull(surface.ContainerGeometry);
            Frames(40); Assert.Null(surface.ContainerGeometry);
            Assert.Equal(Math.Min(surface.Bounds.Width, surface.Bounds.Height) * .25, surface.EffectiveCornerRadius.TopLeft);
            round.TopStart = new(.4, RelativeUnit.Relative); Frames(1);
            Assert.Equal((double)((float)Math.Min(surface.Bounds.Width, surface.Bounds.Height) * .4f), surface.EffectiveCornerRadius.TopLeft);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
}
