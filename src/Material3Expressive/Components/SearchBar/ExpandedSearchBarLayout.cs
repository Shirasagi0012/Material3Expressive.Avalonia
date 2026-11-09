// SearchBar.kt / DockedSearchBarLayoutImpl: physical-pixel constraints and sequential slot measurement.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

internal class ExpandedSearchBarLayout : Panel
{
    public static readonly StyledProperty<double> MinimumResultsHeightProperty = DockedSearchResultsLayout.MinimumResultsHeightProperty.AddOwner<ExpandedSearchBarLayout>();
    public static readonly StyledProperty<double> MaximumWindowHeightFractionProperty = DockedSearchResultsLayout.MaximumWindowHeightFractionProperty.AddOwner<ExpandedSearchBarLayout>();
    public double MinimumResultsHeight { get => GetValue(MinimumResultsHeightProperty); set => SetValue(MinimumResultsHeightProperty, value); }
    public double MaximumWindowHeightFraction { get => GetValue(MaximumWindowHeightFractionProperty); set => SetValue(MaximumWindowHeightFractionProperty, value); }
    protected (Rect? Collapsed, Size Window, float Progress) Inputs;
    static ExpandedSearchBarLayout() => AffectsMeasure<ExpandedSearchBarLayout>(MinimumResultsHeightProperty, MaximumWindowHeightFractionProperty);
    internal void SetInputs(Rect? collapsed, Size window, float progress)
    {
        var inputs = (collapsed, window, progress);
        if (Inputs == inputs) return;
        Inputs = inputs; InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var maximum = MathUtils.DipToPx((float)Inputs.Window.Height * (float)MaximumWindowHeightFraction, (float)scale);
        var collapsedHeight = MathUtils.DipToPx(Inputs.Collapsed?.Height ?? 0, scale);
        var animatedMaximum = MathUtils.LerpRoundToInt(collapsedHeight, maximum, Math.Max(0, Inputs.Progress)) / scale;
        var maxHeight = Math.Min(availableSize.Height, animatedMaximum);
        var minHeight = Math.Min(maxHeight, Math.Min(maximum, MathUtils.DipToPx(MinimumResultsHeight, scale)) / scale);
        var maxWidth = Math.Min(availableSize.Width, Inputs.Collapsed?.Width ?? availableSize.Width);
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
    private Color _scrim;
    private float _progress;
    private SearchBarScrimButton? _scrimButton;
    private SearchBarPopupPositioner? _positioner;
    private IDisposable? _scrimBackground;
    internal event EventHandler? OutsidePressed;
    internal event EventHandler? ScrimInvoked;
    protected override bool BypassFlowDirectionPolicies => true;
    public SearchBarPopupPresenter()
    {
        // Like TooltipBox/Menu, opt in on an ancestor of the opacity-bearing layers.
        // Compose graphicsLayer alpha composites the entire subtree, including custom Skia draws.
        RenderOptions.SetRequiresFullOpacityHandling(this, true);
        EscapeOnKeyDown = true;
        AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (_scrimButton?.HasScrim == true || Children.Count == 0 ||
                (_positioner?.ContentBounds ?? Children[^1].Bounds).Contains(e.GetPosition(this))) return;
            e.Handled = true;
            OutsidePressed?.Invoke(this, EventArgs.Empty);
        }, RoutingStrategies.Bubble);
    }
    internal void SetScrimButton(SearchBarScrimButton? button, SearchBarPopupPositioner? positioner)
    {
        _scrimBackground?.Dispose(); _scrimBackground = null; _progress = float.NaN;
        if (_scrimButton is not null) _scrimButton.Click -= ScrimClicked;
        _scrimButton = button; _positioner = positioner;
        if (button is not null) button.Click += ScrimClicked;
    }
    private void ScrimClicked(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, _scrimButton)) ScrimInvoked?.Invoke(this, EventArgs.Empty);
    }
    internal void SetScrim(Color color, float progress)
    {
        if (_scrim == color && _progress == progress) return;
        _scrim = color; _progress = progress;
        if (_scrimButton is null) return;
        _scrimButton.HasScrim = color != Colors.Transparent;
        if (_positioner is not null) _positioner.SetPlacement(_anchor, _scrimButton.HasScrim);
        _scrimBackground?.Dispose();
        _scrimBackground = _scrimButton.SetValue(Button.BackgroundProperty, new SolidColorBrush(color, progress), BindingPriority.Animation);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _scrimBackground?.Dispose(); _scrimBackground = null; _progress = float.NaN;
        base.OnDetachedFromVisualTree(e);
    }
    internal void SetAnchor(Point anchor)
    {
        if (_anchor == anchor) return;
        _anchor = anchor; _positioner?.SetPlacement(anchor, _scrimButton?.HasScrim == true); InvalidateArrange();
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
        foreach (var child in Children)
            child.Arrange(ReferenceEquals(child, _scrimButton) ? new Rect(finalSize) : new Rect(point, child.DesiredSize));
        return finalSize;
    }
}

// The clickable scrim is an ancestor so its existing Surface indication draws after content.
// Without a scrim it remains a layout host; outside presses are handled by the modal presenter.
internal sealed class SearchBarScrimButton : Button
{
    public static readonly StyledProperty<bool> HasScrimProperty = AvaloniaProperty.Register<SearchBarScrimButton, bool>(nameof(HasScrim));
    public bool HasScrim { get => GetValue(HasScrimProperty); set => SetValue(HasScrimProperty, value); }
    protected override void OnPointerPressed(PointerPressedEventArgs e) { if (HasScrim) base.OnPointerPressed(e); }
    protected override void OnPointerReleased(PointerReleasedEventArgs e) { if (HasScrim) base.OnPointerReleased(e); }
    protected override void OnClick() { if (HasScrim) base.OnClick(); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HasScrimProperty) Focusable = HasScrim;
    }
}

internal sealed class SearchBarPopupPositioner : Decorator
{
    private Point _anchor;
    private bool _relative;
    internal Rect ContentBounds { get; private set; }
    protected override bool BypassFlowDirectionPolicies => true;
    internal void SetPlacement(Point anchor, bool relative)
    {
        if (_anchor == anchor && _relative == relative) return;
        _anchor = anchor; _relative = relative; InvalidateArrange();
    }
    protected override Size MeasureOverride(Size availableSize) { Child?.Measure(availableSize); return availableSize; }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            var point = new Point(MathUtils.DipToPx(_anchor.X, scale) / scale, MathUtils.DipToPx(_anchor.Y, scale) / scale);
            // Core's scrim uses relative Box.offset + TopStart. PopupPositionProvider is physical.
            if (_relative && FlowDirection == FlowDirection.RightToLeft)
                point = new Point(finalSize.Width - child.DesiredSize.Width - point.X, point.Y);
            ContentBounds = new Rect(point, child.DesiredSize);
            child.Arrange(ContentBounds);
        }
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
