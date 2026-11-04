// SearchBar.kt / DockedSearchBar: Column, preferred width and window-relative results height.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class DockedSearchBarLayout : Panel
{
    public static readonly StyledProperty<double> DefaultWidthProperty =
        AvaloniaProperty.Register<DockedSearchBarLayout, double>(nameof(DefaultWidth));
    public double DefaultWidth { get => GetValue(DefaultWidthProperty); set => SetValue(DefaultWidthProperty, value); }
    static DockedSearchBarLayout() => AffectsMeasure<DockedSearchBarLayout>(DefaultWidthProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var width = double.IsNaN(Width)
            ? Math.Min(Math.Max(MinWidth, MathUtils.PxToDip(MathUtils.DipToPx(DefaultWidth, scale), scale)), availableSize.Width)
            : availableSize.Width;
        var height = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(width, Math.Max(0, availableSize.Height - height)));
            height += child.DesiredSize.Height;
        }
        return new(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = 0d;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }
}

internal sealed class DockedSearchResultsLayout : Panel
{
    public static readonly StyledProperty<double> MinimumResultsHeightProperty =
        AvaloniaProperty.Register<DockedSearchResultsLayout, double>(nameof(MinimumResultsHeight));
    public static readonly StyledProperty<double> MaximumWindowHeightFractionProperty =
        AvaloniaProperty.Register<DockedSearchResultsLayout, double>(nameof(MaximumWindowHeightFraction));
    public double MinimumResultsHeight { get => GetValue(MinimumResultsHeightProperty); set => SetValue(MinimumResultsHeightProperty, value); }
    public double MaximumWindowHeightFraction { get => GetValue(MaximumWindowHeightFractionProperty); set => SetValue(MaximumWindowHeightFractionProperty, value); }
    private TopLevel? _topLevel;
    static DockedSearchResultsLayout() => AffectsMeasure<DockedSearchResultsLayout>(MinimumResultsHeightProperty, MaximumWindowHeightFractionProperty);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null) _topLevel.PropertyChanged += WindowChanged;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_topLevel is not null) _topLevel.PropertyChanged -= WindowChanged;
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void WindowChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty) InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        double Pixel(float value) => MathUtils.PxToDip(MathUtils.DipToPx(value, (float)scale), scale);
        var windowMaximum = Pixel((float)(_topLevel?.ClientSize.Height ?? 0) * (float)MaximumWindowHeightFraction);
        var maximum = Math.Min(availableSize.Height, windowMaximum);
        var minimum = Math.Min(maximum, Pixel((float)MinimumResultsHeight));
        var height = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, Math.Max(0, maximum - height)));
            height += child.DesiredSize.Height;
        }
        return new(availableSize.Width, Math.Clamp(height, minimum, maximum));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = 0d;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }
}
