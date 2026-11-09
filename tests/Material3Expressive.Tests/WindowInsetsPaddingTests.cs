using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class WindowInsetsPaddingTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void NestedInsetsUnionWithoutDoublePaddingAndScaffoldReadsTheConsumption(bool rtl)
    {
        var screen = new Scaffold { ContentWindowInsets = new Thickness(20, 40, 30, 50) };
        var inner = new MaterialWindowInsetsPadding { Insets = new Thickness(12, 30, 24, 40), Child = screen };
        var outer = new MaterialWindowInsetsPadding { Insets = new Thickness(8, 20, 16, 10), Child = inner,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(outer);
        try
        {
            Assert.Equal(new Thickness(12, 30, 24, 40), screen.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            Assert.Equal(new Thickness(rtl ? 6 : 8, 10, rtl ? 8 : 6, 10), screen.ContentPadding);
            Assert.Equal(new Size(264, 170), screen.Bounds.Size);
            Assert.Equal(new Point(12, 30), PhysicalOrigin(screen, window));
            outer.Insets = new Thickness(40, 50, 60, 70); Flush(window);
            Assert.Equal(new Thickness(), screen.ContentPadding);
            Assert.Equal(new Size(200, 120), screen.Bounds.Size);
            Assert.Equal(new Point(40, 50), PhysicalOrigin(screen, window));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ExplicitInsetConsumptionUsesMaxWhilePaddingConsumptionAddsAndReparentingRefreshes()
    {
        var child = new MaterialWindowInsetsPadding { Insets = new Thickness(30), Child = new Border() };
        var first = new Border { Child = child }; MaterialWindowInsets.SetConsumeInsets(first, new Thickness(10));
        var second = new Border(); MaterialWindowInsets.SetConsumeInsets(second, new Thickness(25));
        var root = new Grid { Children = { first, second } }; var window = Show(root);
        try
        {
            Assert.Equal(new Thickness(20), child.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            MaterialWindowInsets.SetConsumePadding(first, new Thickness(5)); Flush(window);
            Assert.Equal(new Thickness(20), child.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty)); // max(5,10), not 15.
            first.Child = null; second.Child = child; Flush(window);
            Assert.Equal(new Thickness(5), child.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            MaterialWindowInsets.SetConsumeInsets(second, new Thickness(40)); Flush(window);
            Assert.Equal(default, child.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            second.Child = null; first.Child = child; Flush(window);
            Assert.Equal(new Thickness(20), child.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void AppBarInsidePaddingDoesNotApplyTheSamePhysicalInsetsAgain()
    {
        var bar = new BottomAppBar { WindowInsets = new Thickness(8, 20, 16, 10) };
        var outer = new MaterialWindowInsetsPadding { Insets = bar.WindowInsets, Child = bar };
        var window = Show(outer);
        try
        {
            Assert.Equal(bar.EffectiveExpandedHeight, bar.DesiredSize.Height);
            Assert.Equal(276, bar.Bounds.Width);
            Assert.Equal(new Point(8, 20), PhysicalOrigin(bar, window));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ExcessInsetsConstrainDesiredSizeButKeepPhysicalChildOffset()
    {
        var child = new Border(); var outer = new MaterialWindowInsetsPadding { Insets = new Thickness(200, 150), Child = child };
        var window = Show(outer);
        try
        {
            Assert.Equal(new Size(300, 240), outer.DesiredSize);
            Assert.Equal(default, child.Bounds.Size);
            Assert.Equal(new Point(200, 150), PhysicalOrigin(child, window));
        }
        finally { window.Close(); }
    }
    private static Point PhysicalOrigin(Control control, Window window)
    {
        var first = control.TranslatePoint(default, window)!.Value;
        var last = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), window)!.Value;
        return new(Math.Min(first.X, last.X), Math.Min(first.Y, last.Y));
    }
    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 300, Height = 240 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
