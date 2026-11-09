// Port of Foundation layout/Arrangement.kt at the commits in docs/upstream-baseline.json.
// Copyright 2020 The Android Open Source Project; Apache-2.0.
using System.ComponentModel;
using System.Globalization;
using Avalonia.Layout;
using Avalonia.Media;

namespace Material3Expressive.Foundation;

/// <summary>Places row children in physical pixels. Spacing is expressed in DIP.</summary>
/// <remarks>Implementations must be immutable. Replace the property value to change a layout.</remarks>
[TypeConverter(typeof(MaterialHorizontalArrangementConverter))]
public abstract record MaterialHorizontalArrangement
{
    public virtual double Spacing => 0;

    /// <summary>Writes one physical left position for each child, preserving the supplied child order.</summary>
    public abstract void Arrange(double density, int totalSize, ReadOnlySpan<int> sizes,
        FlowDirection direction, Span<int> positions);

    public static MaterialHorizontalArrangement Start { get; } = new Standard(Kind.Start);
    public static MaterialHorizontalArrangement End { get; } = new Standard(Kind.End);
    public static MaterialHorizontalArrangement Center { get; } = new Standard(Kind.Center);
    public static MaterialHorizontalArrangement SpaceBetween { get; } = new Standard(Kind.SpaceBetween);
    public static MaterialHorizontalArrangement SpaceAround { get; } = new Standard(Kind.SpaceAround);
    public static MaterialHorizontalArrangement SpaceEvenly { get; } = new Standard(Kind.SpaceEvenly);

    /// <summary>Fixed (possibly negative) spacing. Left/Right mean logical Start/End.</summary>
    public static MaterialHorizontalArrangement SpacedBy(double spacing, HorizontalAlignment alignment = HorizontalAlignment.Left) =>
        new SpacedHorizontalArrangement { Space = spacing, Alignment = alignment };

    public static class Absolute
    {
        public static MaterialHorizontalArrangement Left { get; } = new Standard(Kind.Start, true);
        public static MaterialHorizontalArrangement Right { get; } = new Standard(Kind.End, true);
        public static MaterialHorizontalArrangement Center { get; } = new Standard(Kind.Center, true);
        public static MaterialHorizontalArrangement SpaceBetween { get; } = new Standard(Kind.SpaceBetween, true);
        public static MaterialHorizontalArrangement SpaceAround { get; } = new Standard(Kind.SpaceAround, true);
        public static MaterialHorizontalArrangement SpaceEvenly { get; } = new Standard(Kind.SpaceEvenly, true);
        /// <summary>Keeps child order physical; the group alignment still observes layout direction.</summary>
        public static MaterialHorizontalArrangement SpacedBy(double spacing, HorizontalAlignment alignment = HorizontalAlignment.Left) =>
            new SpacedHorizontalArrangement { Space = spacing, Alignment = alignment, Mirror = false };
    }

    /// <summary>Reads a standard name, optionally prefixed by Absolute. Fixed gaps use SpacedBy.</summary>
    public static MaterialHorizontalArrangement Parse(string value) => value.ToLowerInvariant() switch
    {
        "start" => Start, "end" => End, "center" => Center,
        "spacebetween" => SpaceBetween, "spacearound" => SpaceAround, "spaceevenly" => SpaceEvenly,
        "absolute.left" => Absolute.Left, "absolute.right" => Absolute.Right, "absolute.center" => Absolute.Center,
        "absolute.spacebetween" => Absolute.SpaceBetween, "absolute.spacearound" => Absolute.SpaceAround,
        "absolute.spaceevenly" => Absolute.SpaceEvenly,
        _ => throw new FormatException($"Unknown horizontal arrangement '{value}'.")
    };

    private enum Kind { Start, End, Center, SpaceBetween, SpaceAround, SpaceEvenly }
    private sealed record Standard(Kind Mode, bool IsAbsolute = false) : MaterialHorizontalArrangement
    {
        public override void Arrange(double density, int totalSize, ReadOnlySpan<int> sizes, FlowDirection direction, Span<int> positions)
        {
            var rtl = !IsAbsolute && direction == FlowDirection.RightToLeft;
            var consumed = 0;
            foreach (var size in sizes) consumed += size;
            var free = totalSize - consumed;
            var gap = Mode switch
            {
                Kind.SpaceBetween => (float)free / Math.Max(sizes.Length - 1, 1),
                Kind.SpaceAround => sizes.Length == 0 ? 0 : (float)free / sizes.Length,
                Kind.SpaceEvenly => (float)free / (sizes.Length + 1),
                _ => 0f
            };
            // Start/End use integer accumulation upstream; all distribution modes use Float.
            if (Mode is Kind.Start or Kind.End)
            {
                var current = Mode == Kind.Start ? rtl ? free : 0 : rtl ? 0 : free;
                for (var slot = 0; slot < sizes.Length; slot++)
                {
                    var i = rtl ? sizes.Length - 1 - slot : slot;
                    positions[i] = current;
                    current += sizes[i];
                }
                return;
            }
            var position = Mode switch
            {
                Kind.Center => free / 2f,
                Kind.SpaceAround => gap / 2f,
                Kind.SpaceEvenly => gap,
                Kind.SpaceBetween when rtl && sizes.Length == 1 => gap,
                _ => 0f
            };
            for (var slot = 0; slot < sizes.Length; slot++)
            {
                var i = rtl ? sizes.Length - 1 - slot : slot;
                positions[i] = MathUtils.RoundToInt(position);
                position += sizes[i] + gap;
            }
        }
    }
}

/// <summary>XAML-friendly immutable fixed spacing and group alignment.</summary>
public sealed record SpacedHorizontalArrangement : MaterialHorizontalArrangement
{
    public double Space { get; init; }
    public HorizontalAlignment Alignment { get; init; } = HorizontalAlignment.Left;
    /// <summary>False preserves physical child order even in RTL. Alignment remains logical.</summary>
    public bool Mirror { get; init; } = true;
    public override double Spacing => Space;

    public override void Arrange(double density, int totalSize, ReadOnlySpan<int> sizes, FlowDirection direction, Span<int> positions)
    {
        if (sizes.IsEmpty) return;
        var space = MathUtils.DipToPx(Space, density);
        var rtl = Mirror && direction == FlowDirection.RightToLeft;
        var lastSpace = 0;
        var freeSpace = totalSize;
        if (rtl)
        {
            for (var i = 0; i < sizes.Length; i++)
            {
                positions[i] = Math.Max(0, freeSpace - sizes[i]);
                lastSpace = Math.Min(space, positions[i]);
                freeSpace = positions[i] - lastSpace;
            }
            freeSpace += lastSpace;
        }
        else
        {
            var occupied = 0;
            for (var i = 0; i < sizes.Length; i++)
            {
                positions[i] = Math.Min(occupied, totalSize - sizes[i]);
                lastSpace = Math.Min(space, totalSize - positions[i] - sizes[i]);
                occupied = positions[i] + sizes[i] + lastSpace;
            }
            occupied -= lastSpace;
            freeSpace = totalSize - occupied;
        }
        if (freeSpace <= 0) return;
        var bias = Alignment switch
        {
            HorizontalAlignment.Left => -1f,
            HorizontalAlignment.Center => 0f,
            HorizontalAlignment.Right => 1f,
            _ => throw new ArgumentException("A spaced group supports Left, Center or Right alignment.", nameof(Alignment))
        };
        if (direction == FlowDirection.RightToLeft) bias = -bias;
        var groupPosition = MathUtils.RoundToInt(freeSpace / 2f * (1 + bias));
        var offset = rtl ? groupPosition - freeSpace : groupPosition;
        for (var i = 0; i < sizes.Length; i++) positions[i] += offset;
    }
}

public sealed class MaterialHorizontalArrangementConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string text ? MaterialHorizontalArrangement.Parse(text) : base.ConvertFrom(context, culture, value);
}
