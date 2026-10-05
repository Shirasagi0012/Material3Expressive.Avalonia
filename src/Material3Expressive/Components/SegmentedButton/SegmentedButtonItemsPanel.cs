using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Row.width(IntrinsicSize.Min), weight(1f), spacedBy(-space), CenterVertically.
internal sealed class SegmentedButtonItemsPanel : Panel
{
    public static readonly StyledProperty<double> ContainerMinHeightProperty = AvaloniaProperty.Register<SegmentedButtonItemsPanel, double>(nameof(ContainerMinHeight));
    public double ContainerMinHeight { get => GetValue(ContainerMinHeightProperty); set => SetValue(ContainerMinHeightProperty, value); }
    static SegmentedButtonItemsPanel() => AffectsMeasure<SegmentedButtonItemsPanel>(ContainerMinHeightProperty);
    private int[] _widths = [];
    protected override Size MeasureOverride(Size availableSize)
    {
        var row = this.GetVisualAncestors().OfType<SegmentedButtonRow>().First();
        var scale = LayoutHelper.GetLayoutScale(this);
        foreach (var presenter in Children.OfType<ContentPresenter>()) presenter.ApplyTemplate();
        var items = Children.Select(child => child as SegmentedButton ?? (child as ContentPresenter)?.Child as SegmentedButton).ToArray();
        var overlap = MathUtils.DipToPx(row.Space, scale);
        var maxIntrinsic = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            if (items[i] is { } item)
            {
                var shape = row.BaseShape;
                if (Children.Count > 1) shape = i == 0 ? new CornerRadius(shape.TopLeft, 0, 0, shape.BottomLeft)
                    : i == Children.Count - 1 ? new CornerRadius(0, shape.TopRight, shape.BottomRight, 0) : default;
                if (row.FlowDirection == FlowDirection.RightToLeft)
                    shape = new CornerRadius(shape.TopRight, shape.TopLeft, shape.BottomLeft, shape.BottomRight);
                item.RowCornerRadius = shape;
                maxIntrinsic = Math.Max(maxIntrinsic, MathUtils.DipToPx(item.IntrinsicSize(availableSize.Height).Width, scale));
            }
            else
            {
                Children[i].Measure(Size.Infinity);
                maxIntrinsic = Math.Max(maxIntrinsic, MathUtils.DipToPx(Children[i].DesiredSize.Width, scale));
            }
        }
        var naturalWidth = Math.Max(0, maxIntrinsic * Children.Count - overlap * Math.Max(0, Children.Count - 1));
        var width = double.IsFinite(row.Width) ? MathUtils.DipToPx(availableSize.Width, scale)
            : Math.Min(naturalWidth, double.IsFinite(availableSize.Width) ? MathUtils.DipToPx(availableSize.Width, scale) : int.MaxValue);
        width = Math.Max(width, MathUtils.DipToPx(row.MinWidth, scale));
        _widths = new int[Children.Count];
        var total = Math.Max(0, width + overlap * Math.Max(0, Children.Count - 1));
        var unit = Children.Count == 0 ? 0 : total / (float)Children.Count;
        var remainder = total - MathUtils.RoundToInt(unit) * Children.Count;
        var height = Math.Min(ContainerMinHeight, availableSize.Height);
        for (var i = 0; i < Children.Count; i++)
        {
            var correction = Math.Sign(remainder); remainder -= correction;
            _widths[i] = Math.Max(0, MathUtils.RoundToInt(unit) + correction);
            Children[i].Measure(new Size(_widths[i] / scale, availableSize.Height));
            height = Math.Max(height, Children[i].DesiredSize.Height);
        }
        return new Size(width / scale, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var row = this.GetVisualAncestors().OfType<SegmentedButtonRow>().First();
        var scale = LayoutHelper.GetLayoutScale(this);
        var overlap = MathUtils.DipToPx(row.Space, scale);
        var x = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var height = MathUtils.DipToPx(child.DesiredSize.Height, scale);
            var y = MathUtils.RoundToInt((finalSize.Height * scale - height) / 2);
            child.Arrange(new Rect(x / scale, y / scale, _widths[i] / scale, height / scale));
            x += _widths[i] - overlap;
        }
        return finalSize;
    }
}
