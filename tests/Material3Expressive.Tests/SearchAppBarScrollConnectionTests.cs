using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;
using Material3Expressive.TestSupport;
using Xunit;

namespace Material3Expressive.Tests;

public class SearchAppBarScrollConnectionTests
{
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void StandardScaffold_AutomaticallyConnectsSearchAndPreservesWholePreConsumption(double density)
    {
        using var clock = new MotionTestClockScope();
        var scene = Show();
        try
        {
            scene.Window.SetRenderScaling(density); Render(scene.Window);
            var connection = Connection(scene.Screen)!;
            Assert.True(connection.IsConnected);
            var tracker = connection.Tracker!;
            tracker.ApplyVerticalScrollDelta(20); Render(scene.Window);
            Assert.Equal(-20, scene.Behavior.State.ScrollOffset);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            tracker.ApplyVerticalScrollDelta(100); Render(scene.Window);
            Assert.Equal(-64, scene.Behavior.State.ScrollOffset);
            Assert.Equal(0, scene.Viewer.Offset.Y); // The edge-crossing input is wholly consumed.
            tracker.ApplyVerticalScrollDelta(40); Render(scene.Window);
            Assert.Equal(40, scene.Viewer.Offset.Y);
            Assert.Equal(-40, scene.Behavior.State.ContentOffset);
            tracker.ApplyVerticalScrollDelta(-12); Render(scene.Window);
            Assert.Equal(-52, scene.Behavior.State.ScrollOffset);
            Assert.Equal(40, scene.Viewer.Offset.Y);
            Assert.Equal(12, scene.Screen.ContentPadding.Top);
            Assert.Equal(388, scene.Viewer.Viewport.Height);
            Assert.Equal(-52, connection.LastSnapshot.TopCollapsed * -1);
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void ReverseLayout_ObservesBodyAndConsumesDownwardRemainder()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(reverse: true);
        try
        {
            var tracker = Connection(scene.Screen)!.Tracker!;
            tracker.ApplyVerticalScrollDelta(20); Render(scene.Window);
            Assert.Equal(-20, scene.Behavior.State.ScrollOffset);
            Assert.Equal(20, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Behavior.State.ContentOffset);
            tracker.ApplyVerticalScrollDelta(-100); Render(scene.Window);
            Assert.Equal(0, scene.Behavior.State.ScrollOffset);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(80, scene.Behavior.State.ContentOffset);
            tracker.CompleteVerticalScroll(); Render(scene.Window);
            Assert.Equal(80, scene.Behavior.State.ContentOffset); // Search has no AppBar idle reset.
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void CanScrollAndBehaviorReplacement_UpdateLiveConnectionAndReleaseOldState()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show();
        try
        {
            var original = Connection(scene.Screen)!;
            scene.Behavior.CanScroll = false; Render(scene.Window);
            original.Tracker!.ApplyVerticalScrollDelta(30); Render(scene.Window);
            Assert.Equal(30, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Behavior.State.ScrollOffset);
            scene.Behavior.CanScroll = true; Render(scene.Window);
            original.Tracker!.ApplyVerticalScrollDelta(20); Render(scene.Window);
            Assert.Equal(-20, scene.Behavior.State.ScrollOffset);
            var replacement = new SearchBarScrollBehavior();
            scene.Bar.ScrollBehavior = replacement; Render(scene.Window);
            Assert.False(original.IsConnected);
            var next = Connection(scene.Screen)!;
            Assert.NotSame(original, next);
            next.Tracker!.ApplyVerticalScrollDelta(12); Render(scene.Window);
            Assert.Equal(-12, replacement.State.ScrollOffset);
            Assert.Equal(-20, scene.Behavior.State.ScrollOffset);
            scene.Bar.ScrollBehavior = null; Render(scene.Window);
            Assert.False(next.IsConnected);
            Assert.Null(Connection(scene.Screen));
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void NativeWheelConnects_AndNestedViewerRetainsItsOwnInput()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show();
        try
        {
            var inner = new ScrollViewer { Height = 100, IsScrollInertiaEnabled = false,
                Content = new Border { Height = 600 }, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            scene.Viewer.Content = new StackPanel { Children = { inner, new Border { Height = 1200 } } };
            Render(scene.Window);
            scene.Window.MouseWheel(inner.TranslatePoint(new Point(20, 20), scene.Window)!.Value, new Vector(0, -1));
            Render(scene.Window);
            Assert.True(inner.Offset.Y > 0);
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Behavior.State.ScrollOffset);
            scene.Window.MouseWheel(scene.Viewer.TranslatePoint(new Point(20, 150), scene.Window)!.Value, new Vector(0, -1));
            Render(scene.Window);
            Assert.True(scene.Behavior.State.ScrollOffset < 0);
            Assert.Null(scene.Window.MotionRoot.Diagnostics.LastError);
        }
        finally { scene.Window.Close(); }
    }

    private static AppBarSmoothScrollConnection? Connection(Scaffold scaffold)
    {
        var scope = scaffold.GetVisualDescendants().OfType<AppBarScrollScope>().Single();
        return (AppBarSmoothScrollConnection?)typeof(AppBarScrollScope)
            .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(scope);
    }

    private static (MotionTestWindow Window, Scaffold Screen, ScrollViewer Viewer, AppBarWithSearch Bar, SearchBarScrollBehavior Behavior) Show(bool reverse = false)
    {
        var behavior = new SearchBarScrollBehavior { ReverseLayout = reverse };
        var bar = new AppBarWithSearch { InputField = new Border { Height = 56 }, ScrollBehavior = behavior };
        var viewer = new ScrollViewer { IsScrollInertiaEnabled = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            Content = new Border { Height = 1200, Background = Brushes.LightGray } };
        var screen = new Scaffold { TopBar = bar };
        var body = new Border { Child = viewer };
        body.Bind(Border.WidthProperty, new Binding("Bounds.Width") { Source = screen });
        body.Bind(Border.HeightProperty, new Binding("Bounds.Height") { Source = screen });
        body.Bind(Border.PaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        body.Bind(MaterialWindowInsets.ConsumePaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        screen.Content = body;
        var window = new MotionTestWindow { Width = 400, Height = 400, WindowDecorations = WindowDecorations.None };
        window.Styles.Add(new MaterialTheme()); window.Content = screen; window.Show(); Render(window);
        return (window, screen, viewer, bar, behavior);
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
