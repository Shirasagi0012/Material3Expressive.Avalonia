using Avalonia;
using Avalonia.Media;

namespace MaterialSkiaShadow;

/// <summary>The Compose Desktop point-light configuration in physical pixels.</summary>
public readonly record struct ComposeShadowLighting(
    float LightX,
    float LightY,
    float LightZ,
    float LightRadius,
    float AmbientAlpha,
    float SpotAlpha,
    PixelSize Viewport)
{
    public const float DefaultLightYDp = 0f;
    public const float DefaultLightZDp = 500f;
    public const float DefaultLightRadiusDp = 800f;
    public const float DefaultAmbientAlpha = 0.039f;
    public const float DefaultSpotAlpha = 0.19f;
    public const float ReferenceViewportDp = 450f;

    /// <summary>
    /// Reproduces Compose Desktop's <c>SkiaGraphicsContext.setLightingInfo</c> calculation.
    /// </summary>
    public static ComposeShadowLighting ForViewport(Size viewportDip, double renderScaling)
    {
        if (viewportDip.Width <= 0 || viewportDip.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewportDip));
        if (!double.IsFinite(renderScaling) || renderScaling <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderScaling));

        var width = checked((int)Math.Round(viewportDip.Width * renderScaling));
        var height = checked((int)Math.Round(viewportDip.Height * renderScaling));
        var scale = (float)renderScaling;
        var zRatio = MathF.Min(width, height) / (ReferenceViewportDp * scale);
        var zWeightedAdjustment = (zRatio + 2f) / 3f;

        return new ComposeShadowLighting(
            width / 2f,
            DefaultLightYDp * scale,
            DefaultLightZDp * scale * zWeightedAdjustment,
            DefaultLightRadiusDp * scale,
            DefaultAmbientAlpha,
            DefaultSpotAlpha,
            new PixelSize(width, height));
    }
}

/// <summary>A flat convex occluder supported by the first-stage renderer.</summary>
public readonly record struct ComposeShadowShape(Rect Bounds, CornerRadius Corners)
{
    public static ComposeShadowShape Rectangle(Rect bounds) => new(bounds, default);

    public static ComposeShadowShape RoundedRectangle(Rect bounds, CornerRadius corners) =>
        new(bounds, corners);

    public static ComposeShadowShape Circle(Rect bounds)
    {
        if (Math.Abs(bounds.Width - bounds.Height) > 1e-6)
            throw new ArgumentException("A circular shadow shape must have equal width and height.", nameof(bounds));
        return new ComposeShadowShape(bounds, new CornerRadius(bounds.Width / 2));
    }
}

/// <summary>Material shadow inputs before conversion to device-space Skia parameters.</summary>
public readonly record struct ComposeShadowParameters(
    double Elevation,
    Color AmbientColor,
    Color SpotColor,
    double Opacity = 1,
    bool TransparentOccluder = false)
{
    public static ComposeShadowParameters Material(double elevation, Color shadowColor) =>
        new(elevation, shadowColor, shadowColor);
}

/// <summary>Fully resolved parameters sufficient to reproduce one shadow capture.</summary>
public readonly record struct ResolvedComposeShadowParameters(
    ComposeShadowShape Shape,
    ComposeShadowLighting Lighting,
    float ElevationPx,
    Color AmbientColor,
    Color SpotColor,
    float Opacity,
    bool TransparentOccluder,
    double RenderScaling);

/// <summary>Canonical Compose/Skiko shadow constants and parameter resolution.</summary>
public static class ComposeShadowContract
{
    public const string SkiaBehaviorVersion = "m150";

    public static ResolvedComposeShadowParameters Resolve(
        ComposeShadowShape shape,
        ComposeShadowParameters parameters,
        ComposeShadowLighting lighting,
        double renderScaling,
        double inheritedOpacity = 1)
    {
        if (shape.Bounds.Width <= 0 || shape.Bounds.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(shape));
        if (!double.IsFinite(parameters.Elevation) || parameters.Elevation < 0)
            throw new ArgumentOutOfRangeException(nameof(parameters));
        if (!double.IsFinite(parameters.Opacity) || parameters.Opacity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(parameters));
        if (!double.IsFinite(inheritedOpacity) || inheritedOpacity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(inheritedOpacity));
        if (!double.IsFinite(renderScaling) || renderScaling <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderScaling));

        var opacity = (float)(parameters.Opacity * inheritedOpacity);
        return new ResolvedComposeShadowParameters(
            shape,
            lighting,
            (float)(parameters.Elevation * renderScaling),
            parameters.AmbientColor,
            parameters.SpotColor,
            opacity,
            parameters.TransparentOccluder || opacity < 1f,
            renderScaling);
    }
}
