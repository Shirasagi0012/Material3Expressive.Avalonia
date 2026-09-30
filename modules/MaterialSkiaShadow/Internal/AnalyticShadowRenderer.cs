/*
 * Copyright 2016 Google Inc.
 *
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE.Skia.txt file.
 *
 * Managed adaptation of Ganesh SurfaceDrawContext::drawFastShadow,
 * ShadowRRectOp and GrRRectShadowGeoProc at Skia 1f14f1166a.
 */

using SkiaSharp;

namespace MaterialSkiaShadow.Internal;

internal static class AnalyticShadowRenderer
{
    private const float NearlyZero = 1f / 4096f;
    private static readonly SKRuntimeEffect s_effect = CreateEffect();
    private static readonly SKImage s_falloffImage = CreateFalloffImage();
    private static readonly SKShader s_falloffShader = s_falloffImage.ToShader(
        SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
        new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));

    private static readonly ushort[] s_fillCircleIndices =
    [
        0, 1, 8, 1, 2, 8, 2, 3, 8, 3, 4, 8,
        4, 5, 8, 5, 6, 8, 6, 7, 8, 7, 0, 8
    ];

    private static readonly ushort[] s_strokeCircleIndices =
    [
        0, 1, 9, 0, 9, 8, 1, 2, 10, 1, 10, 9,
        2, 3, 11, 2, 11, 10, 3, 4, 12, 3, 12, 11,
        4, 5, 13, 4, 13, 12, 5, 6, 14, 5, 14, 13,
        6, 7, 15, 6, 15, 14, 7, 0, 8, 7, 8, 15
    ];

    private static readonly ushort[] s_rrectIndices =
    [
        // Overstroke quads; omitted for fills and ordinary strokes.
        0, 6, 25, 0, 25, 24, 6, 18, 27, 6, 27, 25,
        18, 12, 26, 18, 26, 27, 12, 0, 24, 12, 24, 26,
        // Corner fans.
        0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5,
        6, 11, 10, 6, 10, 9, 6, 9, 8, 6, 8, 7,
        12, 17, 16, 12, 16, 15, 12, 15, 14, 12, 14, 13,
        18, 19, 20, 18, 20, 21, 18, 21, 22, 18, 22, 23,
        // Edges.
        0, 5, 11, 0, 11, 6, 6, 7, 19, 6, 19, 18,
        18, 23, 17, 18, 17, 12, 12, 13, 1, 12, 1, 0,
        // Center fill; omitted for strokes.
        0, 6, 18, 0, 18, 12
    ];

    public static bool TryDraw(
        SKCanvas canvas,
        SKPath path,
        SKMatrix localToDevice,
        float elevationPx,
        SKPoint deviceLightPosition,
        float lightZ,
        float lightRadius,
        bool transparent,
        SKColor ambientColor,
        SKColor spotColor)
    {
        // Ganesh requires both rectStaysRect() and isSimilarity(): arbitrary rotations,
        // nonuniform scales and skew keep using the general tessellator on the same GPU canvas.
        if (!IsRectPreservingSimilarity(localToDevice) || !localToDevice.TryInvert(out var inverse))
            return false;

        using var rrect = path.GetRoundRect() ?? new SKRoundRect();
        var radius = rrect.GetRadii(SKRoundRectCorner.UpperLeft).X;
        var radii = rrect.Radii;
        var simpleCircular = radius > NearlyZero && radii.All(point =>
            MathF.Abs(point.X - radius) <= NearlyZero && MathF.Abs(point.Y - radius) <= NearlyZero);
        if (!simpleCircular)
        {
            var bounds = path.Bounds;
            if (path.IsOval && MathF.Abs(bounds.Width - bounds.Height) <= NearlyZero && bounds.Width > NearlyZero)
                rrect.SetOval(bounds);
            else if (path.IsRect)
                rrect.SetRect(bounds);
            else
                return false;
        }

        var rect = rrect.Rect;
        if (rect.IsEmpty)
            return true;
        radius = rrect.GetRadii(SKRoundRectCorner.UpperLeft).X;
        var isOval = rrect.Type == SKRoundRectType.Oval;
        var inverseScale = localToDevice.SkewX == 0 && localToDevice.SkewY == 0
            ? 1f / MathF.Abs(localToDevice.ScaleX)
            : 1f / MathF.Sqrt(localToDevice.ScaleX * localToDevice.ScaleX +
                             localToDevice.SkewX * localToDevice.SkewX);

        var saveCount = canvas.Save();
        try
        {
            canvas.SetMatrix(SKMatrix.Identity);
            if (ambientColor.Alpha > 0)
            {
                var insetWidth = ShadowMetrics.AmbientBlurRadius(elevationPx);
                var blurWidth = insetWidth * ShadowMetrics.AmbientReciprocalAlpha(elevationPx);
                var outset = insetWidth * inverseScale;
                var ambientRect = rect;
                ambientRect.Inflate(outset, outset);
                using var ambient = new SKRoundRect();
                if (isOval)
                    ambient.SetOval(ambientRect);
                else
                    ambient.SetRect(ambientRect, radius + outset, radius + outset);
                if (transparent)
                    insetWidth = ambient.Width;
                DrawRRect(canvas, ambientColor, localToDevice, ambient, blurWidth, insetWidth);
            }

            if (spotColor.Alpha > 0)
            {
                var spot = ShadowMetrics.GetSpotParameters(
                    elevationPx, deviceLightPosition.X, deviceLightPosition.Y, lightZ, lightRadius);
                var sourceBlur = spot.Radius * inverseScale;
                var offset = inverse.MapPoint(new SKPoint(
                    spot.Translation.X + spot.Scale * localToDevice.TransX,
                    spot.Translation.Y + spot.Scale * localToDevice.TransY));
                var shadowTransform = new SKMatrix(spot.Scale, 0, offset.X, 0, spot.Scale, offset.Y, 0, 0, 1);
                using var shadow = rrect.Transform(shadowTransform);
                var shadowRect = shadow.Rect;
                var shadowRadius = shadow.GetRadii(SKRoundRectCorner.UpperLeft).X;
                var insetWidth = sourceBlur;
                if (transparent)
                {
                    insetWidth += shadow.Width;
                }
                else
                {
                    float maxOffset;
                    if (rrect.Type == SKRoundRectType.Rect)
                    {
                        maxOffset = MathF.Max(
                            MathF.Max(MathF.Abs(shadowRect.Left - rect.Left), MathF.Abs(shadowRect.Top - rect.Top)),
                            MathF.Max(MathF.Abs(shadowRect.Right - rect.Right), MathF.Abs(shadowRect.Bottom - rect.Bottom)));
                    }
                    else
                    {
                        var dr = shadowRadius - radius;
                        var upperLeft = new SKPoint(shadowRect.Left - rect.Left + dr, shadowRect.Top - rect.Top + dr);
                        var lowerRight = new SKPoint(shadowRect.Right - rect.Right - dr, shadowRect.Bottom - rect.Bottom - dr);
                        maxOffset = MathF.Sqrt(MathF.Max(
                            upperLeft.X * upperLeft.X + upperLeft.Y * upperLeft.Y,
                            lowerRight.X * lowerRight.X + lowerRight.Y * lowerRight.Y)) + dr;
                    }
                    insetWidth += MathF.Max(sourceBlur, maxOffset);
                }

                shadowRect.Inflate(sourceBlur, sourceBlur);
                if (shadow.Type == SKRoundRectType.Oval)
                    shadow.SetOval(shadowRect);
                else
                    shadow.SetRect(shadowRect, shadowRadius + sourceBlur, shadowRadius + sourceBlur);
                DrawRRect(canvas, spotColor, localToDevice, shadow, 2f * spot.Radius, insetWidth);
            }
        }
        finally
        {
            canvas.RestoreToCount(saveCount);
        }
        return true;
    }

    private static bool IsRectPreservingSimilarity(SKMatrix matrix)
    {
        if (matrix.Persp0 != 0 || matrix.Persp1 != 0 || matrix.Persp2 != 1)
            return false;
        if (matrix.SkewX == 0 && matrix.SkewY == 0)
            return MathF.Abs(matrix.ScaleX) > NearlyZero &&
                   MathF.Abs(MathF.Abs(matrix.ScaleX) - MathF.Abs(matrix.ScaleY)) <= NearlyZero;
        return matrix.ScaleX == 0 && matrix.ScaleY == 0 &&
               MathF.Abs(matrix.SkewX * matrix.SkewY) > NearlyZero * NearlyZero &&
               MathF.Abs(MathF.Abs(matrix.SkewX) - MathF.Abs(matrix.SkewY)) <= NearlyZero;
    }

    private static void DrawRRect(
        SKCanvas canvas, SKColor color, SKMatrix matrix, SKRoundRect rrect, float blurWidth, float insetWidth)
    {
        var bounds = matrix.MapRect(rrect.Rect);
        var matrixFactor = matrix.ScaleX + matrix.SkewX;
        var outerRadius = MathF.Abs(rrect.GetRadii(SKRoundRectCorner.UpperLeft).X * matrixFactor);
        insetWidth = MathF.Abs(insetWidth * matrixFactor);
        if (insetWidth <= 0)
            return;

        var isCircle = rrect.Type == SKRoundRectType.Oval;
        var umbraInset = isCircle ? 0 : MathF.Max(outerRadius, blurWidth);
        var innerRadius = isCircle ? outerRadius - insetWidth : 0;
        var filled = isCircle ? innerRadius <= 0 : insetWidth > 0.5f * MathF.Min(bounds.Width, bounds.Height);
        if (!isCircle && !filled)
            innerRadius = MathF.Max(insetWidth - umbraInset, 0);
        var overstroke = !isCircle && !filled && innerRadius > 0;
        var count = isCircle ? filled ? 9 : 16 : overstroke ? 28 : 24;
        var positions = new SKPoint[count];
        var offsets = new SKPoint[count];
        var vertex = 0;
        void Add(float x, float y, float offsetX, float offsetY)
        {
            positions[vertex] = new SKPoint(x, y);
            offsets[vertex++] = new SKPoint(offsetX, offsetY);
        }

        float distanceCorrection;
        ushort[] indices;
        if (isCircle)
        {
            distanceCorrection = outerRadius / blurWidth;
            var cx = bounds.MidX;
            var cy = bounds.MidY;
            var halfWidth = 0.5f * bounds.Width;
            const float octOffset = 0.41421356237f;
            Add(cx - octOffset * halfWidth, cy - halfWidth, -octOffset, -1);
            Add(cx + octOffset * halfWidth, cy - halfWidth, octOffset, -1);
            Add(cx + halfWidth, cy - octOffset * halfWidth, 1, -octOffset);
            Add(cx + halfWidth, cy + octOffset * halfWidth, 1, octOffset);
            Add(cx + octOffset * halfWidth, cy + halfWidth, octOffset, 1);
            Add(cx - octOffset * halfWidth, cy + halfWidth, -octOffset, 1);
            Add(cx - halfWidth, cy + octOffset * halfWidth, -1, octOffset);
            Add(cx - halfWidth, cy - octOffset * halfWidth, -1, -octOffset);
            if (filled)
            {
                Add(cx, cy, 0, 0);
            }
            else
            {
                const float s = 0.382683432f;
                // Preserve the pinned source's literal, including its difference from cos(pi/8).
                const float c = 0.923579533f;
                var normalizedInner = innerRadius / outerRadius;
                Add(cx - s * innerRadius, cy - c * innerRadius, -s * normalizedInner, -c * normalizedInner);
                Add(cx + s * innerRadius, cy - c * innerRadius, s * normalizedInner, -c * normalizedInner);
                Add(cx + c * innerRadius, cy - s * innerRadius, c * normalizedInner, -s * normalizedInner);
                Add(cx + c * innerRadius, cy + s * innerRadius, c * normalizedInner, s * normalizedInner);
                Add(cx + s * innerRadius, cy + c * innerRadius, s * normalizedInner, c * normalizedInner);
                Add(cx - s * innerRadius, cy + c * innerRadius, -s * normalizedInner, c * normalizedInner);
                Add(cx - c * innerRadius, cy + s * innerRadius, -c * normalizedInner, s * normalizedInner);
                Add(cx - c * innerRadius, cy - s * innerRadius, -c * normalizedInner, -s * normalizedInner);
            }
            indices = filled ? s_fillCircleIndices : s_strokeCircleIndices;
        }
        else
        {
            umbraInset = MathF.Min(umbraInset, 0.5f * MathF.Min(bounds.Width, bounds.Height));
            Span<float> xInner = [bounds.Left + umbraInset, bounds.Right - umbraInset, bounds.Left + umbraInset, bounds.Right - umbraInset];
            Span<float> xMid = [bounds.Left + outerRadius, bounds.Right - outerRadius, bounds.Left + outerRadius, bounds.Right - outerRadius];
            Span<float> xOuter = [bounds.Left, bounds.Right, bounds.Left, bounds.Right];
            Span<float> yInner = [bounds.Top + umbraInset, bounds.Top + umbraInset, bounds.Bottom - umbraInset, bounds.Bottom - umbraInset];
            Span<float> yMid = [bounds.Top + outerRadius, bounds.Top + outerRadius, bounds.Bottom - outerRadius, bounds.Bottom - outerRadius];
            Span<float> yOuter = [bounds.Top, bounds.Top, bounds.Bottom, bounds.Bottom];
            var vx = outerRadius - umbraInset;
            var vy = -outerRadius - umbraInset;
            // SkPoint::normalize computes the magnitude and scale in double.
            var inverseLength = 1.0 / Math.Sqrt((double)vx * vx + (double)vy * vy);
            vx = (float)(vx * inverseLength);
            vy = (float)(vy * inverseLength);
            var diagonal = umbraInset / (1.41421356237f * (outerRadius - umbraInset) - outerRadius);
            distanceCorrection = umbraInset / blurWidth;
            for (var corner = 0; corner < 4; corner++)
            {
                Add(xInner[corner], yInner[corner], 0, 0);
                Add(xOuter[corner], yInner[corner], 0, -1);
                Add(xOuter[corner], yMid[corner], vx, vy);
                Add(xOuter[corner], yOuter[corner], diagonal, diagonal);
                Add(xMid[corner], yOuter[corner], vx, vy);
                Add(xInner[corner], yOuter[corner], 0, -1);
            }
            if (overstroke)
            {
                var inset = umbraInset + innerRadius;
                Add(bounds.Left + inset, bounds.Top + inset, 0, 0);
                Add(bounds.Right - inset, bounds.Top + inset, 0, 0);
                Add(bounds.Left + inset, bounds.Bottom - inset, 0, 0);
                Add(bounds.Right - inset, bounds.Bottom - inset, 0, 0);
            }
            var start = overstroke ? 0 : 24;
            var end = filled ? s_rrectIndices.Length : s_rrectIndices.Length - 6;
            indices = s_rrectIndices.AsSpan(start, end - start).ToArray();
        }

        // SKVertices exposes two texture coordinates. The original third varying is constant
        // within this draw, so it becomes a uniform; no custom native geometry processor is needed.
        using var vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, positions, offsets, null, indices);
        using var uniforms = new SKRuntimeEffectUniforms(s_effect)
        {
            ["distanceCorrection"] = distanceCorrection,
            ["shadowColor"] = new[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f }
        };
        using var children = new SKRuntimeEffectChildren(s_effect) { ["falloff"] = s_falloffShader };
        using var shader = s_effect.ToShader(uniforms, children);
        using var paint = new SKPaint { Shader = shader, IsAntialias = false };
        canvas.DrawVertices(vertices, SKBlendMode.Dst, paint);
    }

    private static SKRuntimeEffect CreateEffect()
    {
        const string source = """
            uniform shader falloff;
            uniform float distanceCorrection;
            layout(color) uniform float4 shadowColor;
            half4 main(float2 offset) {
                half3 shadowParams = half3(offset, distanceCorrection);
                half d = length(shadowParams.xy);
                float2 uv = float2(shadowParams.z * (1.0 - d), 0.5);
                half factor = falloff.eval(float2(uv.x * 128.0, uv.y)).a;
                // ShadowRRectOp transforms to destination space, premultiplies and rounds to
                // RGBA8888 before passing the color to the geometry processor.
                float4 pm = float4(shadowColor.rgb * shadowColor.a, shadowColor.a);
                half4 color = half4(floor(pm * 255.0 + 0.5) / 255.0);
                return color * factor;
            }
            """;
        return SKRuntimeEffect.CreateShader(source, out var errors)
            ?? throw new InvalidOperationException($"Unable to compile the Skia analytic shadow shader: {errors}");
    }

    private static unsafe SKImage CreateFalloffImage()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(128, 1, SKColorType.Alpha8, SKAlphaType.Premul));
        var values = (byte*)bitmap.GetPixels();
        for (var index = 0; index < 128; index++)
        {
            var d = 1f - index / 127f;
            values[index] = (byte)MathF.Floor((MathF.Exp(-4f * d * d) - 0.018f) * 255f + 0.5f);
        }
        bitmap.SetImmutable();
        return SKImage.FromBitmap(bitmap);
    }
}
