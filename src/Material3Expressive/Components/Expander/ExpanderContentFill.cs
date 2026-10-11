using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Material3Expressive.Components;

// AnimatedVisibility lays an expanding/shrinking child out at its own measured size, as Compose
// does, so content narrower than the Expander would sit at the anchor instead of stretching. This
// fills the axis across the expansion (width for Down/Up, height for Left/Right) when it is
// bounded, the equivalent of fillMaxWidth()/fillMaxHeight() on the Compose content.
internal sealed class ExpanderContentFill : Decorator
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ExpanderContentFill, Orientation>(nameof(Orientation), Orientation.Vertical);

    static ExpanderContentFill() => AffectsMeasure<ExpanderContentFill>(OrientationProperty);

    // The expansion axis: Vertical fills the width, Horizontal fills the height.
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = base.MeasureOverride(availableSize);
        return Orientation == Orientation.Vertical
            ? double.IsFinite(availableSize.Width) ? desired.WithWidth(Math.Max(desired.Width, availableSize.Width)) : desired
            : double.IsFinite(availableSize.Height) ? desired.WithHeight(Math.Max(desired.Height, availableSize.Height)) : desired;
    }
}
