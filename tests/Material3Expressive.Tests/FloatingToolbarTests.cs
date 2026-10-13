using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class FloatingToolbarTests
{
    [AvaloniaFact]
    public void NativeScrollObservesActualConsumptionAndUnsubscribes()
    {
        using var clock = new MotionTestClockScope();
        var source = new ScrollViewer { Width = 120, Height = 100, Content = new Border { Height = 1000 } };
        var behavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(FloatingToolbarExitDirection.Bottom);
        var toolbar = new HorizontalFloatingToolbar { Content = new Border { Width = 48, Height = 48 }, ScrollSource = source, ScrollBehavior = behavior };
        var window = Show(new Grid { Children = { source, toolbar } });
        source.Theme = (ControlTheme)window.FindResource("MaterialFloatingToolbarScroller")!;
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        source.Offset = new Vector(0, 35); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        Assert.Equal(-35, behavior.State.Offset); Assert.Equal(-35, behavior.State.ContentOffset);
        toolbar.ScrollSource = null;
        source.Offset = new Vector(0, 60); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        Assert.Equal(-35, behavior.State.Offset);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ExpansionSubscriptionUsesConsumedScrollAndDisposal()
    {
        using var clock = new MotionTestClockScope();
        var source = new ScrollViewer { Width = 120, Height = 100, Content = new Border { Height = 1000 } };
        var window = Show(source);
        source.Theme = (ControlTheme)window.FindResource("MaterialFloatingToolbarScroller")!;
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        var expanded = true;
        var subscription = FloatingToolbarDefaults.FloatingToolbarVerticalNestedScroll(source, () => expanded, () => expanded = true, () => expanded = false);
        source.Offset = new Vector(0, 40); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); Assert.False(expanded);
        source.Offset = default; window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); Assert.True(expanded);
        subscription.Dispose(); subscription.Dispose();
        source.Offset = new Vector(0, 80); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); Assert.True(expanded);
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollingSuppressesFocusAndExplorationRestoresIt()
    {
        using var clock = new MotionTestClockScope();
        var button = new Button();
        var behavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(FloatingToolbarExitDirection.Bottom);
        var toolbar = new HorizontalFloatingToolbar { Content = button, ScrollBehavior = behavior };
        var window = Show(toolbar);
        behavior.OnPostScroll(-30); window.UpdateLayout(); Assert.False(button.Focusable);
        var replacement = new Button { Focusable = false }; toolbar.Content = replacement; window.UpdateLayout();
        Assert.True(button.Focusable); Assert.False(replacement.Focusable);
        replacement.Focusable = true; Assert.False(replacement.Focusable);
        toolbar.TouchExplorationEnabled = true; window.UpdateLayout(); Assert.True(replacement.Focusable);
        toolbar.TouchExplorationEnabled = false; window.UpdateLayout(); Assert.False(replacement.Focusable);
        behavior.OnPostScroll(30); window.UpdateLayout(); Assert.True(replacement.Focusable);
        window.Close();
    }

    [AvaloniaFact]
    public void MouseDragUsesExitDirectionAndDetachCancelsSettle()
    {
        using var clock = new MotionTestClockScope();
        var behavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(FloatingToolbarExitDirection.Bottom);
        var toolbar = new HorizontalFloatingToolbar { Content = new Border { Width = 48, Height = 48 }, ScrollBehavior = behavior };
        var window = Show(toolbar);
        var point = toolbar.TranslatePoint(new Point(32, 32), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseMove(point + new Vector(0, 20));
        Assert.True(behavior.State.Offset < -19);
        window.MouseUp(point + new Vector(0, 20), MouseButton.Left);
        toolbar.Expanded = false;
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void IconButtonReserves48WithoutChanging40ContainerOrExplicitConstraints()
    {
        using var clock = new MotionTestClockScope();
        var button = new IconButton();
        var window = Show(button);
        Assert.Equal(new Size(48, 48), button.Bounds.Size);
        var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        Assert.Equal(new Size(40, 40), surface.Bounds.Size);
        button.Width = button.Height = 32; window.UpdateLayout();
        Assert.Equal(new Size(32, 32), button.Bounds.Size);
        button.ClearValue(Control.WidthProperty); button.ClearValue(Control.HeightProperty);
        button.Classes.Add("xsmall"); window.UpdateLayout();
        Assert.Equal(new Size(32, 32), button.Bounds.Size);
        Assert.Equal(new Size(32, 32), surface.Bounds.Size);
        window.Close();
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExpansionThresholdTracksDirectionAndApplicationState(bool reverse)
    {
        var expanded = true; var collapses = 0; var expands = 0;
        var policy = new FloatingToolbarExpansionPolicy(() => expanded, () => { expands++; expanded = true; },
            () => { collapses++; expanded = false; }, reverse, 40, 40);
        var sign = reverse ? -1 : 1;
        policy.OnPostScroll(-39 * sign); Assert.True(expanded);
        policy.OnPostScroll(-1 * sign); Assert.False(expanded); Assert.Equal(1, collapses);
        policy.OnPostScroll(39 * sign); Assert.False(expanded);
        policy.OnPostScroll(1 * sign); Assert.True(expanded); Assert.Equal(1, expands);
        expanded = false; policy.OnPostScroll(20 * sign); Assert.False(expanded);
        policy.OnPostScroll(20 * sign); Assert.True(expanded); Assert.Equal(2, expands);
    }
    [Fact]
    public void TokensAndStatePreserveBaselineAndClamp()
    {
        TokenScan.AssertComplete(new FloatingToolbarTokenSource(), typeof(Tokens.MaterialTokens.Comp.FloatingToolbar));
        var state = new FloatingToolbarState(-100, -30, 12);
        state.Offset = -150; Assert.Equal(-100, state.Offset);
        state.Offset = 30; Assert.Equal(0, state.Offset);
        state.OffsetLimit = -20; Assert.Equal(0, state.Offset);
        Assert.Equal(12, state.ContentOffset);
        var behavior = FloatingToolbarDefaults.ExitAlwaysScrollBehavior(FloatingToolbarExitDirection.End, state);
        behavior.OnPostScroll(-12); Assert.Equal(-12, state.Offset); Assert.Equal(0, state.ContentOffset);
        behavior.OnPostScroll(-20); Assert.Equal(-20, state.Offset); Assert.Equal(-20, state.ContentOffset);
    }
    [AvaloniaTheory]
    [InlineData(true)] [InlineData(false)]
    public void OrdinaryToolbarUsesContentSlotsAndMinimumCrossSize(bool horizontal)
    {
        using var clock = new MotionTestClockScope();
        FloatingToolbar toolbar = horizontal ? new HorizontalFloatingToolbar() : new VerticalFloatingToolbar();
        toolbar.Content = new Border { Width = 48, Height = 48 };
        toolbar.LeadingContent = new Border { Width = 24, Height = 24 };
        toolbar.TrailingContent = new Border { Width = 24, Height = 24 };
        var window = Show(toolbar);
        Assert.Equal(horizontal ? new Size(112, 64) : new Size(64, 112), toolbar.Bounds.Size);
        toolbar.Expanded = false; Frames(clock, window);
        Assert.Equal(new Size(64, 64), toolbar.Bounds.Size);
        toolbar.Expanded = true; Frames(clock, window);
        Assert.Equal(horizontal ? new Size(112, 64) : new Size(64, 112), toolbar.Bounds.Size);
        if (horizontal) toolbar.Width = 224; else toolbar.Height = 224;
        window.UpdateLayout();
        var mainPosition = ((Control)toolbar.Content).TranslatePoint(default, toolbar)!.Value;
        Assert.Equal(88, horizontal ? mainPosition.X : mainPosition.Y);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }
    [AvaloniaTheory]
    [InlineData(true)] [InlineData(false)]
    public async Task FabOverloadRetainsAllocationAndResizesFab(bool horizontal)
    {
        using var clock = new MotionTestClockScope();
        FloatingToolbar toolbar = horizontal ? new HorizontalFloatingToolbar() : new VerticalFloatingToolbar();
        toolbar.Content = new Border { Width = 48, Height = 48 };
        var fab = new FloatingActionButton { Content = new Border { Width = 24, Height = 24 } };
        toolbar.FloatingActionButton = fab;
        var window = Show(toolbar);
        fab.Theme = (ControlTheme)window.FindResource("MaterialStandardFloatingToolbarFabTheme")!; window.UpdateLayout();
        Assert.Equal(horizontal ? new Size(128, 80) : new Size(80, 128), toolbar.Bounds.Size);
        Assert.Equal(new Size(56, 56), fab.Bounds.Size);
        toolbar.Expanded = false;
        // Transition's idle wake is asynchronous. Give the dispatcher a turn between
        // frames when checking the settled layout; timestamp parity tests separately
        // retain the dependency's late-registration evidence.
        for (var i = 0; i < 80; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
            await Task.Yield();
        }
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
        Assert.Equal(horizontal ? new Size(128, 80) : new Size(80, 128), toolbar.Bounds.Size);
        Assert.Equal(new Size(80, 80), fab.Bounds.Size);
        var surface = toolbar.GetVisualDescendants().OfType<MaterialSurface>().First(surface => surface.Name == "PART_Container");
        Assert.Equal(0, horizontal ? surface.Bounds.Width : surface.Bounds.Height);
        window.Close();
    }
    [AvaloniaFact]
    public void AccessibilityCollapseIsIndependentOfApplicationAndExploration()
    {
        using var clock = new MotionTestClockScope();
        var toolbar = new HorizontalFloatingToolbar { Expanded = false, TouchExplorationEnabled = true };
        var window = Show(toolbar);
        Assert.True(toolbar.EffectiveExpanded);
        var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(ControlAutomationPeer.CreatePeerForElement(toolbar));
        provider.Collapse(); Assert.False(toolbar.EffectiveExpanded); Assert.False(toolbar.Expanded);
        provider.Expand(); Assert.True(toolbar.EffectiveExpanded); Assert.False(toolbar.Expanded);
        toolbar.TouchExplorationEnabled = false; Assert.False(toolbar.EffectiveExpanded);
        window.Close();
    }
    [AvaloniaFact]
    public void ButtonInteractivePaddingBalancesOnlyWhenNeitherSideIsVisible()
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Content = new Border { Width = 0, Height = 0 } };
        var toolbar = new HorizontalFloatingToolbar { Expanded = false, Content = button };
        var window = Show(toolbar);
        Assert.Equal(82, toolbar.Bounds.Width); // 58 container + two 4dp balancing pads + 16 content padding.
        toolbar.LeadingContent = new Border { Width = 24, Height = 24 }; toolbar.Expanded = true; Frames(clock, window);
        Assert.Equal(98, toolbar.Bounds.Width);
        window.Close();
    }
    private static void Frames(MotionTestClockScope clock, Window window) { for (var i = 0; i < 80; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 360, Height = 400 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); return window;
    }
}
