using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class NavigationDrawerInsetsTests
{
    [AvaloniaTheory]
    [InlineData("permanent", false)] [InlineData("permanent", true)]
    [InlineData("dismissible", false)] [InlineData("dismissible", true)]
    [InlineData("modal", false)] [InlineData("modal", true)]
    public void SheetsExcludeAncestorInsetsAndPublishConsumptionToContent(string kind, bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var requested = new Thickness(13, 7, 5, 11);
        var nested = new MaterialWindowInsetsPadding { Insets = requested, Child = new Border { Width = 24, Height = 24 } };
        var sheet = CreateSheet(kind); sheet.WindowInsets = requested; sheet.Content = nested;
        var root = new Border { Child = sheet, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        var window = Show(root);
        try
        {
            var padding = sheet.GetVisualDescendants().OfType<MaterialWindowInsetsBorder>().Single();
            Assert.Equal(new Thickness(rtl ? 3 : 9, 4, rtl ? 9 : 3, 5), padding.Padding);
            Assert.Equal(requested, nested.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            Assert.Equal(default, nested.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(24, nested.Bounds.Height);
            var contentBounds = new Rect(nested.Bounds.Size).TransformToAABB(nested.TransformToVisual(window)!.Value);
            var sheetBounds = new Rect(sheet.Bounds.Size).TransformToAABB(sheet.TransformToVisual(window)!.Value);
            Assert.Equal(9, contentBounds.Left - sheetBounds.Left);
            Assert.Equal(4, contentBounds.Top - sheetBounds.Top);

            MaterialWindowInsets.SetConsumeInsets(root, new Thickness(30)); Flush(window);
            Assert.Equal(default, padding.Padding);
            Assert.Equal(new Thickness(30), nested.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));

            sheet.WindowInsets = new Thickness(40, 35, 32, 31); Flush(window);
            Assert.Equal(new Thickness(rtl ? 2 : 10, 5, rtl ? 10 : 2, 1), padding.Padding);
            Assert.Equal(sheet.WindowInsets, nested.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));

            root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty); Flush(window);
            Assert.Equal(sheet.WindowInsets, sheet.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(default, nested.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("permanent", false)] [InlineData("permanent", true)]
    [InlineData("dismissible", false)] [InlineData("dismissible", true)]
    [InlineData("modal", false)] [InlineData("modal", true)]
    public void LogicalPaddingAndPhysicalUnionPrecedeSheetInsetsAndFollowDirection(string kind, bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var sheet = CreateSheet(kind);
        sheet.WindowInsets = new Thickness(30, 20, 40, 25);
        MaterialWindowInsets.SetConsumePadding(sheet, new Thickness(3, 5, 9, 7));
        MaterialWindowInsets.SetConsumeInsets(sheet, new Thickness(12, 8, 15, 10));
        var nested = new MaterialWindowInsetsPadding { Insets = sheet.WindowInsets, Child = new Border { Width = 24, Height = 24 } };
        sheet.Content = nested;
        var root = new Border { Child = sheet, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        MaterialWindowInsets.SetConsumeInsets(root, new Thickness(10, 4, 6, 8));
        var window = Show(root);
        try
        {
            void Verify(bool isRtl)
            {
                // Ancestor consumption + logical padding, then union physical insets, then apply remainder.
                Assert.Equal(new Thickness(isRtl ? 11 : 17, 11, 25, 10), sheet.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
                Assert.Equal(sheet.WindowInsets, nested.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
                Assert.Equal(default, nested.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            }
            Verify(rtl);
            root.FlowDirection = rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft; Flush(window);
            Verify(!rtl);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("permanent")] [InlineData("dismissible")] [InlineData("modal")]
    public void TemplateContentAndReplacedContentUseTheCurrentSheetConsumption(string kind)
    {
        using var clock = new MotionTestClockScope();
        var requested = new Thickness(13, 7, 5, 11);
        var sheet = CreateSheet(kind); sheet.WindowInsets = requested;
        MaterialWindowInsetsPadding? templated = null;
        sheet.ContentTemplate = new FuncDataTemplate<string>((_, _) => templated = new MaterialWindowInsetsPadding
        {
            Insets = requested, Child = new Border { Width = 24, Height = 24 }
        });
        sheet.Content = "template";
        var root = new Border { Child = sheet };
        MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        var window = Show(root);
        try
        {
            Assert.NotNull(templated);
            Assert.Equal(requested, templated.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            Assert.Equal(default, templated.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            sheet.ContentTemplate = null;
            var replacement = new MaterialWindowInsetsPadding { Insets = requested, Child = new Border { Width = 24, Height = 24 } };
            sheet.Content = replacement; Flush(window);
            Assert.Equal(requested, replacement.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
            Assert.Equal(default, replacement.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.False(templated.IsAttachedToVisualTree());

            root.Child = null; Flush(window);
            MaterialWindowInsets.SetConsumeInsets(root, new Thickness(50));
            var otherRoot = new Border { Child = sheet };
            MaterialWindowInsets.SetConsumeInsets(otherRoot, new Thickness(2));
            window.Content = otherRoot; Flush(window);
            Assert.Equal(new Thickness(11, 5, 3, 9), sheet.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(default, replacement.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("permanent")] [InlineData("dismissible")] [InlineData("modal")]
    public void DrawerContainersPreserveDeclaredAncestorConsumptionForTheirSheet(string kind)
    {
        using var clock = new MotionTestClockScope();
        var state = new DrawerState(DrawerValue.Open);
        var sheet = CreateSheet(kind); sheet.WindowInsets = new Thickness(13, 7, 5, 11);
        if (sheet is DismissibleDrawerSheet stateful) stateful.State = state;
        var scaffold = new Scaffold { ContentWindowInsets = sheet.WindowInsets, Content = new Border { Width = 24, Height = 24 } };
        sheet.Content = scaffold;
        NavigationDrawer drawer = kind switch
        {
            "dismissible" => new DismissibleNavigationDrawer { State = state },
            "modal" => new ModalNavigationDrawer { State = state },
            _ => new PermanentNavigationDrawer()
        };
        drawer.DrawerContent = sheet; drawer.Content = new Border();
        var root = new Border { Child = drawer };
        MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        var window = Show(root);
        try
        {
            Assert.Equal(new Thickness(9, 4, 3, 5), sheet.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty));
            Assert.Equal(default, scaffold.ContentPadding);
            Assert.Equal(sheet.WindowInsets, scaffold.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private static DrawerSheet CreateSheet(string kind) => kind switch
    {
        "dismissible" => new DismissibleDrawerSheet { Width = 280 },
        "modal" => new ModalDrawerSheet { Width = 280 },
        _ => new PermanentDrawerSheet { Width = 280 }
    };
    private static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 520, Height = 320 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        window.Content = content; window.Show(); Flush(window);
        return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
