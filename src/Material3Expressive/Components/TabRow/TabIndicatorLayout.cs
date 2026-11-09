using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>
/// An indicator template root that owns its measurement, placement and animation.
/// This is the layout hook corresponding to Material's tabIndicatorLayout.
/// </summary>
/// <remarks>
/// Positions and child arrangement use start-relative DIP coordinates. The row supplies
/// the selected tab's constraints, then applies the upstream reported-size coercion,
/// bottom placement and scrolling. Return the actual layout size, even when it exceeds
/// <see cref="MaximumSize"/>; the row retains that size instead of clipping its measurement.
/// </remarks>
public abstract class TabIndicatorLayout : Decorator
{
    /// <summary>The current tab geometry, measured before this indicator.</summary>
    public IReadOnlyList<TabPosition> TabPositions { get; private set; } = Array.Empty<TabPosition>();
    public int SelectedIndex { get; private set; }
    public bool IsScrollable { get; private set; }
    /// <summary>Minimum width and height supplied by the row, in DIP.</summary>
    public Size MinimumSize { get; private set; }
    /// <summary>Maximum width and height supplied by the row, in DIP.</summary>
    public Size MaximumSize { get; private set; }
    internal Size MeasuredSize { get; private set; }
    private bool _hasLayoutContext;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // A new template root can be created after the row's preceding measure pass.
        // Request a row measure so the layout callback never observes stale/missing positions.
        if (this.GetVisualAncestors().OfType<TabRowChrome>().FirstOrDefault() is { } chrome)
        {
            chrome.Content.InvalidateMeasure();
            chrome.InvalidateMeasure();
        }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _hasLayoutContext = false;
        base.OnDetachedFromVisualTree(e);
    }

    internal void SetLayoutContext(IReadOnlyList<TabPosition> positions, int selectedIndex,
        bool scrollable, Size minimum, Size maximum)
    {
        if (_hasLayoutContext && SelectedIndex == selectedIndex && IsScrollable == scrollable && MinimumSize == minimum &&
            MaximumSize == maximum && TabPositions.SequenceEqual(positions)) return;
        _hasLayoutContext = true;
        TabPositions = positions;
        SelectedIndex = selectedIndex;
        IsScrollable = scrollable;
        MinimumSize = minimum;
        MaximumSize = maximum;
        InvalidateMeasure();
    }

    protected sealed override Size MeasureOverride(Size availableSize)
    {
        MeasuredSize = _hasLayoutContext ? MeasureIndicator(availableSize) : default;
        return MeasuredSize;
    }

    /// <summary>Measure <see cref="Decorator.Child"/> and return the indicator's actual layout size.</summary>
    protected abstract Size MeasureIndicator(Size availableSize);

    protected sealed override Size ArrangeOverride(Size finalSize) => _hasLayoutContext ? ArrangeIndicator(finalSize) : finalSize;

    /// <summary>Place the child in the actual layout size using start-relative coordinates.</summary>
    protected abstract Size ArrangeIndicator(Size finalSize);
}
