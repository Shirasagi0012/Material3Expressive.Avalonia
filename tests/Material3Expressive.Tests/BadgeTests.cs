using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class BadgeTests
{
    [Fact]
    public void TokenSource_PreservesFixedBadgeTokens() => TokenScan.AssertComplete(new BadgeTokenSource(), typeof(Comp.Badge));

    [AvaloniaTheory]
    [InlineData(false, 0, 0, 6, 6)]
    [InlineData(true, 0, 0, 16, 16)]
    [InlineData(true, 4, 8, 16, 16)]
    [InlineData(true, 32, 8, 40, 16)]
    [InlineData(true, 8, 48, 16, 48)]
    public void Badge_MeasuresOptionalContentAndCentersIt(bool present, double contentWidth, double contentHeight, double width, double height)
    {
        var content = new Border { Width = contentWidth, Height = contentHeight };
        var badge = new Badge { Content = present ? content : null };
        var window = Show(badge);
        try
        {
            Assert.Equal(new Size(width, height), badge.Bounds.Size);
            Assert.Equal(badge.CornerRadius, Part<Border>(badge, "PART_Background").CornerRadius);
            if (present)
                Assert.Equal(new Point((width - contentWidth) / 2, (height - contentHeight) / 2), content.TranslatePoint(default, badge));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContentBranch_CanChangeAndEmptyTemplateStillSelectsLargeBadge()
    {
        var badge = new Badge(); var window = Show(badge);
        try
        {
            badge.ContentTemplate = new FuncDataTemplate<object>((_, _) => new Panel()); window.UpdateLayout();
            Assert.Equal(new Size(16, 16), badge.Bounds.Size);
            badge.ContentTemplate = null; window.UpdateLayout(); Assert.Equal(new Size(6, 6), badge.Bounds.Size);
            badge.Content = new Border { Width = 32, Height = 8 }; window.UpdateLayout();
            Assert.Equal(new Size(40, 16), badge.Bounds.Size);
            badge.Content = null; window.UpdateLayout(); Assert.Equal(new Size(6, 6), badge.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(24d, 6d, 0, 0, 24, 6)]
    [InlineData(null, null, 10, 12, 12, 12)]
    public void ExplicitDimensionsAndMinima_ReplaceDefaultMinimum(double? width, double? height, double minWidth, double minHeight, double expectedWidth, double expectedHeight)
    {
        var badge = new Badge { Content = new Border { Width = 4, Height = 8 }, MinWidth = minWidth, MinHeight = minHeight };
        if (width is { } w) badge.Width = w;
        if (height is { } h) badge.Height = h;
        var window = Show(badge);
        try { Assert.Equal(new Size(expectedWidth, expectedHeight), badge.Bounds.Size); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Foreground_FollowsBackgroundRolesAndAmbientAndRespectsLocalOverrides()
    {
        var child = new TextBlock { Text = "8" }; var badge = new Badge { Content = child }; var window = Show(badge);
        try
        {
            TextElement.SetForeground(window, Brushes.Magenta);
            window.Resources[Sys.Color.Error] = Colors.Red; window.Resources[Sys.Color.OnError] = Colors.Yellow;
            Assert.Equal(Colors.Red, ((ISolidColorBrush)badge.Background!).Color);
            Assert.Equal(Colors.Yellow, ((ISolidColorBrush)child.Foreground!).Color);
            badge.Background = Brushes.Navy; Assert.Equal(Colors.Magenta, ((ISolidColorBrush)child.Foreground!).Color);
            TextElement.SetForeground(window, Brushes.Green); Assert.Equal(Colors.Green, ((ISolidColorBrush)child.Foreground!).Color);
            badge.Foreground = Brushes.Lime; Assert.Equal(Colors.Lime, ((ISolidColorBrush)child.Foreground!).Color);
            child.Foreground = Brushes.Blue; badge.Foreground = Brushes.Red;
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)child.Foreground!).Color);
            badge.ClearValue(Badge.ForegroundProperty); child.ClearValue(TextBlock.ForegroundProperty);
            Assert.Equal(Colors.Green, ((ISolidColorBrush)child.Foreground!).Color);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LabelSmall_UsesExistingTypographyAndTracksTokenOverrides()
    {
        var badge = new Badge { Content = "99+" }; var window = Show(badge);
        try
        {
            var label = badge.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(11, label.FontSize); Assert.Equal(FontWeight.Medium, label.FontWeight);
            Assert.Equal(0.5, label.LetterSpacing); Assert.Equal(16, label.LineHeight);
            window.Resources[Sys.TypeScale.LabelSmall.Size] = 13d; Assert.Equal(13, label.FontSize);
            label.FontSize = 20; window.Resources[Sys.TypeScale.LabelSmall.Size] = 14d; Assert.Equal(20, label.FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false, 18, 0)]
    [InlineData(true, false, 12, -2)]
    [InlineData(false, true, 0, 0)]
    [InlineData(true, true, -4, -2)]
    public void BadgedBox_UsesAnchorSizeAndRelativePosition(bool content, bool rtl, double x, double y)
    {
        var badge = new Badge { Content = content ? new Border { Width = 4, Height = 8 } : null };
        var anchor = new Border { Width = 24, Height = 24 };
        var box = new BadgedBox { Content = anchor, Badge = badge };
        var window = Show(box); window.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; window.UpdateLayout();
        try
        {
            Assert.Equal(new Size(24, 24), box.Bounds.Size);
            var origin = badge.TranslatePoint(default, window)!.Value;
            var end = badge.TranslatePoint(new Point(badge.Bounds.Width, 0), window)!.Value;
            var boxOrigin = box.TranslatePoint(default, window)!.Value;
            var boxEnd = box.TranslatePoint(new Point(box.Bounds.Width, 0), window)!.Value;
            Assert.Equal(x, Math.Min(origin.X, end.X) - Math.Min(boxOrigin.X, boxEnd.X));
            Assert.Equal(y, origin.Y - boxOrigin.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BadgeSlot_IsLogicalContentAndSupportsTemplatesReplacementAndReattach()
    {
        var anchor = new Border { Width = 24, Height = 24 };
        var oldBadge = new Badge();
        var box = new BadgedBox { Content = anchor, Badge = oldBadge }; var window = Show(box);
        try
        {
            Assert.Equal(box, oldBadge.Parent);
            var newBadge = new Badge { Content = "8" }; box.Badge = newBadge; window.UpdateLayout();
            Assert.Null(oldBadge.Parent); Assert.Equal(box, newBadge.Parent);
            box.BadgeTemplate = new FuncDataTemplate<string>((value, _) => new Badge { Content = value }); box.Badge = "12"; window.UpdateLayout();
            Assert.Null(newBadge.Parent);
            var rendered = Part<ContentPresenter>(box, "PART_BadgePresenter").Child;
            Assert.IsType<Badge>(rendered);
            ((Grid)window.Content!).Children.Clear(); window.Content = null;
            window.Content = new Grid { Children = { box } }; Flush(window);
            Assert.Equal(new Size(24, 24), box.Bounds.Size);
            Assert.Equal("12", Assert.IsType<Badge>(Part<ContentPresenter>(box, "PART_BadgePresenter").Child).Content);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NavigationBounds_ClampTopAndEndAndUpdateAfterAnchorResize()
    {
        var badge = new Badge { Width = 60, Height = 40 };
        var anchor = new Border { Width = 40, Height = 24 };
        var box = new BadgedBox { Content = anchor, Badge = badge };
        var item = new NavigationRailItem { Icon = box };
        var window = Show(item);
        try
        {
            Check(); anchor.Width = 24; anchor.Height = 48; Flush(window); Check();
            void Check()
            {
                var layout = Part<Panel>(item, "PART_Layout");
                var origin = badge.TranslatePoint(default, layout)!.Value;
                Assert.True(origin.Y >= 0); Assert.True(origin.X + badge.Bounds.Width <= layout.Bounds.Width);
            }
        }
        finally { window.Close(); }
    }

    static Window Show(Control content)
    {
        content.HorizontalAlignment = HorizontalAlignment.Center; content.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 240, Height = 200 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { content } }; window.Show(); Flush(window);
        return window;
    }
    static T Part<T>(Control owner, string name) where T : Control => owner.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
