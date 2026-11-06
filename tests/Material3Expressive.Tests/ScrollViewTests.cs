using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using SmoothScroll.Avalonia.Controls;
using SmoothScroll.Avalonia.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ScrollViewTests
{
    [AvaloniaFact]
    public void MaterialThemeAlone_AppliesOverlayScrollBarsWithoutShrinkingViewport()
    {
        var view = CreateView(new Border { Width = 360, Height = 280 });
        view.IsDeferredScrollingEnabled = true;
        var window = Show(view);
        try
        {
            Assert.Same(window.FindResource("MaterialScrollViewTemplate"), view.Template);
            var presenter = Assert.IsType<ScrollPresenter>(view.ScrollPresenter);
            Assert.True(presenter.IsLoaded);
            var vertical = Part<ScrollBar>(view, "PART_VerticalScrollBar");
            var horizontal = Part<ScrollBar>(view, "PART_HorizontalScrollBar");

            // Overlay scrollbars keep the full viewport size when visible and leave the
            // bottom-right corner clear so both thumbs never overlap at maximum offset.
            Assert.Equal(new Size(160, 140), view.Viewport);
            Assert.Equal(new Size(160, 140), presenter.Bounds.Size);
            Assert.True(vertical.IsVisible);
            Assert.True(horizontal.IsVisible);
            Assert.Equal(160 - vertical.Bounds.Width, horizontal.Bounds.Width);
            Assert.Equal(140 - horizontal.Bounds.Height, vertical.Bounds.Height);
            Assert.Equal(view.Viewport.Height, vertical.ViewportSize);
            Assert.Equal(view.ScrollBarMaximum.Y, vertical.Maximum);
            Assert.True(Part<Track>(vertical, "PART_Track").DeferThumbDrag);
            Assert.True(vertical.AllowAutoHide);

            view.Offset = new Vector(25, 45);
            Render(window);
            Assert.Equal(view.Offset, presenter.Offset);
            Assert.Equal(45, vertical.Value);
            Assert.Equal(25, horizontal.Value);

            var pageDown = Part<RepeatButton>(vertical, "PART_PageDownButton");
            var pagePoint = pageDown.TranslatePoint(new Point(pageDown.Bounds.Width / 2, pageDown.Bounds.Height - 2), window)!.Value;
            window.MouseDown(pagePoint, MouseButton.Left);
            window.MouseUp(pagePoint, MouseButton.Left);
            Render(window);
            Assert.True(view.Offset.Y > 45);
            Assert.Equal(view.Offset.Y, presenter.Offset.Y);
            Assert.Equal(view.Offset.Y, vertical.Value);

            // Shrinking content below viewport hides Auto scrollbars while keeping the exact same viewport.
            view.Content = new Border { Width = 60, Height = 50 };
            Render(window);
            Assert.False(vertical.IsVisible);
            Assert.False(horizontal.IsVisible);
            Assert.Equal(new Size(160, 140), view.Viewport);
            Assert.Equal(default, view.Offset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ZoomTransitionAcrossOverflowThreshold_KeepsViewportConstant()
    {
        using var clock = new MotionTestClockScope();
        var view = CreateView(new Border { Width = 120, Height = 100, Background = Brushes.CornflowerBlue });
        view.IsZoomEnabled = true;
        view.MinZoomFactor = 0.5;
        view.MaxZoomFactor = 4;
        view.HorizontalContentAlignment = HorizontalAlignment.Center;
        view.VerticalContentAlignment = VerticalAlignment.Center;
        var window = Show(view);
        try
        {
            var vertical = Part<ScrollBar>(view, "PART_VerticalScrollBar");
            var horizontal = Part<ScrollBar>(view, "PART_HorizontalScrollBar");
            Assert.Equal(new Size(160, 140), view.Viewport);
            Assert.False(vertical.IsVisible);
            Assert.False(horizontal.IsVisible);
            Assert.Equal(default, view.Offset);

            view.ZoomFactor = 2.0;
            Render(window);
            Assert.Equal(2.0, view.ZoomFactor, 3);
            Assert.Equal(new Size(240, 200), view.Extent);
            Assert.Equal(new Size(160, 140), view.Viewport);
            Assert.True(vertical.IsVisible);
            Assert.True(horizontal.IsVisible);
            Assert.Equal(80, view.ScrollBarMaximum.X, 3);
            Assert.Equal(60, view.ScrollBarMaximum.Y, 3);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TemplateBindings_ForwardScrollAndZoomConfigurationToPresenter()
    {
        var child = new Border { Width = 40, Height = 30, Background = Brushes.Coral };
        var template = new FuncDataTemplate<string>((_, _) => child);
        var customGestures = ScrollGestureBindings.CreateDefault();
        var view = CreateView("canvas");
        view.ContentTemplate = template;
        view.Padding = new Thickness(6, 8, 6, 8);
        view.Background = Brushes.AliceBlue;
        view.HorizontalContentAlignment = HorizontalAlignment.Center;
        view.VerticalContentAlignment = VerticalAlignment.Bottom;
        view.HorizontalAnchorRatio = 0.25;
        view.VerticalAnchorRatio = 1.0;
        view.HorizontalScrollMode = ScrollMode.Disabled;
        view.VerticalScrollMode = ScrollMode.Enabled;
        view.IsHorizontalMeasureInfinite = false;
        view.IsVerticalMeasureInfinite = true;
        view.IsScrollChainingEnabled = false;
        view.IsScrollInertiaEnabled = false;
        view.IsZoomEnabled = true;
        view.MinZoomFactor = 0.25;
        view.MaxZoomFactor = 6.0;
        view.ScrollInputMultiplier = 1.5;
        view.ZoomInputMultiplier = 2.0;
        view.ScrollInertiaDecayRate = 0.9;
        view.ZoomInertiaDecayRate = 0.85;
        view.OverscrollElasticity = 0.4;
        view.OverscrollBounceRate = 0.3;
        view.HorizontalSnapPointsType = SnapPointsType.Mandatory;
        view.VerticalSnapPointsType = SnapPointsType.MandatorySingle;
        view.HorizontalSnapPointsAlignment = SnapPointsAlignment.Center;
        view.VerticalSnapPointsAlignment = SnapPointsAlignment.Far;
        view.GestureBindings = customGestures;
        var window = Show(view);
        try
        {
            var presenter = Assert.IsType<ScrollPresenter>(view.ScrollPresenter);
            Assert.Same(child, presenter.Child);
            Assert.Same(template, presenter.ContentTemplate);
            Assert.Equal(view.Padding, presenter.Padding);
            Assert.Same(Brushes.AliceBlue, presenter.Background);
            Assert.Equal(HorizontalAlignment.Center, presenter.HorizontalContentAlignment);
            Assert.Equal(VerticalAlignment.Bottom, presenter.VerticalContentAlignment);
            Assert.Equal(0.25, presenter.HorizontalAnchorRatio);
            Assert.Equal(1.0, presenter.VerticalAnchorRatio);
            Assert.Equal(ScrollMode.Disabled, presenter.HorizontalScrollMode);
            Assert.Equal(ScrollMode.Enabled, presenter.VerticalScrollMode);
            Assert.False(presenter.IsHorizontalMeasureInfinite);
            Assert.True(presenter.IsVerticalMeasureInfinite);
            Assert.False(presenter.IsScrollChainingEnabled);
            Assert.False(ScrollViewer.GetIsScrollInertiaEnabled(presenter));
            Assert.True(presenter.IsZoomEnabled);
            Assert.Equal(0.25, presenter.MinZoomFactor);
            Assert.Equal(6.0, presenter.MaxZoomFactor);
            Assert.Equal(1.5, presenter.ScrollInputMultiplier);
            Assert.Equal(2.0, presenter.ZoomInputMultiplier);
            Assert.Equal(0.9, presenter.ScrollInertiaDecayRate);
            Assert.Equal(0.85, presenter.ZoomInertiaDecayRate);
            Assert.Equal(0.4, presenter.OverscrollElasticity);
            Assert.Equal(0.3, presenter.OverscrollBounceRate);
            Assert.Equal(SnapPointsType.Mandatory, presenter.HorizontalSnapPointsType);
            Assert.Equal(SnapPointsType.MandatorySingle, presenter.VerticalSnapPointsType);
            Assert.Equal(SnapPointsAlignment.Center, presenter.HorizontalSnapPointsAlignment);
            Assert.Equal(SnapPointsAlignment.Far, presenter.VerticalSnapPointsAlignment);
            Assert.Same(customGestures, presenter.GestureBindings);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MouseDragPan_WorksWithDefaultMiddleButtonAndConfiguredLeftButton()
    {
        using var clock = new MotionTestClockScope();
        var view = CreateView(new Border { Width = 400, Height = 360, Background = Brushes.Beige });
        view.IsScrollInertiaEnabled = false;
        var window = Show(view);
        try
        {
            // By default MouseLeftDrag is None while MouseMiddleDrag is Pan.
            // The first move after press starts the manipulation and records the baseline position.
            window.MouseDown(new Point(100, 90), MouseButton.Left);
            window.MouseMove(new Point(99, 89), RawInputModifiers.LeftMouseButton);
            window.MouseMove(new Point(59, 39), RawInputModifiers.LeftMouseButton);
            window.MouseUp(new Point(59, 39), MouseButton.Left);
            Render(window);
            Assert.Equal(default, view.Offset);

            window.MouseDown(new Point(100, 90), MouseButton.Middle);
            window.MouseMove(new Point(100, 90), RawInputModifiers.MiddleMouseButton);
            window.MouseMove(new Point(60, 40), RawInputModifiers.MiddleMouseButton);
            window.MouseUp(new Point(60, 40), MouseButton.Middle);
            Render(window);
            Assert.Equal(new Vector(40, 50), view.Offset);

            view.GestureBindings[new ScrollGesture(ScrollInputGesture.MouseLeftDrag)] = ScrollGestureAction.Pan;
            window.MouseDown(new Point(100, 90), MouseButton.Left);
            window.MouseMove(new Point(100, 90), RawInputModifiers.LeftMouseButton);
            window.MouseMove(new Point(70, 60), RawInputModifiers.LeftMouseButton);
            window.MouseUp(new Point(70, 60), MouseButton.Left);
            Render(window);
            Assert.Equal(new Vector(70, 80), view.Offset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Scaffold_DoesNotAttachAppBarScrollConnectionToScrollView()
    {
        using var clock = new MotionTestClockScope();
        var topState = new TopAppBarState();
        var topBar = new MediumTopAppBar
        {
            ScrollBehavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.ExitUntilCollapsed)
            {
                State = topState,
                SnapAnimationSpec = null,
                FlingAnimationSpec = null,
            },
        };
        var view = new ScrollView
        {
            IsZoomEnabled = true,
            IsScrollInertiaEnabled = false,
            Content = new Border { Width = 300, Height = 800, Background = Brushes.LightGray },
        };
        var body = new Border { Child = view };
        var scaffold = new Scaffold { TopBar = topBar, Content = body };
        body.Bind(Border.WidthProperty, new Avalonia.Data.Binding("Bounds.Width") { Source = scaffold });
        body.Bind(Border.HeightProperty, new Avalonia.Data.Binding("Bounds.Height") { Source = scaffold });
        body.Bind(Border.PaddingProperty, scaffold.GetObservable(Scaffold.ContentPaddingProperty));
        var window = new MotionTestWindow { Width = 300, Height = 400, WindowDecorations = WindowDecorations.None };
        window.Styles.Add(new MaterialTheme());
        window.Content = scaffold;
        window.Show();
        try
        {
            Render(window);
            view.ZoomFactor = 1.5;
            Render(window);
            view.Offset = new Vector(20, 60);
            Render(window);
            Assert.Equal(0, topState.HeightOffset);
            Assert.Equal(1.5, view.ZoomFactor, 3);
            Assert.Equal(new Vector(20, 60), view.Offset);
        }
        finally { window.Close(); }
    }

    private static ScrollView CreateView(object content) => new()
    {
        Width = 160,
        Height = 140,
        Content = content,
        IsHorizontalMeasureInfinite = true,
        IsVerticalMeasureInfinite = true,
        HorizontalScrollBarVisibility = ScrollBarVisibilityMode.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibilityMode.Auto,
    };

    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow
        {
            Width = 160,
            Height = 140,
            WindowDecorations = WindowDecorations.None,
        };
        window.Styles.Add(new MaterialTheme());
        window.Content = new Grid { Children = { control } };
        window.Show();
        Render(window);
        return window;
    }

    private static void Render(Window window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        for (var i = 0; i < 4; i++)
            using (window.CaptureRenderedFrame()) { }
    }

    private static T Part<T>(Control owner, string name) where T : Control =>
        owner.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
}
