using System.Linq;
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Interactivity;
using Avalonia.Media;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class CarouselDemo : Window
{
    public CarouselDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        // Masking consumes UI layout coordinates. These viewports deliberately use the native
        // physical presenter; the global SmoothScroll theme moves visuals on the compositor.
        HorizontalViewer.Template = NativeTemplate(false);
        VerticalViewer.Template = NativeTemplate(true);
        GridViewer.Template = NativeTemplate(false);
        foreach (var carousel in new[] { MultiBrowse, CenteredHero, Uncontained })
        for (var i = 0; i < 8; i++)
            carousel.Items.Add(new CarouselItem
            {
                CornerRadius = new(24), Background = new[] { Brushes.CornflowerBlue, Brushes.Coral, Brushes.DarkSeaGreen, Brushes.Plum }[i % 4],
                Content = new StackPanel { Margin = new(24), Spacing = 12, Children =
                {
                    new TextBlock { Text = $"Destination {i + 1}", FontSize = 24 },
                    new Button { Content = "Explore", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left }
                } }
            });
        Virtualized.ItemsSource = Enumerable.Range(0, 10_000).ToArray();
        Virtualized.ItemTemplate = new FuncDataTemplate<int>((index, _) => new Border
        {
            Height = 112 + index % 3 * 24,
            Background = new[] { Brushes.CornflowerBlue, Brushes.Coral, Brushes.DarkSeaGreen }[index % 3],
            Padding = new(24),
            Child = new StackPanel { Spacing = 12, Children =
            {
                new TextBlock { Text = $"Destination {index + 1}", FontSize = 24 },
                new Button { Content = "Explore", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left },
            } },
        });
    }

    private static FuncControlTemplate<ScrollViewer> NativeTemplate(bool vertical) => new((owner, scope) =>
        new ScrollContentPresenter
        {
            Name = "PART_ContentPresenter", Content = owner.Content, ClipToBounds = true,
            CanHorizontallyScroll = !vertical, CanVerticallyScroll = vertical,
            [!ScrollContentPresenter.OffsetProperty] = owner[!ScrollViewer.OffsetProperty],
        }.RegisterInNameScope(scope));

    private void ToggleVirtualHeight(object? sender, RoutedEventArgs e) =>
        Virtualized.Height = double.IsNaN(Virtualized.Height) ? 160 : double.NaN;

    private void ScrollPositionChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (HorizontalViewer is not null) HorizontalViewer.Offset = new(e.NewValue, 0);
    }
    private void ToggleDirection(object? sender, RoutedEventArgs e) =>
        HorizontalViewer.FlowDirection = HorizontalViewer.FlowDirection == FlowDirection.LeftToRight
            ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    private void ToggleShape(object? sender, RoutedEventArgs e)
    {
        foreach (var item in HorizontalItems.Children.Concat(VerticalItems.Children).OfType<MultiAspectCarouselItem>())
            item.CornerRadius = item.CornerRadius.TopLeft == 0 ? new(24) : default;
    }
    private async void PreviousClassic(object? sender, RoutedEventArgs e) => await MoveClassic(-1);
    private async void NextClassic(object? sender, RoutedEventArgs e) => await MoveClassic(1);
    private async Task MoveClassic(int delta)
    {
        try { await Task.WhenAll(new[] { MultiBrowse, CenteredHero, Uncontained }.Select(c => c.AnimateScrollToItemAsync(c.CurrentItem + delta))); }
        catch (OperationCanceledException) { /* A newer navigation or closing the window supersedes this request. */ }
    }
    private void ToggleClassicDirection(object? sender, RoutedEventArgs e)
    {
        foreach (var carousel in new[] { MultiBrowse, CenteredHero, Uncontained })
            carousel.FlowDirection = carousel.FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }
    private void SetFlingMode(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value } && Enum.TryParse<CarouselFlingMode>(value, out var mode))
            foreach (var carousel in new[] { MultiBrowse, CenteredHero, Uncontained }) carousel.FlingMode = mode;
    }
    private async void FlingClassic(object? sender, RoutedEventArgs e)
    {
        // Begin between focal positions, as after a drag. Exactly on a snap point, the
        // upstream SingleAdvance behavior can consume velocity without moving a page.
        foreach (var carousel in new[] { MultiBrowse, CenteredHero, Uncontained })
        {
            carousel.ScrollToItem(carousel.CurrentItem);
            carousel.ScrollOffset += 45;
        }
        try { await Task.WhenAll(new[] { MultiBrowse, CenteredHero, Uncontained }.Select(c => c.FlingAsync(1400))); }
        catch (OperationCanceledException) { }
    }
    private void AccessibilityPage(object? sender, RoutedEventArgs e)
    {
        var amount = sender is Button { Tag: "-1" } ? ScrollAmount.LargeDecrement : ScrollAmount.LargeIncrement;
        foreach (var carousel in new[] { MultiBrowse, CenteredHero, Uncontained })
            ControlAutomationPeer.CreatePeerForElement(carousel)!.GetProvider<IScrollProvider>()?.Scroll(amount, ScrollAmount.NoAmount);
    }
    private void ToggleUserScrolling(object? sender, RoutedEventArgs e)
    {
        foreach (var carousel in new[] { MultiBrowse, CenteredHero, Uncontained }) carousel.UserScrollEnabled = !carousel.UserScrollEnabled;
    }

    private void FocusNextHero(object? sender, RoutedEventArgs e)
    {
        var index = (CenteredHero.CurrentItem + 1) % CenteredHero.ItemCount;
        CenteredHero.ScrollIntoView(index);
        if (CenteredHero.Items[index] is CarouselItem { Content: StackPanel content })
            content.Children.OfType<Button>().First().Focus();
    }

    private void JumpVirtual(object? sender, RoutedEventArgs e) => Virtualized.ScrollToItem(5000);
    private void ResetVirtual(object? sender, RoutedEventArgs e) => Virtualized.ScrollToItem(0);
    private async void AnimateVirtual(object? sender, RoutedEventArgs e) => await Virtualized.AnimateScrollToItemAsync(9999);
}
