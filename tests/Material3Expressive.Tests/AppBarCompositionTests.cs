using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using SmoothScroll.Avalonia.Interaction;
using SmoothScroll.Avalonia.Composition;
using System.Runtime.InteropServices;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarCompositionTests
{
    public static IEnumerable<object[]> RenderedFractions() =>
        from twoRows in new[] { false, true }
        from density in new[] { 1d, 1.25d, 2d }
        from fraction in new[] { 0f, .25f, .75f, 1f }
        select new object[] { twoRows, density, fraction };

    [AvaloniaTheory]
    [MemberData(nameof(RenderedFractions))]
    public void CompositedBar_RenderedPixelsEqualStock_AndSurviveLayoutRebase(bool twoRows, double density, float fraction)
    {
        using var clock = new MotionTestClockScope();
        var state = new TopAppBarState();
        TopAppBar bar = twoRows ? new MediumFlexibleTopAppBar() : new TopAppBar();
        bar.ScrollBehavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.ExitUntilCollapsed) { State = state, SnapAnimationSpec = null, FlingAnimationSpec = null };
        bar.Title = "expanded";
        bar.TitleTemplate = new FuncDataTemplate<object>((_, _) => new Border { Width = 80, Height = 20, Background = Brushes.Red });
        bar.CollapsedTitle = "collapsed";
        bar.CollapsedTitleTemplate = new FuncDataTemplate<object>((_, _) => new Border { Width = 60, Height = 16, Background = Brushes.Blue });
        bar.NavigationIcon = new Border { Width = 32, Height = 32, Background = Brushes.Lime };
        bar.Items.Add(new Border { Width = 40, Height = 24, Background = Brushes.Yellow });
        bar.Background = new SolidColorBrush(Color.Parse("#FFFDF7FE"));
        bar.ScrolledContainerColor = Color.Parse("#FFE6DFEB");
        var window = new MotionTestWindow { Width = 300, Height = 180, WindowDecorations = WindowDecorations.None,
            Background = Brushes.White };
        window.Styles.Add((Avalonia.Styling.Styles)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { bar } }; window.Show(); window.SetRenderScaling(density); Render(window);
        var range = -state.HeightOffsetLimit;
        state.HeightOffset = -range * fraction; Render(window);
        var chrome = bar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
        var compositor = ElementComposition.GetElementVisual(chrome)!.Compositor;
        using var offsets = CompositionVector3Output.Create(compositor);
        using var appearance = CompositionVector3Output.Create(compositor);
        using var color = CompositionColorOutput.Create(compositor);
        using var tracker = compositor.CreateInteractionTracker(null);
        try
        {
            using var stock = window.CaptureRenderedFrame()!;
            var expected = Pixels(stock);
            var colors = chrome.GetCompositionColors();
            var config = new AppBarScrollConfiguration(TopAppBarScrollMode.ExitUntilCollapsed, range / density, 0, density,
                true, false, false, true, colors.TwoRows, colors.Base, colors.Scrolled,
                null, null, new AppBarSpring(1, 1600, .01f), 1000, 1000);
            var initial = new AppBarScrollSnapshot(0, 0, range * fraction / density, 0, 0, 0, 0, 1000);
            chrome.SetComposition(offsets, appearance, color); Render(window);
            tracker.ConfigureVerticalScroll(new RenderParticipantFactory(config, initial,
                offsets.Writer, appearance.Writer, color.Writer));
            Render(window);
            using var composed = window.CaptureRenderedFrame()!;
            var actual = Pixels(composed);
            if (!expected.AsSpan().SequenceEqual(actual))
            {
                var directory = Path.Combine("artifacts", "appbar-composition-tests", $"{(twoRows ? "twoRows" : "single")}-{density}x-{fraction}");
                Directory.CreateDirectory(directory); stock.Save(Path.Combine(directory, "stock.png"), new PngBitmapEncoderOptions()); composed.Save(Path.Combine(directory, "composition.png"), new PngBitmapEncoderOptions());
            }
            Assert.Equal(stock.PixelSize, composed.PixelSize);
            Assert.True(expected.AsSpan().SequenceEqual(actual),
                $"Rendered compositor pixels differ for twoRows={twoRows}, density={density}, fraction={fraction}; differing bytes={expected.Zip(actual).Count(pair => pair.First != pair.Second)}; rows={string.Join(';', chrome.Children.OfType<AppBarTopRow>().Select(row => $"{row.Bounds},title={row.Title.Bounds},baseline={row.Title.LastBaseline(density)}"))}.");
            // The UI mirror can catch up to an earlier frame while live compositor
            // geometry is already elsewhere. Re-arranging must not replace its clip.
            state.HeightOffset = -range * (1 - fraction);
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            using (var rebased = window.CaptureRenderedFrame()!)
                Assert.True(expected.AsSpan().SequenceEqual(Pixels(rebased)),
                    $"UI layout rebase replaced compositor geometry for twoRows={twoRows}, density={density}, liveFraction={fraction}.");
            state.HeightOffset = -range * fraction; Render(window);
            chrome.SetComposition(null, null); tracker.ConfigureVerticalScroll(null); Render(window);
            using var restored = window.CaptureRenderedFrame()!;
            if (!expected.AsSpan().SequenceEqual(Pixels(restored)))
            {
                var directory = Path.Combine("artifacts", "appbar-composition-tests", $"{(twoRows ? "twoRows" : "single")}-{density}x-{fraction}");
                Directory.CreateDirectory(directory); stock.Save(Path.Combine(directory, "stock.png"), new PngBitmapEncoderOptions()); restored.Save(Path.Combine(directory, "restored.png"), new PngBitmapEncoderOptions());
            }
            Assert.True(expected.AsSpan().SequenceEqual(Pixels(restored)), "Returning to stock rendering changed pixels.");
        }
        finally { chrome.SetComposition(null, null); tracker.ConfigureVerticalScroll(null); window.Close(); }
    }

    private sealed record RenderParticipantFactory(AppBarScrollConfiguration Config, AppBarScrollSnapshot Initial,
        CompositionVector3OutputWriter Offsets, CompositionVector3OutputWriter Appearance, CompositionColorOutputWriter Color)
        : IScrollMovementParticipantFactory
    {
        public IScrollMovementParticipant Create() => new AppBarScrollParticipant(Config, Initial, Offsets, Appearance, Color, _ => { });
    }

    private static byte[] Pixels(Bitmap bitmap)
    {
        using var copy = new WriteableBitmap(bitmap.PixelSize, bitmap.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var pixels = copy.Lock(); bitmap.CopyPixels(pixels);
        var bytes = new byte[pixels.RowBytes * pixels.Size.Height]; Marshal.Copy(pixels.Address, bytes, 0, bytes.Length);
        return bytes;
    }

    private static void Render(Window window)
    {
        for (var frame = 0; frame < 6; frame++)
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            using (window.CaptureRenderedFrame()) { }
        }
    }

    [AvaloniaTheory]
    [InlineData(false, 64)] [InlineData(true, 48)]
    public async Task FractionalSnapOffsets_KeepExpandedRangeConstant(bool twoRows, float range)
    {
        using var clock = new MotionTestClockScope();
        var state = new TopAppBarState();
        var behavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.ExitUntilCollapsed) { State = state };
        TopAppBar bar = twoRows ? new MediumTopAppBar() : new TopAppBar();
        bar.ScrollBehavior = behavior;
        var window = new MotionTestWindow { Width = 300, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { bar } }; window.Show(); window.UpdateLayout();
        var chrome = bar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
        var compositor = ElementComposition.GetElementVisual(chrome)!.Compositor;
        using var offsets = CompositionVector3Output.Create(compositor);
        using var appearance = CompositionVector3Output.Create(compositor);
        using var colors = CompositionColorOutput.Create(compositor);
        try
        {
            chrome.SetComposition(offsets, appearance, colors);
            state.HeightOffset = -range * .65f - .125f; window.UpdateLayout();
            Assert.Equal(-range, state.HeightOffsetLimit);
            var settling = behavior.SettleAsync(0, window.MotionRoot);
            clock.Track(settling); clock.SendFrame(TimeSpan.Zero);
            for (var frame = 0; frame < 120 && !settling.IsCompleted; frame++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
                Assert.Equal(-range, state.HeightOffsetLimit);
            }
            Assert.True(settling.IsCompletedSuccessfully);
            await settling;
            Assert.Equal(-range, state.HeightOffset);
        }
        finally { chrome.SetComposition(null, null); window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("small")] [InlineData("center")] [InlineData("medium")]
    [InlineData("large")] [InlineData("mediumFlexible")] [InlineData("largeFlexible")]
    [InlineData("twoRows")]
    public void CompositionGeometry_RetainsFullRows_AndHandsLayoutBack(string variant)
    {
        TopAppBar bar = variant switch
        {
            "center" => new CenterAlignedTopAppBar(), "medium" => new MediumTopAppBar(),
            "large" => new LargeTopAppBar(), "mediumFlexible" => new MediumFlexibleTopAppBar(),
            "largeFlexible" => new LargeFlexibleTopAppBar(), "twoRows" => new TwoRowsTopAppBar(),
            _ => new TopAppBar(),
        };
        var state = new TopAppBarState();
        bar.Title = "title";
        bar.TitleTemplate = new FuncDataTemplate<object>((_, _) => new Border { Width = 80, Height = 20 });
        bar.NavigationIcon = new Border { Width = 48, Height = 48 };
        bar.ScrollBehavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.ExitUntilCollapsed) { State = state, SnapAnimationSpec = null, FlingAnimationSpec = null };
        using var clock = new MotionTestClockScope();
        var window = new MotionTestWindow { Width = 300, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(
            new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { bar } };
        window.Show(); window.UpdateLayout();
        var chrome = bar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
        var rows = chrome.Children.OfType<AppBarTopRow>().ToArray();
        var fullHeight = bar.Bounds.Height;
        var rowHeights = rows.Select(row => row.Bounds.Height).ToArray();
        var compositor = ElementComposition.GetElementVisual(chrome)!.Compositor;
        using var offsets = CompositionVector3Output.Create(compositor);
        using var appearance = CompositionVector3Output.Create(compositor);
        using var colors = CompositionColorOutput.Create(compositor);
        try
        {
            chrome.SetComposition(offsets, appearance, colors);
            state.HeightOffset = state.HeightOffsetLimit;
            window.UpdateLayout();
            Assert.True(chrome.CompositionEnabled);
            Assert.Equal(bar.HasTwoRows ? bar.CollapsedHeight : 0, bar.Bounds.Height);
            Assert.Equal(rowHeights[0], rows[0].Bounds.Height);
            if (bar.HasTwoRows) Assert.Equal(rowHeights[1], rows[1].Bounds.Height);
            Assert.All(rows, row => Assert.Equal(1, row.Title.Opacity));
            Assert.Same(Avalonia.Media.Brushes.Transparent, chrome.Background);
            Assert.Single(chrome.Children, child => child.ZIndex == -1);

            bar.ExpandedHeight = fullHeight + 40;
            window.UpdateLayout();
            var resizingHeight = bar.Bounds.Height;
            Assert.Equal(rowHeights[bar.HasTwoRows ? 1 : 0] + 40, rows[bar.HasTwoRows ? 1 : 0].Bounds.Height);
            chrome.SetComposition(null, null);
            window.UpdateLayout();
            Assert.False(chrome.CompositionEnabled);
            Assert.Equal(resizingHeight, bar.Bounds.Height);
            Assert.Equal(2, chrome.Children.Count);
            Assert.NotNull(chrome.Background);
            Assert.Equal(bar.Bounds.Height - (bar.HasTwoRows ? bar.CollapsedHeight : 0), rows[bar.HasTwoRows ? 1 : 0].Bounds.Height);

            state.HeightOffset = 0; window.UpdateLayout();
            Assert.Equal(fullHeight + 40, bar.Bounds.Height);
        }
        finally
        {
            chrome.SetComposition(null, null);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_ReleasesCompositionBackground_AndRestoresTitleLayers()
    {
        using var clock = new MotionTestClockScope();
        var state = new TopAppBarState();
        var bar = new MediumTopAppBar { Title = "title", ScrollBehavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.ExitUntilCollapsed) { State = state } };
        var window = new MotionTestWindow { Width = 300, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = bar; window.Show(); window.UpdateLayout();
        var chrome = bar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
        var compositor = ElementComposition.GetElementVisual(chrome)!.Compositor;
        using var offsets = CompositionVector3Output.Create(compositor);
        using var appearance = CompositionVector3Output.Create(compositor);
        using var colors = CompositionColorOutput.Create(compositor);
        try
        {
            chrome.SetComposition(offsets, appearance, colors); window.UpdateLayout();
            window.Content = null;
            Assert.False(chrome.CompositionEnabled);
            Assert.Equal(2, chrome.Children.Count);
            Assert.NotNull(chrome.Background);
            Assert.Equal(0, chrome.Children.OfType<AppBarTopRow>().First().Title.Opacity);
            window.Content = bar; window.UpdateLayout();
            Assert.Equal(112, bar.Bounds.Height);
        }
        finally { window.Close(); }
    }
}
