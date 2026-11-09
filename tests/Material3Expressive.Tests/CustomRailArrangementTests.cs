using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class CustomRailArrangementTests
{
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public async Task CustomCallbackReordersItemsAndReplacementRetainsStateAndChildren(bool modal)
    {
        using var clock = new MotionTestClockScope();
        WideNavigationRail rail = modal ? new ModalWideNavigationRail() : new WideNavigationRail();
        var state = rail.State;
        var first = new WideNavigationRailItem { Icon = new Border { Width = 24, Height = 24 } };
        var second = new WideNavigationRailItem { Icon = new Border { Width = 24, Height = 24 } };
        rail.Items.Add(first); rail.Items.Add(second);
        rail.Header = new Border { Width = 48, Height = 40 };
        rail.Arrangement = new ReverseArrangement();
        var window = new MotionTestWindow { Width = 520, Height = 360 };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        window.Content = rail; window.Show(); Flush(window);
        try
        {
            Assert.True(first.TranslatePoint(default, window)!.Value.Y > second.TranslatePoint(default, window)!.Value.Y);
            var firstParent = first.Parent;
            rail.Arrangement = NavigationRailArrangement.Top; Flush(window);
            Assert.True(first.TranslatePoint(default, window)!.Value.Y < second.TranslatePoint(default, window)!.Value.Y);
            Assert.Same(firstParent, first.Parent); Assert.Same(state, rail.State);
            rail.Arrangement = new ReverseArrangement(); Flush(window);
            var expansion = state.ExpandAsync();
            for (var ms = 0; ms <= 1200; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); }
            await expansion;
            Assert.True(first.TranslatePoint(default, TopLevel.GetTopLevel(first)!)!.Value.Y > second.TranslatePoint(default, TopLevel.GetTopLevel(second)!)!.Value.Y);
            Assert.Same(state, rail.State); Assert.False(state.IsAnimating);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
    [Fact]
    public void CallbackContractUsesPhysicalPixelsAndRetainsBuiltInSingletons()
    {
        NavigationRailArrangement arrangement = new ReverseArrangement();
        int[] positions = new int[3];
        arrangement.Arrange(4, 800, new[] { 80, 120, 160 }, positions);
        Assert.Equal(new[] { 328, 208, 48 }, positions);
        NavigationRailArrangement.SpaceBetween.Arrange(4, 800, new[] { 80, 120, 160 }, positions);
        Assert.Equal(new[] { 0, 300, 640 }, positions);
        Assert.NotEqual(NavigationRailArrangement.Center, NavigationRailArrangement.SpacedBy(0, Avalonia.Layout.VerticalAlignment.Center));
    }
    private sealed record ReverseArrangement : NavigationRailArrangement
    {
        public override void Arrange(double density, int totalSize, ReadOnlySpan<int> sizes, Span<int> positions)
        {
            var offset = (int)Math.Floor(12 * density + .5);
            for (var index = sizes.Length - 1; index >= 0; index--)
            { positions[index] = offset; offset += sizes[index]; }
        }
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
