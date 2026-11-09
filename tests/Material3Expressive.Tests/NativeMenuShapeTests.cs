using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;
public class NativeMenuShapeTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void NativeMenuShapeSharesSelectedMotionAndReleasesItsSubscriptions(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Shape", ToggleType = MenuItemToggleType.CheckBox,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var normal = new CutCornerShape(4); var selected = new CutCornerShape(20);
        MenuAppearance.SetShape(item, normal); MenuAppearance.SetSelectedShape(item, selected);
        var window = DialogTests.Show(item);
        item.Theme = (ControlTheme)window.FindResource("MaterialVerticalMenuItemTheme")!; window.UpdateLayout();
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        MaterialSurface Surface() => item.GetVisualDescendants().OfType<MaterialSurface>().First(s => s.Name == "PART_ItemSurface");
        try
        {
            Frames(3); Assert.IsType<CutCornerShape>(Surface().Shape);
            item.IsChecked = true; Frames(6); Assert.NotNull(Surface().ContainerGeometry);
            Assert.True(item.IsChecked); Assert.IsType<CutCornerShape>(Surface().Shape);
            item.IsChecked = false; Frames(60); Assert.IsType<CutCornerShape>(Surface().Shape);
            selected.TopStart = new(12, RelativeUnit.Absolute); item.IsChecked = true; Frames(60);
            Assert.False(Surface().ContainerGeometry!.FillContains(new(1, 1)));
            MenuAppearance.SetSelectedShape(item, new RoundedCornerShape(20)); Frames(3);
            Assert.Null(Surface().Shape); Assert.Equal(new CornerRadius(20), Surface().EffectiveCornerRadius);
            MenuAppearance.SetSelectedShape(item, null); Frames(3); Assert.Null(Surface().Shape);
            var root = window.Content; window.Content = null; window.UpdateLayout(); Assert.Equal(0, clock.PendingFrameCount);
            selected.TopStart = new(10, RelativeUnit.Absolute); Assert.Equal(0, clock.PendingFrameCount);
            window.Content = root; window.UpdateLayout(); Frames(3); Assert.True(item.IsChecked);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
}
