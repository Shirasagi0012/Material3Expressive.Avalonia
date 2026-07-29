using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialListItemPanelTests
{
    [AvaloniaFact]
    public void OneLineItem_UsesConditionalGapsAndCentresAllSlots()
    {
        var panel = new MaterialListItemPanel();
        var leading = AddSlot(panel, MaterialListItemSlot.Leading, 24, 24);
        var headline = AddSlot(panel, MaterialListItemSlot.Headline, 80, 20);
        var trailing = AddSlot(panel, MaterialListItemSlot.Trailing, 20, 20);

        Layout(panel, 200);

        Assert.Equal(new Size(200, 56), panel.DesiredSize);
        Assert.Equal(new Rect(16, 16, 24, 24), leading.Bounds);
        Assert.Equal(52, headline.Bounds.X);
        Assert.Equal(18, headline.Bounds.Y);
        Assert.Equal(new Rect(164, 18, 20, 20), trailing.Bounds);
    }

    [AvaloniaFact]
    public void TwoLineItem_DoesNotReserveAnEmptyLeadingSlot()
    {
        var panel = new MaterialListItemPanel();
        var headline = AddSlot(panel, MaterialListItemSlot.Headline, 80, 20);
        var supporting = AddSlot(panel, MaterialListItemSlot.Supporting, 80, 20);

        Layout(panel, 200);

        Assert.Equal(new Size(200, 72), panel.DesiredSize);
        Assert.Equal(16, headline.Bounds.X);
        Assert.Equal(16, headline.Bounds.Y);
        Assert.Equal(36, supporting.Bounds.Y);
    }

    [AvaloniaFact]
    public void MultilineSupportingContent_MakesTheItemThreeLinesAndTopAlignsSlots()
    {
        var panel = new MaterialListItemPanel();
        var leading = AddSlot(panel, MaterialListItemSlot.Leading, 24, 24);
        var headline = AddSlot(panel, MaterialListItemSlot.Headline, 80, 20);
        var supporting = AddSlot(panel, MaterialListItemSlot.Supporting, 80, 32);
        var trailing = AddSlot(panel, MaterialListItemSlot.Trailing, 20, 20);

        Layout(panel, 200);

        Assert.Equal(new Size(200, 88), panel.DesiredSize);
        Assert.Equal(10, leading.Bounds.Y);
        Assert.Equal(10, headline.Bounds.Y);
        Assert.Equal(30, supporting.Bounds.Y);
        Assert.Equal(10, trailing.Bounds.Y);
    }

    [AvaloniaFact]
    public void OverlineAndSupportingContent_UseTheThreeLineMinimumEvenWhenSupportingIsShort()
    {
        var panel = new MaterialListItemPanel();
        var overline = AddSlot(panel, MaterialListItemSlot.Overline, 80, 12);
        var headline = AddSlot(panel, MaterialListItemSlot.Headline, 80, 20);
        var supporting = AddSlot(panel, MaterialListItemSlot.Supporting, 80, 20);

        Layout(panel, 200);

        Assert.Equal(new Size(200, 88), panel.DesiredSize);
        Assert.Equal(10, overline.Bounds.Y);
        Assert.Equal(22, headline.Bounds.Y);
        Assert.Equal(42, supporting.Bounds.Y);
    }

    [AvaloniaFact]
    public void RightToLeft_PlacesLeadingAtStartAndTrailingAtEnd()
    {
        var panel = new MaterialListItemPanel
        {
            FlowDirection = Avalonia.Media.FlowDirection.RightToLeft,
            ContentPadding = new Thickness(20, 8, 24, 12)
        };
        var leading = AddSlot(panel, MaterialListItemSlot.Leading, 24, 24);
        var headline = AddSlot(panel, MaterialListItemSlot.Headline, 80, 20);
        var trailing = AddSlot(panel, MaterialListItemSlot.Trailing, 20, 20);

        Layout(panel, 200);

        Assert.Equal(152, leading.Bounds.X);
        Assert.Equal(60, headline.Bounds.X);
        Assert.Equal(20, trailing.Bounds.X);
    }

    [AvaloniaFact]
    public void ContentPadding_MapsTheOwningPaddingIntoTheContentLane()
    {
        var panel = new MaterialListItemPanel { ContentPadding = new Thickness(20, 8, 24, 12) };
        var leading = AddSlot(panel, MaterialListItemSlot.Leading, 24, 24);
        var headline = AddSlot(panel, MaterialListItemSlot.Headline, 80, 20);
        var trailing = AddSlot(panel, MaterialListItemSlot.Trailing, 20, 20);

        Layout(panel, 200);

        Assert.Equal(new Size(200, 56), panel.DesiredSize);
        Assert.Equal(new Rect(20, 14, 24, 24), leading.Bounds);
        Assert.Equal(new Point(56, 16), headline.Bounds.Position);
        Assert.Equal(new Rect(156, 16, 20, 20), trailing.Bounds);
    }

    [AvaloniaFact]
    public void WrappedSingleTextBlockSupportingContent_UsesItsActualTextLineCount()
    {
        var panel = new MaterialListItemPanel();
        AddSlot(panel, MaterialListItemSlot.Headline, 60, 20);
        var supporting = new TextBlock
        {
            Text = "alpha beta gamma delta",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 8,
            LineHeight = 10,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        MaterialListItemPanel.SetSlot(supporting, MaterialListItemSlot.Supporting);
        panel.Children.Add(supporting);

        Layout(panel, 120);

        Assert.True(supporting.TextLayout.TextLines.Count > 1);
        Assert.True(
            supporting.DesiredSize.Height <= 30,
            $"Expected a multi-line supporting TextBlock at or below the fallback threshold; " +
            $"lines={supporting.TextLayout.TextLines.Count}, height={supporting.DesiredSize.Height}.");
        Assert.Equal(88, panel.DesiredSize.Height);
    }

    [AvaloniaFact]
    public void NonTextBlockSupportingContent_RetainsTheThirtyDpMultilineFallback()
    {
        var oneLineFallback = new MaterialListItemPanel();
        AddSlot(oneLineFallback, MaterialListItemSlot.Headline, 60, 20);
        AddSlot(oneLineFallback, MaterialListItemSlot.Supporting, 60, 30);

        var multilineFallback = new MaterialListItemPanel();
        AddSlot(multilineFallback, MaterialListItemSlot.Headline, 60, 20);
        AddSlot(multilineFallback, MaterialListItemSlot.Supporting, 60, 31);

        Layout(oneLineFallback, 100);
        Layout(multilineFallback, 100);

        Assert.Equal(72, oneLineFallback.DesiredSize.Height);
        Assert.Equal(88, multilineFallback.DesiredSize.Height);
    }

    private static Border AddSlot(MaterialListItemPanel panel, MaterialListItemSlot slot, double width, double height)
    {
        // A fixed-size Border otherwise centres itself inside the rectangle supplied by the
        // panel, hiding the natural Placeable-width position under test. Pin it to the logical
        // start of that rectangle for deterministic geometry assertions.
        var child = new Border { Width = width, Height = height, HorizontalAlignment = HorizontalAlignment.Left };
        MaterialListItemPanel.SetSlot(child, slot);
        panel.Children.Add(child);
        return child;
    }

    private static void Layout(MaterialListItemPanel panel, double width)
    {
        panel.Measure(new Size(width, double.PositiveInfinity));
        panel.Arrange(new Rect(panel.DesiredSize));
    }
}
