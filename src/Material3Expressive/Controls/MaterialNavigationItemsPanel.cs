using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Controls;

/// <summary>
/// Lays out Material navigation destinations using the same fixed item geometry as Compose.
/// </summary>
/// <remarks>
/// <para>
/// A <c>UniformGrid</c> is close for a bottom bar, but it cannot express Compose's
/// inter-item spacing, nor a rail's 80dp-wide, 56dp-tall destinations aligned from the top.
/// Keeping the layout here also means the same item template can serve bar and rail variants.
/// </para>
/// <para>
/// The figures come from NavigationBar.kt's <c>NavigationBarItemHorizontalPadding</c> and
/// NavigationRail.kt's <c>NavigationRailVerticalPadding</c>,
/// <c>NavigationRailItemWidth</c>, and <c>NavigationRailItemHeight</c>. They are expressed in
/// Avalonia logical pixels, which are dp under the parity test's scale contract.
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
            foreach (var child in Children)
            {
                if (child.IsVisible)
                    child.Measure(new Size(RailItemWidth, RailItemHeight));
            }

            return new Size(
                RailItemWidth,
                RailVerticalPadding * 2 + RailItemHeight * visibleCount + ItemSpacing * (visibleCount - 1));
        }

        var width = double.IsInfinity(availableSize.Width)
            ? 0
            : Math.Max(0, (availableSize.Width - ItemSpacing * (visibleCount - 1)) / visibleCount);
        var height = double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : availableSize.Height;

        foreach (var child in Children)
        {
            if (child.IsVisible)
                child.Measure(new Size(width, height));
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
            var x = Math.Max(0, (finalSize.Width - RailItemWidth) / 2);
            var y = RailVerticalPadding;

            foreach (var child in visibleChildren)
            {
                child.Arrange(new Rect(x, y, RailItemWidth, RailItemHeight));
                y += RailItemHeight + ItemSpacing;
            }

            return finalSize;
        }

        var width = Math.Max(0, (finalSize.Width - ItemSpacing * (visibleChildren.Length - 1)) / visibleChildren.Length);
        var left = 0d;

        foreach (var child in visibleChildren)
        {
            child.Arrange(new Rect(left, 0, width, finalSize.Height));
            left += width + ItemSpacing;
        }

        return finalSize;
    }
}
