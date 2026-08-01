using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A row of buttons, either spaced apart or connected into one control.
/// </summary>
/// <remarks>
/// The connected form is the interesting one, and the reason this is a panel rather than a
/// <c>StackPanel</c> with a margin. Material squares off only the corners where two segments meet
/// and leaves the outer ones fully round, so a segment's shape depends on where it sits in the
/// row — something no individual button can know about itself.
/// </remarks>
public class MaterialButtonGroup : Panel
{
    public static readonly StyledProperty<ButtonGroupVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, ButtonGroupVariant>(nameof(Variant));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, Orientation>(nameof(Orientation), Orientation.Horizontal);

    private readonly MaterialThemeWatcher _theme;

    private ButtonGroupTokens _tokens = ButtonGroupTokens.Standard;

    public MaterialButtonGroup()
    {
        _theme = new MaterialThemeWatcher(this);
        _theme.Changed += (_, _) => ApplyTokens();

        ApplyTokens();
    }

    static MaterialButtonGroup()
    {
        AffectsMeasure<MaterialButtonGroup>(VariantProperty, OrientationProperty);
    }

    /// <summary>Standard (spaced) or connected (segmented).</summary>
    public ButtonGroupVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var gap = _tokens.BetweenSpace;

        double along = 0;
        double across = 0;
        var visible = 0;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            child.Measure(availableSize);
            var size = child.DesiredSize;

            along += horizontal ? size.Width : size.Height;
            across = Math.Max(across, horizontal ? size.Height : size.Width);
            visible++;
        }

        if (visible > 1)
            along += gap * (visible - 1);

        across = Math.Max(across, _tokens.ContainerHeight);

        return horizontal ? new Size(along, across) : new Size(across, along);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var gap = _tokens.BetweenSpace;
        var offset = 0.0;

        var index = 0;
        var last = LastVisibleIndex();

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            var length = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;

            child.Arrange(horizontal
                ? new Rect(offset, 0, length, finalSize.Height)
                : new Rect(0, offset, finalSize.Width, length));

            ApplySegmentShape(child, index, last);

            offset += length + gap;
            index++;
        }

        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty)
            ApplyTokens();
    }

    /// <summary>
    /// Tightens the corners a segment shares with its neighbours, leaving the row's outer corners
    /// round.
    /// </summary>
    /// <remarks>
    /// Communicated by setting the child's shape override rather than by drawing here, because the
    /// child paints its own container and morphs it under interaction. A group that drew segment
    /// shapes itself would have to take all of that over.
    /// </remarks>
    private void ApplySegmentShape(Control child, int index, int last)
    {
        if (!_tokens.IsConnected || last < 0)
            return;

        var outer = CornerSize.Full;
        var inner = CornerSize.Absolute(_tokens.InnerCornerSize);

        var horizontal = Orientation == Orientation.Horizontal;
        var isFirst = index == 0;
        var isLast = index == last;

        // A single segment keeps every corner round; otherwise each end keeps the corners that
        // face outwards and tightens the two that face a neighbour.
        var shape = horizontal
            ? new RoundedCornerShape(
                isFirst ? outer : inner,
                isLast ? outer : inner,
                isLast ? outer : inner,
                isFirst ? outer : inner)
            : new RoundedCornerShape(
                isFirst ? outer : inner,
                isFirst ? outer : inner,
                isLast ? outer : inner,
                isLast ? outer : inner);

        switch (child)
        {
            case MaterialToggleButton toggle:
                toggle.ContainerShapeOverride = shape;
                break;
            case MaterialIconButton icon:
                icon.ContainerShapeOverride = shape;
                break;
            case Button button:
                ButtonAssist.SetContainerShapeOverride(button, shape);
                break;
        }
    }

    private int LastVisibleIndex()
    {
        var index = -1;
        var visible = 0;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            index = visible;
            visible++;
        }

        return index;
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.GetButtonGroupTokens(Variant);
        InvalidateMeasure();
    }
}
