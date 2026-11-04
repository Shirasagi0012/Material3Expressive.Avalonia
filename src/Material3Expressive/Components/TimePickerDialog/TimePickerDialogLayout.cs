using Avalonia;
using static Material3Expressive.Tokens.MaterialTokens;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

// Fixed TimePickerCustomLayout and VibrantTimePickerCustomLayout. The vibrant
// template omits the first (title) child. Content is measured before title/actions;
// input content below the 200 DIP dial minimum always uses portrait.
internal sealed class TimePickerDialogLayout : Panel
{
    public static readonly StyledProperty<double> HorizontalPaddingProperty = AvaloniaProperty.Register<TimePickerDialogLayout, double>(nameof(HorizontalPadding), 24d);
    public double HorizontalPadding { get => GetValue(HorizontalPaddingProperty); set => SetValue(HorizontalPaddingProperty, value); }
    public static readonly StyledProperty<double> VerticalPaddingProperty = AvaloniaProperty.Register<TimePickerDialogLayout, double>(nameof(VerticalPadding), 24d);
    public double VerticalPadding { get => GetValue(VerticalPaddingProperty); set => SetValue(VerticalPaddingProperty, value); }
    public static readonly StyledProperty<double> LandscapeAdditionalHeightProperty = AvaloniaProperty.Register<TimePickerDialogLayout, double>(nameof(LandscapeAdditionalHeight), 28d);
    public double LandscapeAdditionalHeight { get => GetValue(LandscapeAdditionalHeightProperty); set => SetValue(LandscapeAdditionalHeightProperty, value); }
    public static readonly StyledProperty<double> LandscapeContentTopOffsetProperty = AvaloniaProperty.Register<TimePickerDialogLayout, double>(nameof(LandscapeContentTopOffset), 16d);
    public double LandscapeContentTopOffset { get => GetValue(LandscapeContentTopOffsetProperty); set => SetValue(LandscapeContentTopOffsetProperty, value); }
    public static readonly StyledProperty<double> LandscapeActionsSpacingProperty = AvaloniaProperty.Register<TimePickerDialogLayout, double>(nameof(LandscapeActionsSpacing), 4d);
    public double LandscapeActionsSpacing { get => GetValue(LandscapeActionsSpacingProperty); set => SetValue(LandscapeActionsSpacingProperty, value); }
    public static readonly StyledProperty<double> LandscapeTallActionsOffsetProperty = AvaloniaProperty.Register<TimePickerDialogLayout, double>(nameof(LandscapeTallActionsOffset), 16d);
    public double LandscapeTallActionsOffset { get => GetValue(LandscapeTallActionsOffsetProperty); set => SetValue(LandscapeTallActionsOffsetProperty, value); }
    public static readonly StyledProperty<double> PortraitActionsSpacingProperty = AvaloniaProperty.Register<TimePickerDialogLayout, double>(nameof(PortraitActionsSpacing));
    public double PortraitActionsSpacing { get => GetValue(PortraitActionsSpacingProperty); set => SetValue(PortraitActionsSpacingProperty, value); }
    static TimePickerDialogLayout() => AffectsMeasure<TimePickerDialogLayout>(HorizontalPaddingProperty, VerticalPaddingProperty, LandscapeAdditionalHeightProperty, LandscapeContentTopOffsetProperty, LandscapeActionsSpacingProperty, LandscapeTallActionsOffsetProperty, PortraitActionsSpacingProperty);
    private bool _landscape;
    private double _layoutWidth, _layoutHeight;
    protected override bool BypassFlowDirectionPolicies => true;
    protected override Size MeasureOverride(Size availableSize)
    {
        var title = Children.Count == 3 ? Children[0] : null;
        var content = Children[^2];
        var actions = Children[^1];
        // Surface propagates the caller's minimum width to the content Column.
        // Avalonia Measure carries only a maximum, so preserve that minimum on
        // the template-owned Column container; its presenter still measures with minWidth=0.
        var owner = (Dialog)TemplatedParent!;
        content.MinWidth = Math.Min(availableSize.Width, double.IsNaN(owner.Width) ? owner.MinWidth : Math.Max(owner.MinWidth, owner.Width));
        content.Measure(availableSize);
        var size = content.DesiredSize;
        _landscape = size.Width > size.Height && size.Height >= 200;
        _layoutWidth = size.Width + HorizontalPadding * 2;
        actions.Measure(new Size(size.Width, availableSize.Height));
        title?.Measure(new Size(size.Width, availableSize.Height));
        _layoutHeight = _landscape
            ? double.IsFinite(availableSize.Height) ? availableSize.Height : size.Height + actions.DesiredSize.Height + LandscapeAdditionalHeight
            : VerticalPadding * 2 + (title?.DesiredSize.Height ?? 0) + size.Height + actions.DesiredSize.Height + PortraitActionsSpacing;
        return new Size(_layoutWidth, _layoutHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var title = Children.Count == 3 ? Children[0] : null;
        var content = Children[^2];
        var actions = Children[^1];
        // Compose place() uses physical coordinates, including in RTL.
        var offset = (finalSize.Width - _layoutWidth) / 2;
        var offsetY = (finalSize.Height - _layoutHeight) / 2;
        if (title is not null) title.Arrange(new Rect(offset + HorizontalPadding, offsetY + VerticalPadding, title.DesiredSize.Width, title.DesiredSize.Height));
        var y = offsetY + VerticalPadding + (title?.DesiredSize.Height ?? 0);
        double actionsY;
        if (_landscape)
        {
            var remaining = _layoutHeight - (content.DesiredSize.Height + actions.DesiredSize.Height + LandscapeAdditionalHeight);
            y = offsetY + LandscapeContentTopOffset + remaining / 2;
            actionsY = y + content.DesiredSize.Height + LandscapeActionsSpacing - (_layoutHeight >= 384 ? LandscapeTallActionsOffset : 0) + remaining / 2;
        }
        else actionsY = y + content.DesiredSize.Height + PortraitActionsSpacing;
        content.Arrange(new Rect(offset + HorizontalPadding, y, content.DesiredSize.Width, content.DesiredSize.Height));
        actions.Arrange(new Rect(offset + HorizontalPadding, actionsY, content.DesiredSize.Width, actions.DesiredSize.Height));
        return finalSize;
    }
}

// Row(spacedBy(8)): unweighted children are measured in declaration order,
// then a weighted spacer consumes the remaining width. There is no wrapping.
internal sealed class TimePickerDialogActions : Panel
{
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<TimePickerDialogActions, double>(nameof(Spacing), 8d);
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }
    static TimePickerDialogActions() => AffectsMeasure<TimePickerDialogActions>(SpacingProperty);
    protected override bool BypassFlowDirectionPolicies => true;
    private double _spacerWidth;
    protected override Size MeasureOverride(Size availableSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToArray();
        double occupied = 0, height = 0;
        foreach (var child in visible.Where(c => c.Name != "PART_Spacer"))
        {
            var remaining = Math.Max(0, availableSize.Width - occupied);
            child.Measure(new Size(remaining, availableSize.Height));
            var lastSpacing = Math.Min(Spacing, Math.Max(0, remaining - child.DesiredSize.Width));
            occupied += child.DesiredSize.Width + lastSpacing;
            height = Math.Max(height, child.DesiredSize.Height);
        }
        // The spacer is the sole weighted child, so Row retains trailing space
        // after every unweighted child, irrespective of its declaration position.
        _spacerWidth = Math.Max(0, availableSize.Width - occupied);
        return new Size(availableSize.Width, height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        // Arrangement.SpacedAligned clamps each position against the remaining
        // width. When the spacer collapses, this can reduce a preceding gap.
        double occupied = 0, freeSpace = finalSize.Width;
        foreach (var child in Children.Where(c => c.IsVisible))
        {
            var width = child.Name == "PART_Spacer" ? _spacerWidth : child.DesiredSize.Width;
            var x = rtl ? Math.Max(0, freeSpace - width) : Math.Min(occupied, finalSize.Width - width);
            child.Arrange(new Rect(x, 0, width, child.DesiredSize.Height));
            if (rtl) freeSpace = x - Math.Min(Spacing, x);
            else occupied = x + width + Math.Min(Spacing, finalSize.Width - x - width);
        }
        return finalSize;
    }
}
