using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// RowColumnMeasurePolicy's weight(1f, fill=true) and Arrangement.spacedBy(8.dp).
internal sealed class NavigationBarItemsPanel : Panel
{
    public static readonly StyledProperty<double> ContainerMinHeightProperty =
        AvaloniaProperty.Register<NavigationBarItemsPanel, double>(nameof(ContainerMinHeight), 80);
    public double ContainerMinHeight { get => GetValue(ContainerMinHeightProperty); set => SetValue(ContainerMinHeightProperty, value); }
    private int[] _widths = [];
    private int _height;

    static NavigationBarItemsPanel() => AffectsMeasure<NavigationBarItemsPanel>(ContainerMinHeightProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var spacing = MathUtils.DipToPx(8f, (float)scale);
        var maximumWidth = double.IsFinite(availableSize.Width) ? MathUtils.DipToPx(availableSize.Width, scale) : int.MaxValue;
        var maximumHeight = double.IsFinite(availableSize.Height) ? MathUtils.DipToPx(availableSize.Height, scale) : int.MaxValue;
        _widths = new int[Children.Count];
        _height = Math.Min(MathUtils.DipToPx((float)ContainerMinHeight, (float)scale), maximumHeight);
        // Arbitrary Row content has no weight. Data templates may wrap a destination in a presenter.
        foreach (var presenter in Children.OfType<ContentPresenter>()) presenter.UpdateChild();
        var weighted = Children.Select(child => child is NavigationBarItem || child.GetVisualDescendants().OfType<NavigationBarItem>().Any()).ToArray();
        var weightCount = weighted.Count(value => value);
        var fixedSpace = 0;
        var lastSpacing = 0;
        for (var index = 0; index < Children.Count; index++)
        {
            if (weighted[index]) continue;
            var child = Children[index];
            child.Measure(new Size(maximumWidth == int.MaxValue ? double.PositiveInfinity : Math.Max(0, maximumWidth - fixedSpace) / scale, availableSize.Height));
            _widths[index] = MathUtils.DipToPx(child.DesiredSize.Width, scale);
            lastSpacing = Math.Min(spacing, Math.Max(0, maximumWidth - fixedSpace - _widths[index]));
            fixedSpace += _widths[index] + lastSpacing;
            _height = Math.Max(_height, MathUtils.DipToPx(child.DesiredSize.Height, scale));
        }
        var weightedSpace = 0;
        if (weightCount == 0) fixedSpace -= lastSpacing;
        else
        {
            var target = maximumWidth == int.MaxValue ? 0 : maximumWidth;
            var totalSpacing = spacing * (weightCount - 1);
            var remaining = Math.Max(0, target - fixedSpace - totalSpacing);
            var unit = (float)remaining / weightCount;
            var remainder = remaining - MathUtils.RoundToInt(unit) * weightCount;
            for (var index = 0; index < Children.Count; index++)
            {
                if (!weighted[index]) continue;
                var remainderUnit = Math.Sign(remainder);
                remainder -= remainderUnit;
                var width = Math.Max(0, MathUtils.RoundToInt(unit) + remainderUnit);
                var child = Children[index];
                child.Measure(new Size(width / scale, availableSize.Height));
                _widths[index] = width;
                weightedSpace += width;
                _height = Math.Max(_height, MathUtils.DipToPx(child.DesiredSize.Height, scale));
            }
            weightedSpace = Math.Clamp(weightedSpace + totalSpacing, 0, Math.Max(0, maximumWidth - fixedSpace));
        }
        var widthResult = maximumWidth == int.MaxValue ? fixedSpace + weightedSpace : maximumWidth;
        return new Size(widthResult / scale, Math.Min(_height, maximumHeight) / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var width = MathUtils.DipToPx(finalSize.Width, scale);
        var height = MathUtils.DipToPx(finalSize.Height, scale);
        var spacing = MathUtils.DipToPx(8f, (float)scale);
        var occupied = 0;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var childWidth = _widths[index];
            // SpacedAligned clamps spacing when the row cannot fit all fixed children.
            var position = Math.Min(occupied, Math.Max(0, width - childWidth));
            var gap = Math.Min(spacing, width - position - childWidth);
            var childHeight = MathUtils.DipToPx(child.DesiredSize.Height, scale);
            var y = MathUtils.RoundToInt((height - childHeight) / 2f);
            child.Arrange(new Rect(position / scale, y / scale, childWidth / scale, childHeight / scale));
            occupied = position + childWidth + gap;
        }
        return finalSize;
    }
}
