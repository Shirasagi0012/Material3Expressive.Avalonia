using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Headless.XUnit;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class LegacyTabRowTests
{
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void LegacyRetainsPrimaryColorFullWidthIndicatorAndOldContentWidth(bool native, bool scrollable)
    {
        var row = Create(native, scrollable); var window = Show(row);
        try
        {
            var positions = TabRowAppearance.GetTabPositions(row);
            Assert.Equal(scrollable ? new[] { 90d, 90d, 110d } : new[] { 80d, 80d, 80d }, positions.Select(p => p.Width));
            Assert.Equal(scrollable ? new[] { -24d, 32d, 78d } : new[] { 24d, 32d, 48d }, positions.Select(p => p.ContentWidth));
            var content = row.GetVisualDescendants().OfType<TabRowContent>().Single();
            Assert.Equal((float)positions[0].Width, content.IndicatorWidth);
            var header = row.GetVisualDescendants().OfType<TabHeader>().First();
            Assert.Equal((Color)row.FindResource(MaterialTokens.Sys.Color.Primary)!, header.PaintedContentColor);
            var indicator = Part<TemplatedControl>(row, "PART_Indicator");
            Assert.Equal(default, indicator.GetVisualDescendants().OfType<Border>().Single().CornerRadius);
            TabRowAppearance.SetIndicatorMatchContentSize(row, true); window.UpdateLayout();
            Assert.Equal((float)positions[0].Width, content.IndicatorWidth);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void LegacyScrollableDividerSpansExtentAboveTheScroller(bool rtl)
    {
        var row = Create(false, true); row.Height = 8; row.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var window = Show(row);
        try
        {
            var divider = Part<TemplatedControl>(row, "PART_Divider");
            var chrome = row.GetVisualDescendants().OfType<TabRowChrome>().Single();
            Assert.Equal(394, divider.Bounds.Width);
            Assert.Equal(11, divider.Bounds.Y);
            Assert.Equal(1, divider.ZIndex);
            Assert.Equal(8, chrome.Bounds.Height);
            TabRowAppearance.SetVariant(row, TabRowVariant.Primary); window.UpdateLayout();
            Assert.Equal(241, divider.Bounds.Width); Assert.Equal(7, divider.Bounds.Y); Assert.Equal(-1, divider.ZIndex);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LegacyPaddingIsRoundedBeforePositionsAccumulate()
    {
        var row = Create(false, true); TabRowAppearance.SetEdgePadding(row, 7.375);
        var window = Show(row);
        try { Assert.Equal(7, TabRowAppearance.GetTabPositions(row)[0].Left); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void VariantReplacementAndDetachDiscardLegacyAnimationAndScrollState()
    {
        using var clock = new MotionTestClockScope();
        var row = (TabRow)Create(false, true); var window = Show(row);
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        try
        {
            Frames(2); row.SelectedIndex = 2; window.UpdateLayout(); Frames(4);
            var content = row.GetVisualDescendants().OfType<TabRowContent>().Single();
            Assert.True(content.IndicatorOffset > 52);
            row.Variant = TabRowVariant.Primary; window.UpdateLayout();
            Assert.Equal((float)row.TabPositions[2].Left, content.IndicatorOffset);
            row.Variant = TabRowVariant.Legacy; window.UpdateLayout();
            Assert.Equal((float)row.TabPositions[2].Width, content.IndicatorWidth);
            window.Content = null; window.UpdateLayout();
            Assert.Equal(0, clock.PendingFrameCount); Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    private static ItemsControl Create(bool native, bool scrollable)
    {
        ItemsControl row = native ? new TabControl() : new TabRow(); row.Width = 241;
        TabRowAppearance.SetVariant(row, TabRowVariant.Legacy); TabRowAppearance.SetIsScrollable(row, scrollable);
        foreach (var width in new[] { 8d, 64d, 110d })
        {
            Control item = native ? new TabItem { Header = new Border { Width = width, Height = 16 } }
                : new Tab { Content = new Border { Width = width, Height = 16 } };
            TabAppearance.SetContentLayout(item, TabContentLayout.Custom); row.Items.Add(item);
        }
        return row;
    }
    private static MotionTestWindow Show(Control content)
    {
        content.HorizontalAlignment = HorizontalAlignment.Left; content.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 320, Height = 180 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { content } }; window.Show(); window.UpdateLayout(); return window;
    }
    private static T Part<T>(Control root, string name) where T : Control => root.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);
}
