using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class TopAppBarInsetsTests
{
    [AvaloniaTheory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void EachRowPublishesItsOwnConsumptionToLogicalSlots(bool twoRows, bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var insets = new Thickness(13, 7, 5, 11);
        MaterialWindowInsetsPadding Slot() => new() { Insets = insets, Child = new Border { Width = 24, Height = 24 } };
        var title = Slot(); var collapsed = Slot(); var nav = Slot(); var action = Slot();
        TopAppBar bar = twoRows ? new MediumFlexibleTopAppBar() : new TopAppBar();
        bar.WindowInsets = insets; bar.Title = title; bar.CollapsedTitle = collapsed; bar.NavigationIcon = nav; bar.Items.Add(action);
        var root = new Border { Child = bar, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        var window = new MotionTestWindow { Width = 400, Height = 300 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme()); window.Content = root; window.Show(); Flush(window);
        try
        {
            foreach (var slot in new[] { nav, action, twoRows ? collapsed : title })
                Assert.Equal(default, slot.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            if (twoRows) Assert.Equal(new Thickness(0, 4, 0, 5), title.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            MaterialWindowInsets.SetConsumeInsets(root, new Thickness(30)); Flush(window);
            Assert.Equal(default, title.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty); Flush(window);
            Assert.Equal(twoRows ? new Thickness(0, 7, 0, 11) : default, title.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(default, nav.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            // Release the row contribution when the old logical content leaves its slot.
            bar.NavigationIcon = null; Flush(window);
            root.Child = nav; Flush(window);
            Assert.Equal(insets, nav.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void GeneratedContentKeepsItsOwnNestedConsumption_AndTemplateReplacementRebinds()
    {
        using var clock = new MotionTestClockScope();
        var insets = new Thickness(13, 7, 5, 11);
        MaterialWindowInsetsPadding? title = null, action = null;
        var bar = new TopAppBar { WindowInsets = insets, Title = "title",
            TitleTemplate = new FuncDataTemplate<string>((_, _) => {
                title = new MaterialWindowInsetsPadding { Insets = new Thickness(30), Child = new Border { Width = 20, Height = 10 } };
                MaterialWindowInsets.SetConsumePadding(title, new Thickness(2, 3, 4, 5)); return title;
            }),
            ItemTemplate = new FuncDataTemplate<string>((_, _) => action = new MaterialWindowInsetsPadding { Insets = insets, Child = new Border { Width = 20, Height = 20 } }) };
        bar.Items.Add("action");
        var window = new MotionTestWindow { Width = 400, Height = 200 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme()); window.Content = bar; window.Show(); Flush(window);
        try
        {
            Assert.NotNull(title); Assert.NotNull(action);
            Assert.Equal(new Thickness(15, 20, 21, 14), title.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(default, action.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            bar.Template = null; Flush(window); bar.ClearValue(TopAppBar.TemplateProperty); Flush(window);
            Assert.Equal(new Thickness(15, 20, 21, 14), title.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(default, action.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
    }

    private static void Flush(Window window)
    { for (var i = 0; i < 3; i++) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); } }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void SearchBarPublishesInsetsToInputNavigationAndDirectActions(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var insets = new Thickness(13, 7, 5, 11);
        MaterialWindowInsetsPadding Slot() => new() { Insets = insets, Child = new Border { Width = 24, Height = 24 } };
        var input = Slot(); var nav = Slot(); var action = Slot();
        var bar = new AppBarWithSearch { WindowInsets = insets, InputField = input, NavigationIcon = nav, Items = { action } };
        var root = new Border { Child = bar, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        var window = new MotionTestWindow { Width = 400, Height = 300 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme()); window.Content = root; window.Show(); Flush(window);
        try
        {
            Assert.Equal(new Thickness(9, 4, 3, 5), bar.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            foreach (var slot in new[] { input, nav, action }) Assert.Equal(default, slot.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            MaterialWindowInsets.SetConsumeInsets(root, new Thickness(30)); Flush(window);
            Assert.Equal(default, bar.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            foreach (var slot in new[] { input, nav, action }) Assert.Equal(new Thickness(30), slot.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            bar.Items.Remove(action); root.Child = action; Flush(window);
            Assert.Equal(new Thickness(30), action.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty); Flush(window);
            Assert.Equal(insets, action.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
    }
}
