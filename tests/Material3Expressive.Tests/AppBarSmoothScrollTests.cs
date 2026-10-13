using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.TestSupport;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;
using SmoothScroll.Avalonia.Controls;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarSmoothScrollTests
{
    private static readonly FieldInfo ScopeConnectionField = typeof(AppBarScrollScope)
        .GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(AppBarScrollScope).FullName, "_connection");

    private static readonly FieldInfo ConnectionViewerField = typeof(AppBarSmoothScrollConnection)
        .GetField("_viewer", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(AppBarSmoothScrollConnection).FullName, "_viewer");

    [AvaloniaTheory]
    [InlineData("Pinned", true)]
    [InlineData("EnterAlways", true)]
    [InlineData("ExitUntilCollapsed", true)]
    [InlineData("ExitUntilCollapsed", false)]
    public void NestedWheel_KeepsInnerIndependent_AndOnlyBubbledInputMovesAppBars(string behavior, bool chaining)
    {
        using var clock = new MotionTestClockScope();
        var kind = Enum.Parse<TopAppBarScrollMode>(behavior);
        var scene = Show(1200, kind);
        var inner = new ScrollViewer
        {
            Height = 100, IsScrollInertiaEnabled = false, IsScrollChainingEnabled = chaining,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            Content = new Border { Height = 600, Background = Brushes.Cyan },
        };
        try
        {
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            scene.Viewer.Content = new StackPanel
            {
                Children = { inner, new Border { Height = 1200, Background = Brushes.LightGray } },
            };
            Render(scene.Window);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.IsType<ScrollViewerPresenter>(inner.Presenter);
            Assert.Null(GetSmoothConnection(scene.Scope, inner));

            Wheel(scene, -1);
            Assert.True(inner.Offset.Y > 0);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Top.HeightOffset);
            Assert.Equal(0, scene.Top.ContentOffset);
            Assert.Equal(0, scene.Bottom.HeightOffset);

            var maximum = inner.ScrollBarMaximum.Y;
            inner.Offset = new Vector(0, maximum - 10); Render(scene.Window);
            Wheel(scene, -1);
            Assert.Equal(maximum, inner.Offset.Y);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Top.ContentOffset);
            Wheel(scene, -1);
            Assert.Equal(chaining, scene.Viewer.Offset.Y > 0);
            Assert.Equal(chaining && kind != TopAppBarScrollMode.Pinned ? scene.Top.HeightOffsetLimit : 0, scene.Top.HeightOffset);
            Assert.Equal(chaining, scene.Bottom.HeightOffset < 0);
            Assert.Equal(100, inner.Viewport.Height);
            Assert.Equal(scene.TopBar.Bounds.Height, scene.Screen.ContentPadding.Top);
            Assert.Equal(scene.BottomBar.Bounds.Height, scene.Screen.ContentPadding.Bottom);
            Assert.Equal(scene.Screen.Bounds.Height - scene.Screen.ContentPadding.Top - scene.Screen.ContentPadding.Bottom,
                scene.Viewer.Viewport.Height);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));

            scene.Body.Height = 300; Render(scene.Window);
            Assert.False(connection.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.IsType<ScrollViewerPresenter>(inner.Presenter);
            var topOffset = scene.Top.HeightOffset;
            var topContentOffset = scene.Top.ContentOffset;
            scene.Viewer.Offset = default;
            inner.Offset = default; Render(scene.Window);
            Wheel(scene, -1);
            Assert.True(inner.Offset.Y > 0);
            Assert.Equal(topOffset, scene.Top.HeightOffset);
            Assert.Equal(topContentOffset, scene.Top.ContentOffset);
            scene.Body.Bind(Border.HeightProperty, new Avalonia.Data.Binding("Bounds.Height") { Source = scene.Screen });
            Render(scene.Window);
            connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.True(connection.IsConnected);
            Assert.IsType<ScrollViewerPresenter>(inner.Presenter);

            scene.Viewer.Content = new Border { Height = 1200 };
            Render(scene.Window);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmbeddedNativeControl_KeepsMainCompositionAndItsOwnSmoothPresenter(bool textBox)
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            scene.Window.Styles.Insert(0, new Avalonia.Themes.Fluent.FluentTheme());
            Control embedded = textBox
                ? new TextBox { Height = 100, AcceptsReturn = true, Text = string.Join('\n', Enumerable.Range(1, 50)) }
                : new ListBox { Height = 100, ItemsSource = Enumerable.Range(1, 80).ToArray() };
            scene.Viewer.Content = new StackPanel { Children = { embedded, new Border { Height = 1200 } } };
            Render(scene.Window);
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var inner = Assert.Single(embedded.GetVisualDescendants().OfType<ScrollViewer>());
            inner.IsScrollInertiaEnabled = false;
            Assert.IsType<ScrollViewerPresenter>(inner.Presenter);
            Assert.Null(GetSmoothConnection(scene.Scope, inner));
            var point = inner.TranslatePoint(new Point(30, 30), scene.Window)!.Value;
            scene.Window.MouseWheel(point, new Vector(0, -1)); Render(scene.Window);
            Assert.True(inner.Offset.Y > 0);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Top.HeightOffset);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public async Task NativeWheel_CancelsPendingUiFling_AndKeepsSmoothConnection()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        CancellationToken uiToken = default;
        var fling = scene.Scope.DispatchFlingAsync(new Vector(0, -500), async (_, token) =>
        {
            uiToken = token;
            await Task.Delay(Timeout.Infinite, token);
            return default;
        }, scene.Window.MotionRoot);
        try
        {
            Assert.False(fling.IsCompleted);
            Assert.True(uiToken.CanBeCanceled);
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Wheel(scene, -1);
            Assert.True(uiToken.IsCancellationRequested);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fling);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.True(scene.Viewer.Offset.Y > 0);
            var bodyOffset = scene.Viewer.Offset.Y;
            Wheel(scene, -1);
            Assert.True(scene.Viewer.Offset.Y > bodyOffset);
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Scope.Interrupt(); scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void PinnedBar_ReverseIdleRetainsContentOverlap_UntilBodyReturnsToStart()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200, TopAppBarScrollMode.Pinned);
        try
        {
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var tracker = connection.Tracker!;
            tracker.ApplyVerticalScrollDelta(100); Render(scene.Window);
            Assert.Equal(100, scene.Viewer.Offset.Y);
            Assert.Equal(-100, scene.Top.ContentOffset);
            tracker.ApplyVerticalScrollDelta(-10);
            tracker.CompleteVerticalScroll(); Render(scene.Window);
            Assert.Equal(90, scene.Viewer.Offset.Y);
            Assert.Equal(-90, scene.Top.ContentOffset);
            Assert.Equal(-90, connection.LastSnapshot.TopContentOffset);
            tracker.ApplyVerticalScrollDelta(-1000);
            tracker.CompleteVerticalScroll(); Render(scene.Window);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Top.ContentOffset);
            Assert.Equal(0, connection.LastSnapshot.TopContentOffset);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("Pinned")]
    [InlineData("EnterAlways")]
    [InlineData("ExitUntilCollapsed")]
    public void NativeWheel_AllSupportedBehaviors_ReachBodyBoundaryAndReverse(string behavior)
    {
        using var clock = new MotionTestClockScope();
        var kind = Enum.Parse<TopAppBarScrollMode>(behavior);
        var scene = Show(1200, kind);
        try
        {
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Wheel(scene, -1);
            Assert.Equal(kind == TopAppBarScrollMode.Pinned ? 0 : scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
            Assert.True(scene.Viewer.Offset.Y > 0);
            Assert.True(scene.Bottom.HeightOffset < 0);
            Wheel(scene, -100);
            Assert.Equal(scene.Viewer.Extent.Height - scene.Viewer.Viewport.Height, scene.Viewer.Offset.Y);
            Assert.Equal(scene.Bottom.HeightOffsetLimit, scene.Bottom.HeightOffset);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));
            var endOffset = scene.Viewer.Offset.Y;
            Wheel(scene, 1);
            Assert.Equal(kind == TopAppBarScrollMode.ExitUntilCollapsed ? scene.Top.HeightOffsetLimit : 0, scene.Top.HeightOffset);
            Assert.Equal(0, scene.Bottom.HeightOffset);
            Assert.True(scene.Viewer.Offset.Y < endOffset);
            Wheel(scene, 100);
            Assert.Equal(0, scene.Top.HeightOffset);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(0, connection.LastSnapshot.BodyOffset);
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void NativeSnapPoints_ReleaseConnectionWithoutReplacingPresenter_AndClearingReconnects()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = scene.Viewer.Template;
            var presenter = scene.Viewer.Presenter;
            scene.Viewer.VerticalSnapPointsType = SnapPointsType.Mandatory;
            Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Equal(SnapPointsType.Mandatory, scene.Viewer.VerticalSnapPointsType);
            Assert.Same(template, scene.Viewer.Template);
            Assert.Same(presenter, scene.Viewer.Presenter);
            scene.Viewer.VerticalSnapPointsType = SnapPointsType.None;
            Render(scene.Window);
            var second = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.True(second.IsConnected);
            Assert.NotSame(first, second);
            Wheel(scene, -1);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
            Assert.True(scene.Viewer.Offset.Y > 0);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void OuterAppBarScope_DoesNotTakeOwnershipOfScaffoldViewport()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = scene.Viewer.Template;
            scene.Window.Content = null;
            var outer = new AppBarScrollScope { Child = scene.Screen };
            scene.Window.Content = outer;
            Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
            Assert.Null(GetSmoothConnection(outer, scene.Viewer));
            Assert.Same(template, scene.Viewer.Template);
            Assert.IsType<ScrollViewerPresenter>(scene.Viewer.Presenter);
            Wheel(scene, -1);
            Assert.True(scene.Viewer.Offset.Y > 0);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
            scene.Window.Content = null;
            outer.Child = null;
            scene.Window.Content = scene.Screen;
            Render(scene.Window);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void BlankBarRegion_NativeTouchDrag_PreservesFractionalStateAndRange()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        using var touch = new TouchDevice();
        void Send(ulong time, RawPointerEventType type, double y) => scene.Window.PlatformImpl!.Input!(
            new RawTouchEventArgs(touch, time,
                (IInputRoot)scene.Window.PlatformImpl!.GetType().GetProperty("InputRoot")!.GetValue(scene.Window.PlatformImpl)!,
                type, new Point(250, y), RawInputModifiers.None, 1));
        try
        {
            var hit = scene.Window.InputHitTest(new Point(250, 90));
            Assert.NotNull(hit);
            Assert.Contains(scene.TopBar, ((Control)hit!).GetVisualAncestors());
            Send(100, RawPointerEventType.TouchBegin, 90);
            Send(116, RawPointerEventType.TouchUpdate, 57.875);
            Render(scene.Window);
            Assert.Equal(-14.125f, scene.Top.HeightOffset);
            Assert.Equal(-48, scene.Top.HeightOffsetLimit);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Send(240, RawPointerEventType.TouchEnd, 57.875);
            Render(scene.Window);
            Assert.Equal(-48, scene.Top.HeightOffsetLimit);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void NativeWheel_ConnectsRealBars_AndRebasesPaddingViewportAndRange()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.True(connection.IsConnected);
            Assert.IsType<ScrollViewerPresenter>(scene.Viewer.Presenter);
            Assert.Equal(new Thickness(0, 112, 0, 80), scene.Screen.ContentPadding);
            Assert.Equal(208, scene.Viewer.Viewport.Height);
            Assert.Equal(1200, scene.Viewer.Extent.Height);

            Wheel(scene, -1);
            Assert.Equal(-48, scene.Top.HeightOffset);
            Assert.True(scene.Viewer.Offset.Y > 0);
            Assert.True(scene.Bottom.HeightOffset < 0);
            Assert.Equal(scene.TopBar.Bounds.Height, scene.Screen.ContentPadding.Top);
            Assert.Equal(scene.BottomBar.Bounds.Height, scene.Screen.ContentPadding.Bottom);
            Assert.Equal(400 - scene.Screen.ContentPadding.Top - scene.Screen.ContentPadding.Bottom, scene.Viewer.Viewport.Height);
            Assert.Equal(scene.Screen.ContentPadding, MaterialWindowInsets.GetConsumePadding(scene.Body));
            Assert.Equal(scene.Viewer.Viewport.Height, connection.LastSnapshot.Viewport);
            Assert.Equal(scene.Viewer.Offset.Y, connection.LastSnapshot.BodyOffset);
            Assert.Equal(scene.Viewer.Extent.Height - scene.Viewer.Viewport.Height, scene.Viewer.ScrollBarMaximum.Y);

            Wheel(scene, 1);
            Assert.Equal(0, scene.Top.HeightOffset);
            Assert.Equal(0, scene.Bottom.HeightOffset);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(208, scene.Viewer.Viewport.Height);
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void DynamicBarHeightAndWindowResize_PreserveBodyPosition_AndRecomputeLiveRange()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            Wheel(scene, -1);
            var collapsed = scene.Top.HeightOffset;
            var bodyOffset = scene.Viewer.Offset.Y;
            scene.TopBar.ExpandedHeight = 160;
            Render(scene.Window);
            Assert.Equal(-96, scene.Top.HeightOffsetLimit);
            Assert.Equal(collapsed, scene.Top.HeightOffset);
            Assert.Equal(bodyOffset, scene.Viewer.Offset.Y);
            Assert.Equal(400 - scene.Screen.ContentPadding.Top - scene.Screen.ContentPadding.Bottom, scene.Viewer.Viewport.Height);
            scene.Window.Height = 500;
            Render(scene.Window);
            Assert.Equal(bodyOffset, scene.Viewer.Offset.Y);
            Assert.Equal(500 - scene.Screen.ContentPadding.Top - scene.Screen.ContentPadding.Bottom, scene.Viewer.Viewport.Height);
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Equal(scene.Viewer.Viewport.Height, connection.LastSnapshot.Viewport);
            connection.Tracker!.ApplyVerticalScrollDelta(10000);
            Render(scene.Window);
            Assert.Equal(scene.Viewer.Extent.Height - scene.Viewer.Viewport.Height, scene.Viewer.Offset.Y);
            Assert.Equal(scene.Viewer.Offset.Y, connection.LastSnapshot.BodyOffset);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ShortBodyAtBoundary_StillDeliversNativeWheelToCollapsingTopBar()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(100);
        try
        {
            Assert.NotNull(GetSmoothConnection(scene.Scope, scene.Viewer));
            Wheel(scene, -1);
            Assert.Equal(-48, scene.Top.HeightOffset);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Bottom.HeightOffset);
            Assert.Equal(256, scene.Viewer.Viewport.Height);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ScopeDetachAndReattach_ReleasesOldConnection_AndRestoresRealChrome()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            Wheel(scene, -1);
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var chrome = scene.TopBar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
            Assert.True(chrome.CompositionEnabled);
            scene.Window.Content = null;
            Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.False(chrome.CompositionEnabled);
            Assert.Equal(2, chrome.Children.Count);
            scene.Window.Content = scene.Screen;
            Render(scene.Window);
            var second = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.NotSame(first, second);
            Assert.True(second.IsConnected);
            Assert.True(chrome.CompositionEnabled);
            Wheel(scene, 1);
            Assert.Equal(0, scene.Top.HeightOffset);
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void BehaviorReplacement_Reconnects_AndNativeWheelTargetsNewState()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            Wheel(scene, -1);
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var previousOffset = scene.Top.HeightOffset;
            var newState = new TopAppBarState();
            var behavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.EnterAlways) { State = newState, SnapAnimationSpec = null, FlingAnimationSpec = null };
            scene.TopBar.ScrollBehavior = behavior;
            Render(scene.Window);
            var second = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.NotSame(first, second);
            Assert.False(first.IsConnected);
            Wheel(scene, -1);
            Assert.Equal(newState.HeightOffsetLimit, newState.HeightOffset);
            Assert.Equal(previousOffset, scene.Top.HeightOffset);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitCustomTemplate_ReleasesConnection_AndPreservesTemplate()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = new FuncControlTemplate<ScrollViewer>((owner, _) =>
                new Avalonia.Controls.Presenters.ScrollContentPresenter { Name = "PART_ContentPresenter", Content = owner.Content });
            scene.Viewer.Template = template;
            Render(scene.Window);
            Assert.Same(template, scene.Viewer.Template);
            Assert.False(connection.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.IsType<Avalonia.Controls.Presenters.ScrollContentPresenter>(scene.Viewer.Presenter);
            Assert.False(scene.TopBar.GetVisualDescendants().OfType<AppBarTopChrome>().Single().CompositionEnabled);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void FixedBodyViewport_KeepsSmoothPresenter_AndRestoringFillLayoutReconnects()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = scene.Viewer.Template;
            var presenter = scene.Viewer.Presenter;
            scene.Body.Height = 300; Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Same(template, scene.Viewer.Template);
            Assert.Same(presenter, scene.Viewer.Presenter);
            Wheel(scene, -1);
            Assert.True(scene.Viewer.Offset.Y > 0);
            Assert.Equal(0, scene.Top.HeightOffset);
            scene.Body.Bind(Border.HeightProperty, new Avalonia.Data.Binding("Bounds.Height") { Source = scene.Screen });
            Render(scene.Window);
            var restored = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.True(restored.IsConnected);
            Assert.NotSame(first, restored);
            Assert.IsType<ScrollViewerPresenter>(scene.Viewer.Presenter);
            Wheel(scene, -1);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
            Assert.True(scene.Viewer.Offset.Y > 0);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ReplacingTopBarTemplate_ReleasesOldChrome_AndReconnectsNewRealChrome()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var originalTemplate = scene.TopBar.Template;
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var oldChrome = scene.TopBar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
            scene.TopBar.Template = new FuncControlTemplate<TopAppBar>((_, _) => new Border { Height = 90, Background = Brushes.Cyan });
            Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.False(oldChrome.CompositionEnabled);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.IsType<ScrollViewerPresenter>(scene.Viewer.Presenter);
            scene.TopBar.Template = originalTemplate; Render(scene.Window);
            var second = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var newChrome = scene.TopBar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
            Assert.NotSame(oldChrome, newChrome);
            Assert.True(newChrome.CompositionEnabled);
            Assert.True(second.IsConnected);
            Wheel(scene, -1);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomSmoothTemplateOrTheme_ConnectsByPresenterCapability_AndClearingReconnects(bool themed)
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var originalTemplate = scene.Viewer.Template;
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = new FuncControlTemplate<ScrollViewer>((owner, names) =>
            {
                var presenter = new ScrollViewerPresenter { Name = "PART_ContentPresenter" };
                names.Register(presenter.Name, presenter);
                presenter.Bind(ContentControl.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
                presenter.Bind(TemplatedControl.BackgroundProperty, new TemplateBinding(TemplatedControl.BackgroundProperty));
                presenter.Bind(ScrollViewer.IsScrollInertiaEnabledProperty, new TemplateBinding(ScrollViewer.IsScrollInertiaEnabledProperty));
                return new Border { Child = presenter, ClipToBounds = true };
            });
            if (themed)
                scene.Viewer.Theme = new ControlTheme(typeof(ScrollViewer))
                {
                    BasedOn = (ControlTheme)scene.Window.FindResource("MaterialScrollViewerTheme")!,
                    Setters = { new Setter(TemplatedControl.TemplateProperty, template) },
                };
            else scene.Viewer.Template = template;
            Render(scene.Window);
            Assert.Same(template, scene.Viewer.Template);
            Assert.False(first.IsConnected);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
            Wheel(scene, -1);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
            Assert.True(scene.Viewer.Offset.Y > 0);
            scene.Viewer.ClearValue(themed ? StyledElement.ThemeProperty : TemplatedControl.TemplateProperty);
            Render(scene.Window);
            Assert.Same(originalTemplate, scene.Viewer.Template);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void LateTemplateStyle_ReleasesConnection_AndRemovingStyleReconnectsWithoutTemplateWrites()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var originalTemplate = scene.Viewer.Template;
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = new FuncControlTemplate<ScrollViewer>((owner, names) =>
            {
                var presenter = new Avalonia.Controls.Presenters.ScrollContentPresenter { Name = "PART_ContentPresenter" };
                names.Register(presenter.Name, presenter);
                presenter.Bind(ContentControl.ContentProperty, new TemplateBinding(ContentControl.ContentProperty));
                return presenter;
            });
            var style = new Style(selector => selector.OfType<ScrollViewer>());
            style.Setters.Add(new Setter(TemplatedControl.TemplateProperty, template));
            scene.Window.Styles.Add(style);
            Render(scene.Window);
            Assert.Same(template, scene.Viewer.Template);
            Assert.False(first.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            scene.Window.Styles.Remove(style);
            Render(scene.Window);
            Assert.Same(originalTemplate, scene.Viewer.Template);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ReplacingMainViewer_ReleasesOldConnection_AndConnectsNewBody()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var replacement = new ScrollViewer
            {
                Content = new Border { Height = 1200 }, IsScrollInertiaEnabled = false,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            };
            scene.Body.Child = replacement;
            Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, replacement)).IsConnected);
            var point = replacement.TranslatePoint(new Point(40, 40), scene.Window)!.Value;
            scene.Window.MouseWheel(point, new Vector(0, -1)); Render(scene.Window);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
            Assert.True(replacement.Offset.Y > 0);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void MovingScreenBetweenScopes_ReleasesOldOwner_AndPreservesTemplate()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = scene.Viewer.Template;
            scene.Window.Content = null;
            var newState = new TopAppBarState();
            var behavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.EnterAlways) { State = newState, SnapAnimationSpec = null, FlingAnimationSpec = null };
            scene.TopBar.ScrollBehavior = behavior;
            var next = new AppBarScrollScope
            {
                Child = scene.Screen, TopScrollBehavior = behavior, BottomScrollBehavior = scene.BottomBar.ScrollBehavior,
            };
            scene.Window.Content = next; Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.Null(GetSmoothConnection(next, scene.Viewer));
            Assert.Same(template, scene.Viewer.Template);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
            Wheel(scene, -1);
            Assert.Equal(newState.HeightOffsetLimit, newState.HeightOffset);
            Assert.Equal(0, scene.Top.HeightOffset);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void MultipleMainViewers_ReleaseConnection_AndRestoringSingleBodyReconnects()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var second = new ScrollViewer { Content = new Border { Height = 1200 } };
            var template = scene.Viewer.Template;
            scene.Body.Child = null;
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            grid.Children.Add(scene.Viewer); grid.Children.Add(second); Grid.SetColumn(second, 1);
            scene.Body.Child = grid; Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Null(GetSmoothConnection(scene.Scope, second));
            Assert.Same(template, scene.Viewer.Template);
            Assert.IsType<ScrollViewerPresenter>(second.Presenter);
            grid.Children.Clear(); scene.Body.Child = scene.Viewer; Render(scene.Window);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ScrollViewerInScaffoldOverlay_DoesNotTakeMainBodyOwnership()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var overlay = new ScrollViewer { Width = 80, Height = 40, Content = new Border { Height = 200 } };
            scene.Screen.SnackbarHost = overlay; Render(scene.Window);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Null(GetSmoothConnection(scene.Scope, overlay));
            Assert.IsType<ScrollViewerPresenter>(overlay.Presenter);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void LogicalMainContent_KeepsSmoothPresenterAndUnits_AndPhysicalContentReconnects()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = scene.Viewer.Template;
            var presenter = Assert.IsType<ScrollViewerPresenter>(scene.Viewer.Presenter);
            var logical = new LogicalBody();
            scene.Viewer.Content = logical; Render(scene.Window);
            Assert.False(first.IsConnected);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Same(template, scene.Viewer.Template);
            Assert.Same(presenter, scene.Viewer.Presenter);
            Assert.False(presenter.IsPhysicalScrollActive);
            scene.Viewer.LineDown();
            Assert.Equal(2, logical.Offset.Y);
            Assert.Equal(logical.Offset, scene.Viewer.Offset);
            Assert.Equal(0, scene.Top.HeightOffset);
            scene.Viewer.Content = new Border { Height = 1200 }; Render(scene.Window);
            Assert.True(presenter.IsPhysicalScrollActive);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void NativeKeyboardBringIntoViewAndScrollBar_KeepWorkingWithMainConnection()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var last = new Border { Height = 20 };
            scene.Viewer.Content = new StackPanel { Children = { new Border { Height = 1000 }, last } };
            scene.Viewer.Focusable = true; Render(scene.Window);
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.True(scene.Viewer.Focus());
            scene.Window.KeyPress(Key.PageDown, RawInputModifiers.None, PhysicalKey.None, null);
            Render(scene.Window);
            Assert.True(scene.Viewer.Offset.Y > 0);
            scene.Viewer.Offset = default;
            Assert.IsType<ScrollViewerPresenter>(scene.Viewer.Presenter).IsBringIntoViewAnimationEnabled = false;
            last.BringIntoView(); Render(scene.Window);
            Assert.True(scene.Viewer.Offset.Y > 700);
            var scrollbar = scene.Viewer.GetVisualDescendants().OfType<ScrollBar>()
                .Single(bar => bar.Orientation == Avalonia.Layout.Orientation.Vertical);
            scrollbar.Value = 100; Render(scene.Window);
            Assert.Equal(100, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Top.HeightOffset);
            Assert.Equal(0, scene.Top.ContentOffset);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void RawTouch_MainSmoothDragAndInertia_InterruptOnNewPress(double density)
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(5000);
        scene.Window.SetRenderScaling(density);
        scene.Viewer.IsScrollInertiaEnabled = true;
        Render(scene.Window);
        using var touch = new TouchDevice();
        void Send(ulong time, RawPointerEventType type, double y) => scene.Window.PlatformImpl!.Input!(
            new RawTouchEventArgs(touch, time,
                (IInputRoot)scene.Window.PlatformImpl!.GetType().GetProperty("InputRoot")!.GetValue(scene.Window.PlatformImpl)!,
                type, new Point(60, y), RawInputModifiers.None, 1));
        try
        {
            var start = scene.Viewer.TranslatePoint(new Point(60, 150), scene.Window)!.Value.Y;
            Send(100, RawPointerEventType.TouchBegin, start);
            Send(116, RawPointerEventType.TouchUpdate, start - 35);
            Send(132, RawPointerEventType.TouchUpdate, start - 70);
            Send(148, RawPointerEventType.TouchUpdate, start - 100);
            Render(scene.Window);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
            Assert.True(scene.Viewer.Offset.Y > 0);
            Assert.True(scene.Bottom.HeightOffset < 0);
            var released = scene.Viewer.Offset.Y;
            Send(164, RawPointerEventType.TouchEnd, start - 100);
            Render(scene.Window);
            Assert.True(scene.Viewer.Offset.Y > released);
            var beforeConfigurationChange = scene.Viewer.Offset.Y;
            scene.TopBar.ScrollBehavior!.IsScrollingContentAtStart = false;
            scene.TopBar.ScrollBehavior.CanScroll = false;
            scene.BottomBar.ScrollBehavior!.CanScroll = false;
            Render(scene.Window);
            Assert.True(scene.Viewer.Offset.Y > beforeConfigurationChange);
            var press = scene.Viewer.TranslatePoint(new Point(60, 50), scene.Window)!.Value.Y;
            Send(180, RawPointerEventType.TouchBegin, press); Render(scene.Window);
            var stopped = scene.Viewer.Offset.Y;
            Render(scene.Window);
            Assert.Equal(stopped, scene.Viewer.Offset.Y);
            Send(196, RawPointerEventType.TouchCancel, press);
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void CompiledXamlScaffold_ConnectsTemplateAndDirectBarsWithoutExplicitScope()
    {
        using var clock = new MotionTestClockScope();
        var screen = (Scaffold)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(new Uri(
            "avares://Material3Expressive.Tests/Fixtures/ScrollableAppBars.axaml"));
        var window = new MotionTestWindow { Width = 300, Height = 400, WindowDecorations = WindowDecorations.None };
        window.Styles.Add(new MaterialTheme());
        window.Content = screen;
        window.Show();
        try
        {
            Render(window);
            var scope = screen.GetVisualDescendants().OfType<AppBarScrollScope>().Single(s => s.Host == screen);
            var viewer = screen.FindControl<ScrollViewer>("Body")!;
            var top = Assert.IsType<MediumTopAppBar>(screen.PresentedTopBar).ScrollBehavior!;
            var bottom = Assert.IsType<BottomAppBar>(screen.PresentedBottomBar).ScrollBehavior!;
            Assert.Same(top, scope.TopScrollBehavior);
            Assert.Same(bottom, scope.BottomScrollBehavior);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scope, viewer)).IsConnected);
            window.MouseWheel(viewer.TranslatePoint(new Point(40, 40), window)!.Value, new Vector(0, -2));
            Render(window);
            Assert.Equal(top.State.HeightOffsetLimit, top.State.HeightOffset);
            Assert.True(bottom.State.HeightOffset < 0);
            Assert.True(viewer.Offset.Y > 0);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ScaffoldTemplateReplacement_ReleasesOldScopeAndReconnectsNewScope()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var template = scene.Screen.Template;
            scene.Screen.Template = null;
            Render(scene.Window);
            Assert.False(first.IsConnected);
            scene.Screen.Template = template;
            Render(scene.Window);
            var scope = scene.Screen.GetVisualDescendants().OfType<AppBarScrollScope>().Single(s => s.Host == scene.Screen);
            Assert.NotSame(scene.Scope, scope);
            Assert.Null(scene.Scope.Host);
            Assert.Null(scene.Scope.TopScrollBehavior);
            Assert.Null(scene.Scope.BottomScrollBehavior);
            Assert.Null(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scope, scene.Viewer)).IsConnected);
            Wheel(scene, -1);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void StateReplacement_ReleasesOldSubscription_WithoutReplacingBehavior()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            Wheel(scene, -1);
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            var behavior = scene.TopBar.ScrollBehavior!;
            var replacement = new TopAppBarState();
            behavior.State = replacement;
            Render(scene.Window);
            var second = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            Assert.Same(behavior, scene.Scope.TopScrollBehavior);
            Assert.NotSame(first, second);
            Assert.False(first.IsConnected);
            Assert.Equal(0, replacement.HeightOffset);
            scene.Top.HeightOffset = -1;
            scene.Top.ContentOffset = -999;
            Render(scene.Window);
            Assert.Equal(0, replacement.HeightOffset);
            Assert.Equal(0, replacement.ContentOffset);
            Wheel(scene, -1);
            Assert.Equal(replacement.HeightOffsetLimit, replacement.HeightOffset);
            Assert.Equal(-1, scene.Top.HeightOffset);
            Assert.Equal(-999, scene.Top.ContentOffset);
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ModeAndCanScrollChanges_ReconfigureTheExistingConnection()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200, TopAppBarScrollMode.Pinned);
        try
        {
            var behavior = scene.TopBar.ScrollBehavior!;
            var connection = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            behavior.Mode = TopAppBarScrollMode.EnterAlways;
            behavior.CanScroll = false;
            Render(scene.Window);
            Wheel(scene, -1);
            Assert.Equal(0, scene.Top.HeightOffset);
            Assert.Equal(0, scene.Top.ContentOffset);
            Assert.True(scene.Viewer.Offset.Y > 0);
            behavior.CanScroll = true;
            Render(scene.Window);
            Wheel(scene, -1);
            Assert.Equal(scene.Top.HeightOffsetLimit, scene.Top.HeightOffset);
            var offset = scene.Top.HeightOffset;
            behavior.Mode = TopAppBarScrollMode.Pinned;
            Render(scene.Window);
            Wheel(scene, 1);
            Assert.Equal(offset, scene.Top.HeightOffset);
            Assert.Same(scene.Top, behavior.State);
            Assert.Same(connection, GetSmoothConnection(scene.Scope, scene.Viewer));
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ContentAtStart_FollowsTheBodyUnlessExplicitlyOverridden()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200, TopAppBarScrollMode.Pinned);
        try
        {
            var behavior = scene.TopBar.ScrollBehavior!;
            Assert.True(scene.Top.IsScrollingContentAtStart);
            scene.Viewer.Offset = new Vector(0, 50);
            Render(scene.Window);
            Assert.False(scene.Top.IsScrollingContentAtStart);
            Assert.Equal(1, scene.Top.OverlappedFraction);
            behavior.IsScrollingContentAtStart = true;
            Render(scene.Window);
            Assert.True(scene.Top.IsScrollingContentAtStart);
            Assert.Equal(0, scene.Top.OverlappedFraction);
            behavior.IsScrollingContentAtStart = null;
            Render(scene.Window);
            Assert.False(scene.Top.IsScrollingContentAtStart);
            scene.Viewer.Offset = default;
            Render(scene.Window);
            Assert.True(scene.Top.IsScrollingContentAtStart);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void TemplateGeneratedBars_AutomaticallySupplyTheirBehaviors()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(1200);
        try
        {
            var first = Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer));
            scene.Screen.TopBarTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((_, _) =>
                new MediumTopAppBar
                {
                    ScrollBehavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.EnterAlways)
                    { SnapAnimationSpec = null, FlingAnimationSpec = null },
                });
            scene.Screen.BottomBarTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((_, _) =>
                new BottomAppBar
                {
                    ScrollBehavior = new BottomAppBarScrollBehavior
                    { SnapAnimationSpec = null, FlingAnimationSpec = null },
                });
            scene.Screen.TopBar = "top";
            scene.Screen.BottomBar = "bottom";
            Render(scene.Window);
            var top = Assert.IsType<MediumTopAppBar>(scene.Screen.PresentedTopBar).ScrollBehavior!;
            var bottom = Assert.IsType<BottomAppBar>(scene.Screen.PresentedBottomBar).ScrollBehavior!;
            Assert.Same(top, scene.Scope.TopScrollBehavior);
            Assert.Same(bottom, scene.Scope.BottomScrollBehavior);
            Assert.False(first.IsConnected);
            Assert.True(Assert.IsType<AppBarSmoothScrollConnection>(GetSmoothConnection(scene.Scope, scene.Viewer)).IsConnected);
            Wheel(scene, -1);
            Assert.Equal(top.State.HeightOffsetLimit, top.State.HeightOffset);
            Assert.True(bottom.State.HeightOffset < 0);
            Assert.Equal(0, scene.Top.HeightOffset);
        }
        finally { scene.Window.Close(); }
    }

    private sealed class LogicalBody : Control, ILogicalScrollable
    {
        private Vector _offset;
        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public bool IsLogicalScrollEnabled => true;
        public Size ScrollSize => new(1, 2);
        public Size PageScrollSize => new(4, 5);
        public Size Extent => new(40, 50);
        public Size Viewport => new(10, 10);
        public Vector Offset
        {
            get => _offset;
            set { _offset = value; RaiseScrollInvalidated(EventArgs.Empty); }
        }
        public event EventHandler? ScrollInvalidated;
        public void RaiseScrollInvalidated(EventArgs e) => ScrollInvalidated?.Invoke(this, e);
        public bool BringIntoView(Control target, Rect targetRect) => false;
        public Control? GetControlInDirection(NavigationDirection direction, Control? from) => null;
        protected override Size MeasureOverride(Size availableSize) => availableSize;
    }

    private sealed record Scene(MotionTestWindow Window, AppBarScrollScope Scope, Scaffold Screen,
        Border Body, ScrollViewer Viewer, MediumTopAppBar TopBar, BottomAppBar BottomBar,
        TopAppBarState Top, BottomAppBarState Bottom);

    private static Scene Show(double contentHeight, TopAppBarScrollMode kind = TopAppBarScrollMode.ExitUntilCollapsed)
    {
        var top = new TopAppBarState(); var bottom = new BottomAppBarState();
        var t = kind switch
        {
            TopAppBarScrollMode.Pinned => new TopAppBarScrollBehavior(TopAppBarScrollMode.Pinned) { State = top },
            TopAppBarScrollMode.EnterAlways => new TopAppBarScrollBehavior(TopAppBarScrollMode.EnterAlways) { State = top, SnapAnimationSpec = null, FlingAnimationSpec = null },
            _ => new TopAppBarScrollBehavior(TopAppBarScrollMode.ExitUntilCollapsed) { State = top, SnapAnimationSpec = null, FlingAnimationSpec = null },
        };
        var b = new BottomAppBarScrollBehavior() { State = bottom, SnapAnimationSpec = null, FlingAnimationSpec = null };
        var viewer = new ScrollViewer { Background = Brushes.White, Content = new Border { Height = contentHeight, Background = Brushes.LightGray },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            IsScrollInertiaEnabled = false };
        var topBar = new MediumTopAppBar { ScrollBehavior = t };
        var bottomBar = new BottomAppBar { ScrollBehavior = b };
        var screen = new Scaffold { TopBar = topBar, BottomBar = bottomBar };
        var body = new Border { Child = viewer };
        body.Bind(Border.WidthProperty, new Avalonia.Data.Binding("Bounds.Width") { Source = screen });
        body.Bind(Border.HeightProperty, new Avalonia.Data.Binding("Bounds.Height") { Source = screen });
        body.Bind(Border.PaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        body.Bind(MaterialWindowInsets.ConsumePaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        screen.Content = body;
        var window = new MotionTestWindow { Width = 300, Height = 400, WindowDecorations = WindowDecorations.None };
        window.Styles.Add(new MaterialTheme()); window.Content = screen; window.Show(); Render(window);
        var scope = screen.GetVisualDescendants().OfType<AppBarScrollScope>().Single(s => s.Host == screen);
        return new(window, scope, screen, body, viewer, topBar, bottomBar, top, bottom);
    }

    // Connection identity and disposal are lifecycle assertions. Keep private-field access
    // here so production types do not need a query API solely for these tests.
    private static AppBarSmoothScrollConnection? GetSmoothConnection(AppBarScrollScope scope, ScrollViewer viewer)
    {
        var connection = (AppBarSmoothScrollConnection?)ScopeConnectionField.GetValue(scope);
        return connection is not null && ReferenceEquals(ConnectionViewerField.GetValue(connection), viewer)
            ? connection
            : null;
    }

    private static void Wheel(Scene scene, double delta)
    {
        var position = scene.Viewer.TranslatePoint(new Point(40, 40), scene.Window)!.Value;
        scene.Window.MouseWheel(position, new Vector(0, delta));
        Render(scene.Window);
    }

    private static void Render(Window window)
    {
        for (var i = 0; i < 6; i++)
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            using (window.CaptureRenderedFrame()) { }
        }
    }
}
