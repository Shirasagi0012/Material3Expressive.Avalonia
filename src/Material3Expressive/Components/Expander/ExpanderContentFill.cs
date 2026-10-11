using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ComposeAnimation;

namespace Material3Expressive.Components;

// Stretches Expander content across the expansion axis (width for Down/Up, height for Left/Right)
// to the Expander's arranged size, without making that size part of the Expander's desired size.
//
// AnimatedVisibility lays an expanding/shrinking child out at the child's own desired size, as
// Compose does, so the arranged size cannot reach the content through it. The host sits outside
// the AnimatedVisibility and learns the arranged cross size; the fill inside reports that size as
// its desired size so AnimatedVisibility lays it out stretched. The host's own desired size leaves
// that extra out, so the Expander still measures to max(header, content) as in Fluent.
internal sealed class ExpanderContentHost : Decorator
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ExpanderContentHost, Orientation>(nameof(Orientation), Orientation.Vertical);

    static ExpanderContentHost() => AffectsMeasure<ExpanderContentHost>(OrientationProperty);

    // The expansion axis: Vertical stretches the width, Horizontal the height.
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    private ExpanderContentFill? Fill =>
        Child is AnimatedVisibility { IsContentPresent: true, Content: ExpanderContentFill fill } ? fill : null;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child) return default;
        child.Measure(availableSize);
        var desired = child.DesiredSize;
        if (Fill is not { } fill) return desired;
        var stretch = fill.DesiredSize - fill.Natural;
        return Orientation == Orientation.Vertical
            ? desired.WithWidth(Math.Max(0, desired.Width - Math.Max(0, stretch.Width)))
            : desired.WithHeight(Math.Max(0, desired.Height - Math.Max(0, stretch.Height)));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Fill?.Stretch(Orientation, Orientation == Orientation.Vertical ? finalSize.Width : finalSize.Height);
        Child?.Arrange(new Rect(finalSize));
        return finalSize;
    }
}

internal sealed class ExpanderContentFill : Decorator
{
    private Orientation _orientation;
    private double _extent = double.NaN;

    // The content's own desired size, before stretching.
    internal Size Natural { get; private set; }

    internal void Stretch(Orientation orientation, double extent)
    {
        if (orientation == _orientation && extent.Equals(_extent)) return;
        _orientation = orientation;
        _extent = extent;
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Natural = base.MeasureOverride(availableSize);
        if (!double.IsFinite(_extent)) return Natural;
        return _orientation == Orientation.Vertical
            ? Natural.WithWidth(Math.Max(Natural.Width, _extent))
            : Natural.WithHeight(Math.Max(Natural.Height, _extent));
    }
}
