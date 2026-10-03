using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

/// <summary>The fixed Compose Arrangement.Vertical forms, including spaced alignment.</summary>
public sealed record NavigationRailArrangement
{
    internal enum Form { Top, Center, Bottom, SpaceBetween, SpaceAround, SpaceEvenly, Spaced }
    internal Form Kind { get; }
    internal double Spacing { get; }
    internal VerticalAlignment Alignment { get; }
    private NavigationRailArrangement(Form kind, double spacing = 0, VerticalAlignment alignment = VerticalAlignment.Top)
        => (Kind, Spacing, Alignment) = (kind, spacing, alignment);

    public static NavigationRailArrangement Top { get; } = new(Form.Top);
    public static NavigationRailArrangement Center { get; } = new(Form.Center);
    public static NavigationRailArrangement Bottom { get; } = new(Form.Bottom);
    public static NavigationRailArrangement SpaceBetween { get; } = new(Form.SpaceBetween);
    public static NavigationRailArrangement SpaceAround { get; } = new(Form.SpaceAround);
    public static NavigationRailArrangement SpaceEvenly { get; } = new(Form.SpaceEvenly);
    public static NavigationRailArrangement SpacedBy(double spacing, VerticalAlignment alignment = VerticalAlignment.Top) => new(Form.Spaced, spacing, alignment);

    internal int[] Arrange(int totalSize, int[] sizes, float density)
    {
        var positions = new int[sizes.Length];
        if (sizes.Length == 0) return positions;
        var consumed = sizes.Sum();
        var gap = Kind switch
        {
            Form.SpaceBetween => (totalSize - consumed) / (float)Math.Max(sizes.Length - 1, 1),
            Form.SpaceAround => (totalSize - consumed) / (float)sizes.Length,
            Form.SpaceEvenly => (totalSize - consumed) / (float)(sizes.Length + 1),
            _ => 0f
        };
        var current = Kind switch
        {
            Form.Center => (totalSize - consumed) / 2f,
            Form.Bottom => (float)(totalSize - consumed),
            Form.SpaceAround => gap / 2,
            Form.SpaceEvenly => gap,
            _ => 0f
        };
        if (Kind == Form.Spaced)
        {
            // Arrangement.SpacedAligned clips each next space to the remaining size;
            // alignment is applied to the measured group only when it fits.
            var space = MathUtils.DipToPx((float)Spacing, density);
            var occupied = 0;
            var lastSpace = 0;
            for (var i = 0; i < sizes.Length; i++)
            {
                positions[i] = Math.Min(occupied, totalSize - sizes[i]);
                lastSpace = Math.Min(space, totalSize - positions[i] - sizes[i]);
                occupied = positions[i] + sizes[i] + lastSpace;
            }
            occupied -= lastSpace;
            if (occupied < totalSize)
            {
                var offset = Alignment switch { VerticalAlignment.Center => MathUtils.RoundToInt((totalSize - occupied) / 2f), VerticalAlignment.Bottom => totalSize - occupied, _ => 0 };
                for (var i = 0; i < positions.Length; i++) positions[i] += offset;
            }
            return positions;
        }
        for (var i = 0; i < sizes.Length; i++) { positions[i] = MathUtils.RoundToInt(current); current += sizes[i] + gap; }
        return positions;
    }

    // Same placement, clipping and alignment rules, without rounding each animated
    // position. WideNavigationRailItemsPanel supplies the static rounding residual.
    internal double[] ArrangeContinuous(double totalSize, double[] sizes, float density)
    {
        var positions = new double[sizes.Length];
        if (sizes.Length == 0) return positions;
        var consumed = sizes.Sum();
        var gap = Kind switch
        {
            Form.SpaceBetween => (totalSize - consumed) / Math.Max(sizes.Length - 1, 1),
            Form.SpaceAround => (totalSize - consumed) / sizes.Length,
            Form.SpaceEvenly => (totalSize - consumed) / (sizes.Length + 1),
            _ => 0
        };
        var current = Kind switch
        {
            Form.Center => (totalSize - consumed) / 2,
            Form.Bottom => totalSize - consumed,
            Form.SpaceAround => gap / 2,
            Form.SpaceEvenly => gap,
            _ => 0
        };
        if (Kind == Form.Spaced)
        {
            var space = MathUtils.DipToPx((float)Spacing, density);
            double occupied = 0, lastSpace = 0;
            for (var i = 0; i < sizes.Length; i++)
            {
                positions[i] = Math.Min(occupied, totalSize - sizes[i]);
                lastSpace = Math.Min(space, totalSize - positions[i] - sizes[i]);
                occupied = positions[i] + sizes[i] + lastSpace;
            }
            occupied -= lastSpace;
            if (occupied < totalSize)
            {
                var offset = Alignment switch { VerticalAlignment.Center => (totalSize - occupied) / 2, VerticalAlignment.Bottom => totalSize - occupied, _ => 0 };
                for (var i = 0; i < positions.Length; i++) positions[i] += offset;
            }
            return positions;
        }
        for (var i = 0; i < sizes.Length; i++) { positions[i] = current; current += sizes[i] + gap; }
        return positions;
    }
}

