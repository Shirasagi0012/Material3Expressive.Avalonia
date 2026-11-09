using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class WideRailShapeTests
{
    [AvaloniaTheory]
    [InlineData("plain")] [InlineData("retained")] [InlineData("hidden")]
    public void OutlinesRemainIndependentAcrossExpansionReplacementAndReopening(string kind)
    {
        using var clock = new MotionTestClockScope();
        var cut = new CutCornerShape { TopStart = new(24, RelativeUnit.Absolute), BottomEnd = new(12, RelativeUnit.Absolute) };
        WideNavigationRail rail = kind == "plain" ? new WideNavigationRail { Shape = cut } :
            new ModalWideNavigationRail { HideOnCollapse = kind == "hidden", CollapsedShape = cut, ExpandedShape = new CutCornerShape(16) };
        rail.Items.Add(new WideNavigationRailItem { Icon = new Border { Width = 24, Height = 24, Background = Brushes.Blue }, Label = "Home", IsSelected = true });
        var window = DialogTests.Show(rail);
        void Frames(int count = 80) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        MaterialSurface Surface() => window.GetVisualDescendants().OfType<MaterialSurface>().First(s =>
            kind == "plain" ? s.TemplatedParent == rail : s.Name == "PART_ModalSurface");
        try
        {
            _ = rail.State.ExpandAsync(); Frames();
            var size = Surface().Bounds.Size;
            var expected = rail is ModalWideNavigationRail m ? m.ExpandedShape : rail.Shape;
            Assert.Same(expected, Surface().Shape); Assert.NotNull(Surface().ContainerGeometry);
            rail.FlowDirection = FlowDirection.RightToLeft; window.UpdateLayout();
            Assert.Equal(size, Surface().Bounds.Size);
            if (rail is ModalWideNavigationRail modal) modal.ExpandedShape = cut; else rail.Shape = cut;
            Assert.Same(cut, Surface().Shape);
            cut.TopStart = new(0, RelativeUnit.Absolute); Assert.NotNull(Surface().ContainerGeometry);
            _ = rail.State.CollapseAsync(); Frames(4); _ = rail.State.ExpandAsync(); Frames();
            Assert.Equal(WideNavigationRailValue.Expanded, rail.State.TargetValue); Assert.Same(cut, Surface().Shape);
            if (rail is ModalWideNavigationRail modal2)
            {
                Assert.Same(cut, modal2.CollapsedShape);
                modal2.ExpandedShape = null; Assert.Null(Surface().ContainerGeometry); Assert.Equal(new CornerRadius(16), Surface().EffectiveCornerRadius);
                _ = rail.State.CollapseAsync(); Frames(); _ = rail.State.ExpandAsync(); Frames(); Assert.Null(Surface().Shape);
            }
            else { rail.Shape = null; Assert.Null(Surface().ContainerGeometry); Assert.Equal(default(CornerRadius), Surface().EffectiveCornerRadius); }
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
}
