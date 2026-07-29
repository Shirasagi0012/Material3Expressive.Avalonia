// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt:552-855,
// ListItemDefaults.kt:246-399, 438-536, 1135-1159, and tokens/ListTokens.kt
// (VERSION 29.0.0). Compose callers combine SegmentedListItem with
// ListItemDefaults.segmentedShapes(index, count), segmentedColors(), and a 2dp SegmentedGap.
// These tests lock the Avalonia/XAML adaptation in which MaterialSegmentedList owns visible-index
// inference and layout while each MaterialListItem keeps its own InteractionMode and selection.

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialSegmentedListTests
{
    [AvaloniaFact]
    public void ThreeVisibleChildren_UseFirstMiddleLastShapesAndTwoDpGaps()
    {
        var first = Row("First");
        var middle = Row("Middle");
        var last = Row("Last");
        var list = new MaterialSegmentedList
        {
            Width = 240,
            Children = { first, middle, last }
        };

        ThemeTestHelper.ShowWindow(list);

        Assert.Equal(2, list.Spacing);
        Assert.Equal(172, list.DesiredSize.Height);
        Assert.Equal(0, first.Bounds.Y);
        Assert.Equal(58, middle.Bounds.Y);
        Assert.Equal(116, last.Bounds.Y);
        Assert.Equal(MaterialListItemSegmentPosition.First, first.SegmentPosition);
        Assert.Equal(MaterialListItemSegmentPosition.Middle, middle.SegmentPosition);
        Assert.Equal(MaterialListItemSegmentPosition.Last, last.SegmentPosition);
        Assert.Equal(new CornerRadius(16, 16, 4, 4), first.ParityShadowShape.Corners);
        Assert.Equal(new CornerRadius(4), middle.ParityShadowShape.Corners);
        Assert.Equal(new CornerRadius(4, 4, 16, 16), last.ParityShadowShape.Corners);
    }

    [AvaloniaFact]
    public void HiddenChildren_DoNotConsumePositionsOrGaps()
    {
        var hiddenBefore = Row("Hidden before");
        hiddenBefore.IsVisible = false;
        var visible = Row("Visible");
        var hiddenAfter = Row("Hidden after");
        hiddenAfter.IsVisible = false;
        var list = new MaterialSegmentedList
        {
            Width = 240,
            Children = { hiddenBefore, visible, hiddenAfter }
        };

        ThemeTestHelper.ShowWindow(list);

        Assert.Equal(56, list.DesiredSize.Height);
        Assert.Equal(MaterialListItemSegmentPosition.None, hiddenBefore.SegmentPosition);
        Assert.Equal(MaterialListItemSegmentPosition.Single, visible.SegmentPosition);
        Assert.Equal(MaterialListItemSegmentPosition.None, hiddenAfter.SegmentPosition);
        Assert.Equal(new CornerRadius(16), visible.ParityShadowShape.Corners);
    }

    [AvaloniaFact]
    public void SelectedMiddleRow_UsesTheSelectedStateShapeInsteadOfItsRestingSegmentShape()
    {
        var first = Row("First");
        var selected = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.SingleSelection,
            IsSelected = true,
            Content = "Selected",
            Height = 56
        };
        var last = Row("Last");
        var list = new MaterialSegmentedList
        {
            Width = 240,
            Children = { first, selected, last }
        };

        ThemeTestHelper.ShowWindow(list);

        Assert.Equal(MaterialListItemSegmentPosition.Middle, selected.SegmentPosition);
        Assert.Equal(new CornerRadius(16), selected.ParityShadowShape.Corners);
    }

    [AvaloniaFact]
    public void RemovingAChild_ClearsItsPositionAndReflowsTheRemainingRows()
    {
        var removed = Row("Removed");
        var next = Row("Next");
        var last = Row("Last");
        var list = new MaterialSegmentedList
        {
            Width = 240,
            Children = { removed, next, last }
        };

        ThemeTestHelper.ShowWindow(list);
        list.Children.Remove(removed);

        Assert.Equal(MaterialListItemSegmentPosition.None, removed.SegmentPosition);
        Assert.Equal(MaterialListItemSegmentPosition.First, next.SegmentPosition);
        Assert.Equal(MaterialListItemSegmentPosition.Last, last.SegmentPosition);
        Assert.Equal(new CornerRadius(16, 16, 4, 4), next.ParityShadowShape.Corners);
        Assert.Equal(new CornerRadius(4, 4, 16, 16), last.ParityShadowShape.Corners);
    }

    [AvaloniaFact]
    public void ExplicitSpacing_IsRoundedToPhysicalPixelsAndOverridesTheTokenDefault()
    {
        var first = Row("First");
        var last = Row("Last");
        var list = new MaterialSegmentedList
        {
            Width = 240,
            Spacing = 3,
            Children = { first, last }
        };
        var window = ThemeTestHelper.ShowWindow(list);
        window.SetRenderScaling(1.25);

        list.Measure(new Size(240, double.PositiveInfinity));
        list.Arrange(new Rect(0, 0, 240, list.DesiredSize.Height));

        Assert.Equal(3, list.Spacing);
        Assert.Equal(3.2, last.Bounds.Y - first.Bounds.Bottom, precision: 10);
    }

    [AvaloniaFact]
    public void NonListItemChildren_AreRejectedBeforeLayout()
    {
        var list = new MaterialSegmentedList
        {
            Children = { new Border() }
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => list.Measure(new Size(240, double.PositiveInfinity)));

        Assert.Contains(nameof(MaterialListItem), exception.Message, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void AutomationRole_IsAListWithoutAnOwnedSelectionProvider()
    {
        var list = new MaterialSegmentedList
        {
            Children =
            {
                new MaterialListItem
                {
                    InteractionMode = ListItemInteractionMode.MultiSelection,
                    Content = "Independent choice"
                }
            }
        };
        var peer = ControlAutomationPeer.CreatePeerForElement(list);

        Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
        Assert.Null(peer.GetProvider<ISelectionProvider>());
    }

    private static MaterialListItem Row(string headline) =>
        new()
        {
            InteractionMode = ListItemInteractionMode.Click,
            Content = headline,
            Height = 56
        };
}
