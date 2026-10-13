using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using MaterialShapes;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class LoadingIndicatorTests
{
    static LoadingIndicatorBase Create(bool contained) => contained ? new ContainedLoadingIndicator() : new LoadingIndicator();

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Layout_DefaultsConstraintsExplicitSizeAndDefaultSequences(bool contained)
    {
        var indicator = Create(contained);
        using var host = new Host(indicator);
        Assert.Equal(new Size(48, 48), indicator.Bounds.Size);
        Assert.True(indicator.IsIndeterminate); Assert.False(indicator.Focusable);
        Assert.Same(LoadingIndicatorDefaults.IndeterminateIndicatorPolygons, indicator.EffectivePolygons);
        indicator.IsIndeterminate = false;
        Assert.Same(LoadingIndicatorDefaults.DeterminateIndicatorPolygons, indicator.EffectivePolygons);
        indicator.Width = 80; indicator.Height = 24; host.Draw();
        Assert.Equal(new Size(80, 24), indicator.Bounds.Size);
        indicator.ClearValue(Control.WidthProperty); indicator.ClearValue(Control.HeightProperty);
        host.Root.Width = 20; host.Root.Height = 12; host.Draw();
        Assert.Equal(new Size(20, 12), indicator.Bounds.Size);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Automation_UsesUnexpandedBoundsAndReadOnlyClampedProgress(bool contained)
    {
        var indicator = Create(contained); using var host = new Host(indicator);
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator)!;
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.Null(peer.GetProvider<IRangeValueProvider>());
        indicator.IsIndeterminate = false;
        var range = peer.GetProvider<IRangeValueProvider>()!;
        Assert.True(range.IsReadOnly); Assert.Equal(0, range.Minimum); Assert.Equal(1, range.Maximum);
        indicator.Progress = 2; Assert.Equal(1, range.Value); Assert.Equal(2, indicator.Progress);
        indicator.Progress = -1; Assert.Equal(0, range.Value);
        indicator.Progress = double.NaN; Assert.Equal(0, range.Value); host.Draw();
        indicator.Progress = double.PositiveInfinity; Assert.Equal(1, range.Value); host.Draw();
        indicator.Progress = .3; Assert.Equal((double)(float).3, range.Value);
        Assert.Equal(indicator.Bounds.Size, peer.GetBoundingRectangle().Size);
        Assert.Throws<InvalidOperationException>(() => range.SetValue(.8));
        indicator.IsIndeterminate = true; Assert.Null(peer.GetProvider<IRangeValueProvider>());
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DetachAndReattach_CancelDelayAndFramesAndStartFresh(bool contained)
    {
        var indicator = Create(contained); using var host = new Host(indicator);
        host.Frame(0); host.Advance(80);
        Assert.True(indicator.MorphProgress > 0); Assert.True(indicator.GlobalRotation > 0);
        var old = indicator.AnimationScope!;
        var sibling = host.Window.AnimationRoot.DelayAsync(TimeSpan.FromSeconds(2));
        host.Root.Children.Remove(indicator); host.Draw();
        Assert.True(old.Diagnostics.IsEnding); Assert.Null(indicator.AnimationScope);
        Assert.False(host.Window.AnimationRoot.Diagnostics.IsEnding); Assert.False(sibling.IsCompleted);
        Assert.Equal(1, host.Time.ActiveTimerCount);
        host.Root.Children.Add(indicator); host.Draw();
        Assert.NotSame(old, indicator.AnimationScope);
        Assert.Equal(0, indicator.MorphIndex); Assert.Equal(0, indicator.MorphProgress);
        Assert.Equal(90, indicator.RotationTarget); Assert.Equal(0, indicator.GlobalRotation);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Modes_ResetRememberedStateWhilePolygonsRetainAnimationValues(bool contained)
    {
        var indicator = Create(contained); using var host = new Host(indicator);
        host.Frame(0); host.Advance(80);
        var progress = indicator.MorphProgress; var rotation = indicator.GlobalRotation;
        indicator.Polygons = [MaterialShapeCatalog.Cookie4Sided, MaterialShapeCatalog.Oval];
        Assert.Equal(0, indicator.MorphIndex); Assert.Equal(progress, indicator.MorphProgress);
        Assert.Equal(rotation, indicator.GlobalRotation); Assert.Equal(90, indicator.RotationTarget);
        host.Advance(600); Assert.True(indicator.MorphIndex > 0);
        var index = indicator.MorphIndex;
        indicator.Polygons = indicator.Polygons.ToArray(); Assert.Equal(index, indicator.MorphIndex);
        indicator.IsIndeterminate = false; host.Draw();
        Assert.Equal(0, host.Time.ActiveTimerCount);
        indicator.IsIndeterminate = true;
        Assert.Equal(0, indicator.MorphProgress); Assert.Equal(0, indicator.GlobalRotation);
        Assert.Equal(0, indicator.MorphIndex); Assert.Equal(90, indicator.RotationTarget);
        Assert.Same(indicator.Polygons, indicator.EffectivePolygons);
    }

    [AvaloniaFact]
    public void Delay_DoesNotAdvanceMorphWithoutAnimationFrames()
    {
        var indicator = new LoadingIndicator(); using var host = new Host(indicator);
        host.Frame(0);
        host.Time.AdvanceBy(TimeSpan.FromMilliseconds(1000)); host.Draw();
        Assert.Equal(0, indicator.MorphProgress); Assert.Equal(0, indicator.MorphIndex);
        Assert.Equal(0, indicator.GlobalRotation);
        host.Clock.SendFrame(TimeSpan.FromMilliseconds(1000)); host.Draw();
        Assert.Equal(1, indicator.MorphIndex); Assert.Equal(0, indicator.MorphProgress);
        Assert.Equal(180, indicator.RotationTarget); Assert.True(indicator.GlobalRotation > 0);
        Assert.Equal(1, host.Time.ActiveTimerCount);
    }

    [AvaloniaFact]
    public void PolygonRestart_PreservesFinalOldFrameAndCoalescesPendingKeys()
    {
        var reference = new LoadingIndicator(); var changing = new LoadingIndicator();
        using var host = new Host(new StackPanel { Children = { reference, changing } });
        host.Frame(0); host.Advance(80);
        changing.Polygons = [MaterialShapeCatalog.Pill, MaterialShapeCatalog.SoftBurst];
        host.Frame(100); host.Frame(100);
        Assert.Equal(reference.MorphProgress, changing.MorphProgress);
        Assert.Equal(reference.GlobalRotation, changing.GlobalRotation);
        changing.Polygons = LoadingIndicatorDefaults.IndeterminateIndicatorPolygons;
        host.Frame(120); host.Frame(120);
        changing.Polygons = [MaterialShapeCatalog.Pill, MaterialShapeCatalog.SoftBurst];
        changing.Polygons = null; // Revert the uncommitted key; the old effect keeps running.
        var before = changing.MorphProgress;
        host.Draw(); Assert.Equal(before, changing.MorphProgress);
        host.Frame(160); Assert.True(changing.MorphProgress > before);
    }

    [AvaloniaFact]
    public void PendingShorterSequence_DrawRetainsOldSequenceUntilTheRecompositionFrame()
    {
        var indicator = new LoadingIndicator(); using var host = new Host(indicator);
        host.Frame(0); host.Advance(3500); Assert.True(indicator.MorphIndex > 1);
        var index = indicator.MorphIndex;
        indicator.Polygons = [MaterialShapeCatalog.Circle, MaterialShapeCatalog.Pill];
        host.Draw(); Assert.Equal(index, indicator.MorphIndex);
        host.Frame(3500); host.Frame(3500); Assert.InRange(indicator.MorphIndex, 0, 1);
    }

    [AvaloniaFact]
    public void Polygons_RejectShortSequencesAndAllowModeDependentDefaults()
    {
        var indicator = new LoadingIndicator();
        Assert.Throws<ArgumentException>(() => indicator.Polygons = []);
        Assert.Throws<ArgumentException>(() => indicator.Polygons = [MaterialShapeCatalog.Circle]);
        indicator.Polygons = [MaterialShapeCatalog.Pill, MaterialShapeCatalog.SoftBurst];
        indicator.IsIndeterminate = false; Assert.Equal(2, indicator.EffectivePolygons.Count);
        indicator.Polygons = null; Assert.Same(LoadingIndicatorDefaults.DeterminateIndicatorPolygons, indicator.EffectivePolygons);
    }

    [AvaloniaFact]
    public void TokensAndCustomOutline_UpdateWithoutRestartingMotion()
    {
        var indicator = new ContainedLoadingIndicator(); using var host = new Host(indicator);
        host.Frame(0); host.Advance(200); var progress = indicator.MorphProgress;
        host.Window.Resources[Comp.LoadingIndicator.ContainedActiveColor] = Colors.Red;
        host.Window.Resources[Comp.LoadingIndicator.ContainedContainerColor] = Colors.Blue;
        host.Window.Resources[Comp.LoadingIndicator.ContainerShape] = new CornerRadius(12);
        host.Draw(); Assert.Equal(Colors.Red, indicator.IndicatorColor); Assert.Equal(Colors.Blue, indicator.ContainerColor);
        Assert.Equal(new CornerRadius(12), indicator.CornerRadius); Assert.Equal(progress, indicator.MorphProgress);
        indicator.IndicatorColor = Colors.Green;
        host.Window.Resources[Comp.LoadingIndicator.ContainedActiveColor] = Colors.Yellow;
        host.Draw(); Assert.Equal(Colors.Green, indicator.IndicatorColor);
        indicator.ClearValue(ContainedLoadingIndicator.IndicatorColorProperty); Assert.Equal(Colors.Yellow, indicator.IndicatorColor);
        indicator.FlowDirection = FlowDirection.RightToLeft;
        Size? size = null; FlowDirection? direction = null;
        indicator.ContainerShape = (actualSize, flow) =>
        { size = actualSize; direction = flow; return new EllipseGeometry(new Rect(actualSize)); };
        host.Draw(); Assert.Equal(indicator.Bounds.Size, size); Assert.Equal(FlowDirection.RightToLeft, direction);
        Assert.Equal(progress, indicator.MorphProgress);
    }

    [AvaloniaFact]
    public void RealXaml_ResolvesBothControlsAndApprovedProperties()
    {
        var root = (StackPanel)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/LoadingIndicator.axaml"));
        using var host = new Host(root);
        var plain = Assert.IsType<LoadingIndicator>(root.Children[0]);
        var contained = Assert.IsType<ContainedLoadingIndicator>(root.Children[1]);
        Assert.False(plain.IsIndeterminate); Assert.Equal(.5, plain.Progress);
        Assert.Equal(new CornerRadius(12), contained.CornerRadius);
    }

    sealed class Host : IDisposable
    {
        public ManualFrameSource Clock { get; } = new(Dispatcher.UIThread.VerifyAccess);
        public ManualTimeProvider Time { get; } = new(Dispatcher.UIThread.VerifyAccess);
        public AnimationTestWindow Window { get; }
        public Grid Root { get; } = new();
        int frame;
        public Host(Control content)
        {
            Window = new(Clock, Time) { Width = 320, Height = 160 };
            Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
            Root.Children.Add(content); Window.Content = Root; Window.Show(); Draw();
        }
        public void Draw()
        {
            Window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        }
        public void Frame(int ms) { Clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Draw(); }
        public void Advance(int target)
        {
            while (frame + 16 <= target)
            {
                frame += 16;
                Time.AdvanceBy(TimeSpan.FromMilliseconds(frame) - Time.Elapsed); Dispatcher.UIThread.RunJobs(); Frame(frame);
            }
        }
        public void Dispose()
        {
            Window.Close(); Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, Clock.PendingFrameCount); Assert.Equal(0, Time.ActiveTimerCount); Assert.Null(Window.AnimationRoot.Diagnostics.LastError);
            Clock.Dispose();
        }
    }
}
