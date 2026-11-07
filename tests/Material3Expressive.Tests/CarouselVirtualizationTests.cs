using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class CarouselVirtualizationTests
{
    [AvaloniaFact]
    public void TabSearch_HonorsTabIndexConsumedKeysAndOwnerNavigationMode()
    {
        var carousel = Create(CarouselVariant.Uncontained);
        carousel.ItemWidth = 320; carousel.ItemSpacing = 0;
        var first = new Border { Focusable = true, TabIndex = 1 };
        var second = new Border { Focusable = true, TabIndex = 2 };
        carousel.ItemsSource = null;
        carousel.Items.Add(new CarouselItem { Content = new StackPanel { Children = { second, first } } });
        carousel.Items.Add(new CarouselItem { Content = new Border { Focusable = true } });
        var outside = new Button { Content = "Outside" };
        var window = DialogTests.Show(new StackPanel { Children = { carousel, outside } });
        try
        {
            first.Focus(); Tab(); Assert.True(second.IsFocused);
            second.KeyDown += Consume;
            Tab(); Assert.True(second.IsFocused); Assert.Null(carousel.ContainerFromIndex(1));
            second.KeyDown -= Consume;
            KeyboardNavigation.SetTabNavigation(carousel, KeyboardNavigationMode.Once);
            Tab(); Assert.True(outside.IsFocused); Assert.Null(carousel.ContainerFromIndex(1));
        }
        finally { window.Close(); }
        void Tab()
        {
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.None, null);
            Flush(window);
        }
        void Consume(object? sender, KeyEventArgs e) { if (e.Key == Key.Tab) e.Handled = true; }
    }

    [AvaloniaFact]
    public void TabSearch_PreservesContentOrderAndExitsAtTheCollectionBoundary()
    {
        var carousel = Create(CarouselVariant.Uncontained);
        carousel.ItemWidth = 320; carousel.ItemSpacing = 0;
        carousel.ItemsSource = new[] { 0, 1 };
        carousel.ItemTemplate = new FuncDataTemplate<int>((item, _) => new StackPanel
        {
            Children = { new Border { Focusable = true, Tag = item * 10 }, new Border { Focusable = true, Tag = item * 10 + 1 } },
        });
        var outside = new Button { Content = "Outside" };
        var window = DialogTests.Show(new StackPanel { Children = { carousel, outside } });
        try
        {
            carousel.ContainerFromIndex(0)!.GetVisualDescendants().OfType<Control>().Single(c => c.Tag is 0).Focus();
            Tab(); Assert.Equal(1, ((Control)window.FocusManager!.GetFocusedElement()!).Tag);
            Tab(); Assert.Equal(10, ((Control)window.FocusManager!.GetFocusedElement()!).Tag);
            Tab(); Assert.Equal(11, ((Control)window.FocusManager!.GetFocusedElement()!).Tag);
            Tab(); Assert.True(outside.IsFocused);
        }
        finally { window.Close(); }
        void Tab()
        {
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.None, null);
            Flush(window);
        }
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void KeyboardSearch_RealizesNextPageAndReleasesTemporaryPages(bool rtl, bool tab)
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.Uncontained);
        carousel.UserScrollEnabled = false;
        carousel.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        carousel.ItemTemplate = new FuncDataTemplate<int>((item, _) => new Border { Tag = item, Focusable = true });
        var window = DialogTests.Show(carousel);
        try
        {
            var from = Child(1);
            Assert.True(from.Focus()); Flush(window);
            Advance();
            // Focus relocation may itself expose the next page. Move to the final
            // composed page so each request exercises the beyond-bounds path.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var last = carousel.GetRealizedContainers().Select(carousel.IndexFromContainer).Max();
                Child(last).Focus(); Flush(window);
                var key = tab ? Key.Tab : rtl ? Key.Left : Key.Right;
                window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
                if (!tab) { Assert.True(Child(last).IsFocused); return; }
                // Fixed Core RTL Uncontained leaves these focus-pinned pages outside the
                // viewport. Once the two-page search limit is exhausted, root Tab wraps.
                var expected = rtl && last == 3 ? 0 : last + 1;
                Assert.True(((Control?)window.FocusManager!.GetFocusedElement())?.Tag is int selected && selected == expected,
                    $"Expected {expected}, actual {((Control?)window.FocusManager!.GetFocusedElement())?.Tag}; pages {string.Join(',', carousel.GetRealizedContainers().Select(carousel.IndexFromContainer))}");
                Advance();
                if (expected == 0) { Assert.Null(carousel.ContainerFromIndex(3)); return; }
                Assert.InRange(carousel.GetRealizedContainers().Count(), 1, 4);
            }
            var current = (Control)window.FocusManager!.GetFocusedElement()!;
            var index = (int)current.Tag!;
            var reverse = tab ? Key.Tab : rtl ? Key.Right : Key.Left;
            var modifiers = tab ? RawInputModifiers.Shift : RawInputModifiers.None;
            window.KeyPress(reverse, modifiers, PhysicalKey.None, null); window.KeyRelease(reverse, modifiers, PhysicalKey.None, null); Flush(window); Advance();
            Assert.True(Child(index - 1).IsFocused);
        }
        finally { window.Close(); }
        Control Child(int index) => carousel.ContainerFromIndex(index)!.GetVisualDescendants().OfType<Control>().Single(c => c.Tag is int);
        void Advance() { for (var i = 0; i < 60; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
    }

    [AvaloniaFact]
    public void KeyboardSearch_StopsAtTwoViewportsAndSkipsNonFocusablePages()
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.Uncontained);
        carousel.ItemTemplate = new FuncDataTemplate<int>((item, _) => new Border { Tag = item, Focusable = item is 0 or 3 });
        var window = DialogTests.Show(carousel);
        try
        {
            ((Control)carousel.ContainerFromIndex(0)!.GetVisualDescendants().Single(c => c is Control { Tag: 0 })).Focus();
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.None, null); window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            Assert.Equal(3, ((Control)window.FocusManager!.GetFocusedElement()!).Tag);
            for (var i = 0; i < 60; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.None, null); window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            // No further target within two viewports: the window resumes normal Tab wrapping.
            Assert.Equal(0, ((Control)window.FocusManager!.GetFocusedElement()!).Tag);
            Assert.Null(carousel.ContainerFromIndex(10));
            Assert.InRange(carousel.GetRealizedContainers().Count(), 1, 4);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AutomaticHeight_FollowsRealizedPagesAndRemounts()
    {
        var carousel = Create(CarouselVariant.Uncontained);
        carousel.Height = double.NaN;
        carousel.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        carousel.ItemTemplate = new FuncDataTemplate<int>((item, _) => new Border { Height = item < 10 ? 40 : 90 });
        var window = DialogTests.Show(carousel);
        try
        {
            Assert.Equal(40, carousel.Bounds.Height);
            carousel.ScrollToItem(50); Flush(window);
            Assert.Equal(90, carousel.Bounds.Height);
            carousel.ScrollToItem(0); Flush(window);
            Assert.Equal(40, carousel.Bounds.Height);
            window.Content = null; Flush(window);
            window.Content = carousel; Flush(window);
            Assert.Equal(40, carousel.Bounds.Height);
            Assert.NotNull(carousel.ContainerFromIndex(0));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(CarouselVariant.MultiBrowse, false)]
    [InlineData(CarouselVariant.MultiBrowse, true)]
    [InlineData(CarouselVariant.CenteredHero, false)]
    [InlineData(CarouselVariant.CenteredHero, true)]
    [InlineData(CarouselVariant.Uncontained, false)]
    [InlineData(CarouselVariant.Uncontained, true)]
    public void LargeCollection_RealizesBoundedPagesAndReusesContainers(CarouselVariant variant, bool rtl)
    {
        var built = 0;
        var carousel = Create(variant);
        carousel.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        carousel.ItemTemplate = new FuncDataTemplate<int>((item, _) =>
        {
            built++;
            return new Border { Tag = item, Background = Brushes.Coral };
        });
        var window = DialogTests.Show(carousel);
        try
        {
            Assert.InRange(built, 1, 8);
            Assert.Null(carousel.ContainerFromIndex(500));
            var initial = carousel.GetRealizedContainers().ToArray();
            carousel.ScrollToItem(500); Flush(window);
            Assert.Equal(500, carousel.CurrentItem);
            Assert.InRange(carousel.GetRealizedContainers().Count(), 1, 8);
            Assert.NotNull(carousel.ContainerFromIndex(500));
            Assert.Null(carousel.ContainerFromIndex(0));
            Assert.Contains(carousel.GetRealizedContainers(), initial.Contains);
            foreach (var child in carousel.GetRealizedContainers().Cast<CarouselItem>())
            {
                var index = carousel.IndexFromContainer(child);
                Assert.Equal(index, child.DataContext);
                Assert.Equal(index, child.GetVisualDescendants().OfType<Border>().Single(b => b.Tag is int).Tag);
            }
            carousel.ScrollToItem(999); Flush(window);
            Assert.Equal(999, carousel.CurrentItem);
            Assert.NotNull(carousel.ContainerFromIndex(999));
            carousel.ScrollToItem(0); Flush(window);
            Assert.Equal(0, carousel.CurrentItem);
            Assert.InRange(carousel.GetRealizedContainers().Count(), 1, 8);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CollectionChangesAndTemplateReplacement_ReprepareCurrentData()
    {
        var items = new ObservableCollection<int>(Enumerable.Range(0, 1000));
        var carousel = Create(CarouselVariant.MultiBrowse); carousel.ItemsSource = items;
        var window = DialogTests.Show(carousel);
        try
        {
            carousel.ScrollToItem(500); Flush(window);
            items[500] = 1234; Flush(window);
            Assert.Equal(1234, carousel.ContainerFromIndex(500)!.DataContext);
            items.Insert(0, -1); Flush(window);
            Assert.Equal(499, carousel.ContainerFromIndex(500)!.DataContext);
            items.Move(501, 500); Flush(window);
            Assert.Equal(1234, carousel.ContainerFromIndex(500)!.DataContext);
            carousel.ItemTemplate = new FuncDataTemplate<int>((item, _) => new Border { Tag = item + 10 });
            Flush(window);
            Assert.Equal(1244, carousel.ContainerFromIndex(500)!.GetVisualDescendants().OfType<Border>().Single(b => b.Tag is int).Tag);
            items.Clear(); Flush(window);
            Assert.Empty(carousel.GetRealizedContainers());
            Assert.Equal(0, carousel.MaximumScrollOffset);
            items.Add(55); Flush(window);
            Assert.Equal(55, carousel.ContainerFromIndex(0)!.DataContext);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void FocusedPage_IsPinnedUntilFocusLeavesAndDistantItemsCanBeRealizedExplicitly()
    {
        var carousel = Create(CarouselVariant.Uncontained);
        carousel.ItemTemplate = new FuncDataTemplate<int>((item, _) => new Border { Focusable = true, Tag = item });
        var window = DialogTests.Show(carousel);
        try
        {
            var focused = carousel.ContainerFromIndex(0)!.GetVisualDescendants().OfType<Border>().Single(b => b.Tag is int);
            Assert.True(focused.Focus()); Flush(window);
            carousel.ScrollToItem(500); Flush(window);
            Assert.True(focused.IsFocused);
            Assert.NotNull(carousel.ContainerFromIndex(0));
            carousel.ScrollIntoView(900); Flush(window);
            var next = carousel.ContainerFromIndex(900)!.GetVisualDescendants().OfType<Border>().Single(b => b.Tag is int);
            Assert.True(next.Focus()); Flush(window);
            Assert.Null(carousel.ContainerFromIndex(0));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DirectItems_DetachOutsideViewportAndReturnWithoutLosingContent()
    {
        var carousel = Create(CarouselVariant.Uncontained); carousel.ItemsSource = null;
        var items = Enumerable.Range(0, 20).Select(i => new CarouselItem { Content = new Border { Tag = i, Focusable = true } }).ToArray();
        foreach (var item in items) carousel.Items.Add(item);
        var window = DialogTests.Show(carousel);
        try
        {
            Assert.Null(TopLevel.GetTopLevel(items[19]));
            Assert.False(((Control)items[19].Content!).Focus());
            carousel.ScrollToItem(19); Flush(window);
            Assert.Same(window, TopLevel.GetTopLevel(items[19]));
            Assert.Null(TopLevel.GetTopLevel(items[0]));
            carousel.ScrollToItem(0); Flush(window);
            Assert.Same(items[0], carousel.ContainerFromIndex(0));
            Assert.Equal(0, ((Border)items[0].Content!).Tag);
        }
        finally { window.Close(); }
    }

    private static HorizontalCarousel Create(CarouselVariant variant) => new()
    {
        Width = 320, Height = 120, ItemWidth = 160, ItemSpacing = 8, Variant = variant,
        ItemsSource = Enumerable.Range(0, 1000).ToArray(),
        ItemTemplate = new FuncDataTemplate<int>((item, _) => new Border { Tag = item }),
    };
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
