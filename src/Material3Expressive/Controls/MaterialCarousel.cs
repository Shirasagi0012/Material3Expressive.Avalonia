using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A carousel: a row of items where the ones at the edges are squeezed rather than clipped.
/// </summary>
/// <remarks>
/// The squeeze is what makes this a carousel rather than a scrolling row. An item near the edge
/// narrows towards <c>SmallItemWidth</c> instead of being cut off, so the row always ends on a
/// whole item and the edge reads as a boundary the content is passing through.
/// <para>
/// The scroll position is a spring, so a flick carries and a change of item mid-scroll redirects
/// rather than restarting. Widths are recomputed from that position every frame, which is why the
/// squeeze follows the scroll continuously instead of snapping between states.
/// </para>
/// <para>
/// Upstream ships no generated token file for the carousel; the metrics here come from the spec
/// and are marked as such in the table.
/// </para>
/// </remarks>
public class MaterialCarousel : Panel
{
    public static readonly StyledProperty<CarouselVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialCarousel, CarouselVariant>(nameof(Variant));

    /// <inheritdoc cref="Offset" />
    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<MaterialCarousel, double>(nameof(Offset));

    private readonly MaterialThemeWatcher _theme;
    private readonly MotionGroup _motion;

    private SpringAnimation? _scroll;
    private CarouselTokens _tokens = CarouselTokens.MultiBrowse;

    public MaterialCarousel()
    {
        _theme = new MaterialThemeWatcher(this);
        _motion = new MotionGroup(this);
        _motion.Ticked += (_, _) => InvalidateArrange();

        _theme.Changed += (_, _) => ApplyTokens();
        ApplyTokens();
    }

    static MaterialCarousel()
    {
        AffectsArrange<MaterialCarousel>(VariantProperty, OffsetProperty);
        ClipToBoundsProperty.OverrideDefaultValue<MaterialCarousel>(true);
    }

    /// <summary>Multi-browse or hero.</summary>
    public CarouselVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>How far along the row is, in pixels from the start.</summary>
    public double Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    private SpringAnimation Scroll =>
        _scroll ??= _motion.AddSpring(Offset, _theme.Spring(MotionRole.DefaultSpatial));

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(_tokens.LargeItemWidth, _tokens.ItemHeight));

        return new Size(
            double.IsInfinity(availableSize.Width) ? _tokens.LargeItemWidth * 3 : availableSize.Width,
            _tokens.ItemHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scroll = Scroll.Value;
        var x = -scroll;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            var width = WidthAt(x, finalSize.Width);
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width + _tokens.ItemSpacing;
        }

        return finalSize;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        // A vertical wheel scrolls the row: a carousel is horizontal, and asking for shift is
        // a worse answer than doing the obvious thing.
        var delta = (e.Delta.X != 0 ? e.Delta.X : e.Delta.Y) * -(_tokens.LargeItemWidth / 2);
        Offset = Math.Max(0, Offset + delta);
        e.Handled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OffsetProperty)
        {
            Scroll.AnimateTo(Offset, _theme.Spring(MotionRole.DefaultSpatial));
            _motion.Start();
        }
        else if (change.Property == VariantProperty)
        {
            ApplyTokens();
        }
    }

    /// <summary>
    /// How wide an item at <paramref name="x" /> should be.
    /// </summary>
    /// <remarks>
    /// Full width in the middle, narrowing to the small width as it approaches either edge. The
    /// transition happens over one large-item width, so an item is fully squeezed by the time it
    /// leaves rather than jumping at the boundary.
    /// </remarks>
    private double WidthAt(double x, double viewport)
    {
        var large = _tokens.LargeItemWidth;
        var small = _tokens.SmallItemWidth;

        // How far into the viewport the item's leading edge is, as a fraction of a large item.
        var fromStart = Math.Clamp(x / large, 0, 1);
        var fromEnd = Math.Clamp((viewport - x - large) / large, 0, 1);
        var openness = Math.Min(fromStart, fromEnd);

        return small + (large - small) * openness;
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.GetCarouselTokens(Variant);
        InvalidateMeasure();
    }
}
