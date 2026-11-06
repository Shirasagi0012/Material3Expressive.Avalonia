using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
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
    }

    private static FuncControlTemplate<ScrollViewer> NativeTemplate(bool vertical) => new((owner, scope) =>
        new ScrollContentPresenter
        {
            Name = "PART_ContentPresenter", Content = owner.Content, ClipToBounds = true,
            CanHorizontallyScroll = !vertical, CanVerticallyScroll = vertical,
            [!ScrollContentPresenter.OffsetProperty] = owner[!ScrollViewer.OffsetProperty],
        }.RegisterInNameScope(scope));

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
}
