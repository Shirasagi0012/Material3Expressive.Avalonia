using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class SheetShapeTests
{
    [AvaloniaTheory]
    [InlineData("plain")] [InlineData("modal")] [InlineData("scaffold")]
    public void SheetShapeChangesPreserveAnchorsAndRestoreThemedOutline(string kind)
    {
        using var clock = new MotionTestClockScope();
        var shape = new CutCornerShape(24);
        var state = new SheetState(SheetValue.PartiallyExpanded);
        var content = new Border { Height = 400, Background = Brushes.Blue };
        Control control = kind switch
        {
            "modal" => new ModalBottomSheet { State = state, IsOpen = true, Content = content, Shape = shape },
            "scaffold" => new BottomSheetScaffold { State = new(state, new()), SheetContent = content, SheetShape = shape },
            _ => new BottomSheet { State = state, Content = content, Shape = shape }
        };
        void SetShape(MaterialShape? value)
        {
            switch (control) { case ModalBottomSheet m: m.Shape = value; break; case BottomSheetScaffold s: s.SheetShape = value; break; case BottomSheet b: b.Shape = value; break; }
        }
        var window = DialogTests.Show(control);
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        MaterialSurface Surface() => window.GetVisualDescendants().OfType<MaterialSurface>().First(s => s.TemplatedParent is BottomSheet);
        try
        {
            Frames(50);
            var offset = state.CurrentOffset; var size = Surface().Bounds.Size;
            Assert.Same(shape, Surface().Shape); Assert.False(Surface().ContainerGeometry!.FillContains(new(1, 1)));
            shape.TopStart = new(0, RelativeUnit.Absolute);
            Assert.True(Surface().ContainerGeometry!.FillContains(new(1, 1)));
            SetShape(new RoundedCornerShape(new RelativeScalar(.25, RelativeUnit.Relative)));
            Assert.Null(Surface().ContainerGeometry); Assert.Equal(offset, state.CurrentOffset); Assert.Equal(size, Surface().Bounds.Size);
            _ = state.ExpandAsync(); Frames(4); SetShape(shape); Frames(60);
            Assert.Equal(SheetValue.Expanded, state.CurrentValue); Assert.Same(shape, Surface().Shape);
            SetShape(null); Assert.Null(Surface().ContainerGeometry); Assert.Equal(new CornerRadius(28, 28, 0, 0), Surface().EffectiveCornerRadius);
            if (control is ModalBottomSheet modal)
            {
                modal.IsOpen = false; Frames(2); SetShape(shape); modal.IsOpen = true; Frames(50);
                Assert.True(modal.IsPresented); Assert.Same(shape, Surface().Shape);
            }
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("modal")] [InlineData("dismissible")] [InlineData("permanent")]
    public void DrawerOutlineAndItemOutlineRemainIndependentAcrossDirectionAndSelection(string kind)
    {
        using var clock = new MotionTestClockScope();
        DrawerSheet sheet = kind switch { "modal" => new ModalDrawerSheet(), "dismissible" => new DismissibleDrawerSheet(), _ => new PermanentDrawerSheet() };
        var shape = new CutCornerShape { TopStart = new(32, RelativeUnit.Absolute) };
        var itemShape = new CutCornerShape(12);
        var item = new NavigationDrawerItem { Content = "Inbox", Shape = itemShape, IsSelected = true };
        sheet.Shape = shape; sheet.Content = item;
        var window = DialogTests.Show(sheet);
        MaterialSurface Surface(Control owner) => owner.GetVisualDescendants().OfType<MaterialSurface>().Single(s => s.TemplatedParent == owner);
        try
        {
            var size = sheet.Bounds.Size;
            Assert.Same(shape, Surface(sheet).Shape); Assert.Same(itemShape, Surface(item).Shape);
            Assert.False(Surface(sheet).ContainerGeometry!.FillContains(new(1, 1)));
            sheet.FlowDirection = FlowDirection.RightToLeft; window.UpdateLayout();
            // Geometry is in the surface's local space, before Avalonia applies the visual RTL mirror.
            Assert.Equal(size, sheet.Bounds.Size); Assert.NotNull(Surface(sheet).ContainerGeometry);
            var clicks = 0; item.Click += (_, _) => clicks++;
            item.Focus(NavigationMethod.Tab); window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.Equal(1, clicks); Assert.True(item.IsSelected);
            item.IsSelected = false; Assert.Same(itemShape, Surface(item).Shape);
            sheet.Shape = null; Assert.Null(Surface(sheet).ContainerGeometry); Assert.Same(itemShape, Surface(item).Shape);
            item.Shape = null; Assert.Null(Surface(item).ContainerGeometry);
        }
        finally { window.Close(); }
    }
}
