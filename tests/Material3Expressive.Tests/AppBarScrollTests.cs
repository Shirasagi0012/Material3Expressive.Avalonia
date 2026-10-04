using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarScrollTests
{
    [AvaloniaFact]
    public void StandardTemplate_KeepsParts_AndConsumesTopThenContentThenBottom()
    {
        var top = new TopAppBarState(-64, -24);
        var bottom = new BottomAppBarState(-80);
        var viewer = new ScrollViewer { Content = new Border { Height = 800 } };
        var scope = new AppBarScrollScope { TopScrollBehavior = TopAppBarDefaults.EnterAlwaysScrollBehavior(top),
            BottomScrollBehavior = BottomAppBarDefaults.ExitAlwaysScrollBehavior(bottom), Child = viewer };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        try
        {
            Assert.Single(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            Assert.Equal(2, viewer.GetVisualDescendants().OfType<ScrollBar>().Count());
            window.MouseWheel(new Point(60, 60), new Vector(0, -1));
            Assert.Equal(-64, top.HeightOffset); Assert.Equal(0, viewer.Offset.Y); Assert.Equal(0, bottom.HeightOffset);
            window.MouseWheel(new Point(60, 60), new Vector(0, -1));
            Assert.Equal(50, viewer.Offset.Y); Assert.Equal(-50, bottom.HeightOffset); Assert.Equal(-50, top.ContentOffset);
            window.MouseWheel(new Point(60, 60), new Vector(0, 1)); Assert.Equal(-14, top.HeightOffset); Assert.Equal(50, viewer.Offset.Y);
            viewer.Offset = new Vector(0, 100); window.UpdateLayout(); Assert.Equal(-50, top.ContentOffset); // programmatic scrolling is native, not fake nested input
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Scaffold_TopAndBottomShareNativeInput_AndConsumedPaddingTracksTheirLayout()
    {
        var top = new TopAppBarState(); var bottom = new BottomAppBarState();
        var t = TopAppBarDefaults.EnterAlwaysScrollBehavior(top); var b = BottomAppBarDefaults.ExitAlwaysScrollBehavior(bottom);
        var viewer = new ScrollViewer { Content = new Border { Height = 1200 } };
        var screen = new Scaffold { TopBar = new MediumTopAppBar { ScrollBehavior = t }, BottomBar = new BottomAppBar { ScrollBehavior = b } };
        var body = new Border { Child = viewer, Width = 300, Height = 240 };
        body.Bind(Border.PaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        body.Bind(MaterialWindowInsets.ConsumePaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty)); screen.Content = body;
        var scope = new AppBarScrollScope { Child = screen, TopScrollBehavior = t, BottomScrollBehavior = b };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        try
        {
            using (window.CaptureRenderedFrame()) { }
            window.MouseWheel(new Point(40, 130), new Vector(0, -1)); window.UpdateLayout();
            Assert.Equal(-48, top.HeightOffset); Assert.Equal(0, viewer.Offset.Y); Assert.Equal(64, screen.ContentPadding.Top);
            window.MouseWheel(new Point(40, 100), new Vector(0, -1)); window.UpdateLayout();
            Assert.Equal(50, viewer.Offset.Y); Assert.Equal(-50, bottom.HeightOffset); Assert.Equal(30, screen.ContentPadding.Bottom);
            Assert.Equal(body.Padding, MaterialWindowInsets.GetConsumePadding(body));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PartialWheel_TravelsToOuterViewer_AndNestedScopesComposeOnce()
    {
        var inner = new ScrollViewer { Height = 100, Content = new Border { Height = 110 } };
        var innerScope = new AppBarScrollScope { Child = inner };
        var outer = new ScrollViewer { Content = new StackPanel { Children = { innerScope, new Border { Height = 700 } } } };
        var state = new BottomAppBarState(-80);
        var scope = new AppBarScrollScope { Child = outer, BottomScrollBehavior = BottomAppBarDefaults.ExitAlwaysScrollBehavior(state) };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        try
        {
            Assert.Equal(2, scope.GetVisualDescendants().OfType<AppBarScrollContentPresenter>().Count());
            window.MouseWheel(new Point(40, 40), new Vector(0, -1));
            Assert.Equal(10, inner.Offset.Y); Assert.Equal(40, outer.Offset.Y); Assert.Equal(-50, state.ContentOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CustomTemplate_IsPreserved_ClearRestoresStandard_AndDetachReleasesAdapter()
    {
        var custom = new FuncControlTemplate<ScrollViewer>((owner, scope) =>
        {
            var presenter = new ScrollContentPresenter { Name = "PART_ContentPresenter" }; scope.Register(presenter.Name, presenter);
            return presenter;
        });
        var viewer = new ScrollViewer { Template = custom, Content = new Border { Height = 800 } };
        var scope = new AppBarScrollScope { Child = viewer };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        try
        {
            Assert.Same(custom, viewer.Template); Assert.Empty(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            viewer.ClearValue(TemplatedControl.TemplateProperty); window.UpdateLayout(); window.UpdateLayout();
            Assert.Single(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            viewer.Template = custom; window.UpdateLayout(); Assert.Same(custom, viewer.Template);
            Assert.Empty(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            viewer.ClearValue(TemplatedControl.TemplateProperty); window.UpdateLayout(); window.UpdateLayout();
            scope.Child = null; window.Content = viewer; window.UpdateLayout();
            Assert.False(AppBarScrollScope.GetUseStandardPresenter(viewer)); Assert.Empty(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LateStyleTemplate_IsPreserved_AndRemovingItRestoresStandardAdapter()
    {
        var custom = new FuncControlTemplate<ScrollViewer>((owner, names) =>
        {
            var presenter = new ScrollContentPresenter { Name = "PART_ContentPresenter" };
            names.Register(presenter.Name, presenter); return presenter;
        });
        var viewer = new ScrollViewer { Content = new Border { Height = 800 } };
        var scope = new AppBarScrollScope { Child = viewer };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        try
        {
            Assert.Single(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            var style = new Style(selector => selector.OfType<ScrollViewer>());
            style.Setters.Add(new Setter(TemplatedControl.TemplateProperty, custom));
            window.Styles.Add(style); window.UpdateLayout(); window.UpdateLayout();
            Assert.Same(custom, viewer.Template);
            Assert.Empty(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            window.Styles.Remove(style); window.UpdateLayout(); window.UpdateLayout();
            Assert.Single(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            scope.Child = null; window.Content = viewer; window.UpdateLayout();
            Assert.Empty(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void EmbeddedViewers_UseSameProtocol(bool textBox)
    {
        Control body = textBox ? new TextBox { Text = string.Join('\n', Enumerable.Range(0, 80)), AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.NoWrap } :
            new ListBox { ItemsSource = Enumerable.Range(0, 80) };
        var state = new TopAppBarState(-64);
        var scope = new AppBarScrollScope { Child = body, TopScrollBehavior = TopAppBarDefaults.EnterAlwaysScrollBehavior(state) };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        try
        {
            Assert.NotEmpty(body.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            window.MouseWheel(new Point(60, 60), new Vector(0, -1)); Assert.Equal(-50, state.HeightOffset);
            window.MouseWheel(new Point(60, 60), new Vector(0, -1)); Assert.Equal(-64, state.HeightOffset);
            window.MouseWheel(new Point(60, 60), new Vector(0, -1));
            Assert.Contains(body.GetVisualDescendants().OfType<ScrollViewer>(), viewer => viewer.Offset.Y > 0);
        }
        finally { window.Close(); }
    }

    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 300, Height = 240 };
        window.Styles.Add(new FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = control; window.Show(); window.UpdateLayout(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); return window;
    }

    [AvaloniaTheory]
    [InlineData(1)] [InlineData(2)]
    public async Task RawTouch_RecognizesVerticalDrag_RunsSpline_AndInterrupts(double density)
    {
        var viewer = new ScrollViewer { Content = new Border { Height = 1200 } };
        var state = new BottomAppBarState((float)(-80 * density));
        var scope = new AppBarScrollScope { Child = viewer, BottomScrollBehavior = BottomAppBarDefaults.ExitAlwaysScrollBehavior(state, density: (float)density) };
        using var clock = new MotionTestClockScope(); var window = Show(scope); window.SetRenderScaling(density); window.UpdateLayout();
        using var touch = new TouchDevice();
        void Send(ulong time, RawPointerEventType type, double y) => window.PlatformImpl!.Input!(new RawTouchEventArgs(touch, time, (IInputRoot)window.PlatformImpl!.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!, type, new Point(60, y), RawInputModifiers.None, 1));
        try
        {
            Send(100, RawPointerEventType.TouchBegin, 160);
            Send(116, RawPointerEventType.TouchUpdate, 130); Send(132, RawPointerEventType.TouchUpdate, 95);
            Assert.True(viewer.Offset.Y > 0); Assert.True(state.ContentOffset < 0);
            var presenter = Assert.Single(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            Send(148, RawPointerEventType.TouchEnd, 95);
            Assert.NotNull(presenter.LastFling);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
            var first = viewer.Offset.Y; Assert.True(first > 40);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); Assert.True(viewer.Offset.Y > first);
            Send(164, RawPointerEventType.TouchBegin, 100); var interrupted = viewer.Offset.Y;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await presenter.LastFling!);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(100)); window.UpdateLayout(); Assert.Equal(interrupted, viewer.Offset.Y);
            Send(180, RawPointerEventType.TouchCancel, 100);
            Assert.Null(MotionTestClockScope.Error(viewer));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitPresenter_CustomTemplate_KeepsScrollProtocol()
    {
        var template = new FuncControlTemplate<ScrollViewer>((owner, scope) =>
        {
            var presenter = new AppBarScrollContentPresenter { Name = "PART_ContentPresenter" };
            presenter.Bind(TemplatedControl.BackgroundProperty, owner.GetObservable(TemplatedControl.BackgroundProperty));
            scope.Register(presenter.Name, presenter); return presenter;
        });
        var state = new TopAppBarState(-64, -64);
        var viewer = new ScrollViewer { Template = template, Content = new Border { Height = 800 } };
        var scope = new AppBarScrollScope { Child = viewer, TopScrollBehavior = TopAppBarDefaults.EnterAlwaysScrollBehavior(state) };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        try
        {
            using (window.CaptureRenderedFrame()) { }
            Assert.Same(template, viewer.Template); Assert.Single(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            window.MouseWheel(new Point(40, 40), new Vector(0, -1)); Assert.Equal(50, viewer.Offset.Y); Assert.Equal(-50, state.ContentOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeKeyboard_BringIntoView_AndScrollBarRemainFunctional()
    {
        var last = new Border { Height = 20, Background = Avalonia.Media.Brushes.Gray };
        var viewer = new ScrollViewer { Focusable = true, Content = new StackPanel { Children = { new Border { Height = 1000 }, last } } };
        var scope = new AppBarScrollScope { Child = viewer };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        try
        {
            using (window.CaptureRenderedFrame()) { }
            Assert.True(viewer.Focus()); window.KeyPress(Key.PageDown, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.True(viewer.Offset.Y > 0); viewer.Offset = default; last.BringIntoView(); window.UpdateLayout();
            Assert.True(viewer.Offset.Y > 700);
            var scrollbar = viewer.GetVisualDescendants().OfType<ScrollBar>().Single(bar => bar.Orientation == Avalonia.Layout.Orientation.Vertical);
            scrollbar.Value = 100; window.UpdateLayout(); Assert.Equal(100, viewer.Offset.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void MovingViewerBetweenScopes_ReleasesOldOwnership_AndPreservesThemeTemplate()
    {
        var viewer = new ScrollViewer { Content = new Border { Height = 800 } };
        var a = new TopAppBarState(-64); var b = new TopAppBarState(-64);
        var first = new AppBarScrollScope { Child = viewer, TopScrollBehavior = TopAppBarDefaults.EnterAlwaysScrollBehavior(a) };
        var second = new AppBarScrollScope { TopScrollBehavior = TopAppBarDefaults.EnterAlwaysScrollBehavior(b) };
        using var clock = new MotionTestClockScope(); var window = Show(first);
        try
        {
            window.MouseWheel(new Point(30, 30), new Vector(0, -1)); Assert.Equal(-50, a.HeightOffset);
            first.Child = null; second.Child = viewer; window.Content = second; window.UpdateLayout(); window.UpdateLayout();
            window.MouseWheel(new Point(30, 30), new Vector(0, -1)); Assert.Equal(-50, b.HeightOffset); Assert.Equal(-50, a.HeightOffset);
            var custom = new FuncControlTemplate<ScrollViewer>((_, _) => new Border());
            var theme = new Avalonia.Styling.ControlTheme(typeof(ScrollViewer)) { Setters = { new Avalonia.Styling.Setter(TemplatedControl.TemplateProperty, custom) } };
            viewer.Theme = theme; window.UpdateLayout(); Assert.Same(custom, viewer.Template);
            Assert.False(AppBarScrollScope.GetUseStandardPresenter(viewer));
            viewer.ClearValue(StyledElement.ThemeProperty); window.UpdateLayout(); window.UpdateLayout();
            Assert.Single(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            window.Content = null; window.UpdateLayout(); Assert.False(AppBarScrollScope.GetUseStandardPresenter(viewer));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task RawTouch_ContentBoundary_PassesActualResidualVelocityToBar_ThenSettles()
    {
        var viewer = new ScrollViewer { Content = new Border { Height = 270 } };
        var state = new BottomAppBarState(-800);
        var recording = new RecordingBottom( BottomAppBarDefaults.ExitAlwaysScrollBehavior(state, density: 1));
        var scope = new AppBarScrollScope { Child = viewer, BottomScrollBehavior = recording };
        using var clock = new MotionTestClockScope(); var window = Show(scope); using var touch = new TouchDevice();
        void Send(ulong time, RawPointerEventType type, double y) => window.PlatformImpl!.Input!(new RawTouchEventArgs(touch, time,
            (IInputRoot)window.PlatformImpl.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!, type, new Point(60, y), RawInputModifiers.None, 1));
        try
        {
            using (window.CaptureRenderedFrame()) { }
            Send(100, RawPointerEventType.TouchBegin, 160); Send(116, RawPointerEventType.TouchUpdate, 145);
            Send(132, RawPointerEventType.TouchUpdate, 128); Send(148, RawPointerEventType.TouchEnd, 128);
            var presenter = Assert.Single(viewer.GetVisualDescendants().OfType<AppBarScrollContentPresenter>());
            clock.SendFrame(TimeSpan.Zero);
            for (var i = 0; i < 120 && presenter.LastFling is { IsCompleted: false }; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.True(recording.Available is not null, $"offset={viewer.Offset.Y}, state={state.HeightOffset}, fling={presenter.LastFling?.Status}, waiters={clock.PendingFrameCount}"); Assert.True(recording.Available!.Value.Y < -1);
            Assert.Equal(30, viewer.Offset.Y); Assert.Equal(0, state.HeightOffset);
            Assert.True(presenter.LastFling!.IsCompletedSuccessfully); Assert.Null(MotionTestClockScope.Error(viewer));
            await presenter.LastFling;
        }
        finally { window.Close(); }
    }

    private sealed class RecordingBottom : BottomAppBarScrollBehavior
    {
        private readonly BottomAppBarScrollBehavior inner;
        public RecordingBottom(BottomAppBarScrollBehavior behavior)
        { inner = behavior; NestedScrollConnection = new RecordingConnection(inner, available => Available = available); }
        public Vector? Available;
        public override BottomAppBarState State => inner.State;
        public override bool IsPinned => inner.IsPinned;
        public override IAnimationSpec<float>? SnapAnimationSpec => inner.SnapAnimationSpec;
        public override IDecayAnimationSpec<float>? FlingAnimationSpec => inner.FlingAnimationSpec;
        public override AppBarNestedScrollConnection NestedScrollConnection { get; }
        private sealed class RecordingConnection(BottomAppBarScrollBehavior behavior, Action<Vector> record) : AppBarNestedScrollConnection
        {
            public override Vector OnPostScroll(Vector consumed, Vector available, AppBarScrollSource source) => behavior.NestedScrollConnection.OnPostScroll(consumed, available, source);
            public override async ValueTask<Vector> OnPostFlingAsync(Vector consumed, Vector available, IAnimationFrameClock clock, CancellationToken cancellationToken = default)
            { record(available); return await behavior.NestedScrollConnection.OnPostFlingAsync(consumed, available, clock, cancellationToken); }
        }
    }

    [AvaloniaFact]
    public async Task RawTouch_BarDrag_Snaps_AndNewPressCancelsSnap()
    {
        var state = new TopAppBarState();
        var behavior = TopAppBarDefaults.ExitUntilCollapsedScrollBehavior(state);
        var bar = new MediumTopAppBar { ScrollBehavior = behavior };
        var scope = new AppBarScrollScope { TopScrollBehavior = behavior, Child = new Scaffold { TopBar = bar } };
        using var clock = new MotionTestClockScope(); var window = Show(scope);
        using var touch = new TouchDevice();
        void Send(ulong time, RawPointerEventType type, double y) => window.PlatformImpl!.Input!(new RawTouchEventArgs(touch, time, (IInputRoot)window.PlatformImpl!.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!, type, new Point(200, y), RawInputModifiers.None, 1));
        try
        {
            using (window.CaptureRenderedFrame()) { } Send(100, RawPointerEventType.TouchBegin, 90); Send(116, RawPointerEventType.TouchUpdate, 58);
            Assert.True(state.HeightOffset == -14, $"offset={state.HeightOffset}; limit={state.HeightOffsetLimit}; bounds={bar.Bounds}; hit={window.InputHitTest(new Point(200, 90))}"); // Compose draggable's 18dp touch slop
            Send(200, RawPointerEventType.TouchEnd, 58); // pause removes release velocity; snap remains
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); Assert.True(state.HeightOffset > -14);
            Send(216, RawPointerEventType.TouchBegin, 50); var interrupted = state.HeightOffset;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(100)); window.UpdateLayout(); Assert.Equal(interrupted, state.HeightOffset);
            Send(232, RawPointerEventType.TouchCancel, 50);
            Assert.Null(MotionTestClockScope.Error(bar));
            await Task.CompletedTask;
        }
        finally { window.Close(); }
    }
}
