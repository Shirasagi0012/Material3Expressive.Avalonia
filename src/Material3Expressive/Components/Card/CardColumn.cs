using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

/// <summary>
/// Card.kt's default Column: top-to-bottom placement, start alignment and no spacing.
/// Unweighted children consume height first; CardColumnItem provides explicit Box + weight hosts.
/// </summary>
public sealed class CardColumn : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        if (!Children.OfType<CardColumnItem>().Any(item => item.IsVisible && item.Weight > 0))
        {
            // Preserve the original direct-child path, including fractional DIP sizes.
            double height = 0, naturalWidth = 0;
            foreach (var child in Children)
            {
                var available = new Size(availableSize.Width, Math.Max(0, availableSize.Height - height));
                if (child is CardColumnItem item) item.MeasureInColumn(available, 0);
                else child.Measure(available);
                height += child.DesiredSize.Height;
                naturalWidth = Math.Max(naturalWidth, child.DesiredSize.Width);
            }
            return new Size(naturalWidth, height);
        }
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => double.IsPositiveInfinity(value) ? int.MaxValue : MathUtils.DipToPx(value, scale);
        var maxHeight = Px(availableSize.Height);
        var minHeight = Math.Min(maxHeight, Px(double.IsNaN(Height) ? MinHeight : Math.Max(Height, MinHeight)));
        var totalWeight = 0f;
        var fixedHeight = 0;
        double width = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) { child.Measure(default); continue; }
            if (child is CardColumnItem { Weight: > 0 } item)
            {
                totalWeight += (float)Math.Min(item.Weight, float.MaxValue);
                continue;
            }
            var available = new Size(availableSize.Width, maxHeight == int.MaxValue
                ? double.PositiveInfinity : Math.Max(0, maxHeight - fixedHeight) / scale);
            if (child is CardColumnItem unweighted) unweighted.MeasureInColumn(available, 0);
            else child.Measure(available);
            fixedHeight += Px(child.DesiredSize.Height);
            width = Math.Max(width, child.DesiredSize.Width);
        }
        var weightedHeight = 0;
        if (totalWeight > 0)
        {
            // RowColumnMeasurePolicy: Float weights, physical-pixel rounding, then
            // distribute +/- one pixel to the first weighted items to exhaust the remainder.
            var remaining = Math.Max(0, (maxHeight == int.MaxValue ? minHeight : maxHeight) - fixedHeight);
            var unit = remaining / totalWeight;
            var weighted = Children.OfType<CardColumnItem>().Where(item => item.IsVisible && item.Weight > 0).ToArray();
            var sizes = weighted.Select(item => MathUtils.RoundToInt(unit * (float)Math.Min(item.Weight, float.MaxValue))).ToArray();
            var remainder = remaining - sizes.Sum();
            for (var i = 0; i < weighted.Length; i++)
            {
                var correction = Math.Sign(remainder);
                remainder -= correction;
                var allocation = Math.Max(0, sizes[i] + correction) / scale;
                var item = weighted[i];
                item.MeasureInColumn(new Size(availableSize.Width, allocation), item.Fill ? allocation : 0);
                weightedHeight += Px(item.DesiredSize.Height);
                width = Math.Max(width, item.DesiredSize.Width);
            }
        }
        return new Size(width, Math.Max(fixedHeight + weightedHeight, minHeight) / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        foreach (var child in Children)
        {
            if (child is CardColumnItem item)
            {
                var scale = LayoutHelper.GetLayoutScale(this);
                var remaining = MathUtils.DipToPx(finalSize.Width - child.DesiredSize.Width, scale);
                var x = item.Alignment switch
                {
                    CardColumnAlignment.Center => (FlowDirection == FlowDirection.RightToLeft
                        ? remaining - MathUtils.RoundToInt(remaining / 2f)
                        : MathUtils.RoundToInt(remaining / 2f)) / scale,
                    CardColumnAlignment.End => remaining / scale,
                    _ => 0
                };
                // Arrange coordinates are logical; Avalonia mirrors the visual tree for RTL.
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height;
                continue;
            }
            // Stretch is Avalonia's default. Here it means Column's default Start; an
            // explicit Center/Right uses the full allocation for ColumnScope.align.
            var width = child.HorizontalAlignment is HorizontalAlignment.Stretch or HorizontalAlignment.Left
                ? child.DesiredSize.Width : finalSize.Width;
            child.Arrange(new Rect(0, y, width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }
}
