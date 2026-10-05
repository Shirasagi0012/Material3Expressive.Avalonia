using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class IndicatorConfigurationTests
{
    [AvaloniaFact]
    public void CircularTrackModeDefault_DoesNotOverrideOrdinaryStyle()
    {
        var indicator = new CircularProgressIndicator { IsIndeterminate = true };
        var style = new Style { Setters = { new Setter(ProgressIndicator.TrackColorProperty, Colors.Red) } };
        indicator.Styles.Add(style);
        using var clock = new MotionTestClockScope(); var window = Show(indicator);
        try
        {
            Assert.Equal(Colors.Red, indicator.TrackColor);
            indicator.IsIndeterminate = false; Flush(window); Assert.Equal(Colors.Red, indicator.TrackColor);
            indicator.IsIndeterminate = true; Flush(window); Assert.Equal(Colors.Red, indicator.TrackColor);
            indicator.TrackColor = Colors.Blue; Flush(window); indicator.ClearValue(ProgressIndicator.TrackColorProperty); Flush(window);
            Assert.Equal(Colors.Red, indicator.TrackColor);
            indicator.Styles.Remove(style); Flush(window); Assert.Equal(Colors.Transparent, indicator.TrackColor);
            indicator.Resources[Comp.CircularProgressIndicator.IndeterminateTrackColor] = Colors.Green; Flush(window);
            Assert.Equal(Colors.Green, indicator.TrackColor);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void WavyDefaults_KeepStyleAndSemanticNullBehavior()
    {
        var indicator = new LinearWavyProgressIndicator { WaveSpeed = 0 };
        var style = new Style { Setters = { new Setter(ProgressIndicator.DefaultWavelengthProperty, 32d) } };
        indicator.Styles.Add(style);
        using var clock = new MotionTestClockScope(); var window = Show(indicator);
        try
        {
            Assert.Equal(32, indicator.DefaultWavelength);
            indicator.IsIndeterminate = true; Flush(window); Assert.Equal(32, indicator.DefaultWavelength);
            indicator.Styles.Remove(style); Flush(window); Assert.Equal(20, indicator.DefaultWavelength);
            indicator.Resources[Comp.LinearProgressIndicator.IndeterminateActiveWaveWavelength] = 28d;
            indicator.Resources[Comp.LinearProgressIndicator.Width] = 180d;
            indicator.Resources[Comp.LinearProgressIndicator.ActiveThickness] = 7d; Flush(window);
            Assert.Equal(28, indicator.DefaultWavelength); Assert.Equal(180, indicator.Bounds.Width); Assert.Equal(7, indicator.ActiveStroke.Width);
            indicator.Stroke = new(9); Flush(window); Assert.Equal(9, indicator.ActiveStroke.Width);
            indicator.Stroke = null; Flush(window); Assert.Equal(7, indicator.ActiveStroke.Width);
            Assert.Null(indicator.Wavelength); Assert.Equal(0, indicator.WaveSpeed);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LoadingDimensions_ConsumeCurrentThemeValues_AndKeepPolygonStateSeparate()
    {
        var indicator = new LoadingIndicator { IsIndeterminate = false };
        using var clock = new MotionTestClockScope(); var window = Show(indicator);
        try
        {
            var polygons = indicator.EffectivePolygons;
            Assert.Equal(new Size(48, 48), indicator.Bounds.Size); Assert.Equal(38, indicator.IndicatorSize);
            indicator.Resources[Comp.LoadingIndicator.ContainerWidth] = 60d;
            indicator.Resources[Comp.LoadingIndicator.ContainerHeight] = 52d;
            indicator.Resources[Comp.LoadingIndicator.ActiveSize] = 42d; Flush(window);
            Assert.Equal(new Size(60, 52), indicator.Bounds.Size); Assert.Equal(42, indicator.IndicatorSize);
            Assert.Same(polygons, indicator.EffectivePolygons); Assert.Null(indicator.Polygons);
            indicator.Width = 32; indicator.Height = 24; Flush(window); Assert.Equal(new Size(32, 24), indicator.Bounds.Size);
            indicator.ClearValue(Control.WidthProperty); indicator.ClearValue(Control.HeightProperty); Flush(window);
            Assert.Equal(new Size(60, 52), indicator.Bounds.Size);
        }
        finally { window.Close(); }
    }

    private static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 500, Height = 400 };
        window.Styles.Add(new MaterialTheme()); window.Content = new StackPanel { Children = { content } };
        window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}
