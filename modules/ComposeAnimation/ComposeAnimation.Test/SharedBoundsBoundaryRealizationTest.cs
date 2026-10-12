using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

// Regression: showing a RemeasureToBounds endpoint whose content is realized lazily must not
// fail when template realization flips inherited inputs (UseLayoutRounding) mid-pass.
public class SharedBoundsBoundaryRealizationTest
{

    [Fact]
    public Task FirstShow_WithBoundaryContent_RealizesWithoutLosingTarget() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var parent = new Transition<bool>(true);

        var swatch = new SharedBounds
        {
            Key = "dialog-color", HorizontalAlignment = HorizontalAlignment.Center,
            Content = new Border { Width = 120, Height = 80, CornerRadius = new CornerRadius(12), Background = Brushes.SteelBlue }
        };
        var sourceBoundary = new LookaheadBoundary
        {
            Child = new Button { Content = new StackPanel { Children = { swatch, new TextBlock { Text = "Open" } } } }
        };
        var source = new SharedBounds
        {
            Key = "dialog", ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
            Content = sourceBoundary
        };
        var sourceRoot = new LookaheadRoot { Width = 160, Height = 150, Child = source };
        var listPage = new AnimatedVisibility
        {
            Content = new StackPanel { Children = { sourceRoot } },
            Enter = EnterTransition.None, Exit = ExitTransition.None
        };

        // Nested shared bounds as cooperative content, ordinary Border behind a boundary.
        var header = new SharedBounds
        {
            Key = "dialog-color", ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
            Content = new LookaheadBoundary
            {
                Child = new Border { Width = 440, Height = 170, CornerRadius = new CornerRadius(16, 16, 0, 0), Background = Brushes.SteelBlue }
            }
        };
        var title = new LookaheadTextBlock { Text = "Title", FontSize = 24 };
        var description = new TextBlock
        {
            Text = "An ordinary TextBlock wrapped in LookaheadBoundary, outside the lookahead scope. " +
                "It keeps native layout at the destination constraints.",
            FontSize = 15, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap
        };
        var boundary = new LookaheadBoundary { Child = description };
        var stack = new LookaheadStackPanel { Margin = new Thickness(24), Spacing = 16 };
        stack.Children.Add(header);
        stack.Children.Add(title);
        stack.Children.Add(boundary);
        var dialog = new SharedBounds
        {
            Key = "dialog", ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
            Background = new SolidColorBrush(Color.Parse("#222222")), CornerRadius = new CornerRadius(16),
            Content = stack
        };
        var root = new LookaheadRoot
        {
            Width = 440, Height = 360,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = dialog
        };
        var detailsPage = new AnimatedVisibility
        {
            Content = new Grid { Children = { root } },
            Enter = EnterTransition.None, Exit = ExitTransition.None
        };

        listPage.Source = VisibilitySource.From(parent, list => list);
        detailsPage.Source = VisibilitySource.From(parent, list => !list);

        var host = new SharedTransitionHost { Transition = parent, PreparationTimeout = Timeout.InfiniteTimeSpan };
        host.Content = new Grid { Children = { listPage, detailsPage } };
        var window = new AnimationTestWindow(clock) { Width = 720, Height = 520, Content = host };
        window.Show();
        Layout();
        parent.OnFrame(TimeSpan.Zero);

        parent.UpdateTarget(false);
        Layout();

        Assert.Null(host.LastPreparationError);
        Assert.NotNull(LookaheadRoot.TargetFor(boundary));
        Assert.NotNull(LookaheadRoot.TargetFor(dialog));
        Assert.Equal(2, host.Coordinator!.Motions.Count);

        parent.OnFrame(TimeSpan.FromMilliseconds(100));
        Layout();
        Assert.Null(host.LastSessionError);

        parent.UpdateTarget(true);
        Layout();
        Assert.Null(host.LastPreparationError);

        window.Close();
        parent.Dispose();

        void Layout() { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    });
}
