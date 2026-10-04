using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class PlainTooltipTests
{
    [AvaloniaTheory]
    [InlineData(0, 0, 40, 24)]
    [InlineData(4, 8, 40, 24)]
    [InlineData(64, 16, 80, 24)]
    [InlineData(300, 8, 200, 24)]
    [InlineData(8, 48, 40, 56)]
    public void ContentSizing_PreservesMinimumMaximumAndPadding(double width, double height, double expectedWidth, double expectedHeight)
    {
        var marker = new IntrinsicContent(width, height);
        var tooltip = new PlainTooltip { Content = marker };
        var window = Show(tooltip);
        try
        {
            Assert.Equal(new Size(expectedWidth, expectedHeight), tooltip.Bounds.Size);
            Assert.Equal(new Point(8, 4), marker.TranslatePoint(default, tooltip));
            Assert.Equal(new CornerRadius(4), tooltip.CornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CallerConstraints_PrecedeInternalSizeIn()
    {
        var tooltip = new PlainTooltip { Content = new IntrinsicContent(400, 8), TooltipMaxWidth = 100 };
        var window = Show(tooltip);
        try
        {
            Assert.Equal(100, tooltip.Bounds.Width);
            tooltip.Width = 300; Flush(window); Assert.Equal(300, tooltip.Bounds.Width);
            tooltip.Width = 20; tooltip.Height = 12; Flush(window);
            Assert.Equal(new Size(20, 12), tooltip.Bounds.Size);
            tooltip.Width = double.NaN; tooltip.Height = double.NaN; tooltip.MinWidth = 160; Flush(window);
            Assert.Equal(160, tooltip.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TypographyAndForeground_TrackTokensAndRespectContentOverrides()
    {
        var text = new TextBlock { Text = "Tooltip" };
        var tooltip = new PlainTooltip { Content = text };
        var window = Show(tooltip);
        try
        {
            Assert.Equal(12, text.FontSize); Assert.Equal(16, text.LineHeight);
            Assert.Equal(.4, text.LetterSpacing); Assert.Equal(FontWeight.Normal, text.FontWeight);
            window.Resources[Sys.Color.InverseOnSurface] = Colors.Yellow;
            Assert.Equal(Colors.Yellow, ((ISolidColorBrush)text.Foreground!).Color);
            tooltip.Background = Brushes.Blue;
            Assert.Equal(Colors.Yellow, ((ISolidColorBrush)text.Foreground!).Color);
            window.Resources[Sys.TypeScale.BodySmall.Size] = 14d;
            Assert.Equal(14, text.FontSize);
            text.Foreground = Brushes.Red; text.FontSize = 18;
            window.Resources[Sys.Color.InverseOnSurface] = Colors.Green;
            window.Resources[Sys.TypeScale.BodySmall.Size] = 15d;
            Assert.Equal(Colors.Red, ((ISolidColorBrush)text.Foreground!).Color); Assert.Equal(18, text.FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Surface_ReceivesElevationAndContentTemplateReplacement()
    {
        var tooltip = new PlainTooltip { Content = "old", TonalElevation = 6, ShadowElevation = 4 };
        var window = Show(tooltip);
        try
        {
            var surface = tooltip.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(6, surface.TonalElevation); Assert.Equal(4, surface.ShadowElevation);
            tooltip.Content = "new"; Flush(window);
            Assert.Equal("new", tooltip.GetVisualDescendants().OfType<ContentPresenter>().Single().Content);
        }
        finally { window.Close(); }
    }

    static Window Show(PlainTooltip tooltip)
    {
        tooltip.HorizontalAlignment = HorizontalAlignment.Center; tooltip.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 480, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { tooltip } }; window.Show(); Flush(window);
        return window;
    }
    static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    sealed class IntrinsicContent(double width, double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
    }
}
