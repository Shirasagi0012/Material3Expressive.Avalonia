namespace Material3Expressive.Foundation.Tokens;

/// <summary>
/// Constrained derivation: the value of a color token with an opacity applied (design doc §5.3),
/// e.g. "onSurface @ 12%" for disabled states. Evaluated at the consumption side — the inner
/// references resolve from the requester's position and therefore follow local overrides like
/// any alias.
/// </summary>
public sealed class WithAlpha
{
    /// <summary>Opacity comes from another (double) token key.</summary>
    public WithAlpha(MaterialTokenKey color, MaterialTokenKey opacity)
    {
        Color = color ?? throw new ArgumentNullException(nameof(color));
        Opacity = opacity ?? throw new ArgumentNullException(nameof(opacity));
    }

    /// <summary>Opacity is a literal in the 0–1 range.</summary>
    public WithAlpha(MaterialTokenKey color, double opacity)
    {
        Color = color ?? throw new ArgumentNullException(nameof(color));
        OpacityValue = opacity;
    }

    /// <summary>Key resolving to the base <see cref="Avalonia.Media.Color"/>.</summary>
    public MaterialTokenKey Color { get; }

    /// <summary>Key resolving to the opacity (double, 0–1), when it is not a literal.</summary>
    public MaterialTokenKey? Opacity { get; }

    /// <summary>Literal opacity (0–1), when it is not a key reference.</summary>
    public double? OpacityValue { get; }

    public override string ToString() =>
        $"WithAlpha({Color}, {(Opacity is { } key ? key.ToString() : OpacityValue?.ToString())})";
}
