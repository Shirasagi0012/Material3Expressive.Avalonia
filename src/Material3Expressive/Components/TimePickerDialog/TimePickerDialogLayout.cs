using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3Expressive.Components;

// Direct translation of fixed TimePickerCustomLayout: content is measured before
// title/actions; input content below the 200 DIP dial minimum always uses portrait.
internal sealed class TimePickerDialogLayout : Panel
{
    private bool _landscape;
    private double _layoutWidth, _layoutHeight;
    protected override bool BypassFlowDirectionPolicies => true;
    protected override Size MeasureOverride(Size availableSize)
    {
        var title = Children[0];
        var content = Children[1];
        var actions = Children[2];
        // Surface propagates the caller's minimum width to the content Column.
        // Avalonia Measure carries only a maximum, so preserve that minimum on
        // the template-owned Column container; its presenter still measures with minWidth=0.
        var owner = (TimePickerDialog)TemplatedParent!;
        content.MinWidth = Math.Min(availableSize.Width, double.IsNaN(owner.Width) ? owner.MinWidth : Math.Max(owner.MinWidth, owner.Width));
        content.Measure(availableSize);
        var size = content.DesiredSize;
        _landscape = size.Width > size.Height && size.Height >= 200;
        _layoutWidth = size.Width + 48;
        actions.Measure(new Size(size.Width, availableSize.Height));
        title.Measure(new Size(size.Width, availableSize.Height));
        _layoutHeight = _landscape
            ? double.IsFinite(availableSize.Height) ? availableSize.Height : size.Height + actions.DesiredSize.Height + 28
            : 48 + title.DesiredSize.Height + size.Height + actions.DesiredSize.Height;
        return new Size(_layoutWidth, _layoutHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var title = Children[0];
        var content = Children[1];
        var actions = Children[2];
        // Compose place() uses physical coordinates, including in RTL.
        var offset = (finalSize.Width - _layoutWidth) / 2;
        var offsetY = (finalSize.Height - _layoutHeight) / 2;
        title.Arrange(new Rect(offset + 24, offsetY + 24, title.DesiredSize.Width, title.DesiredSize.Height));
        var y = offsetY + 24 + title.DesiredSize.Height;
        double actionsY;
        if (_landscape)
        {
            var remaining = _layoutHeight - (content.DesiredSize.Height + actions.DesiredSize.Height + 28);
            y = offsetY + 16 + remaining / 2;
            actionsY = y + content.DesiredSize.Height + 4 - (_layoutHeight >= 384 ? 16 : 0) + remaining / 2;
        }
        else actionsY = y + content.DesiredSize.Height;
        content.Arrange(new Rect(offset + 24, y, content.DesiredSize.Width, content.DesiredSize.Height));
        actions.Arrange(new Rect(offset + 24, actionsY, content.DesiredSize.Width, actions.DesiredSize.Height));
        return finalSize;
    }
}

// Row(spacedBy(8)): unweighted children are measured in declaration order,
// then a weighted spacer consumes the remaining width. There is no wrapping.
internal sealed class TimePickerDialogActions : Panel
{
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
            var lastSpacing = Math.Min(8, Math.Max(0, remaining - child.DesiredSize.Width));
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
            if (rtl) freeSpace = x - Math.Min(8, x);
            else occupied = x + width + Math.Min(8, finalSize.Width - x - width);
        }
        return finalSize;
    }
}
