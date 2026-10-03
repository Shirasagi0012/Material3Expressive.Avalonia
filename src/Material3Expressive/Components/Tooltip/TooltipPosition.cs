using Avalonia;

namespace Material3Expressive.Components;

internal enum TooltipSide { Above, Below, Left, Right, Start, End }

// Compose's IntRect/IntSize arithmetic is physical-pixel arithmetic. In particular,
// division rounds toward zero, and an oversized tooltip clamps its origin to zero.
internal static class TooltipPosition
{
    internal static PixelPoint Calculate(PixelRect anchor, PixelSize window, PixelSize tooltip,
        TooltipSide side, int spacing, bool rightToLeft)
    {
        side = side switch
        {
            TooltipSide.Start => rightToLeft ? TooltipSide.Right : TooltipSide.Left,
            TooltipSide.End => rightToLeft ? TooltipSide.Left : TooltipSide.Right,
            _ => side,
        };
        var x = anchor.X + (anchor.Width - tooltip.Width) / 2;
        var y = (anchor.Y + anchor.Bottom - tooltip.Height) / 2;
        switch (side)
        {
            case TooltipSide.Left:
                x = anchor.X - tooltip.Width - spacing;
                if (x < 0) x = anchor.Right + spacing;
                break;
            case TooltipSide.Right:
                x = anchor.Right + spacing;
                if (x + tooltip.Width > window.Width) x = anchor.X - tooltip.Width - spacing;
                break;
            case TooltipSide.Above:
                y = anchor.Y - tooltip.Height - spacing;
                if (y < 0) y = anchor.Bottom + spacing;
                break;
            case TooltipSide.Below:
                y = anchor.Bottom + spacing;
                if (y + tooltip.Height > window.Height) y = anchor.Y - tooltip.Height - spacing;
                break;
        }
        return new PixelPoint(Math.Clamp(x, 0, Math.Max(0, window.Width - tooltip.Width)),
            Math.Clamp(y, 0, Math.Max(0, window.Height - tooltip.Height)));
    }
}
