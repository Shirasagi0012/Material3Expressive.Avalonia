using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ProgressIndicatorTests
{
    static ProgressIndicator Create(string kind) => kind switch
    { "linear" => new LinearProgressIndicator(), "circular" => new CircularProgressIndicator(),
        "linear-wavy" => new LinearWavyProgressIndicator(), _ => new CircularWavyProgressIndicator() };

    [AvaloniaTheory]
    [InlineData("linear", 240, 4)]
    [InlineData("circular", 40, 40)]
    [InlineData("linear-wavy", 240, 10)]
    [InlineData("circular-wavy", 48, 48)]
    public void DefaultSize_UsesSoftConstraintsAndSupportsExplicitSize(string kind, double width, double height)
    {
        var indicator = Create(kind);
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = Show(clock, indicator);
        Assert.Equal(new Size(width, height), indicator.Bounds.Size);
        Assert.False(indicator.Focusable);
        indicator.Width = 80; indicator.Height = 24; Draw(window);
        Assert.Equal(new Size(80, 24), indicator.Bounds.Size);
        indicator.ClearValue(Control.WidthProperty); indicator.ClearValue(Control.HeightProperty);
        ((Grid)window.Content!).Width = 20; ((Grid)window.Content!).Height = 2; Draw(window);
        Assert.Equal(new Size(20, 2), indicator.Bounds.Size);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("linear")]
    [InlineData("circular")]
    [InlineData("linear-wavy")]
    [InlineData("circular-wavy")]
    public void Automation_IsReadOnlyAndSuppressesRangeWhenIndeterminate(string kind)
    {
        var indicator = Create(kind);
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = Show(clock, indicator);
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator)!;
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        var range = peer.GetProvider<IRangeValueProvider>()!;
        Assert.True(range.IsReadOnly); Assert.Equal(0, range.Minimum); Assert.Equal(1, range.Maximum);
        indicator.Progress = 2; Assert.Equal(1, range.Value); Assert.Equal(2, indicator.Progress);
        indicator.Progress = double.NaN; Assert.Equal(0, range.Value);
        indicator.Progress = double.PositiveInfinity; Assert.Equal(1, range.Value);
        indicator.Progress = .3; Assert.Equal((double)(float).3, range.Value);
        Assert.Throws<InvalidOperationException>(() => range.SetValue(.8));
        if (kind.StartsWith("linear")) Assert.Equal(indicator.Bounds.Height + 20, peer.GetBoundingRectangle().Height);
        indicator.IsIndeterminate = true; Assert.Null(peer.GetProvider<IRangeValueProvider>());
        indicator.IsIndeterminate = false; Assert.Same(range, peer.GetProvider<IRangeValueProvider>());
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("linear")]
    [InlineData("circular")]
    [InlineData("linear-wavy")]
    [InlineData("circular-wavy")]
    public void Detach_CancelsAnimationsAndReattachStartsFreshWithoutEndingSibling(string kind)
    {
        var indicator = Create(kind); indicator.IsIndeterminate = true;
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = Show(clock, indicator);
        var grid = (Grid)window.Content!;
        var sibling = new Animatable<float>(new FloatConverter(), 0f, VisibilityThresholds.Float);
        var playing = sibling.AnimateToAsync(window.AnimationRoot, 1, new TweenSpec<float>(TimeSpan.FromSeconds(2)));
        clock.SendFrame(TimeSpan.FromSeconds(1)); Draw(window);
        clock.SendFrame(TimeSpan.FromMilliseconds(1400)); Draw(window);
        Assert.NotNull(indicator.AnimationScope);
        Assert.True(indicator.Circular ? indicator.GlobalRotation > 0 : indicator.Fractions[1] > 0);
        var old = indicator.AnimationScope!;
        grid.Children.Remove(indicator); Draw(window);
        Assert.True(old.Diagnostics.IsEnding); Assert.Null(indicator.AnimationScope);
        Assert.False(window.AnimationRoot.Diagnostics.IsEnding); Assert.False(playing.IsCompleted);
        Assert.Equal(0, window.AnimationRoot.Diagnostics.ActiveChildCount);
        grid.Children.Add(indicator); Draw(window);
        Assert.NotSame(old, indicator.AnimationScope);
        Assert.Equal(0, indicator.GlobalRotation); Assert.Equal(0, indicator.Fractions[1]);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount); Assert.Null(window.AnimationRoot.Diagnostics.LastError);
    }

    [AvaloniaFact]
    public void Tokens_UpdateDefaultsWhileExplicitPaintAndStrokeRemainLocal()
    {
        var indicator = new LinearWavyProgressIndicator { Progress = .5, WaveSpeed = 0 };
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = Show(clock, indicator);
        window.Resources[Comp.ProgressIndicator.ActiveIndicatorColor] = Colors.Red;
        window.Resources[Comp.LinearProgressIndicator.WaveHeight] = 16d;
        window.Resources[Comp.LinearProgressIndicator.ActiveThickness] = 6d;
        window.Resources[Comp.LinearProgressIndicator.ActiveWaveWavelength] = 50d; Draw(window);
        Assert.Equal(Colors.Red, indicator.Color); Assert.Equal(16, indicator.Bounds.Height);
        Assert.Equal(6, indicator.ActiveStroke.Width); Assert.Equal(50, indicator.DefaultWavelength);
        indicator.Color = Colors.Blue; indicator.Stroke = new(8);
        window.Resources[Comp.ProgressIndicator.ActiveIndicatorColor] = Colors.Green;
        window.Resources[Comp.LinearProgressIndicator.ActiveThickness] = 2d; Draw(window);
        Assert.Equal(Colors.Blue, indicator.Color); Assert.Equal(8, indicator.ActiveStroke.Width);
        indicator.ClearValue(ProgressIndicator.ColorProperty); indicator.ClearValue(WavyProgressIndicator.StrokeProperty);
        Assert.Equal(Colors.Green, indicator.Color); Assert.Equal(2, indicator.ActiveStroke.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void CircularAmplitudeCache_ObservesPendingTargetWhenWaveMotionIsStopped()
    {
        var indicator = new CircularWavyProgressIndicator { Progress = .05, WaveSpeed = 0 };
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = Show(clock, indicator);
        var time = 0;
        void Advance(int target)
        {
            while (time + 16 <= target) { time += 16; clock.SendFrame(TimeSpan.FromMilliseconds(time)); Draw(window); }
        }
        void Change(int target, double progress)
        {
            Advance(target); indicator.Progress = progress; Draw(window);
            clock.SendFrame(TimeSpan.FromMilliseconds(target)); Draw(window);
            clock.SendFrame(TimeSpan.FromMilliseconds(target)); Draw(window);
        }
        clock.SendFrame(TimeSpan.Zero); Draw(window);
        Change(200, .8); Change(350, .98); Change(800, .4); Change(1000, .98); Change(1250, .5);
        Advance(1600);
        Assert.True(indicator.CurrentAmplitude > 0, $"Expected the observed pending target to resume; amplitude={indicator.CurrentAmplitude}");
        Advance(1900); Assert.True(indicator.CurrentAmplitude > .9);
        window.Close();
    }

    [AvaloniaFact]
    public void CircularMode_ChangesDefaultTrackAndLinearMode_ChangesDefaultWavelength()
    {
        var circular = new CircularProgressIndicator(); var linear = new LinearWavyProgressIndicator { WaveSpeed = 0 };
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = Show(clock, circular);
        var initial = circular.TrackColor;
        circular.IsIndeterminate = true; Assert.Equal(Colors.Transparent, circular.TrackColor);
        circular.TrackColor = Colors.Red; circular.IsIndeterminate = false; Assert.Equal(Colors.Red, circular.TrackColor);
        circular.ClearValue(ProgressIndicator.TrackColorProperty); Assert.Equal(initial, circular.TrackColor);
        ((Grid)window.Content!).Children.Add(linear); Draw(window);
        Assert.Equal(40, linear.DefaultWavelength);
        linear.IsIndeterminate = true; Assert.Equal(20, linear.DefaultWavelength);
        window.Close();
    }

    static AnimationTestWindow Show(ManualFrameSource clock, ProgressIndicator indicator)
    {
        var window = new AnimationTestWindow(clock) { Width = 320, Height = 160 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { indicator } };
        window.Show(); Draw(window); return window;
    }
    static void Draw(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
    }
}
