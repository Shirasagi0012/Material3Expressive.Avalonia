using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class AnchoredContainerInputTests
{
    [AvaloniaTheory]
    [InlineData("swipe")]
    [InlineData("sheet")]
    public void DraggingContentSlider_PreservesNativeCapture(string kind)
    {
        using var clock = new MotionTestClockScope();
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 40,
            Orientation = kind == "sheet" ? Orientation.Vertical : Orientation.Horizontal,
            Width = kind == "sheet" ? 48 : 240, Height = kind == "sheet" ? 240 : 48 };
        ContentControl container = kind == "swipe"
            ? new SwipeToDismissBox { Width = 240, Height = 64, Content = slider }
            : new BottomSheet { State = new(SheetValue.Expanded), Content = slider, DragHandle = null, DragHandleTemplate = null };
        var window = DialogTests.Show(container);
        try
        {
            double Offset() => container is SwipeToDismissBox swipe ? swipe.State.CurrentOffset : ((BottomSheet)container).State.CurrentOffset;
            var offset = Offset();
            var thumb = slider.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Thumb>().Single();
            var point = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), window)!.Value;
            var end = point + (kind == "sheet" ? new Vector(0, 60) : new Vector(60, 0));
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left);
            window.MouseMove(end, RawInputModifiers.LeftMouseButton); window.MouseUp(end, MouseButton.Left);
            Assert.NotEqual(40, slider.Value);
            Assert.Equal(offset, Offset());
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("swipe")]
    [InlineData("sheet")]
    public void DisablingParentGestures_PreservesPendingChildClick(string kind)
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Content = "Action", HorizontalAlignment = HorizontalAlignment.Left };
        ContentControl container = kind == "swipe"
            ? new SwipeToDismissBox { Width = 240, Height = 64, Content = button }
            : new BottomSheet { State = new(SheetValue.Expanded), Content = new Border { Height = 128, Child = button } };
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        var window = DialogTests.Show(container);
        try
        {
            var point = button.TranslatePoint(new Point(20, 20), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left);
            if (container is SwipeToDismissBox swipe) swipe.GesturesEnabled = false;
            else ((BottomSheet)container).GesturesEnabled = false;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            window.MouseUp(point, MouseButton.Left);
            Assert.Equal(1, clicks);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
}
