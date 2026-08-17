using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>Creates Material value endpoints from the supported, inspectable raw projections.</summary>
public static class MaterialValue
{
    /// <summary>Creates an endpoint which returns one raw token unchanged.</summary>
    public static MaterialValueKey<T> FromRaw<T>(string path, MaterialToken<T> token) =>
        new(path, new DirectRawPlan<T>(token));

    /// <summary>Creates an immutable solid brush endpoint from a color token.</summary>
    public static MaterialValueKey<IImmutableBrush> SolidBrush(
        string path,
        MaterialToken<Color> color) =>
        new(path, new RawRecipePlan<IImmutableBrush>(new SolidBrushRecipe(color, null)));

    /// <summary>Creates an immutable solid brush endpoint by scaling a color token's alpha.</summary>
    public static MaterialValueKey<IImmutableBrush> SolidBrush(
        string path,
        MaterialToken<Color> color,
        MaterialToken<double> opacity) =>
        new(path, new RawRecipePlan<IImmutableBrush>(new SolidBrushRecipe(color, opacity)));

    /// <summary>Creates an immutable fixed-colour brush endpoint for component policy values.</summary>
    public static MaterialValueKey<IImmutableBrush> SolidBrush(string path, Color color) =>
        new(path, new RawRecipePlan<IImmutableBrush>(new ConstantSolidBrushRecipe(color)));

    /// <summary>Creates a four-sided thickness endpoint.</summary>
    public static MaterialValueKey<Thickness> Thickness(
        string path,
        MaterialToken<double> left,
        MaterialToken<double> top,
        MaterialToken<double> right,
        MaterialToken<double> bottom) =>
        new(path, new RawRecipePlan<Thickness>(new ThicknessRecipe(left, top, right, bottom)));

    /// <summary>Creates a uniform thickness endpoint.</summary>
    public static MaterialValueKey<Thickness> Thickness(
        string path,
        MaterialToken<double> uniform) =>
        Thickness(path, uniform, uniform, uniform, uniform);

    /// <summary>
    /// Creates a horizontal Raw-token thickness endpoint with a fixed vertical component.
    /// </summary>
    /// <remarks>
    /// This bounded operator represents component policy constants without exposing an arbitrary
    /// projector. Its complete Raw dependency set remains inspectable.
    /// </remarks>
    public static MaterialValueKey<Thickness> Thickness(
        string path,
        MaterialToken<double> left,
        double vertical,
        MaterialToken<double> right) =>
        new(path, new RawRecipePlan<Thickness>(
            new HorizontalTokenThicknessRecipe(left, vertical, right)));

    /// <summary>Creates an immutable fixed-thickness endpoint for component policy values.</summary>
    public static MaterialValueKey<Thickness> Thickness(
        string path,
        double left,
        double top,
        double right,
        double bottom) =>
        new(path, new RawRecipePlan<Thickness>(
            new ConstantThicknessRecipe(left, top, right, bottom)));
}
