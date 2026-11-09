using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class CardColumnTests
{
    [AvaloniaTheory]
    [InlineData(100, 34, 33, 33)]
    [InlineData(2, 0, 1, 1)]
    [InlineData(0, 0, 0, 0)]
    public void EqualWeightsDistributeRoundingRemainderInChildOrder(double height, double a, double b, double c)
    {
        var column = new CardColumn();
        var items = Enumerable.Range(0, 3).Select(_ => Item(1)).ToArray();
        column.Children.AddRange(items);
        Layout(column, height);
        Assert.Equal(new[] { a, b, c }, items.Select(item => item.Bounds.Height));
        Assert.Equal(height, column.DesiredSize.Height);
    }

    [AvaloniaFact]
    public void UnweightedChildrenAreMeasuredFirstEvenWhenTheyFollowWeightedChildren()
    {
        var first = Item(1); var second = Item(2); var third = Item(3);
        var fixedChild = new RecordingChild(20, 20);
        var column = new CardColumn { Children = { first, fixedChild, second, third } };
        Layout(column, 101);
        Assert.Equal(new[] { 13d, 27d, 41d }, new[] { first.Bounds.Height, second.Bounds.Height, third.Bounds.Height });
        Assert.Equal(new Size(100, 101), fixedChild.LastConstraint);
        Assert.Equal(13, fixedChild.Bounds.Y);
        Assert.Equal(33, second.Bounds.Y);
        Assert.Equal(60, third.Bounds.Y);
    }

    [AvaloniaFact]
    public void FillFalseDoesNotRedistributeUnusedSpaceAndNeverWritesChildMinimums()
    {
        var first = Item(1); first.Fill = false;
        var second = Item(1);
        var column = new CardColumn { Children = { first, second } };
        Layout(column, 100);
        Assert.Equal(10, first.Bounds.Height); Assert.Equal(50, second.Bounds.Height);
        Assert.Equal(60, column.DesiredSize.Height); Assert.Equal(10, second.Bounds.Y);
        foreach (var item in new[] { first, second })
        {
            var child = (RecordingChild)item.Child!;
            Assert.Equal(new Size(100, 50), child.LastConstraint);
            Assert.Equal(10, child.Bounds.Height);
            Assert.False(child.IsSet(Control.MinHeightProperty));
        }
    }

    [AvaloniaTheory]
    [InlineData(0, 0, 0)]
    [InlineData(100, 30, 60)]
    public void UnboundedHeightDistributesOnlyColumnMinimum(double minimum, double firstHeight, double secondHeight)
    {
        var first = Item(1); var second = Item(2);
        var column = new CardColumn { MinHeight = minimum, Children = { new RecordingChild(20, 10), first, second } };
        Layout(column, double.PositiveInfinity);
        Assert.Equal(firstHeight, first.Bounds.Height); Assert.Equal(secondHeight, second.Bounds.Height);
        Assert.Equal(Math.Max(10, minimum), column.DesiredSize.Height);
    }

    [AvaloniaFact]
    public void AllocationUpdatesForWeightsFillConstraintsAndChildVisibility()
    {
        var first = Item(1); var second = Item(1);
        var column = new CardColumn { Children = { first, second } };
        Layout(column, 100); Assert.Equal(50, first.Bounds.Height);
        first.Weight = 3; Layout(column, 100); Assert.Equal(75, first.Bounds.Height); Assert.Equal(25, second.Bounds.Height);
        first.Fill = false; Layout(column, 100); Assert.Equal(10, first.Bounds.Height); Assert.Equal(25, second.Bounds.Height);
        first.Fill = true; Layout(column, 120); Assert.Equal(90, first.Bounds.Height); Assert.Equal(30, second.Bounds.Height);
        second.IsVisible = false; Layout(column, 120); Assert.Equal(120, first.Bounds.Height);
        second.IsVisible = true; first.Weight = double.NaN; Layout(column, 120);
        Assert.Equal(10, first.Bounds.Height); Assert.Equal(110, second.Bounds.Height);
        column.Children.Remove(second); var other = new StackPanel { Children = { second } };
        other.Measure(new Size(100, 120)); Assert.Equal(10, second.DesiredSize.Height);
    }

    [AvaloniaTheory]
    [InlineData(CardColumnAlignment.Start, 0)]
    [InlineData(CardColumnAlignment.Center, 40)]
    [InlineData(CardColumnAlignment.End, 80)]
    public void ItemHasExplicitLogicalAlignment(CardColumnAlignment alignment, double x)
    {
        var item = Item(1); item.Alignment = alignment;
        var column = new CardColumn { Width = 100, Children = { item } };
        Layout(column, 100); Assert.Equal(x, item.Bounds.X); Assert.Equal(20, item.Bounds.Width);
        Assert.Equal(0, item.Child!.Bounds.Y); Assert.Equal(10, item.Child.Bounds.Height);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void LogicalAlignmentUsesPhysicalCenterRoundingInEitherDirection(bool rtl)
    {
        var items = Enumerable.Range(0, 3).Select(i => new CardColumnItem
        { Weight = 1, Alignment = (CardColumnAlignment)i, Child = new RecordingChild(20, 10) }).ToArray();
        var column = new CardColumn { Width = 101, Height = 100, Children = { items[0], items[1], items[2] } };
        var window = new Window { Width = 160, Height = 160, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Content = column };
        window.Show(); window.UpdateLayout();
        try
        {
            // TranslatePoint on an RTL child starts at its physical right edge.
            double PhysicalLeft(Control child)
            {
                var a = child.TranslatePoint(default, window)!.Value.X;
                var b = child.TranslatePoint(new Point(child.Bounds.Width, 0), window)!.Value.X;
                return Math.Min(a, b);
            }
            var columnLeft = PhysicalLeft(column);
            Assert.Equal(rtl ? 81 : 0, PhysicalLeft(items[0]) - columnLeft);
            Assert.Equal(41, PhysicalLeft(items[1]) - columnLeft);
            Assert.Equal(rtl ? 0 : 81, PhysicalLeft(items[2]) - columnLeft);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void UnweightedItemAndPlainChildrenRetainPreferredSizes()
    {
        var item = Item(double.NaN); var plain = new RecordingChild(40, 60);
        var column = new CardColumn { Children = { item, plain } };
        Layout(column, 50);
        Assert.Equal(10, item.Bounds.Height); Assert.Equal(40, plain.Bounds.Height);
        Assert.Equal(new Size(100, 40), plain.LastConstraint);
    }

    [AvaloniaFact]
    public void WeightValidationMatchesOptionalPositiveWeightContract()
    {
        var item = Item(double.PositiveInfinity);
        Assert.Throws<ArgumentException>(() => item.Weight = 0);
        Assert.Throws<ArgumentException>(() => item.Weight = -1);
        Assert.Throws<ArgumentException>(() => item.Weight = double.Epsilon);
        var column = new CardColumn { Children = { item } };
        Layout(column, 100); Assert.Equal(100, item.Bounds.Height);
    }

    private static CardColumnItem Item(double weight) => new() { Weight = weight, Child = new RecordingChild(20, 10) };
    private static void Layout(CardColumn column, double maxHeight)
    {
        column.Measure(new Size(100, maxHeight));
        column.Arrange(new Rect(column.DesiredSize));
    }
    private sealed class RecordingChild(double width, double height) : Control
    {
        public Size LastConstraint { get; private set; }
        protected override Size MeasureOverride(Size availableSize)
        {
            LastConstraint = availableSize;
            return new Size(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        }
    }
}
