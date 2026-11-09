using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class TabIndicatorLayoutTests
{
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void CustomLayoutGetsCurrentPositionsAndOwnsChildPlacement(bool native, bool scrollable)
    {
        using var clock = new MotionTestClockScope();
        var indicator = new ProbeIndicator();
        var row = CreateRow(native, scrollable, indicator);
        var window = Show(row);
        try
        {
            var positions = TabRowAppearance.GetTabPositions(row);
            Assert.Equal(positions, indicator.TabPositions);
            Assert.Equal(scrollable ? 0 : positions[0].Width, indicator.MinimumSize.Width);
            Assert.Equal(scrollable ? positions[0].ContentWidth : positions[0].Width, indicator.MaximumSize.Width);
            Assert.Equal(new Size(indicator.MaximumSize.Width, 48), indicator.Bounds.Size);
            Assert.Equal(48, indicator.Child!.Bounds.Height);
            Assert.False(row.GetVisualDescendants().OfType<TemplatedControl>().Single(x => x.Name == "PART_Indicator").ClipToBounds);
            Select(row, 1); window.UpdateLayout();
            Assert.Equal(1, indicator.SelectedIndex);
            Assert.Equal(positions[1].Width, indicator.Child.Bounds.Width);
            Assert.Equal(positions[1].Left - (scrollable ? (positions[1].Width - positions[1].ContentWidth) / 2 : 0), indicator.Child.Bounds.X);
            Assert.Equal(0, row.GetVisualDescendants().OfType<TabRowContent>().Single().IndicatorWidth);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void CustomLayoutRetainsOversizeMeasurementAndCoercion(bool rtl)
    {
        var indicator = new ProbeIndicator { Oversize = true };
        var row = CreateRow(false, false, indicator);
        row.FlowDirection = rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
        var window = Show(row);
        try
        {
            Assert.Equal(new Size(240, 48), indicator.Bounds.Size);
            var host = row.GetVisualDescendants().OfType<TemplatedControl>().Single(x => x.Name == "PART_Indicator");
            Assert.Equal(-80, host.Bounds.X);
            Assert.Equal(240, host.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void ChangingTemplateCancelsDefaultMotionAndRestoresCurrentSelection(bool native)
    {
        using var clock = new MotionTestClockScope();
        var row = CreateRow(native, false, null);
        var window = Show(row);
        try
        {
            clock.SendFrame(TimeSpan.Zero); Select(row, 2); window.UpdateLayout();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(32)); window.UpdateLayout();
            var indicator = new ProbeIndicator();
            TabRowAppearance.SetIndicator(row, new FuncControlTemplate((_, _) => indicator)); window.UpdateLayout();
            Assert.Equal(2, indicator.SelectedIndex);
            Assert.Equal(0, row.GetVisualDescendants().OfType<TabRowContent>().Single().IndicatorWidth);
            var label = native ? (Control)((TabItem)row.Items[2]!).Header! : (Control)((Tab)row.Items[2]!).Content!;
            label.Width = 96; window.UpdateLayout();
            Assert.Equal(TabRowAppearance.GetTabPositions(row), indicator.TabPositions);
            row.ClearValue(TabRowAppearance.IndicatorProperty); window.UpdateLayout();
            Assert.Equal(160, row.GetVisualDescendants().OfType<TabRowContent>().Single().IndicatorOffset);
            var root = window.Content;
            window.Content = null; window.UpdateLayout();
            window.Content = root; window.UpdateLayout();
            Assert.Equal(160, row.GetVisualDescendants().OfType<TabRowContent>().Single().IndicatorOffset);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void ReattachedCustomRootReceivesSelectionChangedWhileDetached(bool native)
    {
        var indicator = new ProbeIndicator();
        var row = CreateRow(native, false, indicator);
        var window = Show(row);
        try
        {
            var root = (Grid)window.Content!;
            root.Children.Clear(); window.UpdateLayout();
            Select(row, 2);
            root.Children.Add(row); window.UpdateLayout();
            Assert.Equal(2, indicator.SelectedIndex);
            Assert.Equal(160, indicator.Child!.Bounds.X);
            Assert.Equal(TabRowAppearance.GetTabPositions(row), indicator.TabPositions);
        }
        finally { window.Close(); }
    }

    private sealed class ProbeIndicator : TabIndicatorLayout
    {
        public bool Oversize { get; init; }
        public ProbeIndicator() => Child = new Border();
        protected override Size MeasureIndicator(Size availableSize)
        {
            Child!.Measure(new Size(TabPositions[SelectedIndex].Width, availableSize.Height));
            return new Size(Oversize ? TabPositions[^1].Right : availableSize.Width, availableSize.Height);
        }
        protected override Size ArrangeIndicator(Size finalSize)
        {
            var p = TabPositions[SelectedIndex];
            Child!.Arrange(new Rect(p.Left - (IsScrollable ? (p.Width - p.ContentWidth) / 2 : 0), 0, p.Width, finalSize.Height));
            return finalSize;
        }
    }
    private static ItemsControl CreateRow(bool native, bool scrollable, ProbeIndicator? indicator)
    {
        ItemsControl row = native ? new TabControl() : new TabRow();
        row.Width = 240; row.HorizontalAlignment = HorizontalAlignment.Left; row.VerticalAlignment = VerticalAlignment.Top;
        TabRowAppearance.SetIsScrollable(row, scrollable);
        if (indicator is not null) TabRowAppearance.SetIndicator(row, new FuncControlTemplate((_, _) => indicator));
        foreach (var width in new[] { 32d, 64d, 110d })
        {
            var label = new Border { Width = width, Height = 16 };
            row.Items.Add(native ? new TabItem { Header = label } : new Tab { Content = label });
        }
        return row;
    }
    private static void Select(ItemsControl row, int index)
    {
        if (row is TabRow tabs) tabs.SelectedIndex = index;
        else ((TabControl)row).SelectedIndex = index;
    }
    private static MotionTestWindow Show(Control row)
    {
        var window = new MotionTestWindow { Width = 320, Height = 180 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { row } };
        window.Show(); window.UpdateLayout(); return window;
    }
}
