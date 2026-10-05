using Avalonia;
using Avalonia.Controls;

namespace ComposeAnimation;

/// <summary>Experimental four-bound layout constraints in DIP. Minima are finite and non-negative;
/// maxima may be positive infinity. A default(struct) value is fixed zero; new() is unbounded.</summary>
public readonly record struct LookaheadConstraints
{
    public double MinWidth { get; }
    public double MaxWidth { get; }
    public double MinHeight { get; }
    public double MaxHeight { get; }
    public Size MinSize => new(MinWidth, MinHeight);
    public Size MaxSize => new(MaxWidth, MaxHeight);
    public bool HasMinimum => MinWidth > 0 || MinHeight > 0;
    public bool HasFixedWidth => MinWidth == MaxWidth;
    public bool HasFixedHeight => MinHeight == MaxHeight;

    public LookaheadConstraints() : this(0, double.PositiveInfinity, 0, double.PositiveInfinity) { }

    public LookaheadConstraints(double minWidth = 0, double maxWidth = double.PositiveInfinity,
        double minHeight = 0, double maxHeight = double.PositiveInfinity)
    {
        Validate(minWidth, maxWidth, nameof(minWidth), nameof(maxWidth));
        Validate(minHeight, maxHeight, nameof(minHeight), nameof(maxHeight));
        MinWidth = minWidth;
        MaxWidth = maxWidth;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
    }

    public static LookaheadConstraints Unbounded => new();
    public static LookaheadConstraints AtMost(Size size) => new(0, size.Width, 0, size.Height);
    public static LookaheadConstraints Fixed(Size size) => new(size.Width, size.Width, size.Height, size.Height);
    public LookaheadConstraints Loosen() => new(0, MaxWidth, 0, MaxHeight);

    /// <summary>Clamp a size to these bounds. NaN is rejected; infinity is useful with unbounded axes.</summary>
    public Size Constrain(Size size)
    {
        if (double.IsNaN(size.Width) || double.IsNaN(size.Height))
            throw new ArgumentException("Cannot constrain a size containing NaN.", nameof(size));
        return new Size(Math.Clamp(size.Width, MinWidth, MaxWidth), Math.Clamp(size.Height, MinHeight, MaxHeight));
    }

    /// <summary>Fit another constraint range within this range; incoming (this) bounds win conflicts.</summary>
    public LookaheadConstraints Constrain(LookaheadConstraints other) => new(
        Math.Clamp(other.MinWidth, MinWidth, MaxWidth), Math.Clamp(other.MaxWidth, MinWidth, MaxWidth),
        Math.Clamp(other.MinHeight, MinHeight, MaxHeight), Math.Clamp(other.MaxHeight, MinHeight, MaxHeight));

    /// <summary>Shift both bounds, clamping at zero. Unbounded maxima remain unbounded.</summary>
    public LookaheadConstraints Offset(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height))
            throw new ArgumentOutOfRangeException(nameof(width), "Constraint offsets must be finite.");
        return new LookaheadConstraints(Math.Max(0, MinWidth + width), ShiftMax(MaxWidth, width),
            Math.Max(0, MinHeight + height), ShiftMax(MaxHeight, height));
    }

    public LookaheadConstraints Deflate(Thickness padding)
        => Offset(-(padding.Left + padding.Right), -(padding.Top + padding.Bottom));

    internal LookaheadConstraints ApplyTo(Control control)
    {
        // Same preferred local range as Avalonia MinMax; the explicit incoming range then
        // constrains that preference. Ordinary Size-based measurement does not use this rule.
        static (double Min, double Max) Range(double value, double min, double max)
        {
            max = Math.Max(min, Math.Min(double.IsNaN(value) ? double.PositiveInfinity : value, max));
            min = Math.Max(min, Math.Min(double.IsNaN(value) ? 0 : value, max));
            return (min, max);
        }
        var width = Range(control.Width, control.MinWidth, control.MaxWidth);
        var height = Range(control.Height, control.MinHeight, control.MaxHeight);
        return Constrain(new LookaheadConstraints(width.Min, width.Max, height.Min, height.Max));
    }

    private static double ShiftMax(double maximum, double offset)
    {
        if (double.IsPositiveInfinity(maximum)) return maximum;
        var value = Math.Max(0, maximum + offset);
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(offset), "Bounded constraint overflow.");
        return value;
    }

    private static void Validate(double min, double max, string minName, string maxName)
    {
        if (!double.IsFinite(min) || min < 0) throw new ArgumentOutOfRangeException(minName);
        if (double.IsNaN(max) || max < min) throw new ArgumentOutOfRangeException(maxName);
    }
}
