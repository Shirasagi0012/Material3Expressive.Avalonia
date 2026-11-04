using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Components;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class DockedSearchBarTests
{
    [AvaloniaTheory]
    [InlineData(false, 600, 40, 56)]
    [InlineData(true, 600, 40, 296)]
    [InlineData(true, 600, 700, 456)]
    [InlineData(true, 240, 700, 216)]
    public void SurfaceUsesPreferredWidthAndWindowRelativeResultsHeight(bool expanded, int windowHeight, int resultHeight, int expectedHeight)
    {
        var bar = Create(expanded, resultHeight);
        var window = Show(bar, 600, windowHeight);
        Assert.Equal(360, bar.Bounds.Width);
        Assert.Equal(expectedHeight, bar.Bounds.Height);
        Assert.Equal(new CornerRadius(28), bar.CornerRadius);
        Assert.Equal(0, bar.TonalElevation);
        Assert.Equal(0, bar.ShadowElevation);
        if (expanded)
        {
            var divider = bar.GetVisualDescendants().OfType<Separator>().Single();
            Assert.Equal(bar.DividerColor, MaterialDivider.GetColor(divider));
            Assert.Equal(1, divider.Bounds.Height);
        }
        window.Close();
    }

    [AvaloniaFact]
    public void ParentConstraintsAndWindowResizeRemeasureTheSameContent()
    {
        var bar = Create(true, 700);
        var window = Show(bar, 220, 600);
        Assert.Equal(new Size(220, 456), bar.Bounds.Size);
        var results = bar.Content;
        window.Height = 240; Flush(window);
        Assert.Equal(new Size(220, 216), bar.Bounds.Size);
        Assert.Same(results, bar.Content);
        window.Width = 600; bar.Width = 440; Flush(window);
        Assert.Equal(440, bar.Bounds.Width);
        bar.MaxHeight = 180; Flush(window);
        Assert.Equal(180, bar.Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void ContentTemplateIsLazyAndInputSlotRetainsLogicalOwnership()
    {
        using var clock = new MotionTestClockScope();
        var builds = 0;
        var input = new Border { Height = 56 };
        var bar = new DockedSearchBar { InputField = input, Content = "query",
            ContentTemplate = new FuncDataTemplate<string>((value, _) => { builds++; return new TextBlock { Text = value }; }) };
        var window = Show(bar);
        Assert.Equal(0, builds);
        Assert.Same(bar, input.Parent);
        clock.SendFrame(TimeSpan.Zero);
        bar.IsExpanded = true; Flush(window);
        Assert.Equal(1, builds);
        Frames(clock, window, 16, 816);
        Assert.True(Visibility(bar).IsIdle);
        bar.IsExpanded = false; Flush(window);
        Assert.True(Visibility(bar).IsContentPresent);
        Frames(clock, window, 832, 1440);
        Assert.False(Visibility(bar).IsContentPresent);
        Assert.Equal(56, bar.Bounds.Height);
        var replacement = new Border { Height = 64 };
        bar.InputField = replacement; Flush(window);
        Assert.Null(input.Parent);
        Assert.Same(bar, replacement.Parent);
        Assert.Equal(64, bar.Bounds.Height);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void AnimationDelayAndReversalKeepPresentedContentUntilExitCompletes()
    {
        using var clock = new MotionTestClockScope();
        var bar = Create(false);
        var window = Show(bar);
        clock.SendFrame(TimeSpan.Zero);
        bar.IsExpanded = true; Flush(window);
        Frames(clock, window, 16, 96);
        Assert.Equal(56, bar.Bounds.Height);
        Frames(clock, window, 112, 208);
        Assert.InRange(bar.Bounds.Height, 57, 295);
        var partial = bar.Bounds.Height;
        var content = bar.GetVisualDescendants().OfType<DockedSearchResultsLayout>().Single();
        bar.IsExpanded = false; Flush(window);
        Assert.Same(content, bar.GetVisualDescendants().OfType<DockedSearchResultsLayout>().Single());
        Assert.Equal(partial, bar.Bounds.Height);
        Frames(clock, window, 224, 288);
        bar.IsExpanded = true; Flush(window);
        Frames(clock, window, 304, 1280);
        Assert.Equal(296, bar.Bounds.Height);
        Assert.True(Visibility(bar).IsIdle);
        Assert.Same(content, bar.GetVisualDescendants().OfType<DockedSearchResultsLayout>().Single());
        window.Content = null; Flush(window);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Content = bar; Flush(window);
        Assert.Equal(296, bar.Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void EscapeRequestsControlledCollapseForLastExpandedBarAndHonorsHandledKeys()
    {
        var first = Create(true); var last = Create(true);
        var outside = new TextBox();
        var root = new StackPanel { Children = { first, last, outside } };
        var firstRequests = 0; var lastRequests = 0;
        first.CollapseRequested += (_, _) => firstRequests++;
        last.CollapseRequested += (_, _) => lastRequests++;
        var window = Show(root, 700, 900);
        void Escape(bool handled = false) => outside.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape, Handled = handled });
        Escape();
        Assert.Equal(0, firstRequests); Assert.Equal(1, lastRequests);
        Assert.True(last.IsExpanded);
        Escape(true); Assert.Equal(1, lastRequests);
        root.Children.Remove(last); Flush(window);
        Escape(); Assert.Equal(1, firstRequests); Assert.Equal(1, lastRequests);
        first.IsExpanded = false; Flush(window);
        Escape(); Assert.Equal(1, firstRequests);
        window.Close();
    }

    [AvaloniaFact]
    public void ComponentResourcesUpdateSurfaceAndBoundsWithoutRecreatingSlots()
    {
        var bar = Create(true);
        var window = Show(bar);
        var input = bar.InputField;
        bar.Resources[Comp.SearchBar.DefaultWidth] = 400d;
        bar.Resources[Comp.SearchBar.MinimumResultsHeight] = 200d;
        bar.Resources[Comp.SearchBar.ContainerColor] = Colors.Red;
        bar.Resources[Comp.SearchBar.DividerColor] = Colors.Blue;
        bar.Resources[Comp.SearchBar.DockedContainerShape] = new CornerRadius(12);
        Flush(window);
        Assert.Equal(new Size(400, 256), bar.Bounds.Size);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)bar.Background!).Color);
        Assert.Equal(Colors.Blue, MaterialDivider.GetColor(bar.GetVisualDescendants().OfType<Separator>().Single()));
        Assert.Equal(new CornerRadius(12), bar.GetVisualDescendants().OfType<MaterialSurface>().Single().CornerRadius);
        Assert.Same(input, bar.InputField);
        window.Close();
    }

    private static DockedSearchBar Create(bool expanded, double contentHeight = 40) => new() {
        IsExpanded = expanded, InputField = new Border { Height = 56, Width = 360, Background = Brushes.Red },
        Content = new Border { Height = contentHeight, Background = Brushes.Blue }
    };
    private static AnimatedVisibility Visibility(DockedSearchBar bar) => bar.GetVisualDescendants().OfType<AnimatedVisibility>().Single();
    private static MotionTestWindow Show(Control control, double width = 600, double height = 600)
    {
        var window = new MotionTestWindow { Width = width, Height = height };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = control; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    {
        for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
    }
}
