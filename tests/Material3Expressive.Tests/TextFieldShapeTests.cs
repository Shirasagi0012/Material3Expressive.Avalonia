using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class TextFieldShapeTests
{
    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ShapeDoesNotChangeEditorLayoutOrNativeEditing(bool outlined, bool secure)
    {
        var window = Show(out var box, outlined, secure);
        try
        {
            var presenter = Part<TextPresenter>(box);
            var layout = Part<TextFieldLayout>(box);
            var before = (presenter.Bounds, box.Bounds, layout.ContainerBounds);
            MaterialTextField.SetShape(box, new CutCornerShape(24)); Flush(window);
            Assert.Equal(before, (presenter.Bounds, box.Bounds, layout.ContainerBounds));
            Assert.Null(presenter.Clip);
            Assert.Null(layout.Clip);
            box.Focus(); window.KeyTextInput("sample");
            Assert.Equal("sample", box.Text);
            box.SelectAll(); window.KeyTextInput("changed");
            Assert.Equal("changed", box.Text);
            MaterialTextField.SetShape(box, new RoundedCornerShape(new RelativeScalar(.5, RelativeUnit.Relative)));
            Flush(window);
            Assert.Same(presenter, Part<TextPresenter>(box));
            Assert.Equal("changed", box.Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenericOutlineTracksShapeMutationDirectionAndContainerSize(bool rtl)
    {
        var window = Show(out var box, true);
        try
        {
            box.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            var shape = new CutCornerShape { TopStart = new(.5, RelativeUnit.Relative) };
            MaterialTextField.SetShape(box, shape); Flush(window);
            var outline = Part<SurfaceOutline>(box);
            // Local coordinates cancel the tree mirror: logical start remains local left.
            Assert.False(outline.ShapePath!.FillContains(new Point(1, 1)));
            Assert.True(outline.ShapePath.FillContains(new Point(outline.Bounds.Width - 1, 1)));
            var before = outline.ShapePath;
            box.Height = 100; Flush(window);
            Assert.NotSame(before, outline.ShapePath);
            Assert.Equal(Part<TextFieldLayout>(box).ContainerBounds.Size, outline.Bounds.Size);
            shape.TopStart = new(0, RelativeUnit.Absolute); Flush(window);
            Assert.Null(outline.ShapePath); // CutCornerShape(0) is Outline.Rectangle upstream.
            MaterialTextField.SetShape(box, new CutCornerShape(12)); Flush(window);
            before = outline.ShapePath;
            shape.TopStart = new(40, RelativeUnit.Absolute); Flush(window);
            Assert.Same(before, outline.ShapePath); // Old mutable shape is disconnected.
            MaterialTextField.SetShape(box, null); Flush(window);
            Assert.Null(outline.ShapePath);
            Assert.Equal(box.CornerRadius, outline.CornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void OutlinedShapePreservesCutoutAndRoundedBorderBranch()
    {
        var window = Show(out var box, true);
        try
        {
            MaterialTextField.SetLabel(box, new Border { Width = 40, Height = 12 });
            MaterialTextField.SetAlwaysMinimizeLabel(box, true);
            MaterialTextField.SetShape(box, new RoundedCornerShape(18)); Flush(window);
            var layout = Part<TextFieldLayout>(box);
            var decoration = Part<TextFieldDecoration>(box);
            var outline = Part<SurfaceOutline>(box);
            Assert.Null(outline.ShapePath);
            Assert.Equal(new CornerRadius(18), outline.CornerRadius);
            Assert.True(layout.CutoutBounds.Width > 0);
            Assert.False(decoration.Clip!.FillContains(layout.CutoutBounds.Center));
            Assert.True(decoration.Clip.FillContains(layout.ContainerBounds.Center));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ShapeSubscriptionEndsOnDetachAndReconnectsOnRemount()
    {
        var shape = new CountingShape();
        var window = Show(out var box, true);
        try
        {
            MaterialTextField.SetShape(box, shape); Flush(window);
            Assert.True(shape.Calls > 0);
            var root = window.Content;
            window.Content = null; Flush(window);
            var before = shape.Calls;
            shape.Inset = 12; Flush(window);
            Assert.Equal(before, shape.Calls);
            window.Content = root; Flush(window);
            Assert.True(shape.Calls > before);
            Assert.Equal(12, Part<SurfaceOutline>(box).ShapePath!.Bounds.X);
        }
        finally { window.Close(); }
    }

    private sealed class CountingShape : MaterialShape
    {
        public static readonly StyledProperty<double> InsetProperty = AvaloniaProperty.Register<CountingShape, double>(nameof(Inset));
        public double Inset { get => GetValue(InsetProperty); set => SetValue(InsetProperty, value); }
        internal int Calls { get; private set; }
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
        {
            Calls++;
            return RoundedRectangleGeometry.Create(new Rect(Inset, 0, Math.Max(0, size.Width - Inset), size.Height), default);
        }
    }

    private static MotionTestWindow Show(out TextBox box, bool outlined, bool secure = false)
    {
        var window = new MotionTestWindow { Width = 420, Height = 220 };
        window.Styles.Add(new FluentTheme()); window.Styles.Add(new MaterialTheme());
        var name = $"Material{(outlined ? "Outlined" : "Filled")}{(secure ? "Secure" : "")}TextFieldTheme";
        box = new TextBox { Width = 300, Height = 80, Theme = (ControlTheme)window.FindResource(name)! };
        window.Content = new StackPanel { Children = { new Button { Content = "Focus target" }, box } };
        window.Show(); Flush(window); return window;
    }
    private static T Part<T>(TextBox box) where T : Control => box.GetVisualDescendants().OfType<T>().Single();
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
