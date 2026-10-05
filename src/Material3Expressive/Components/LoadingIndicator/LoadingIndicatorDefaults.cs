using Avalonia;
using MaterialShapes;

namespace Material3Expressive.Components;

/// <summary>The fixed Compose LoadingIndicatorDefaults polygon sequences.</summary>
public static class LoadingIndicatorDefaults
{
    public static IReadOnlyList<RoundedPolygon> IndeterminateIndicatorPolygons { get; } = Array.AsReadOnly<RoundedPolygon>(
        [MaterialShapeCatalog.SoftBurst, MaterialShapeCatalog.Cookie9Sided, MaterialShapeCatalog.Pentagon,
            MaterialShapeCatalog.Pill, MaterialShapeCatalog.Sunny, MaterialShapeCatalog.Cookie4Sided, MaterialShapeCatalog.Oval]);
    public static IReadOnlyList<RoundedPolygon> DeterminateIndicatorPolygons { get; } = Array.AsReadOnly<RoundedPolygon>(
        [MaterialShapeCatalog.Circle.Transformed(Matrix.CreateRotation(18 * Math.PI / 180)), MaterialShapeCatalog.SoftBurst]);
}
