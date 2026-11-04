// SearchBar.kt / DockedSearchBarLayoutImpl: physical-pixel constraints and sequential slot measurement.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

internal sealed class ExpandedSearchBarLayout : Panel
{
    public static readonly StyledProperty<double> MinimumResultsHeightProperty = DockedSearchResultsLayout.MinimumResultsHeightProperty.AddOwner<ExpandedSearchBarLayout>();
    public static readonly StyledProperty<double> MaximumWindowHeightFractionProperty = DockedSearchResultsLayout.MaximumWindowHeightFractionProperty.AddOwner<ExpandedSearchBarLayout>();
    public double MinimumResultsHeight { get => GetValue(MinimumResultsHeightProperty); set => SetValue(MinimumResultsHeightProperty, value); }
    public double MaximumWindowHeightFraction { get => GetValue(MaximumWindowHeightFractionProperty); set => SetValue(MaximumWindowHeightFractionProperty, value); }
    private (Size? Collapsed, Size Window, float Progress) _inputs;
    static ExpandedSearchBarLayout() => AffectsMeasure<ExpandedSearchBarLayout>(MinimumResultsHeightProperty, MaximumWindowHeightFractionProperty);
    internal void SetInputs(Size? collapsed, Size window, float progress)
    {
        var inputs = (collapsed, window, progress);
        if (_inputs == inputs) return;
        _inputs = inputs; InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var maximum = MathUtils.DipToPx((float)_inputs.Window.Height * (float)MaximumWindowHeightFraction, (float)scale);
        var collapsedHeight = MathUtils.DipToPx(_inputs.Collapsed?.Height ?? 0, scale);
        var animatedMaximum = MathUtils.LerpRoundToInt(collapsedHeight, maximum, Math.Max(0, _inputs.Progress)) / scale;
        var maxHeight = Math.Min(availableSize.Height, animatedMaximum);
        var minHeight = Math.Min(maxHeight, Math.Min(maximum, MathUtils.DipToPx(MinimumResultsHeight, scale)) / scale);
        var maxWidth = Math.Min(availableSize.Width, _inputs.Collapsed?.Width ?? availableSize.Width);
        var input = Children[0];
        input.Measure(new Size(maxWidth, maxHeight));
        var results = Children[1];
        results.Measure(new Size(input.DesiredSize.Width, Math.Max(0, maxHeight - input.DesiredSize.Height)));
        return new(Math.Max(input.DesiredSize.Width, results.DesiredSize.Width), Math.Clamp(input.DesiredSize.Height + results.DesiredSize.Height, minHeight, maxHeight));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Children[0].Arrange(new Rect(default(Point), Children[0].DesiredSize));
        Children[1].Arrange(new Rect(new Point(0, Children[0].DesiredSize.Height), Children[1].DesiredSize));
        return finalSize;
    }
}

// The existing modal presenter supplies focus isolation, nested-popup ownership and restoration.
// Only the search-specific physical position and outside-press timing live here.
internal sealed class SearchBarPopupPresenter : ModalPresenter
{
    private Point _anchor;
    internal event EventHandler? OutsidePressed;
    protected override bool BypassFlowDirectionPolicies => true;
    public SearchBarPopupPresenter()
    {
        EscapeOnKeyDown = true;
        AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (Children.Count == 0 || Children[0].Bounds.Contains(e.GetPosition(this))) return;
            e.Handled = true;
            OutsidePressed?.Invoke(this, EventArgs.Empty);
        }, RoutingStrategies.Bubble);
    }
    internal void SetAnchor(Point anchor)
    {
        if (_anchor == anchor) return;
        _anchor = anchor; InvalidateArrange();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(availableSize);
        return availableSize;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var point = new Point(MathUtils.DipToPx(_anchor.X, scale) / scale, MathUtils.DipToPx(_anchor.Y, scale) / scale);
        foreach (var child in Children) child.Arrange(new Rect(point, child.DesiredSize));
        return finalSize;
    }
}

internal sealed class SearchResultsColumn : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0d; var width = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - height)));
            height += child.DesiredSize.Height; width = Math.Max(width, child.DesiredSize.Width);
        }
        return new(width, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = 0d;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height)); y += child.DesiredSize.Height;
        }
        return finalSize;
    }
}
