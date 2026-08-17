using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Whether a button group leaves gaps between its children or joins them into one control.</summary>
public enum ButtonGroupVariant
{
    Standard,
    Connected
}

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

    private static readonly ButtonGroupGeometryProfile DefaultGeometryProfile = new(
        BetweenSpace: 12,
        ContainerHeight: 40,
        InnerCornerSize: 0,
        IsConnected: false);

    /// <summary>Internal geometry profile supplied by the Material control style.</summary>
    /// <remarks>
    /// This is a style transport property, not a public authoring surface. Keeping it separate
    /// from the public layout properties lets application and local values retain their ordinary
    /// Avalonia precedence while the variant selector chooses a typed MaterialValue endpoint.
    /// </remarks>
    internal static readonly StyledProperty<ButtonGroupGeometryProfile?> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, ButtonGroupGeometryProfile?>(
            nameof(GeometryProfile),
            defaultValue: null);

    // The ControlTheme writes the selected Material endpoint here. Keeping it separate from the
    // public/style-facing GeometryProfile lets application styles and local values retain normal
    // Avalonia precedence while the variant selector continues to provide a default.
    internal static readonly StyledProperty<ButtonGroupGeometryProfile?> MaterialGeometryProfileProperty =
        AvaloniaProperty.Register<MaterialButtonGroup, ButtonGroupGeometryProfile?>(
            nameof(MaterialGeometryProfile));

    // A connected group temporarily owns these child policies. Remember the values it replaced so
    // switching back to Standard, hiding a segment, or removing a child releases the group policy
    // instead of leaving a stale seam shape or stretch alignment behind.
    private readonly HashSet<Control> _shapedChildren = new();
    private readonly Dictionary<Control, RoundedCornerShape?> _previousShapes = new();
    private readonly Dictionary<Control, VerticalAlignment> _previousVerticalAlignments = new();

    public MaterialButtonGroup()
    {
    }

    static MaterialButtonGroup()
    {
        AffectsMeasure<MaterialButtonGroup>(
            VariantProperty,
            OrientationProperty,
            GeometryProfileProperty,
            MaterialGeometryProfileProperty);
        AffectsArrange<MaterialButtonGroup>(
            GeometryProfileProperty,
            MaterialGeometryProfileProperty);
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

    /// <summary>Gets or sets the hidden geometry profile supplied by the control style.</summary>
    internal ButtonGroupGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty)
            ?? GetValue(MaterialGeometryProfileProperty)
            ?? DefaultGeometryProfile;
        set => SetValue(GeometryProfileProperty, value);
    }

    internal ButtonGroupGeometryProfile? MaterialGeometryProfile
    {
        get => GetValue(MaterialGeometryProfileProperty);
        set => SetValue(MaterialGeometryProfileProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var gap = GeometryProfile.BetweenSpace;

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

        across = Math.Max(across, GeometryProfile.ContainerHeight);

        return horizontal ? new Size(along, across) : new Size(across, along);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var gap = GeometryProfile.BetweenSpace;
        var offset = 0.0;

        var index = 0;
        var last = LastVisibleIndex();
        var activeChildren = new HashSet<Control>();

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            var length = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;

            // Connected segments must stay contiguous even when a parent stretches the group
            // taller than the buttons; the theme's centered default would gap them.
            if (GeometryProfile.IsConnected)
            {
                RememberVerticalAlignment(child);
                child.VerticalAlignment = VerticalAlignment.Stretch;
            }

            child.Arrange(horizontal
                ? new Rect(offset, 0, length, finalSize.Height)
                : new Rect(0, offset, finalSize.Width, length));

            if (ApplySegmentShape(child, index, last))
                activeChildren.Add(child);

            offset += length + gap;
            index++;
        }

        ReleaseSegmentPolicies(activeChildren);

        return finalSize;
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
    private bool ApplySegmentShape(Control child, int index, int last)
    {
        if (!GeometryProfile.IsConnected || last < 0)
            return false;

        var outer = CornerSize.Full;
        var inner = CornerSize.Absolute(GeometryProfile.InnerCornerSize);

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
            case Button icon when M3IconButton.GetIsIconButton(icon):
                RememberShape(icon, M3IconButton.GetContainerShapeOverride(icon));
                M3IconButton.SetContainerShapeOverride(icon, shape);
                return true;
            case Button button:
                RememberShape(button, M3Button.GetContainerShapeOverride(button));
                M3Button.SetContainerShapeOverride(button, shape);
                return true;
        }

        return false;
    }

    private void RememberShape(Button button, RoundedCornerShape? previous)
    {
        if (_shapedChildren.Add(button))
            _previousShapes.Add(button, previous);
    }

    private void RememberVerticalAlignment(Control child)
    {
        if (!_previousVerticalAlignments.ContainsKey(child))
            _previousVerticalAlignments.Add(child, child.VerticalAlignment);
    }

    private void ReleaseSegmentPolicies(ISet<Control> activeChildren)
    {
        foreach (var child in _shapedChildren.Where(child => !activeChildren.Contains(child)).ToArray())
        {
            if (_previousShapes.TryGetValue(child, out var previousShape))
            {
                switch (child)
                {
                    case Button icon when M3IconButton.GetIsIconButton(icon):
                        M3IconButton.SetContainerShapeOverride(icon, previousShape);
                        break;
                    case Button button:
                        M3Button.SetContainerShapeOverride(button, previousShape);
                        break;
                }
            }

            _previousShapes.Remove(child);
            _shapedChildren.Remove(child);
        }

        foreach (var child in _previousVerticalAlignments.Keys
                     .Where(child => !activeChildren.Contains(child))
                     .ToArray())
        {
            child.VerticalAlignment = _previousVerticalAlignments[child];
            _previousVerticalAlignments.Remove(child);
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

}
