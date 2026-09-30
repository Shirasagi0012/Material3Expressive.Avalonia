/*
 * Copyright 2017 Google LLC
 *
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE.Skia.txt file.
 *
 * Managed adaptation of Skia m150 SkDrawShadowInfo.
 */

using SkiaSharp;

namespace MaterialSkiaShadow.Internal;

internal static class ShadowMetrics
{
    private const float AmbientHeightFactor = 1f / 128f;
    private const float AmbientGeometryFactor = 64f;
    private const float MaximumAmbientRadius = 300f * AmbientHeightFactor * AmbientGeometryFactor;

    public static float AmbientBlurRadius(float height) =>
        MathF.Min(height * AmbientHeightFactor * AmbientGeometryFactor, MaximumAmbientRadius);

    public static float AmbientReciprocalAlpha(float height) =>
        1f + MathF.Max(height * AmbientHeightFactor, 0f);

    public static (float Radius, float Scale, SKPoint Translation) GetSpotParameters(
        float occluderZ,
        float lightX,
        float lightY,
        float lightZ,
        float lightRadius)
    {
        var zRatio = DivideAndPin(occluderZ, lightZ - occluderZ, 0f, 0.95f);
        return (
            lightRadius * zRatio,
            DivideAndPin(lightZ, lightZ - occluderZ, 1f, 1.95f),
            new SKPoint(-zRatio * lightX, -zRatio * lightY));
    }

    public static SKRect GetLocalBounds(
        SKRect pathBounds,
        float elevationPx,
        SKPoint deviceLightPosition,
        float lightZ,
        float lightRadius,
        float deviceScale)
    {
        var inverseScale = 1f / deviceScale;
        var ambientBlur = AmbientBlurRadius(elevationPx) * inverseScale;
        var spot = GetSpotParameters(
            elevationPx,
            deviceLightPosition.X,
            deviceLightPosition.Y,
            lightZ,
            lightRadius);

        var ambient = pathBounds;
        ambient.Inflate(ambientBlur, ambientBlur);

        var spotBounds = new SKRect(
            pathBounds.Left * spot.Scale,
            pathBounds.Top * spot.Scale,
            pathBounds.Right * spot.Scale,
            pathBounds.Bottom * spot.Scale);
        spotBounds.Offset(spot.Translation.X * inverseScale, spot.Translation.Y * inverseScale);
        spotBounds.Inflate(spot.Radius * inverseScale, spot.Radius * inverseScale);

        var result = SKRect.Union(ambient, spotBounds);
        result.Inflate(1f, 1f);
        return result;
    }

    private static float DivideAndPin(float numerator, float denominator, float min, float max)
    {
        var result = numerator / denominator;
        if (float.IsNaN(result))
            return min;
        return Math.Clamp(result, min, max);
    }
}
