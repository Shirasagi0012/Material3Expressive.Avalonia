using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using MaterialSkiaShadow.Internal;
using SkiaSharp;

namespace MaterialSkiaShadow;

/// <summary>Draws Compose-compatible elevation shadows through Avalonia's Skia backend.</summary>
public static class ComposeShadowRenderer
{
    /// <summary>
    /// Adds a shadow custom draw operation at the current drawing-context transform.
    /// Call this immediately before drawing the occluder fill.
    /// </summary>
    public static void Draw(
        DrawingContext context,
        Visual owner,
        ComposeShadowShape shape,
        ComposeShadowParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(owner);
        if (parameters.Elevation <= 0 || shape.Bounds.Width <= 0 || shape.Bounds.Height <= 0)
            return;

        var topLevel = TopLevel.GetTopLevel(owner)
            ?? throw new InvalidOperationException("A Compose shadow can only be drawn by a visual attached to a TopLevel.");
        var lighting = ComposeShadowLighting.ForViewport(topLevel.ClientSize, topLevel.RenderScaling);

        // The operation may cast outside the owner. Use the top-level rectangle expressed in the
        // owner's local coordinates as a conservative first-stage scene-graph bound. The actual
        // mesh remains tightly bounded and the current parent clip is preserved by Skia.
        var topLevelOrigin = topLevel.TranslatePoint(default, owner) ?? default;
        var operationBounds = new Rect(topLevelOrigin, topLevel.ClientSize);
        context.Custom(new ShadowDrawOperation(operationBounds, shape, parameters, lighting, topLevel.RenderScaling));
    }

    private sealed class ShadowDrawOperation : ICustomDrawOperation
    {
        private static readonly SKRuntimeEffect s_rasterGaussianEffect = CreateRasterGaussianEffect();
        private static readonly SKRuntimeEffect s_ganeshGaussianEffect = CreateGaneshGaussianEffect();

        private readonly ComposeShadowShape _shape;
        private readonly ComposeShadowParameters _parameters;
        private readonly ComposeShadowLighting _lighting;
        private readonly double _renderScaling;

        public ShadowDrawOperation(
            Rect bounds,
            ComposeShadowShape shape,
            ComposeShadowParameters parameters,
            ComposeShadowLighting lighting,
            double renderScaling)
        {
            Bounds = bounds;
            _shape = shape;
            _parameters = parameters;
            _lighting = lighting;
            _renderScaling = renderScaling;
        }

        public Rect Bounds { get; }

        public bool HitTest(Point point) => false;

        public bool Equals(ICustomDrawOperation? other) =>
            other is ShadowDrawOperation operation &&
            Bounds == operation.Bounds &&
            _shape == operation._shape &&
            _parameters == operation._parameters &&
            _lighting == operation._lighting &&
            _renderScaling.Equals(operation._renderScaling);

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
                ?? throw new NotSupportedException("MaterialSkiaShadow requires the Avalonia.Skia rendering backend.");

            using var lease = leaseFeature.Lease();
            var resolved = ComposeShadowContract.Resolve(
                _shape,
                _parameters,
                _lighting,
                _renderScaling,
                lease.CurrentOpacity);
            if (resolved.ElevationPx <= 0 || resolved.Opacity <= 0)
                return;

            var canvas = lease.SkCanvas;
            var gaussianEffect = lease.GrContext is null
                ? s_rasterGaussianEffect
                : s_ganeshGaussianEffect;
            var localToDevice = canvas.TotalMatrix;
            if (localToDevice.Persp0 != 0 || localToDevice.Persp1 != 0 || localToDevice.Persp2 != 1)
                throw new NotSupportedException("Perspective transforms are outside MaterialSkiaShadow's first-stage contract.");

            using var path = CreatePath(_shape);
            var lightPosition = new SKPoint(_lighting.LightX, _lighting.LightY);
            var ambient = ConvexShadowTessellator.MakeAmbient(
                path,
                localToDevice,
                resolved.ElevationPx,
                resolved.TransparentOccluder);
            var spot = ConvexShadowTessellator.MakeSpot(
                path,
                localToDevice,
                resolved.ElevationPx,
                lightPosition,
                _lighting.LightZ,
                _lighting.LightRadius,
                resolved.TransparentOccluder);

            var saveCount = canvas.Save();
            try
            {
                canvas.SetMatrix(SKMatrix.Identity);
                var ambientColor = MultiplyAlpha(
                    resolved.AmbientColor,
                    _lighting.AmbientAlpha * resolved.Opacity);
                if (ambient is null)
                    DrawAmbientBlurFallback(canvas, path, localToDevice, resolved.ElevationPx, ambientColor);
                else
                    DrawMesh(canvas, ambient, ambientColor, gaussianEffect);

                var spotColor = MultiplyAlpha(
                    resolved.SpotColor,
                    _lighting.SpotAlpha * resolved.Opacity);
                if (spot is null)
                {
                    DrawSpotBlurFallback(
                        canvas,
                        path,
                        localToDevice,
                        resolved.ElevationPx,
                        lightPosition,
                        _lighting.LightZ,
                        _lighting.LightRadius,
                        spotColor);
                }
                else
                {
                    DrawMesh(canvas, spot, spotColor, gaussianEffect);
                }
            }
            finally
            {
                canvas.RestoreToCount(saveCount);
            }
        }

        public void Dispose()
        {
        }

        private static void DrawMesh(
            SKCanvas canvas,
            ShadowMesh mesh,
            SKColor color,
            SKRuntimeEffect gaussianEffect)
        {
            if (mesh.IsEmpty || color.Alpha == 0)
                return;

            using var vertices = mesh.ToVertices();
            using var uniforms = new SKRuntimeEffectUniforms(gaussianEffect);
            using var gaussian = gaussianEffect.ToColorFilter(uniforms);
            using var modulate = SKColorFilter.CreateBlendMode(color, SKBlendMode.Modulate);
            using var composed = SKColorFilter.CreateCompose(modulate, gaussian);
            using var paint = new SKPaint { ColorFilter = composed };
            canvas.DrawVertices(vertices, SKBlendMode.Dst, paint);
        }

        private static void DrawAmbientBlurFallback(
            SKCanvas canvas,
            SKPath path,
            SKMatrix localToDevice,
            float elevationPx,
            SKColor color)
        {
            if (color.Alpha == 0)
                return;

            var outset = ShadowMetrics.AmbientBlurRadius(elevationPx);
            var reciprocalAlpha = ShadowMetrics.AmbientReciprocalAlpha(elevationPx);
            var blurRadius = 0.5f * outset * reciprocalAlpha;
            var strokeWidth = 0.5f * (outset - blurRadius);

            using var devicePath = new SKPath();
            path.Transform(localToDevice, devicePath);
            using var maskFilter = SKMaskFilter.CreateBlur(
                SKBlurStyle.Normal,
                SKMaskFilter.ConvertRadiusToSigma(blurRadius),
                respectCTM: false);
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.StrokeAndFill,
                StrokeWidth = strokeWidth,
                MaskFilter = maskFilter
            };
            canvas.DrawPath(devicePath, paint);
        }

        private static void DrawSpotBlurFallback(
            SKCanvas canvas,
            SKPath path,
            SKMatrix localToDevice,
            float elevationPx,
            SKPoint lightPosition,
            float lightZ,
            float lightRadius,
            SKColor color)
        {
            if (color.Alpha == 0)
                return;

            var spot = ShadowMetrics.GetSpotParameters(
                elevationPx,
                lightPosition.X,
                lightPosition.Y,
                lightZ,
                lightRadius);
            using var devicePath = new SKPath();
            path.Transform(localToDevice, devicePath);
            using var shadowPath = new SKPath();
            devicePath.Transform(
                new SKMatrix(
                    spot.Scale, 0, spot.Translation.X,
                    0, spot.Scale, spot.Translation.Y,
                    0, 0, 1),
                shadowPath);
            using var maskFilter = SKMaskFilter.CreateBlur(
                SKBlurStyle.Normal,
                SKMaskFilter.ConvertRadiusToSigma(spot.Radius),
                respectCTM: false);
            using var paint = new SKPaint
            {
                Color = color,
                MaskFilter = maskFilter
            };
            canvas.DrawPath(shadowPath, paint);
        }

        private static SKPath CreatePath(ComposeShadowShape shape)
        {
            var bounds = shape.Bounds;
            var rect = new SKRect(
                (float)bounds.Left,
                (float)bounds.Top,
                (float)bounds.Right,
                (float)bounds.Bottom);
            using var builder = new SKPathBuilder();
            if (shape.Corners == default)
            {
                builder.AddRect(rect);
            }
            else
            {
                using var rounded = new SKRoundRect();
                Span<SKPoint> radii =
                [
                    Radius(shape.Corners.TopLeft),
                    Radius(shape.Corners.TopRight),
                    Radius(shape.Corners.BottomRight),
                    Radius(shape.Corners.BottomLeft)
                ];
                rounded.SetRectRadii(rect, radii);
                builder.AddRoundRect(rounded, SKPathDirection.Clockwise);
            }

            return builder.Detach();
        }

        private static SKPoint Radius(double radius)
        {
            var value = (float)Math.Max(0, radius);
            return new SKPoint(value, value);
        }

        private static SKColor MultiplyAlpha(Color color, float alpha)
        {
            var resultAlpha = (byte)Math.Clamp((int)(color.A * alpha), 0, 255);
            return new SKColor(color.R, color.G, color.B, resultAlpha);
        }

        private static SKRuntimeEffect CreateRasterGaussianEffect()
        {
            const string source = """
                half4 main(half4 color) {
                    float a = color.a;
                    const float c4 = -2.26661229133605957031;
                    const float c3 =  2.89795351028442382812;
                    const float c2 =  0.21345567703247070312;
                    const float c1 =  0.15489584207534790039;
                    const float c0 =  0.00030726194381713867;
                    a = a * (a * (a * (a * c4 + c3) + c2) + c1) + c0;
                    return half4(a, a, a, a);
                }
                """;
            var effect = SKRuntimeEffect.CreateColorFilter(source, out var errors);
            return effect ?? throw new InvalidOperationException($"Unable to compile the Skia m150 raster Gaussian color filter: {errors}");
        }

        private static SKRuntimeEffect CreateGaneshGaussianEffect()
        {
            // Skia's Ganesh implementation deliberately uses its shader-native exponential,
            // while the CPU raster pipeline uses the quartic approximation above. Selecting by
            // Avalonia's active GrContext reproduces that backend-dependent Skia behavior.
            const string source = """
                half4 main(half4 color) {
                    half factor = 1 - color.a;
                    factor = exp(-factor * factor * 4) - 0.018;
                    return half4(factor);
                }
                """;
            var effect = SKRuntimeEffect.CreateColorFilter(source, out var errors);
            return effect ?? throw new InvalidOperationException($"Unable to compile the Skia m150 Ganesh Gaussian color filter: {errors}");
        }
    }
}
