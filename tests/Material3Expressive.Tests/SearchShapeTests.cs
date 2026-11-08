using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class SearchShapeTests
{
    [AvaloniaFact]
    public void CollapsedAndLegacyDockedSurfacesUseTheExplicitShapeWithoutChangingSlots()
    {
        var shape = new CutCornerShape(16);
        var input = new Border { Width = 360, Height = 56 };
        var collapsed = new SearchBar { Content = input, Shape = shape };
        var docked = new DockedSearchBar { InputField = new Border { Height = 56 }, Content = new Border { Height = 40 }, Shape = shape };
        var window = Show(new StackPanel { Children = { collapsed, docked } });
        try
        {
            Assert.Same(shape, collapsed.GetVisualDescendants().OfType<MaterialSurface>().First().Shape);
            Assert.Same(shape, docked.GetVisualDescendants().OfType<MaterialSurface>().First().Shape);
            Assert.Equal(new Size(360, 56), input.Bounds.Size);
            shape.TopStart = new(8, RelativeUnit.Absolute); Flush(window);
            Assert.False(collapsed.GetVisualDescendants().OfType<MaterialSurface>().First().ContainerGeometry!.FillContains(new Point(1, 1)));
            collapsed.Shape = null; Flush(window);
            Assert.Null(collapsed.GetVisualDescendants().OfType<MaterialSurface>().First().Shape);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AppBarForwardsShapeToInnerSearchAndGapHasIndependentDropdownShape()
    {
        var state = SearchBarState.CreateWithGap(SearchBarValue.Expanded);
        var shape = new CutCornerShape(12); var dropdown = new RoundedCornerShape(4);
        var bar = new AppBarWithSearch { State = state, InputField = new Border { Width = 360, Height = 56 }, Shape = shape };
        var popup = new ExpandedDockedSearchBarWithGap { State = state, Shape = shape, DropdownShape = dropdown,
            InputField = new Border { Width = 360, Height = 56 }, Content = new Border { Height = 60 } };
        var window = Show(new Grid { Children = { bar, popup } });
        try
        {
            Assert.Same(shape, bar.GetVisualDescendants().OfType<SearchBar>().Single().Shape);
            var surfaces = popup.Presentation!.GetVisualDescendants().OfType<MaterialSurface>().ToArray();
            Assert.Contains(surfaces, s => ReferenceEquals(s.Shape, shape));
            Assert.Contains(surfaces, s => ReferenceEquals(s.Shape, dropdown));
            popup.DropdownShape = shape; Flush(window);
            Assert.DoesNotContain(surfaces, s => ReferenceEquals(s.Shape, dropdown));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public async Task FullScreenRemembersSurfaceShapeButInputUsesCurrentShapeAndRemountRecaptures(bool contained)
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        await state.SnapToAsync(.25f);
        var original = new CutCornerShape(24); var replacement = new CutCornerShape(4);
        var bar = new SearchBar { State = state, Content = new Border { Width = 360, Height = 56 } };
        ExpandedFullScreenSearchBar popup = contained ? new ExpandedFullScreenContainedSearchBar() : new();
        popup.State = state; popup.Shape = original;
        popup.InputField = new Border { Height = 56 }; popup.Content = new Border { Height = 40 };
        var window = Show(new Grid { Children = { bar, popup } });
        try
        {
            var surface = (MaterialSurface)popup.Presentation!;
            double Corner() => ((PathGeometry)surface.ContainerGeometry!).Figures![0].StartPoint.Y;
            Assert.Equal(24, Corner());
            popup.Shape = replacement; Flush(window);
            Assert.Equal(24, Corner());
            var inputSurface = ((Control)popup.InputField).GetVisualAncestors().OfType<MaterialSurface>().First();
            Assert.Same(replacement, inputSurface.Shape);
            await state.SnapToAsync(.5f); Flush(window);
            Assert.True(surface.ContainerGeometry!.FillContains(new Point(1, 1)));
            await state.SnapToAsync(.25f); Flush(window); Assert.Equal(24, Corner());
            original.TopStart = new(12, RelativeUnit.Absolute); Flush(window); Assert.Equal(12, Corner());
            await state.SnapToAsync(0); Flush(window); Assert.False(popup.IsPresented);
            await state.SnapToAsync(.25f); Flush(window);
            Assert.Equal(4, ((PathGeometry)((MaterialSurface)popup.Presentation!).ContainerGeometry!).Figures![0].StartPoint.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void InputFocusRingFollowsShapeAndDirectionWithoutClippingEditor(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var field = new SearchBarInputField { State = new(), InteractionSource = source, IsReadOnly = true,
            Width = 360, Height = 56, Shape = new CutCornerShape { TopStart = new(16, RelativeUnit.Absolute) },
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MaterialInteraction.SetFocusFeedback(field, FocusFeedback.InsetRing);
        var window = Show(field);
        try
        {
            source.Emit(new MaterialInteractionEvent.Focus());
            for (var i = 0; i < 20; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            var ring = field.GetVisualDescendants().OfType<InsetFocusRingPresenter>().Single();
            Assert.Equal(field.FlowDirection, ring.FlowDirection);
            Assert.NotNull(ring.ShapePath);
            Assert.False(ring.ShapePath.FillContains(new Point(1, 1)));
            Assert.Null(field.GetVisualDescendants().OfType<TextFieldLayout>().Single().Clip);
            field.Shape = new OverflowShape(); Flush(window);
            var decoration = field.GetVisualDescendants().OfType<TextFieldDecoration>().Single();
            Assert.True(decoration.Clip!.Bounds.Bottom >= 64);
            field.Shape = new RoundedCornerShape(8); Flush(window);
            Assert.Null(ring.ShapePath); Assert.Equal(new CornerRadius(8), ring.Corners);
            field.Shape = null; field.CornerRadius = new(24, 8, 0, 16); Flush(window);
            Assert.Equal(FlowDirection.LeftToRight, ring.FlowDirection);
            Assert.Equal(field.CornerRadius, ring.Corners);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public async Task DefaultToExplicitShapeUpdatesInputAndRecreatedPresentation()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        await state.SnapToAsync(.25f);
        var bar = new SearchBar { State = state, Content = new Border { Width = 360, Height = 56 } };
        var popup = new ExpandedFullScreenSearchBar { State = state,
            InputField = new Border { Height = 56 }, Content = new Border { Height = 40 } };
        var window = Show(new Grid { Children = { bar, popup } });
        try
        {
            var surface = (MaterialSurface)popup.Presentation!;
            Assert.Null(surface.Shape);
            var replacement = new CutCornerShape(12);
            popup.Shape = replacement; Flush(window);
            Assert.Same(replacement, ((Control)popup.InputField).GetVisualAncestors().OfType<MaterialSurface>().First().Shape);
            await state.SnapToAsync(0); Flush(window);
            await state.SnapToAsync(.25f); Flush(window);
            Assert.IsType<FullScreenSearchShape>(((MaterialSurface)popup.Presentation!).Shape);
            Assert.Equal(12, ((PathGeometry)((MaterialSurface)popup.Presentation!).ContainerGeometry!).Figures![0].StartPoint.Y);
        }
        finally { window.Close(); }
    }

    private sealed class OverflowShape : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density) =>
            PathGeometry.Parse("M0,0 L120,0 L120,64 L0,64 Z");
    }

    private static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 600, Height = 600 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add(new MaterialTheme()); window.Content = content; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
