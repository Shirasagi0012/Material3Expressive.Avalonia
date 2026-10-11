using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ProgressBarThemeTests
{
    [Theory]
    [InlineData(50, 0, 100, .5)]
    [InlineData(0, 0, 100, 0)]
    [InlineData(100, 0, 100, 1)]
    [InlineData(-10, 0, 100, 0)]
    [InlineData(150, 0, 100, 1)]
    [InlineData(-60, -100, -20, .5)]
    [InlineData(-120, -100, -20, 0)]
    [InlineData(0, -100, -20, 1)]
    [InlineData(5, -10, 10, .75)]
    [InlineData(10, 10, 10, 1)]
    [InlineData(-3, -3, -3, 1)]
    [InlineData(double.NaN, 0, 100, 0)]
    [InlineData(50, double.NaN, 100, 0)]
    [InlineData(50, 0, double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0, 100, 1)]
    [InlineData(50, 0, double.PositiveInfinity, 0)]
    public void ProgressMapping_ClampsAndHandlesEmptyOrInvalidRanges(double value, double minimum, double maximum, double expected) =>
        Assert.Equal(expected, ProgressBarProgressConverter.Map(value, minimum, maximum), 12);

    [Fact]
    public void ProgressConverter_ReturnsUnsetForIncompleteInputs()
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        Assert.Same(AvaloniaProperty.UnsetValue, ProgressBarProgressConverter.Instance.Convert([1d, 0d], typeof(double), null, culture));
        Assert.Same(AvaloniaProperty.UnsetValue, ProgressBarProgressConverter.Instance.Convert([1d, AvaloniaProperty.UnsetValue, 2d], typeof(double), null, culture));
        Assert.Equal(.5, ProgressBarProgressConverter.Instance.Convert([1d, 0d, 2d], typeof(double), null, culture));
    }

    [AvaloniaFact]
    public void NativeProgressBar_UsesImplicitThemeAndMapsRangeToIndicator()
    {
        var bar = new ProgressBar { Value = 25 };
        var window = Show(bar);
        Assert.True(bar.TryFindResource("MaterialProgressBarTheme", out var theme));
        Assert.IsType<ControlTheme>(theme);
        var indicator = Indicator(bar);
        Assert.Equal(.25, indicator.Progress, 12);
        Assert.False(indicator.IsIndeterminate);

        bar.Minimum = -100; bar.Maximum = -20; bar.Value = -60; Draw(window);
        Assert.Equal(.5, indicator.Progress, 12);
        // RangeBase coerces Value into [Minimum, Maximum]; the indicator follows the coerced value.
        bar.Value = 500; Draw(window);
        Assert.Equal(1, indicator.Progress, 12);
        bar.Value = -500; Draw(window);
        Assert.Equal(0, indicator.Progress, 12);
        // An empty range draws full, matching Avalonia's Percentage (100) and Fluent's indicator.
        bar.Minimum = 10; bar.Maximum = 10; Draw(window);
        Assert.Equal(10, bar.Value);
        Assert.Equal(1, indicator.Progress, 12);
        Assert.Equal(100, bar.Percentage, 12);
        bar.Maximum = 30; bar.Value = 25; Draw(window);
        Assert.Equal(.75, indicator.Progress, 12);
        window.Close();
    }

    [AvaloniaFact]
    public void IndeterminateToggle_PassesThroughAndKeepsPseudoClass()
    {
        var bar = new ProgressBar { Value = 40 };
        var window = Show(bar);
        var indicator = Indicator(bar);
        bar.IsIndeterminate = true; Draw(window);
        Assert.True(indicator.IsIndeterminate);
        Assert.Contains(":indeterminate", bar.Classes);
        Assert.Equal(.4, indicator.Progress, 12);
        bar.IsIndeterminate = false; Draw(window);
        Assert.False(indicator.IsIndeterminate);
        Assert.DoesNotContain(":indeterminate", bar.Classes);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Horizontal_StretchesIndicatorAtTokenHeightAndFollowsFlowDirection(FlowDirection direction)
    {
        var bar = new ProgressBar { Value = 50, VerticalAlignment = VerticalAlignment.Top, FlowDirection = direction };
        var window = Show(bar);
        var indicator = Indicator(bar);
        Assert.Contains(":horizontal", bar.Classes); Assert.DoesNotContain(":vertical", bar.Classes);
        Assert.Equal(new Size(160, 4), bar.Bounds.Size);
        Assert.Equal(new Size(160, 4), indicator.Bounds.Size);
        Assert.Equal(direction, indicator.FlowDirection);
        Assert.Null(Transform(bar).LayoutTransform);
        window.Resources[Comp.ProgressIndicator.ActiveIndicatorColor] = Colors.Red;
        window.Resources[Comp.ProgressIndicator.TrackColor] = Colors.Blue;
        Draw(window);
        using (var frame = window.CaptureRenderedFrame()!)
        {
            var rtl = direction == FlowDirection.RightToLeft;
            Assert.Equal((255, 0, 0), PixelAt(frame, rtl ? 120 : 40, 2));
            Assert.Equal((0, 0, 255), PixelAt(frame, rtl ? 40 : 120, 2));
        }
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Vertical_RotatesIndicatorSoProgressFillsBottomToTop(FlowDirection direction)
    {
        var bar = new ProgressBar { Value = 50, Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Left, FlowDirection = direction };
        var window = Show(bar);
        var indicator = Indicator(bar);
        Assert.Contains(":vertical", bar.Classes); Assert.DoesNotContain(":horizontal", bar.Classes);
        Assert.Equal(new Size(4, 120), bar.Bounds.Size);
        Assert.Equal(new Size(120, 4), indicator.Bounds.Size);
        Assert.Equal(-90, Assert.IsType<RotateTransform>(Transform(bar).LayoutTransform).Angle);
        // The indicator draws from its local left (LTR) or right (RTL) edge; both must land at the bottom.
        Assert.Equal(direction, indicator.FlowDirection);
        var rtl = direction == FlowDirection.RightToLeft;
        var start = indicator.TranslatePoint(new Point(rtl ? 120 : 0, 2), bar)!.Value;
        var end = indicator.TranslatePoint(new Point(rtl ? 0 : 120, 2), bar)!.Value;
        Assert.Equal(120, start.Y, 3); Assert.Equal(0, end.Y, 3);
        // Also check in window space, which includes the RTL mirror applied at the ProgressBar.
        var startInWindow = indicator.TranslatePoint(new Point(rtl ? 120 : 0, 2), window)!.Value;
        var endInWindow = indicator.TranslatePoint(new Point(rtl ? 0 : 120, 2), window)!.Value;
        Assert.True(startInWindow.Y > endInWindow.Y);
        // And in actual pixels: the active (red) half is at the bottom, the track (blue) at the top.
        window.Resources[Comp.ProgressIndicator.ActiveIndicatorColor] = Colors.Red;
        window.Resources[Comp.ProgressIndicator.TrackColor] = Colors.Blue;
        Draw(window);
        using (var frame = window.CaptureRenderedFrame()!)
        {
            Assert.Equal((255, 0, 0), PixelAt(frame, 2, 100));
            Assert.Equal((0, 0, 255), PixelAt(frame, 2, 20));
        }

        bar.Orientation = Orientation.Horizontal; bar.ClearValue(Layoutable.HorizontalAlignmentProperty);
        bar.VerticalAlignment = VerticalAlignment.Top; Draw(window);
        Assert.Contains(":horizontal", bar.Classes);
        Assert.Equal(new Size(160, 4), indicator.Bounds.Size);
        Assert.Equal(direction, indicator.FlowDirection);
        window.Close();
    }

    [AvaloniaFact]
    public void ShowProgressText_UsesNativeFormatAndHidesWhenIndeterminate()
    {
        var bar = new ProgressBar { Value = 50, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(bar);
        var text = Text(bar);
        Assert.False(text.IsVisible);
        bar.ShowProgressText = true; Draw(window);
        Assert.True(text.IsVisible);
        Assert.Equal("50%", text.Text);
        // The trailing label takes space from the track instead of overlapping it.
        Assert.True(Indicator(bar).Bounds.Width < 160);
        Assert.Equal(160, Indicator(bar).Bounds.Width + text.Bounds.Width + text.Margin.Left, 3);

        bar.Minimum = 50; bar.Maximum = 100; bar.Value = 70;
        bar.ProgressTextFormat = "{0:0} of {2:0}-{3:0} ({1:0}%)"; Draw(window);
        Assert.Equal("70 of 50-100 (40%)", text.Text);

        bar.IsIndeterminate = true; Draw(window);
        Assert.False(text.IsVisible);
        Assert.Equal(160, Indicator(bar).Bounds.Width, 3);
        bar.IsIndeterminate = false; Draw(window);
        Assert.True(text.IsVisible);
        bar.ShowProgressText = false; Draw(window);
        Assert.False(text.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void ProgressText_UsesLabelMediumAndOnSurfaceVariantTokens()
    {
        var bar = new ProgressBar { Value = 50, ShowProgressText = true, Foreground = Brushes.Red };
        var window = Show(bar);
        var text = Text(bar);
        Assert.Equal(12, text.FontSize); Assert.Equal(FontWeight.Medium, text.FontWeight);
        Assert.Equal(.5, text.LetterSpacing); Assert.Equal(16, text.LineHeight);
        window.Resources[Sys.TypeScale.LabelMedium.Size] = 13d;
        Assert.Equal(13, text.FontSize);
        Assert.NotEqual(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color);
        window.Resources[Sys.Color.OnSurfaceVariant] = Colors.Lime;
        Assert.Equal(Colors.Lime, Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color);
        window.Close();
    }

    [AvaloniaFact]
    public void Colors_ComeFromIndicatorTokensNotFluentBrushes()
    {
        var bar = new ProgressBar { Value = 50, Foreground = Brushes.Red, Background = Brushes.Blue };
        var standalone = new LinearProgressIndicator { Progress = .5 };
        var window = Show(bar, standalone);
        var indicator = Indicator(bar);
        Assert.Equal(standalone.Color, indicator.Color);
        Assert.Equal(standalone.TrackColor, indicator.TrackColor);
        Assert.Equal(standalone.StopSize, indicator.StopSize);
        Assert.Equal(standalone.GapSize, indicator.GapSize);
        Assert.Equal(standalone.StrokeCap, indicator.StrokeCap);
        Assert.NotEqual(Colors.Red, indicator.Color);

        window.Resources[Comp.ProgressIndicator.ActiveIndicatorColor] = Colors.Green;
        window.Resources[Comp.ProgressIndicator.TrackColor] = Colors.Yellow;
        Assert.Equal(Colors.Green, indicator.Color);
        Assert.Equal(Colors.Yellow, indicator.TrackColor);
        window.Close();
    }

    [AvaloniaFact]
    public void DisabledAndDarkMode_MatchStandaloneIndicator()
    {
        var bar = new ProgressBar { Value = 50 };
        var standalone = new LinearProgressIndicator { Progress = .5 };
        var window = Show(bar, standalone);
        var indicator = Indicator(bar);
        var light = indicator.Color;
        window.RequestedThemeVariant = ThemeVariant.Dark; Draw(window);
        Assert.NotEqual(light, indicator.Color);
        Assert.Equal(standalone.Color, indicator.Color);
        Assert.Equal(standalone.TrackColor, indicator.TrackColor);
        bar.IsEnabled = false; standalone.IsEnabled = false; Draw(window);
        Assert.Equal(standalone.Color, indicator.Color);
        Assert.Equal(standalone.TrackColor, indicator.TrackColor);
        window.Close();
    }

    [AvaloniaFact]
    public void Automation_KeepsNativeProgressBarPeerAndHidesInnerIndicator()
    {
        var bar = new ProgressBar { Minimum = 10, Maximum = 20, Value = 15 };
        var window = Show(bar);
        var peer = ControlAutomationPeer.CreatePeerForElement(bar);
        Assert.IsType<ProgressBarAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.Equal(15, peer.GetProvider<Avalonia.Automation.Provider.IRangeValueProvider>()!.Value);
        var inner = ControlAutomationPeer.CreatePeerForElement(Indicator(bar));
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(Indicator(bar)));
        Assert.False(inner.IsControlElement()); Assert.False(inner.IsContentElement());
        window.Close();
    }

    [AvaloniaFact]
    public void MaterialTemplate_ProvidesHiddenIndicatorPartWithoutErrors()
    {
        var sink = new CapturingLogSink();
        var previous = Logger.Sink; Logger.Sink = sink;
        try
        {
            var bar = new ProgressBar { Value = 30, ShowProgressText = true };
            var window = Show(bar);
            bar.IsIndeterminate = true; Draw(window);
            bar.IsIndeterminate = false; bar.Orientation = Orientation.Vertical; Draw(window);
            var part = bar.GetTemplateDescendants().OfType<Border>().Single(b => b.Name == "PART_Indicator");
            Assert.False(part.IsVisible);
            // Percentage (and therefore the progress text) is only computed while PART_Indicator exists.
            Assert.Equal(30, bar.Percentage, 12);
            window.Close();
        }
        finally { Logger.Sink = previous; }
        Assert.Empty(sink.Errors);
    }

    [AvaloniaFact]
    public void NativeProgressBar_RequiresIndicatorPart()
    {
        // Documents why the Material template keeps a hidden PART_Indicator:
        // ProgressBar.OnApplyTemplate uses NameScope.Get, which throws when the part is missing.
        var bar = new ProgressBar
        {
            Template = new FuncControlTemplate<ProgressBar>((_, _) => new Panel()),
        };
        var window = new Window { Width = 200, Height = 160, Content = bar };
        var error = Assert.Throws<KeyNotFoundException>(window.Show);
        Assert.Contains("PART_Indicator", error.Message);
        window.Close();
    }

    static (int R, int G, int B) PixelAt(Avalonia.Media.Imaging.WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var row = new byte[buffer.RowBytes];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, buffer.RowBytes);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (row[x * 4], row[x * 4 + 1], row[x * 4 + 2])
            : (row[x * 4 + 2], row[x * 4 + 1], row[x * 4]);
    }

    static LinearProgressIndicator Indicator(ProgressBar bar) =>
        bar.GetVisualDescendants().OfType<LinearProgressIndicator>().Single();
    static LayoutTransformControl Transform(ProgressBar bar) =>
        bar.GetVisualDescendants().OfType<LayoutTransformControl>().Single();
    static TextBlock Text(ProgressBar bar) =>
        bar.GetTemplateDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_ProgressText");

    static AnimationTestWindow Show(ProgressBar bar, Control? sibling = null)
    {
        var clock = new ManualAnimationFrameClock(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(clock) { Width = 320, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        var grid = new Grid { Width = 160, Height = 120, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { bar } };
        window.Content = sibling is null ? grid : new StackPanel { Children = { grid, sibling } };
        window.Show(); Draw(window); return window;
    }

    static void Draw(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    sealed class CapturingLogSink : ILogSink
    {
        public List<string> Errors { get; } = [];
        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;
        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
            Log(level, area, source, messageTemplate, []);
        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            if (level >= LogEventLevel.Warning) Errors.Add($"{level} [{area}] {source?.GetType().Name}: {messageTemplate} {string.Join(", ", propertyValues)}");
        }
    }
}
