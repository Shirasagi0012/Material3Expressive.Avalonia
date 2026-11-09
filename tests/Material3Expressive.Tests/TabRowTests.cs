using Avalonia;
using Avalonia.Media;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Typography;
using Xunit;

namespace Material3Expressive.Tests;

public class TabRowTests
{
    [AvaloniaTheory]
    [InlineData(false, TabRowVariant.Primary)]
    [InlineData(false, TabRowVariant.Secondary)]
    [InlineData(true, TabRowVariant.Primary)]
    [InlineData(true, TabRowVariant.Secondary)]
    public void Row_PreservesPixelDivisionAndContentWidths(bool scrollable, TabRowVariant variant)
    {
        var row = new TabRow { Width = 241, IsScrollable = scrollable, Variant = variant };
        foreach (var size in new[] { 32d, 64d, 110d }) row.Items.Add(new Tab { Content = new Border { Width = size, Height = 16 } });
        var window = Show(row);
        try
        {
            Assert.Equal(48, row.Bounds.Height);
            Assert.Equal(scrollable ? new[] { 90d, 96d, 142d } : new[] { 80d, 80d, 80d }, row.TabPositions.Select(position => position.Width));
            Assert.Equal(scrollable ? new[] { 52d, 142d, 238d } : new[] { 0d, 80d, 160d }, row.TabPositions.Select(position => position.Left));
            Assert.Equal(scrollable ? new[] { 32d, 64d, 110d } : new[] { 32d, 48d, 48d }, row.TabPositions.Select(position => position.ContentWidth));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(TabContentLayout.Stacked, false, 48)]
    [InlineData(TabContentLayout.Stacked, true, 72)]
    [InlineData(TabContentLayout.Leading, true, 48)]
    [InlineData(TabContentLayout.Custom, false, 16)]
    public void Header_DefaultHeightUsesTheActualContentPolicy(TabContentLayout layout, bool icon, double expectedHeight)
    {
        var label = new Border { Width = 40, Height = 16 };
        TextBlock.SetBaselineOffset(label, 12);
        var tab = new Tab { Width = 120, Content = label, ContentLayout = layout, Icon = icon ? new Border { Width = 24, Height = 24 } : null };
        var window = Show(tab);
        try { Assert.Equal(expectedHeight, tab.Bounds.Height); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void IndependentTab_ClickAndAutomationRequest_DoNotMutateSelection()
    {
        var tab = new Tab { Content = "Music", Width = 120 };
        var row = new TabRow { Width = 240, Items = { tab } };
        var window = Show(row); var clicks = 0; tab.Click += (_, _) => clicks++;
        try
        {
            var center = tab.TranslatePoint(new Point(tab.Bounds.Width / 2, tab.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
            Assert.Equal(1, clicks); Assert.False(tab.IsSelected); Assert.Equal(0, row.SelectedIndex);
            var peer = Assert.IsAssignableFrom<ISelectionItemProvider>(ControlAutomationPeer.CreatePeerForElement(tab));
            peer.Select(); Assert.Equal(2, clicks); Assert.False(tab.IsSelected);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter)]
    [InlineData(Key.Space)]
    public void IndependentTab_KeyboardInvokesOnReleaseAndCancelsWhenDisabled(Key key)
    {
        var tab = new Tab { Content = "Music", Width = 120 };
        var window = Show(tab); var clicks = 0; tab.Click += (_, _) => clicks++;
        try
        {
            tab.Focus(); window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(0, clicks);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(1, clicks);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); tab.IsEnabled = false;
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(1, clicks);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeTabControl_RetainsContentSelectionAndDataTemplates()
    {
        var first = new Border { Width = 80, Height = 30 };
        var second = new Border { Width = 100, Height = 40 };
        var native = new TabControl { Width = 240, Items = { new TabItem { Header = "First", Content = first }, new TabItem { Header = "Second", Content = second } } };
        var window = Show(native);
        try
        {
            Assert.Same(first, native.SelectedContent);
            Assert.Equal(2, TabRowAppearance.GetTabPositions(native).Count);
            Assert.Same(first, Part<ContentPresenter>(native, "PART_SelectedContentHost").Child);
            native.SelectedIndex = 1; window.UpdateLayout();
            Assert.Same(second, native.SelectedContent);
            Assert.Same(second, Part<ContentPresenter>(native, "PART_SelectedContentHost").Child);
            Assert.True(((TabItem)native.Items[1]!).IsSelected);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TemplateReplacement_CancelsTracksAndRestoresPositions()
    {
        using var clock = new MotionTestClockScope();
        var row = new TabRow { Width = 240, Items = { new Tab { Content = "One" }, new Tab { Content = "Two" } } };
        var window = Show(row);
        try
        {
            clock.SendFrame(TimeSpan.Zero); row.SelectedIndex = 1; window.UpdateLayout(); clock.SendFrame(TimeSpan.FromMilliseconds(16));
            row.Template = new FuncControlTemplate<TabRow>((_, _) => new Border()); row.ApplyTemplate(); window.UpdateLayout();
            clock.SendFrame(TimeSpan.FromMilliseconds(32)); Assert.Equal(0, clock.PendingFrameCount); Assert.Empty(row.TabPositions);
            row.ClearValue(TemplatedControl.TemplateProperty); row.ApplyTemplate(); window.UpdateLayout(); Assert.Equal(2, row.TabPositions.Count);
            Assert.Equal(1, row.SelectedIndex); Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LabelPolicy_UsesTitleSmallAndRealMultilineBaselines()
    {
        var text = new TextBlock { Text = "First\nSecond" };
        var tab = new Tab { Width = 120, Content = text, Icon = new Border { Width = 24, Height = 24 } };
        var window = Show(tab);
        try
        {
            var presenter = Part<ContentPresenter>(tab, "PART_Label");
            Assert.Equal(14, text.FontSize); Assert.Equal(Avalonia.Media.TextAlignment.Center, text.TextAlignment);
            Assert.Equal(2, text.TextLayout!.TextLines.Count);
            var first = Math.Round(text.TextLayout.TextLines[0].Baseline, MidpointRounding.AwayFromZero);
            var last = Math.Round(text.TextLayout.TextLines[0].Height + text.TextLayout.TextLines[1].Baseline, MidpointRounding.AwayFromZero);
            Assert.Equal(tab.Bounds.Height - last - 9, presenter.Bounds.Y);
            Assert.Equal(presenter.Bounds.Y - (24 + 20 - first), Part<ContentPresenter>(tab, "PART_Icon").Bounds.Y);
            tab.ContentLayout = TabContentLayout.Custom; window.UpdateLayout();
            Assert.Null(MaterialText.GetStyle(presenter)); Assert.Equal(16, text.FontSize); Assert.Equal(48, tab.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeTabControl_DirectionalFocusRetainsNativeSelection()
    {
        var native = new TabControl { Width = 240, Items = { new TabItem { Header = "First" }, new TabItem { Header = "Second" }, new TabItem { Header = "Third" } } };
        var window = Show(native);
        try
        {
            ((TabItem)native.Items[0]!).Focus(NavigationMethod.Directional);
            window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(1, native.SelectedIndex);
            Assert.True(((TabItem)native.Items[1]!).IsFocused);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScrollableRow_UserInputInterruptsCenteringAndDetachCancelsFrames(bool gesture)
    {
        using var clock = new MotionTestClockScope();
        var row = new TabRow { Width = 240, IsScrollable = true };
        for (var i = 0; i < 8; i++) row.Items.Add(new Tab { Content = new Border { Width = 40, Height = 16 } });
        var window = Show(row);
        var root = window.Content;
        try
        {
            clock.SendFrame(TimeSpan.Zero); row.SelectedIndex = 7; window.UpdateLayout();
            for (var i = 1; i <= 6; i++) { clock.SendFrame(TimeSpan.FromMilliseconds(i * 16)); window.UpdateLayout(); }
            var scroller = row.GetVisualDescendants().OfType<ScrollViewer>().Single(); Assert.True(scroller.Offset.X > 0);
            if (gesture) ((Control)scroller.Content!).RaiseEvent(new ScrollGestureEventArgs(1, new Vector(50, 0)));
            else window.MouseWheel(new Point(100, 20), new Vector(-1, 0));
            window.UpdateLayout();
            var manualOffset = scroller.Offset.X;
            for (var i = 7; i <= 30; i++) { clock.SendFrame(TimeSpan.FromMilliseconds(i * 16)); window.UpdateLayout(); }
            Assert.Equal(manualOffset, scroller.Offset.X);
            row.SelectedIndex = 0; window.UpdateLayout();
            for (var i = 31; i <= 60; i++) { clock.SendFrame(TimeSpan.FromMilliseconds(i * 16)); window.UpdateLayout(); }
            Assert.Equal(0, scroller.Offset.X);
            row.SelectedIndex = 7; window.UpdateLayout(); clock.SendFrame(TimeSpan.FromMilliseconds(976));
            window.Content = null; window.UpdateLayout(); clock.SendFrame(TimeSpan.FromMilliseconds(992));
            Assert.Equal(0, clock.PendingFrameCount); Assert.Null(window.MotionRoot.Diagnostics.LastError);
            Assert.Empty(row.TabPositions);
            window.Content = root; window.UpdateLayout(); Assert.Equal(8, row.TabPositions.Count);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeAndControlledDataTemplates_RealizeMaterialHeaders()
    {
        var native = new TabControl { Width = 240, ItemsSource = new[] { "One", "Two" },
            ItemTemplate = new FuncDataTemplate<string>((value, _) => new TextBlock { Text = value }),
            ContentTemplate = new FuncDataTemplate<string>((value, _) => new TextBlock { Text = $"Page {value}" }) };
        var window = Show(native);
        try
        {
            Assert.Equal(2, TabRowAppearance.GetTabPositions(native).Count);
            Assert.Equal("Page One", Assert.IsType<TextBlock>(Part<ContentPresenter>(native, "PART_SelectedContentHost").Child).Text);
            native.SelectedIndex = 1; window.UpdateLayout();
            Assert.Equal("Page Two", Assert.IsType<TextBlock>(Part<ContentPresenter>(native, "PART_SelectedContentHost").Child).Text);
            var controlled = new TabRow { Width = 240, ItemsSource = new[] { "One", "Two" },
                ItemTemplate = new FuncDataTemplate<string>((value, _) => new Tab { Content = value }) };
            window.Content = controlled; window.UpdateLayout();
            Assert.Equal(2, controlled.TabPositions.Count); Assert.Equal(2, controlled.GetVisualDescendants().OfType<Tab>().Count());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContentResize_ReplaysRealMeasureAfterIntrinsicProbes()
    {
        var label = new Border { Width = 32, Height = 16 };
        var row = new TabRow { Width = 352, Items = { new Tab { Content = label }, new Tab { Content = "Other" }, new Tab { Content = "Last" } } };
        var window = Show(row);
        try
        {
            Assert.Equal(32, label.Bounds.Width);
            label.Width = 90; window.UpdateLayout();
            Assert.Equal(85, ((ContentPresenter)label.GetVisualParent()!).Bounds.Width); // 117 pixels minus 32 DIP padding.
            Assert.Equal(90, label.Bounds.Width); // Explicit Avalonia Width can overflow its allocated box.
            Assert.Equal(85, row.TabPositions[0].ContentWidth);
            label.Width = 24; window.UpdateLayout();
            Assert.Equal(24, label.Bounds.Width); Assert.Equal(24, row.TabPositions[0].ContentWidth);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void ContentResize_StartsIndicatorOnTheSameFrameForFixedAndScrollableRows(bool native, bool scrollable)
    {
        using var clock = new MotionTestClockScope();
        var label = new Border { Width = 32, Height = 16 };
        ItemsControl row = native ? new TabControl() : new TabRow();
        row.Width = 300;
        TabRowAppearance.SetIsScrollable(row, scrollable);
        row.Items.Add(native ? new TabItem { Header = label } : new Tab { Content = label });
        row.Items.Add(native ? new TabItem { Header = new Border { Width = 32, Height = 16 } }
            : new Tab { Content = new Border { Width = 32, Height = 16 } });
        var window = Show(row);
        try
        {
            var content = row.GetVisualDescendants().OfType<TabRowContent>().Single();
            clock.SendFrame(TimeSpan.Zero); window.UpdateLayout();
            label.Width = 90; window.UpdateLayout();
            Assert.Equal(32, content.IndicatorWidth);
            clock.SendFrame(TimeSpan.FromMilliseconds(1)); window.UpdateLayout();
            clock.SendFrame(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
            Assert.Equal(32, content.IndicatorWidth); // The first animation sample keeps its start value.
            clock.SendFrame(TimeSpan.FromMilliseconds(32)); window.UpdateLayout();
            Assert.True(content.IndicatorWidth > 32, "Content remeasure must not insert an extra intrinsic-only frame.");
            label.Width = 24; window.UpdateLayout();
            for (var frame = 48; frame <= 800; frame += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(frame)); window.UpdateLayout(); }
            Assert.Equal(24, content.IndicatorWidth);
            window.Content = null; window.UpdateLayout();
            Assert.Equal(0, clock.PendingFrameCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Row_CustomSlotsAndEmptyItems_UseNormalTemplates()
    {
        var row = new TabRow { Width = 240, Indicator = null, Divider = null };
        var window = Show(row);
        try
        {
            Assert.Empty(row.TabPositions); Assert.Equal(0, row.Bounds.Height);
            row.Items.Add(new Tab { Content = new Border { Width = 40, Height = 16 } }); window.UpdateLayout();
            Assert.Single(row.TabPositions); Assert.Equal(48, row.Bounds.Height);
            Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<TabPosition>>(Part<TemplatedControl>(row, "PART_Indicator").DataContext));
            row.Indicator = new FuncControlTemplate((_, _) => new Border { Height = 7 }); window.UpdateLayout();
            Assert.Equal(7, Part<Avalonia.Controls.Primitives.TemplatedControl>(row, "PART_Indicator").Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void IndicatorWidthPolicyIsIndependentAndInterruptible(bool native, bool scrollable)
    {
        using var clock = new MotionTestClockScope();
        ItemsControl row = native ? new TabControl() : new TabRow();
        row.Width = 300;
        TabRowAppearance.SetVariant(row, TabRowVariant.Secondary);
        TabRowAppearance.SetIsScrollable(row, scrollable);
        TabRowAppearance.SetIndicatorMatchContentSize(row, true);
        foreach (var width in new[] { 32d, 64d })
        {
            var label = new Border { Width = width, Height = 16 };
            row.Items.Add(native ? new TabItem { Header = label } : new Tab { Content = label });
        }
        var window = Show(row);
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        try
        {
            var content = row.GetVisualDescendants().OfType<TabRowContent>().Single();
            Frames(50); Assert.Equal(32, content.IndicatorWidth);
            TabRowAppearance.SetIndicatorMatchContentSize(row, false); window.UpdateLayout(); Frames(5);
            var moving = content.IndicatorWidth; Assert.True(moving > 32);
            TabRowAppearance.SetIndicatorMatchContentSize(row, true); window.UpdateLayout();
            Assert.Equal(moving, content.IndicatorWidth); Frames(60); Assert.Equal(32, content.IndicatorWidth);
            TabRowAppearance.SetIndicatorMatchContentSize(row, null); window.UpdateLayout(); Frames(60);
            Assert.Equal(TabRowAppearance.GetTabPositions(row)[0].Width, content.IndicatorWidth);
            TabRowAppearance.SetVariant(row, TabRowVariant.Primary); window.UpdateLayout(); Frames(60);
            Assert.Equal(32, content.IndicatorWidth);
            // An explicit false survives changes to the visual variant, and label changes do not
            // change its target when the tab's allocated width is unchanged.
            TabRowAppearance.SetIndicatorMatchContentSize(row, false); window.UpdateLayout(); Frames(60);
            Assert.Equal(TabRowAppearance.GetTabPositions(row)[0].Width, content.IndicatorWidth);
            if (row is TabControl tabs) Assert.Equal(0, tabs.SelectedIndex);
            else Assert.Equal(0, ((TabRow)row).SelectedIndex);
            var root = window.Content;
            window.Content = null; window.UpdateLayout(); Assert.Equal(0, clock.PendingFrameCount);
            window.Content = root; window.UpdateLayout(); Frames(60);
            Assert.Equal(TabRowAppearance.GetTabPositions(row)[0].Width, row.GetVisualDescendants().OfType<TabRowContent>().Single().IndicatorWidth);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void NativeSelectionUsesMaterialCenteringWithoutAnImmediateScrollJump(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var row = new TabControl { Width = 240, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        TabRowAppearance.SetIsScrollable(row, true);
        TabRowAppearance.SetIndicatorMatchContentSize(row, true);
        for (var i = 0; i < 5; i++) row.Items.Add(new TabItem { Header = new Border { Width = 64, Height = 16 } });
        var window = Show(row);
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
        try
        {
            Frames(50); var scroller = row.GetVisualDescendants().OfType<ScrollViewer>().Single();
            Assert.False(row.AutoScrollToSelectedItem); Assert.Equal(0, scroller.Offset.X);
            row.SelectedIndex = 4; window.UpdateLayout();
            Assert.Equal(0, scroller.Offset.X); Frames(5); Assert.True(scroller.Offset.X > 0);
            var moving = scroller.Offset.X;
            row.SelectedIndex = 0; window.UpdateLayout(); Assert.Equal(moving, scroller.Offset.X);
            Frames(70); Assert.Equal(0, scroller.Offset.X);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    static MotionTestWindow Show(Control content)
    {
        content.HorizontalAlignment = HorizontalAlignment.Left; content.VerticalAlignment = VerticalAlignment.Top;
        var window = new MotionTestWindow { Width = 320, Height = 180 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { content } }; window.Show(); window.UpdateLayout(); return window;
    }
    static T Part<T>(Control root, string name) where T : Control => root.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);
}
