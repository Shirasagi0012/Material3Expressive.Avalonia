using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class RichTooltipTests
{
    [AvaloniaTheory]
    [InlineData(false, false, 24, 4)]
    [InlineData(true, false, 86, 62)]
    [InlineData(false, true, 84, 24)]
    [InlineData(true, true, 122, 62)]
    public void Slots_UseUpstreamMissingBaselineAndActionMinimum(bool title, bool action, double height, double contentY)
    {
        var content = new Marker(64, 8);
        var tooltip = new RichTooltip { Content = content, Title = title ? new Marker(32, 10) : null,
            Action = action ? new Marker(24, 12) : null };
        var window = Show(tooltip);
        try
        {
            Assert.Equal(new Size(96, height), tooltip.Bounds.Size);
            Assert.Equal(new Point(16, contentY), content.TranslatePoint(default, tooltip));
            Assert.Equal(new CornerRadius(12), tooltip.CornerRadius);
            Assert.Equal(3, tooltip.ShadowElevation);
            if (title) Assert.Equal(new Point(16, 28), ((Control)tooltip.Title!).TranslatePoint(default, tooltip));
            if (action) Assert.Equal(new Point(16, contentY + 24), ((Control)tooltip.Action!).TranslatePoint(default, tooltip));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Baselines_UseNativeTextAndExplicitOffsets()
    {
        var title = new TextBlock { Text = "Title" };
        var text = new TextBlock { Text = "Supporting text" };
        var tooltip = new RichTooltip { Title = title, Content = text };
        var window = Show(tooltip);
        try
        {
            Assert.Equal(28, title.TranslatePoint(default, tooltip)!.Value.Y + Math.Round(title.TextLayout.TextLines[0].Baseline), 5);
            var titleBottom = title.TranslatePoint(default, tooltip)!.Value.Y + title.Bounds.Height;
            Assert.Equal(24, text.TranslatePoint(default, tooltip)!.Value.Y - titleBottom + Math.Round(text.TextLayout.TextLines[0].Baseline), 5);
            var marker = new Marker(40, 10);
            TextBlock.SetBaselineOffset(marker, 7);
            tooltip.Title = marker; Flush(window);
            Assert.Equal(21, marker.TranslatePoint(default, tooltip)!.Value.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Slots_TrackTemplatesRemovalAndRtl()
    {
        var tooltip = new RichTooltip { Content = new Marker(64, 8), FlowDirection = FlowDirection.RightToLeft };
        var window = Show(tooltip);
        try
        {
            tooltip.TitleTemplate = new FuncDataTemplate<object>((_, _) => new Marker(20, 10));
            tooltip.Action = new Marker(24, 12); Flush(window);
            var title = tooltip.GetVisualDescendants().OfType<ContentPresenter>().Single(c => c.Name == "PART_Title");
            Assert.NotNull(title.Child);
            Assert.Equal(new Size(96, 122), tooltip.Bounds.Size);
            var start = title.Child!.TranslatePoint(default, tooltip)!.Value;
            var end = title.Child.TranslatePoint(new Point(title.Child.Bounds.Width, 0), tooltip)!.Value;
            Assert.Equal(new Point(60, 28), new Point(Math.Min(start.X, end.X), start.Y));
            tooltip.TitleTemplate = null; tooltip.Action = null; Flush(window);
            Assert.Equal(new Size(96, 24), tooltip.Bounds.Size);
            Assert.False(title.IsVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TypographyColorsAndElevation_TrackTokensWithoutReplacingLocalValues()
    {
        var title = new TextBlock { Text = "Title" };
        var text = new TextBlock { Text = "Text" };
        var action = new TextBlock { Text = "Action" };
        var tooltip = new RichTooltip { Title = title, Content = text, Action = action };
        var window = Show(tooltip);
        try
        {
            Assert.Equal(14, title.FontSize); Assert.Equal(FontWeight.Medium, title.FontWeight); Assert.Equal(20, title.LineHeight);
            Assert.Equal(14, text.FontSize); Assert.Equal(FontWeight.Normal, text.FontWeight); Assert.Equal(20, text.LineHeight);
            Assert.Equal(14, action.FontSize); Assert.Equal(FontWeight.Medium, action.FontWeight);
            window.Resources[Sys.Color.OnSurfaceVariant] = Colors.Yellow;
            window.Resources[Sys.Color.Primary] = Colors.Blue;
            Assert.Equal(Colors.Yellow, ((ISolidColorBrush)title.Foreground!).Color);
            Assert.Equal(Colors.Yellow, ((ISolidColorBrush)text.Foreground!).Color);
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)action.Foreground!).Color);
            window.Resources[Sys.TypeScale.BodyMedium.Size] = 17d;
            Assert.Equal(17, text.FontSize); Assert.Equal(14, title.FontSize);
            title.Foreground = Brushes.Red; tooltip.ActionForeground = Brushes.Green;
            window.Resources[Sys.Color.OnSurfaceVariant] = Colors.Black;
            window.Resources[Sys.Color.Primary] = Colors.Yellow;
            Assert.Equal(Colors.Red, ((ISolidColorBrush)title.Foreground!).Color);
            Assert.Equal(Colors.Green, ((ISolidColorBrush)action.Foreground!).Color);
            window.Resources[Sys.Elevation.Level2] = 6d;
            Assert.Equal(6, tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single().ShadowElevation);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CallerConstraints_PrecedeSoftDefaults()
    {
        var tooltip = new RichTooltip { Content = new Marker(500, 8) };
        var window = Show(tooltip);
        try
        {
            Assert.Equal(320, tooltip.Bounds.Width);
            tooltip.Width = 360; Flush(window); Assert.Equal(360, tooltip.Bounds.Width);
            tooltip.Width = 20; tooltip.Height = 12; Flush(window); Assert.Equal(new Size(20, 12), tooltip.Bounds.Size);
            tooltip.Width = double.NaN; tooltip.Height = double.NaN; tooltip.TooltipMaxWidth = 100; tooltip.MinWidth = 160;
            Flush(window); Assert.Equal(160, tooltip.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DensityTokens_UpdateBaselinesAndActionMeasurements(bool rtl)
    {
        var title = new Marker(32, 10);
        var content = new Marker(64, 8);
        var action = new Marker(24, 12);
        TextBlock.SetBaselineOffset(title, 6);
        TextBlock.SetBaselineOffset(content, 3);
        var tooltip = new RichTooltip { Title = title, Content = content, Action = action,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(tooltip);
        try
        {
            window.Resources[Comp.RichTooltip.SubheadFirstBaselineOffset] = 34d;
            window.Resources[Comp.RichTooltip.TextFirstBaselineOffset] = 30d;
            window.Resources[Comp.RichTooltip.TextBottomPadding] = 20d;
            window.Resources[Comp.RichTooltip.ActionMinHeight] = 48d;
            window.Resources[Comp.RichTooltip.ActionBottomPadding] = 18d;
            Flush(window);
            Assert.Equal(28, title.TranslatePoint(default, tooltip)!.Value.Y);
            Assert.Equal(65, content.TranslatePoint(default, tooltip)!.Value.Y);
            Assert.Equal(93, action.TranslatePoint(default, tooltip)!.Value.Y);
            Assert.Equal(141, tooltip.Bounds.Height);

            // The action minimum and its bottom padding participate at different layout stages.
            window.Resources[Comp.RichTooltip.ActionMinHeight] = 20d;
            Flush(window);
            Assert.Equal(123, tooltip.Bounds.Height);
            window.Resources[Comp.RichTooltip.ActionBottomPadding] = 4d;
            Flush(window);
            Assert.Equal(113, tooltip.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DensityTokens_TextOnlyPaddingTracksLocalOverrideAndSlotChanges()
    {
        var content = new Marker(64, 8);
        var tooltip = new RichTooltip { Content = content };
        var window = Show(tooltip);
        try
        {
            window.Resources[Comp.RichTooltip.TextVerticalPadding] = 12d;
            Flush(window);
            Assert.Equal(32, tooltip.Bounds.Height);
            Assert.Equal(12, content.TranslatePoint(default, tooltip)!.Value.Y);

            tooltip.Resources[Comp.RichTooltip.TextVerticalPadding] = 16d;
            window.Resources[Comp.RichTooltip.TextVerticalPadding] = 20d;
            Flush(window);
            Assert.Equal(40, tooltip.Bounds.Height);
            Assert.Equal(16, content.TranslatePoint(default, tooltip)!.Value.Y);

            tooltip.Title = new Marker(32, 10);
            Flush(window);
            Assert.Equal(86, tooltip.Bounds.Height);
            Assert.Equal(62, content.TranslatePoint(default, tooltip)!.Value.Y);
            tooltip.Title = null;
            tooltip.Resources.Remove(Comp.RichTooltip.TextVerticalPadding);
            Flush(window);
            Assert.Equal(48, tooltip.Bounds.Height);
        }
        finally { window.Close(); }
    }

    static Window Show(RichTooltip tooltip)
    {
        tooltip.HorizontalAlignment = HorizontalAlignment.Center; tooltip.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 480, Height = 300 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { tooltip } }; window.Show(); Flush(window);
        return window;
    }
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    sealed class Marker(double width, double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
    }
}
