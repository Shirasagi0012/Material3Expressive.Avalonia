using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class ToolbarShapeTests
{
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task ShapeFollowsAnimatedBoundsAndSurvivesTemplateSwitch(bool horizontal, bool templateFab)
    {
        using var clock = new MotionTestClockScope();
        FloatingToolbar toolbar = horizontal ? new HorizontalFloatingToolbar() : new VerticalFloatingToolbar();
        var shape = new CutCornerShape(new RelativeScalar(.2, RelativeUnit.Relative));
        toolbar.Shape = shape; toolbar.Content = new Border { Width = 48, Height = 48 };
        toolbar.LeadingContent = new Border { Width = 24, Height = 24 };
        toolbar.TrailingContent = new Border { Width = 24, Height = 24 };
        var window = DialogTests.Show(toolbar);
        FloatingActionButton? fab = null;
        MaterialSurface Surface() => toolbar.GetVisualDescendants().OfType<MaterialSurface>().First();
        try
        {
            var surface = Surface(); Assert.Same(shape, surface.Shape);
            Assert.NotNull(surface.ContainerGeometry);
            var original = surface.Bounds.Size; toolbar.Expanded = false;
            for (var i = 0; i < 45; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); await Task.Yield(); }
            Assert.True(horizontal ? surface.Bounds.Width < original.Width : surface.Bounds.Height < original.Height);
            var path = (PathGeometry)surface.ContainerGeometry!;
            Assert.Equal((double)((float)Math.Min(surface.Bounds.Width, surface.Bounds.Height) * .2f), path.Figures![0].StartPoint.Y);
            FloatingActionButton CreateFab() => fab = new FloatingActionButton { Content = new Border { Width = 24, Height = 24 },
                Shape = new RoundedCornerShape(new RelativeScalar(.25, RelativeUnit.Relative)),
                Theme = (ControlTheme)window.FindResource("MaterialStandardFloatingToolbarFabTheme")! };
            if (templateFab) toolbar.FloatingActionButtonTemplate = new FuncDataTemplate<object?>((_, _) => CreateFab());
            else toolbar.FloatingActionButton = CreateFab();
            window.UpdateLayout();
            Assert.NotSame(surface, Surface()); Assert.Same(shape, Surface().Shape);
            var fabSurface = fab!.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Same(fab!.Shape, fabSurface.Shape); Assert.Equal(new CornerRadius(20), fabSurface.EffectiveCornerRadius);
            toolbar.Expanded = true;
            for (var i = 0; i < 45; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); await Task.Yield(); }
            Assert.Equal(new CornerRadius(14), fabSurface.EffectiveCornerRadius);
            toolbar.Shape = null; Assert.Null(Surface().ContainerGeometry);
            toolbar.Shape = shape; window.Content = null; shape.TopStart = new(7, RelativeUnit.Absolute);
            window.Content = toolbar; window.UpdateLayout();
            Assert.Equal(7, ((PathGeometry)Surface().ContainerGeometry!).Figures![0].StartPoint.Y);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
}
