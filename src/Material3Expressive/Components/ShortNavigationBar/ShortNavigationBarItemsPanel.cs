using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// ShortNavigationBar.kt: EqualWeightContentMeasurePolicy / CenteredContentMeasurePolicy.
internal sealed class ShortNavigationBarItemsPanel : Panel
{
    public static readonly StyledProperty<double> ContainerMinHeightProperty = AvaloniaProperty.Register<ShortNavigationBarItemsPanel, double>(nameof(ContainerMinHeight), 64);
    public static readonly StyledProperty<ShortNavigationBarArrangement> ArrangementProperty = AvaloniaProperty.Register<ShortNavigationBarItemsPanel, ShortNavigationBarArrangement>(nameof(Arrangement));
    public double ContainerMinHeight { get => GetValue(ContainerMinHeightProperty); set => SetValue(ContainerMinHeightProperty, value); }
    public ShortNavigationBarArrangement Arrangement { get => GetValue(ArrangementProperty); set => SetValue(ArrangementProperty, value); }
    private int[] _widths = [];
    private int _height, _padding;
    static ShortNavigationBarItemsPanel() => AffectsMeasure<ShortNavigationBarItemsPanel>(ContainerMinHeightProperty, ArrangementProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var maximumHeight = double.IsFinite(availableSize.Height) ? MathUtils.DipToPx(availableSize.Height, scale) : int.MaxValue;
        var owner = this.GetVisualAncestors().OfType<ShortNavigationBar>().FirstOrDefault();
        // defaultMinSize only supplies its 64dp minimum when the incoming minimum is zero.
        var minimumHeight = owner is { Height: var explicitHeight } && double.IsFinite(explicitHeight)
            ? maximumHeight : Math.Min(MathUtils.DipToPx((float)(owner?.MinHeight > 0 ? owner.MinHeight : ContainerMinHeight), (float)scale), maximumHeight);
        _height = minimumHeight; _padding = 0; _widths = new int[Children.Count];
        if (Arrangement is not (ShortNavigationBarArrangement.EqualWeight or ShortNavigationBarArrangement.Centered))
            throw new ArgumentOutOfRangeException(nameof(Arrangement));
        foreach (var presenter in Children.OfType<ContentPresenter>()) presenter.UpdateChild();
        var items = Children.Select(child => child as ShortNavigationBarItem ?? (child as ContentPresenter)?.Child as ShortNavigationBarItem).ToArray();
        if (!double.IsFinite(availableSize.Width))
        {
            // Both policies return constraints.maxWidth as the bar width. Avalonia
            // cannot arrange an infinite width; supply a bounded host or Width.
            throw new InvalidOperationException("ShortNavigationBar requires a finite width constraint.");
        }
        var width = MathUtils.DipToPx(availableSize.Width, scale);
        if (Children.Count == 0) return new Size(width / scale, _height / scale);
        var maximumItemWidth = width / Children.Count;
        if (Arrangement == ShortNavigationBarArrangement.Centered && Children.Count <= 6)
            _padding = MathUtils.RoundToInt(((100 - 10 * (Children.Count + 3)) / 2f) / 100 * width);
        var minimumItemWidth = (width - _padding * 2) / Children.Count;
        for (var i = 0; i < Children.Count; i++)
        {
            if (items[i] is { } item) _height = Math.Max(_height, Math.Min(item.IntrinsicHeight(minimumItemWidth, scale), maximumHeight));
            else
            {
                Children[i].Measure(new Size(minimumItemWidth / scale, double.PositiveInfinity));
                _height = Math.Max(_height, Math.Min(MathUtils.DipToPx(Children[i].DesiredSize.Height, scale), maximumHeight));
            }
        }
        for (var i = 0; i < Children.Count; i++)
        {
            var currentWidth = minimumItemWidth;
            if (Arrangement == ShortNavigationBarArrangement.Centered)
            {
                int intrinsicWidth;
                if (items[i] is { } item) intrinsicWidth = item.IntrinsicWidth(minimumHeight, scale);
                else
                {
                    Children[i].Measure(new Size(double.PositiveInfinity, minimumHeight / scale));
                    intrinsicWidth = MathUtils.DipToPx(Children[i].DesiredSize.Width, scale);
                }
                if (currentWidth < intrinsicWidth)
                {
                    currentWidth = Math.Min(intrinsicWidth, maximumItemWidth);
                    _padding -= (currentWidth - minimumItemWidth) / 2;
                }
            }
            _widths[i] = currentWidth;
            items[i]?.Allocate(new Size(currentWidth / scale, _height / scale));
            Children[i].Measure(new Size(currentWidth / scale, _height / scale));
        }
        return new Size(width / scale, _height / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var x = _padding;
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Arrange(new Rect(x / scale, 0, _widths[i] / scale, _height / scale));
            x += _widths[i];
        }
        return finalSize;
    }
}
