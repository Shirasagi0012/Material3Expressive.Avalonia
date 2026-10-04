using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Styles;
using SmoothScroll.Avalonia.Controls;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ScrollViewerTests
{
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void NestedWheel_UsesNativeBoundaryBubbling(bool chaining)
    {
        using var clock = new MotionTestClockScope();
        var inner = new ScrollViewer
        {
            Height = 70,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            IsScrollInertiaEnabled = false,
            IsScrollChainingEnabled = chaining,
            Content = new Border { Height = 400, Background = Brushes.Red },
        };
        var outer = Viewer(new StackPanel
        {
            Children = { inner, new Border { Height = 500, Background = Brushes.Blue } },
        });
        outer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        outer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        outer.IsScrollInertiaEnabled = false;
        var window = Show(outer);
        try
        {
            Assert.IsType<ScrollViewerPresenter>(inner.Presenter);
            Assert.IsType<ScrollViewerPresenter>(outer.Presenter);
            window.MouseWheel(new Point(50, 30), new Vector(0, -1));
            Render(window);
            Assert.True(inner.Offset.Y > 0);
            Assert.Equal(0, outer.Offset.Y);

            // A partially available inner viewport keeps this entire event. The original
            // input route does not redistribute the remainder of the same wheel event.
            var maximum = inner.ScrollBarMaximum.Y;
            inner.Offset = new Vector(0, maximum - 10);
            Render(window);
            window.MouseWheel(new Point(50, 30), new Vector(0, -1));
            Render(window);
            Assert.Equal(maximum, inner.Offset.Y);
            Assert.Equal(0, outer.Offset.Y);

            // Once the UI mirror is at the boundary, the next event may bubble outward.
            window.MouseWheel(new Point(50, 30), new Vector(0, -1));
            Render(window);
            Assert.Equal(maximum, inner.Offset.Y);
            Assert.Equal(chaining, outer.Offset.Y > 0);
            Assert.Null(window.MotionRoot.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MaterialThemeAlone_ReservesScrollBarsAndKeepsNativeBindings()
    {
        var viewer = Viewer(new Border { Width = 400, Height = 500 });
        viewer.IsDeferredScrollingEnabled = true;
        viewer.AllowAutoHide = false;
        var window = Show(viewer);
        try
        {
            Assert.Same(window.FindResource("MaterialScrollViewerTemplate"), viewer.Template);
            var presenter = Assert.IsType<ScrollViewerPresenter>(viewer.Presenter);
            Assert.True(presenter.IsLoaded);
            Assert.NotEqual(default, presenter.Viewport);
            var vertical = Part<ScrollBar>(viewer, "PART_VerticalScrollBar");
            var horizontal = Part<ScrollBar>(viewer, "PART_HorizontalScrollBar");
            Assert.Equal(new Size(152, 132), viewer.Viewport);
            Assert.Equal(presenter.Bounds.Right, vertical.Bounds.Left);
            Assert.Equal(presenter.Bounds.Bottom, horizontal.Bounds.Top);
            Assert.Equal(viewer.Viewport.Height, vertical.ViewportSize);
            Assert.Equal(viewer.ScrollBarMaximum.Y, vertical.Maximum);
            Assert.True(Part<Track>(vertical, "PART_Track").DeferThumbDrag);
            Assert.False(vertical.AllowAutoHide);
            viewer.Offset = new Vector(20, 40);
            Assert.Equal(viewer.Offset, presenter.Offset);
            Assert.Equal(40, vertical.Value);
            vertical.Value = 60;
            Assert.Equal(60, viewer.Offset.Y);
            Assert.Equal(60, presenter.Offset.Y);
            viewer.Content = new Border { Width = 40, Height = 40 };
            Render(window);
            Assert.False(vertical.IsVisible);
            Assert.False(horizontal.IsVisible);
            Assert.Equal(new Size(160, 140), viewer.Viewport);
            Assert.Equal(default, viewer.Offset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void HiddenAndDisabledAxes_KeepDistinctScrollAndMeasureSemantics()
    {
        var viewer = Viewer(new Border { Width = 400, Height = 500 });
        viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        var window = Show(viewer);
        try
        {
            var presenter = Assert.IsType<ScrollViewerPresenter>(viewer.Presenter);
            Assert.Equal(new Size(160, 140), viewer.Viewport);
            Assert.True(presenter.CanHorizontallyScroll);
            Assert.True(presenter.IsHorizontalMeasureInfinite);
            Assert.False(presenter.CanVerticallyScroll);
            Assert.False(presenter.IsVerticalMeasureInfinite);
            Assert.Equal(ScrollMode.Disabled, presenter.VerticalScrollMode);
            viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            Render(window);
            Assert.False(presenter.CanHorizontallyScroll);
            Assert.False(presenter.IsHorizontalMeasureInfinite);
            Assert.True(presenter.CanVerticallyScroll);
            Assert.Equal(new Size(152, 140), viewer.Viewport);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContentTemplateAndPadding_AreBoundAndEmptyViewportIsHitTestable()
    {
        var child = new Border { Width = 24, Height = 20, Background = Brushes.Red };
        var template = new FuncDataTemplate<string>((_, _) => child);
        var viewer = Viewer("value");
        viewer.ContentTemplate = template;
        viewer.Padding = new Thickness(4);
        viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        viewer.IsScrollChainingEnabled = false;
        viewer.IsScrollInertiaEnabled = false;
        var window = Show(viewer);
        try
        {
            var presenter = Assert.IsType<ScrollViewerPresenter>(viewer.Presenter);
            Assert.Same(child, presenter.Child);
            Assert.Same(template, presenter.ContentTemplate);
            Assert.Equal(viewer.Padding, presenter.Padding);
            Assert.False(presenter.IsScrollChainingEnabled);
            Assert.False(ScrollViewer.GetIsScrollInertiaEnabled(presenter));
            Assert.Same(presenter, window.InputHitTest(new Point(140, 100)));
            viewer.Background = Brushes.Blue;
            Assert.Same(Brushes.Blue, presenter.Background);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LogicalContent_PreservesItsUnitsAndNativePaging()
    {
        var content = new LogicalContent();
        var viewer = Viewer(content);
        var window = Show(viewer);
        try
        {
            Assert.Equal(content.Extent, viewer.Extent);
            Assert.Equal(content.Viewport, viewer.Viewport);
            Assert.Equal(content.ScrollSize, viewer.SmallChange);
            Assert.Equal(content.PageScrollSize, viewer.LargeChange);
            viewer.Offset = new Vector(3, 8);
            Assert.Equal(viewer.Offset, content.Offset);
            viewer.LineDown();
            Assert.Equal(new Vector(3, 10), content.Offset);
            content.Offset = new Vector(6, 12);
            Assert.Equal(content.Offset, viewer.Offset);
            Assert.Equal(12, Part<ScrollBar>(viewer, "PART_VerticalScrollBar").Value);
            Assert.True(double.IsFinite(content.LastConstraint.Width));
            Assert.True(double.IsFinite(content.LastConstraint.Height));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextFieldWithoutFluent_UsesSmoothPresenterAndPreservesNativeEditing(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var box = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            MinLines = 2, MaxLines = 3, Width = 160 };
        var window = Show(box);
        try
        {
            box.Theme = (ControlTheme)window.FindResource(outlined
                ? "MaterialOutlinedTextFieldTheme" : "MaterialFilledTextFieldTheme")!;
            Render(window);
            box.Focus();
            window.KeyTextInput("a\nb\nc\nd\ne\nf");
            Render(window);
            Assert.Equal("a\nb\nc\nd\ne\nf", box.Text);
            var viewer = Part<ScrollViewer>(box, "PART_ScrollViewer");
            Assert.IsType<ScrollViewerPresenter>(viewer.Presenter);
            Assert.True(viewer.Extent.Height > viewer.Viewport.Height);
            viewer.ScrollToEnd();
            Render(window);
            Assert.True(viewer.Offset.Y > 0);
            Assert.Null(window.MotionRoot.LastError);
        }
        finally { window.Close(); }
    }

    private static ScrollViewer Viewer(object content) => new()
    {
        Width = 160, Height = 140, Content = content,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
    };

    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 160, Height = 140,
            WindowDecorations = WindowDecorations.None };
        window.Styles.Add(new MaterialTheme());
        window.Content = new Grid { Children = { control } };
        window.Show(); Render(window);
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

    private sealed class LogicalContent : Control, ILogicalScrollable
    {
        private Vector _offset;
        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public bool IsLogicalScrollEnabled => true;
        public Size ScrollSize => new(1, 2);
        public Size PageScrollSize => new(4, 5);
        public Size Extent => new(40, 50);
        public Size Viewport => new(10, 10);
        public Size LastConstraint { get; private set; }
        public Vector Offset
        {
            get => _offset;
            set { _offset = value; RaiseScrollInvalidated(EventArgs.Empty); }
        }
        public event EventHandler? ScrollInvalidated;
        public void RaiseScrollInvalidated(EventArgs e) => ScrollInvalidated?.Invoke(this, e);
        public bool BringIntoView(Control target, Rect targetRect) => false;
        public Control? GetControlInDirection(NavigationDirection direction, Control? from) => null;
        protected override Size MeasureOverride(Size availableSize)
        {
            LastConstraint = availableSize;
            return availableSize;
        }
    }
}
