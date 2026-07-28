using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Material3Expressive.Controls;

/// <summary>
/// Lays out Material navigation destinations using the same fixed item geometry as Compose.
/// </summary>
/// <remarks>
/// <para>
/// A <c>UniformGrid</c> is close for a bottom bar, but it cannot express Compose's
/// weighted-item remainder distribution, inter-item spacing, nor a rail's 80dp-wide, 56dp-tall
/// destinations aligned from the top. Keeping the layout here also means the same item template
/// can serve bar and rail variants.
/// </para>
/// <para>
/// The figures come from NavigationBar.kt's <c>NavigationBarItemHorizontalPadding</c> and
/// NavigationRail.kt's <c>NavigationRailVerticalPadding</c>,
/// <c>NavigationRailItemWidth</c>, and <c>NavigationRailItemHeight</c>. They are expressed in
/// Avalonia logical pixels, which are dp under the parity test's scale contract.
/// </para>
/// <para>
/// Compose measures layout in integral physical pixels. In particular, RowColumnMeasurePolicy
/// rounds each weighted share then gives any remainder to the first weighted children. Arranging
/// equal fractional logical rectangles delegates that decision to Avalonia and shifts centred
/// labels by a physical pixel at high DPI. This panel performs the same allocation before
/// converting the resulting pixel slots back to Avalonia coordinates.
/// </para>
/// </remarks>
public sealed class MaterialNavigationItemsPanel : Panel
{
    /// <summary>Whether destinations are arranged as a vertical navigation rail.</summary>
    public static readonly StyledProperty<bool> IsVerticalProperty =
        AvaloniaProperty.Register<MaterialNavigationItemsPanel, bool>(nameof(IsVertical));

    /// <summary>The space between adjacent destinations.</summary>
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<MaterialNavigationItemsPanel, double>(nameof(ItemSpacing));

    private const double RailItemWidth = 80;
    private const double RailItemHeight = 56;
    private const double RailVerticalPadding = 4;

    static MaterialNavigationItemsPanel()
    {
        AffectsMeasure<MaterialNavigationItemsPanel>(IsVerticalProperty, ItemSpacingProperty);
    }

    /// <inheritdoc cref="IsVerticalProperty" />
    public bool IsVertical
    {
        get => GetValue(IsVerticalProperty);
        set => SetValue(IsVerticalProperty, value);
    }

    /// <inheritdoc cref="ItemSpacingProperty" />
    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var visibleCount = Children.Count(child => child.IsVisible);
        if (visibleCount == 0)
            return default;

        if (IsVertical)
        {
            var verticalScale = GetScale();
            var railItemSize = new Size(
                ToLogicalPixels(ToComposePixels(RailItemWidth, verticalScale), verticalScale),
                ToLogicalPixels(ToComposePixels(RailItemHeight, verticalScale), verticalScale));

            foreach (var child in Children)
            {
                if (child.IsVisible)
                    child.Measure(railItemSize);
            }

            return new Size(
                railItemSize.Width,
                ToLogicalPixels(
                    ToComposePixels(RailVerticalPadding * 2 + RailItemHeight * visibleCount + ItemSpacing * (visibleCount - 1), verticalScale),
                    verticalScale));
        }

        var scale = GetScale();
        var slots = double.IsInfinity(availableSize.Width)
            ? Array.Empty<int>()
            : GetWeightedSlots(availableSize.Width, visibleCount, scale);
        var height = double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : availableSize.Height;
        var slotIndex = 0;

        foreach (var child in Children)
        {
            if (child.IsVisible)
            {
                var width = slots.Length == 0
                    ? 0
                    : ToLogicalPixels(slots[slotIndex++], scale);
                child.Measure(new Size(width, height));
            }
        }

        if (!double.IsInfinity(availableSize.Width) && !double.IsInfinity(availableSize.Height))
            return availableSize;

        var desiredHeight = Children.Where(child => child.IsVisible).Select(child => child.DesiredSize.Height).DefaultIfEmpty().Max();
        var desiredWidth = Children.Where(child => child.IsVisible).Sum(child => child.DesiredSize.Width) +
            ItemSpacing * (visibleCount - 1);
        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return finalSize;

        if (IsVertical)
        {
            var scale = GetScale();
            var finalWidth = ToComposePixels(finalSize.Width, scale);
            var itemWidth = ToComposePixels(RailItemWidth, scale);
            var itemHeight = ToComposePixels(RailItemHeight, scale);
            var x = Math.Max(0, (finalWidth - itemWidth) / 2);
            var y = ToComposePixels(RailVerticalPadding, scale);
            var itemSpacing = ToComposePixels(ItemSpacing, scale);

            foreach (var child in visibleChildren)
            {
                child.Arrange(new Rect(
                    ToLogicalPixels(x, scale),
                    ToLogicalPixels(y, scale),
                    ToLogicalPixels(itemWidth, scale),
                    ToLogicalPixels(itemHeight, scale)));
                y += itemHeight + itemSpacing;
            }

            return finalSize;
        }

        var horizontalScale = GetScale();
        var slots = GetWeightedSlots(finalSize.Width, visibleChildren.Length, horizontalScale);
        var itemSpacingPx = ToComposePixels(ItemSpacing, horizontalScale);
        var left = 0;

        for (var index = 0; index < visibleChildren.Length; index++)
        {
            var width = slots[index];
            visibleChildren[index].Arrange(new Rect(
                ToLogicalPixels(left, horizontalScale),
                0,
                ToLogicalPixels(width, horizontalScale),
                finalSize.Height));
            left += width + itemSpacingPx;
        }

        return finalSize;
    }

    /// <summary>
    /// Gets the same physical-pixel widths as Compose's RowColumnMeasurePolicy for equally
    /// weighted children. The policy rounds each share and then allocates the rounding remainder
    /// to the first children in source order.
    /// </summary>
    private int[] GetWeightedSlots(double availableWidth, int count, double scale)
    {
        var available = ToComposePixels(availableWidth, scale);
        var spacing = ToComposePixels(ItemSpacing, scale);
        var target = Math.Max(0, available - spacing * (count - 1));
        var roundedShare = ToComposePixels(target / (double)count, 1);
        var remainder = target - roundedShare * count;
        var slots = new int[count];

        for (var index = 0; index < count; index++)
        {
            var adjustment = Math.Sign(remainder);
            remainder -= adjustment;
            slots[index] = Math.Max(0, roundedShare + adjustment);
        }

        return slots;
    }

    private double GetScale()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        return scale > 0 && !double.IsNaN(scale) && !double.IsInfinity(scale) ? scale : 1;
    }

    private static int ToComposePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));

    private static double ToLogicalPixels(int value, double scale) => value / scale;
}
