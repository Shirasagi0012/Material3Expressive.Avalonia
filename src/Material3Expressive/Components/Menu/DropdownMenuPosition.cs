/* Menu.kt / internal/MenuPosition.kt at docs/upstream-baseline.json.
 * Copyright 2023-2026 The Android Open Source Project; Apache-2.0. */
using Avalonia;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

public readonly record struct MenuPositionScope(PixelRect AnchorBounds, PixelSize WindowSize,
    PixelSize MenuSize, bool RightToLeft);

/// <summary>Ordered candidate coordinates in physical window pixels, as in MenuAnchorPosition.</summary>
public sealed class MenuAnchorPosition
{
    internal Func<MenuPositionScope, IReadOnlyList<int>> XCandidates { get; }
    internal Func<MenuPositionScope, IReadOnlyList<int>> YCandidates { get; }
    private MenuAnchorPosition(Func<MenuPositionScope, IReadOnlyList<int>> x, Func<MenuPositionScope, IReadOnlyList<int>> y)
    { XCandidates = x; YCandidates = y; }

    public static MenuAnchorPosition Custom(Func<MenuPositionScope, IReadOnlyList<int>> xCandidates,
        Func<MenuPositionScope, IReadOnlyList<int>> yCandidates) => new(xCandidates, yCandidates);
    public static MenuAnchorPosition Above { get; } = new(VerticalX, s => VerticalY(s, true));
    public static MenuAnchorPosition Below { get; } = new(VerticalX, s => VerticalY(s, false));
    public static MenuAnchorPosition Start { get; } = new(s => HorizontalX(s, true), HorizontalY);
    public static MenuAnchorPosition End { get; } = new(s => HorizontalX(s, false), HorizontalY);
    // The fixed upstream implementation uses the same logical alignments for Left/Start
    // and Right/End, despite its absolute-direction KDoc. Preserve the executable source.
    public static MenuAnchorPosition Left { get; } = new(s => HorizontalX(s, true), HorizontalY);
    public static MenuAnchorPosition Right { get; } = new(s => HorizontalX(s, false), HorizontalY);

    private static int WindowX(MenuPositionScope s) => s.AnchorBounds.X + s.AnchorBounds.Width / 2 < s.WindowSize.Width / 2
        ? 0 : s.WindowSize.Width - s.MenuSize.Width;
    private static int WindowY(MenuPositionScope s) => s.AnchorBounds.Y + s.AnchorBounds.Height / 2 < s.WindowSize.Height / 2
        ? 0 : s.WindowSize.Height - s.MenuSize.Height;
    private static int[] VerticalX(MenuPositionScope s) => s.RightToLeft
        ? [s.AnchorBounds.Right - s.MenuSize.Width, s.AnchorBounds.X, WindowX(s)]
        : [s.AnchorBounds.X, s.AnchorBounds.Right - s.MenuSize.Width, WindowX(s)];
    private static int[] VerticalY(MenuPositionScope s, bool above) =>
        [above ? s.AnchorBounds.Y - s.MenuSize.Height : s.AnchorBounds.Bottom,
         above ? s.AnchorBounds.Bottom : s.AnchorBounds.Y - s.MenuSize.Height,
         s.AnchorBounds.Y - MathUtils.RoundToInt(s.MenuSize.Height / 2f), WindowY(s)];
    private static int[] HorizontalX(MenuPositionScope s, bool start)
    {
        var first = s.RightToLeft == start ? s.AnchorBounds.Right : s.AnchorBounds.X - s.MenuSize.Width;
        var second = s.RightToLeft == start ? s.AnchorBounds.X - s.MenuSize.Width : s.AnchorBounds.Right;
        return [first, second, WindowX(s)];
    }
    private static int[] HorizontalY(MenuPositionScope s) =>
        [s.AnchorBounds.Y, s.AnchorBounds.Bottom - s.MenuSize.Height, WindowY(s)];
}

public interface IDropdownMenuPositionProvider
{
    RelativePoint TransformOrigin { get; }
    PixelPoint CalculatePosition(PixelRect anchorBounds, PixelSize windowSize, bool rightToLeft, PixelSize menuSize);
}

/// <summary>Fixed Material candidate selection, margins, offset and intersection-based pivot.</summary>
public sealed class DropdownMenuPositionProvider : IDropdownMenuPositionProvider
{
    public DropdownMenuPositionProvider(MenuAnchorPosition? anchorPosition = null, Vector offset = default,
        double density = 1, double verticalMargin = 48, double horizontalMargin = 8)
    {
        AnchorPosition = anchorPosition ?? MenuAnchorPosition.Below;
        Offset = offset; Density = density; VerticalMargin = verticalMargin; HorizontalMargin = horizontalMargin;
    }
    public MenuAnchorPosition AnchorPosition { get; }
    public Vector Offset { get; }
    public double Density { get; }
    public double VerticalMargin { get; }
    public double HorizontalMargin { get; }
    public RelativePoint TransformOrigin { get; private set; } = RelativePoint.Center;

    public PixelPoint CalculatePosition(PixelRect anchorBounds, PixelSize windowSize, bool rightToLeft, PixelSize menuSize)
    {
        var scope = new MenuPositionScope(anchorBounds, windowSize, menuSize, rightToLeft);
        var x = Select(AnchorPosition.XCandidates(scope), MathUtils.DipToPx(Offset.X, Density) * (rightToLeft ? -1 : 1),
            MathUtils.DipToPx(HorizontalMargin, Density), windowSize.Width, menuSize.Width);
        var y = Select(AnchorPosition.YCandidates(scope), MathUtils.DipToPx(Offset.Y, Density),
            MathUtils.DipToPx(VerticalMargin, Density), windowSize.Height, menuSize.Height);
        TransformOrigin = CalculateTransformOrigin(anchorBounds, new PixelRect(x, y, menuSize.Width, menuSize.Height));
        return new(x, y);
    }

    private static int Select(IReadOnlyList<int> candidates, int offset, int margin, int windowSize, int menuSize)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index] + offset;
            if (candidate >= margin && candidate + menuSize <= windowSize - margin) return candidate;
            if (index == candidates.Count - 1)
                return menuSize >= windowSize - 2 * margin
                    ? MathUtils.RoundToInt((windowSize - menuSize) / 2f)
                    : Math.Clamp(candidate, margin, windowSize - margin - menuSize);
        }
        return 0;
    }

    internal static RelativePoint CalculateTransformOrigin(PixelRect anchor, PixelRect menu)
    {
        static float Pivot(int start, int end, int menuStart, int menuEnd, int extent) =>
            menuStart >= end ? 0 : menuEnd <= start ? 1 : extent == 0 ? 0
                : ((Math.Max(start, menuStart) + Math.Min(end, menuEnd)) / 2 - menuStart) / (float)extent;
        return new(Pivot(anchor.X, anchor.Right, menu.X, menu.Right, menu.Width),
            Pivot(anchor.Y, anchor.Bottom, menu.Y, menu.Bottom, menu.Height), RelativeUnit.Relative);
    }
}
