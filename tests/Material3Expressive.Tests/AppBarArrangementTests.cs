using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarArrangementTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void AbsoluteOrderSurvivesDirectionChangeAndClearRestoresTheme(bool rtl)
    {
        var bar = new FlexibleBottomAppBar { Width = 300, ContentPadding = default,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var first = new Border { Width = 40, Height = 24 };
        var second = new Border { Width = 60, Height = 32 };
        bar.Items.Add(first); bar.Items.Add(second);
        using var clock = new MotionTestClockScope(); var window = DialogTests.Show(bar);
        double X(Control c) => new Rect(c.Bounds.Size).TransformToAABB(c.TransformToVisual(window)!.Value).X
            - new Rect(bar.Bounds.Size).TransformToAABB(bar.TransformToVisual(window)!.Value).X;
        try
        {
            bar.HorizontalArrangement = MaterialHorizontalArrangement.Absolute.Left; window.UpdateLayout();
            Assert.Equal(0, X(first)); Assert.Equal(40, X(second));
            bar.FlowDirection = rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft; window.UpdateLayout();
            Assert.Equal(0, X(first)); Assert.Equal(40, X(second));
            bar.HorizontalArrangement = MaterialHorizontalArrangement.SpacedBy(-8, HorizontalAlignment.Center); window.UpdateLayout();
            Assert.Equal(32, Math.Abs(X(first) - X(second)) - (bar.FlowDirection == FlowDirection.RightToLeft ? 20 : 0));
            bar.ClearValue(BottomAppBar.HorizontalArrangementProperty); window.UpdateLayout();
            Assert.Equal(MaterialHorizontalArrangement.SpaceBetween, bar.HorizontalArrangement);
            Assert.Equal(bar.FlowDirection == FlowDirection.RightToLeft ? 260 : 0, X(first));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CustomArrangementReceivesMeasuredItemsAndUpdatesAfterCollectionChanges()
    {
        var arrangement = new ObservedArrangement();
        var bar = new FlexibleBottomAppBar { Width = 300, ContentPadding = default, HorizontalArrangement = arrangement };
        bar.Items.Add(new Border { Width = 40, Height = 24 });
        using var clock = new MotionTestClockScope(); var window = DialogTests.Show(bar);
        try
        {
            Assert.Equal(new[] { 40 }, arrangement.Sizes);
            bar.Items.Add(new Border { Width = 60, Height = 32 }); window.UpdateLayout();
            Assert.Equal(new[] { 40, 60 }, arrangement.Sizes);
            bar.FlowDirection = FlowDirection.RightToLeft; window.UpdateLayout();
            Assert.Equal(FlowDirection.RightToLeft, arrangement.Direction);
            bar.Items.Clear(); window.UpdateLayout(); Assert.Empty(arrangement.Sizes);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RuntimeArrangementKeepsButtonActivationAndBoundContent()
    {
        var button = new Button { Content = "Action" };
        var bar = new FlexibleBottomAppBar { Width = 300, Items = { button } };
        using var clock = new MotionTestClockScope(); var window = DialogTests.Show(bar);
        try
        {
            var clicks = 0; button.Click += (_, _) => clicks++;
            bar.HorizontalArrangement = MaterialHorizontalArrangement.Absolute.Right; window.UpdateLayout();
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            Assert.Equal(1, clicks); Assert.Equal("Action", button.Content);
        }
        finally { window.Close(); }
    }

    private sealed record ObservedArrangement : MaterialHorizontalArrangement
    {
        public int[] Sizes { get; private set; } = [];
        public FlowDirection Direction { get; private set; }
        public override void Arrange(double density, int totalSize, ReadOnlySpan<int> sizes, FlowDirection direction, Span<int> positions)
        {
            Sizes = sizes.ToArray(); Direction = direction;
            Center.Arrange(density, totalSize, sizes, direction, positions);
        }
    }
}
