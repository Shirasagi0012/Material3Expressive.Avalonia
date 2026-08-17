using Avalonia.Media;
using Avalonia.Media.Immutable;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready values used by <see cref="MaterialSurface" />.
/// </summary>
/// <remarks>
/// Surface keeps the semantic colours together because its renderer needs both the brushes used
/// by ordinary Avalonia properties and the tint/shadow colours used by the late tonal-elevation
/// policy. The profile does not contain elevation, bounds or a resolved geometry; those remain
/// properties of the surface itself.
/// </remarks>
public static class SurfaceValueKeys
{
    /// <summary>Gets the surface brushes and renderer colours for the current Material scope.</summary>
    public static MaterialValueKey<SurfaceVisualColors> VisualColors { get; } =
        new(
            "Value.Surface.VisualColors",
            new RawRecipePlan<SurfaceVisualColors>(
                new SurfaceVisualColorsRecipe(
                    SystemColorTokens.Surface,
                    SystemColorTokens.OnSurface,
                    SystemColorTokens.SurfaceTint,
                    SystemColorTokens.Shadow)));
}

/// <summary>Immutable colour values consumed by a <see cref="MaterialSurface" />.</summary>
public sealed record SurfaceVisualColors(
    IImmutableBrush SurfaceBrush,
    IImmutableBrush OnSurfaceBrush,
    Color SurfaceTintColor,
    Color ShadowColor);

internal sealed class SurfaceVisualColorsRecipe : IRawValueRecipe<SurfaceVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _surface;
    private readonly MaterialToken<Color> _onSurface;
    private readonly MaterialToken<Color> _surfaceTint;
    private readonly MaterialToken<Color> _shadow;

    internal SurfaceVisualColorsRecipe(
        MaterialToken<Color> surface,
        MaterialToken<Color> onSurface,
        MaterialToken<Color> surfaceTint,
        MaterialToken<Color> shadow)
    {
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        _onSurface = onSurface ?? throw new ArgumentNullException(nameof(onSurface));
        _surfaceTint = surfaceTint ?? throw new ArgumentNullException(nameof(surfaceTint));
        _shadow = shadow ?? throw new ArgumentNullException(nameof(shadow));
        _dependencies = Array.AsReadOnly<MaterialToken>([surface, onSurface, surfaceTint, shadow]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SurfaceVisualColors Evaluate(MaterialValueContext context) => new(
        new ImmutableSolidColorBrush(context.Get(_surface)),
        new ImmutableSolidColorBrush(context.Get(_onSurface)),
        context.Get(_surfaceTint),
        context.Get(_shadow));
}
