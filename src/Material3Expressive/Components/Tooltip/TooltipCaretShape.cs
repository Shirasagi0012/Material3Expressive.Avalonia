using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Creates a tooltip caret path in local DIP coordinates. The origin is the attachment
/// point on the tooltip edge; positive Y points outwards before placement rotates the path.</summary>
/// <remarks>The size is the tooltip surface size, not the caret size. The returned geometry is
/// snapshotted; assign a new delegate when captured shape parameters change.</remarks>
public delegate PathGeometry TooltipCaretShape(Size tooltipSize, FlowDirection flowDirection);
