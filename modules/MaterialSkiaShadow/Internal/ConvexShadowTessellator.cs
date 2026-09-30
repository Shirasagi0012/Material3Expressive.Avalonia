/*
 * Copyright 2017 Google LLC
 *
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE.Skia.txt file.
 *
 * Managed, deliberately scoped adaptation of Skia m150 SkShadowTessellator.cpp,
 * SkPolyUtils.cpp, and GrPathUtils.cpp. This implementation accepts one flat simple contour
 * containing lines, quadratics, and conics. Cubics, perspective, tilted Z planes, directional
 * lights fail explicitly.
 */

using SkiaSharp;

namespace MaterialSkiaShadow.Internal;

internal static class ConvexShadowTessellator
{
    public static ShadowMesh? MakeAmbient(
        SKPath path,
        SKMatrix localToDevice,
        float elevationPx,
        bool transparent)
    {
        // AmbientVerticesFactory tessellates without translation, then translates the mesh.
        // Quantizing the already translated contour changes fractional raster coverage.
        var noTranslation = localToDevice;
        noTranslation.TransX = 0;
        noTranslation.TransY = 0;
        var polygon = PathPolygon.CreateAmbient(path, noTranslation);
        var outset = ShadowMetrics.AmbientBlurRadius(elevationPx);
        var inset = outset * ShadowMetrics.AmbientReciprocalAlpha(elevationPx) - outset;
        var mesh = new Builder(polygon.Path, [], elevationPx, transparent, path.Bounds)
            .TryBuild(inset, outset, false);
        if (mesh is not null)
        {
            for (var index = 0; index < mesh.Positions.Count; index++)
            {
                var point = mesh.Positions[index];
                mesh.Positions[index] = new SKPoint(point.X + localToDevice.TransX, point.Y + localToDevice.TransY);
            }
        }
        return mesh;
    }

    public static ShadowMesh? MakeSpot(
        SKPath path,
        SKMatrix localToDevice,
        float elevationPx,
        SKPoint deviceLightPosition,
        float lightZ,
        float lightRadius,
        bool transparent)
    {
        var localCenter = new SKPoint(path.Bounds.MidX, path.Bounds.MidY);
        var deviceCenter = localToDevice.MapPoint(localCenter);
        var relativeSpot = ShadowMetrics.GetSpotParameters(
            elevationPx,
            deviceLightPosition.X - deviceCenter.X,
            deviceLightPosition.Y - deviceCenter.Y,
            lightZ,
            lightRadius);

        // SkShadowUtils normally reaches SkShadowTessellator through SpotVerticesFactory. When
        // the opaque occluder covers the whole umbra, that factory deliberately builds a cached
        // mesh at a canonical zero-translation position with the light centered over the path,
        // then translates the completed vertices. This is mathematically equivalent to building
        // at the final position, but not bit-for-bit equivalent: scale and z-ratio are calculated
        // independently as floats. Reproduce the canonical route so raster edge coverage matches
        // Skia m150, especially for low elevation in short viewports.
        var deviceBounds = localToDevice.MapRect(path.Bounds);
        var offsetLength = MathF.Sqrt(
            relativeSpot.Translation.X * relativeSpot.Translation.X +
            relativeSpot.Translation.Y * relativeSpot.Translation.Y);
        var transparentUmbra = transparent ||
            MathF.Abs(relativeSpot.Translation.X) > 0.5f * deviceBounds.Width ||
            MathF.Abs(relativeSpot.Translation.Y) > 0.5f * deviceBounds.Height;
        var occluderCoversUmbra = !transparentUmbra &&
            offsetLength * relativeSpot.Scale + relativeSpot.Scale < relativeSpot.Radius;
        if (!transparentUmbra && !occluderCoversUmbra && !path.IsConvex)
            transparentUmbra = true;

        if ((transparentUmbra || occluderCoversUmbra) && localToDevice.Persp0 == 0 && localToDevice.Persp1 == 0)
        {
            var noTranslation = localToDevice;
            noTranslation.TransX = 0;
            noTranslation.TransY = 0;
            var canonicalCenter = noTranslation.MapPoint(localCenter);
            var canonicalSpot = ShadowMetrics.GetSpotParameters(
                elevationPx,
                canonicalCenter.X,
                canonicalCenter.Y,
                lightZ,
                lightRadius);
            var canonicalPolygon = PathPolygon.CreateSpot(
                path,
                noTranslation,
                canonicalSpot.Scale,
                canonicalSpot.Translation);
            var canonicalMesh = new Builder(canonicalPolygon.Path, canonicalPolygon.Clip, elevationPx, transparentUmbra, path.Bounds)
                .TryBuild(canonicalSpot.Radius, canonicalSpot.Radius, true);
            if (canonicalMesh is not null)
            {
                var translation = new SKPoint(
                    relativeSpot.Translation.X + localToDevice.TransX,
                    relativeSpot.Translation.Y + localToDevice.TransY);
                for (var index = 0; index < canonicalMesh.Positions.Count; index++)
                {
                    var point = canonicalMesh.Positions[index];
                    canonicalMesh.Positions[index] = new SKPoint(
                        point.X + translation.X,
                        point.Y + translation.Y);
                }
            }

            return canonicalMesh;
        }

        var spot = ShadowMetrics.GetSpotParameters(
            elevationPx,
            deviceLightPosition.X,
            deviceLightPosition.Y,
            lightZ,
            lightRadius);
        var polygon = PathPolygon.CreateSpot(path, localToDevice, spot.Scale, spot.Translation);
        return new Builder(polygon.Path, polygon.Clip, elevationPx, transparentUmbra, path.Bounds)
            .TryBuild(spot.Radius, spot.Radius, true);
    }

    private sealed class Builder
    {
        private const float NearlyZero = 1f / 4096f;
        private static readonly SKColor PenumbraColor = SKColors.Transparent;
        private static readonly SKColor UmbraColor = SKColors.Black;

        private readonly List<SKPoint> _path;
        private readonly List<SKPoint> _clip;
        private readonly ShadowMesh _mesh = new();
        private readonly List<SKPoint> _clipVectors = [];
        private readonly SKPoint _centroid;
        private readonly bool _isConvex;
        private readonly SKRect _originalBounds;
        private bool _transparent;
        private bool _validUmbra = true;
        private readonly float _direction;

        private int _firstVertexIndex = -1;
        private SKPoint _firstOutset;
        private SKPoint _firstPoint;
        private int _previousUmbraIndex = -1;
        private int _currentUmbraIndex;
        private int _currentClipIndex;
        private bool _previousUmbraOutside;
        private bool _firstUmbraOutside;
        private SKPoint _previousOutset;
        private SKPoint _previousPoint;

        public Builder(
            PathPolygon polygon,
            List<SKPoint> clip,
            float elevationPx,
            bool transparent,
            SKRect originalBounds)
        {
            if (elevationPx < 0 || !float.IsFinite(elevationPx))
                throw new ArgumentOutOfRangeException(nameof(elevationPx));
            if (polygon.Points.Count < 3)
                throw new NotSupportedException("MaterialSkiaShadow requires one finite contour.");

            _originalBounds = originalBounds;
            _path = polygon.Points;
            _clip = clip;
            _centroid = polygon.Centroid;
            _isConvex = polygon.IsConvex;
            _direction = polygon.Area > 0 ? -1f : 1f;
            _transparent = transparent;
        }

        public ShadowMesh? TryBuild(float inset, float outset, bool clipUmbra)
        {
            if (!float.IsFinite(inset) || !float.IsFinite(outset) || inset < 0 || outset < 0)
                throw new InvalidOperationException("Skia produced non-finite shadow geometry parameters.");
            if (!_isConvex)
                return BuildConcave(inset, outset);
            if (clipUmbra)
            {
                if (_clip.Count < 3)
                    throw new InvalidOperationException("The spot shadow has no valid occluder clip polygon.");
                ComputeClipVectorsAndTestCentroid();
            }

            var umbraColor = UmbraColor;
            var minimumDistanceSquared = DistanceToSegmentSquared(_centroid, _path[0], _path[1]);
            for (var index = 1; index < _path.Count; index++)
            {
                var next = (index + 1) % _path.Count;
                minimumDistanceSquared = MathF.Min(
                    minimumDistanceSquared,
                    DistanceToSegmentSquared(_centroid, _path[index], _path[next]));
            }

            List<SKPoint> insetPolygon = [];
            if (inset > NearlyZero)
            {
                const float tolerance = 1e-2f;
                if (minimumDistanceSquared < (inset + tolerance) * (inset + tolerance))
                {
                    var newInset = MathF.Sqrt(minimumDistanceSquared) - tolerance;
                    var ratio = (uint)(128f * (newInset / inset + 1f));
                    // SkPMLerp(kUmbraColor, kPenumbraColor, ratio): source black is weighted by
                    // ratio/256 and transparent contributes zero.
                    var alpha = (byte)Math.Clamp((int)((255u * ratio) >> 8), 0, 255);
                    umbraColor = new SKColor(0, 0, 0, alpha);
                    inset = newInset;
                }

                if (!TryInsetConvexPolygon(_path, inset, out insetPolygon))
                    _validUmbra = false;
            }

            var umbraPolygon = inset > NearlyZero ? insetPolygon : _path;
            if (_transparent)
            {
                AddVertex(_centroid, umbraColor);
            }

            _currentUmbraIndex = 0;
            var count = _path.Count;
            if (!TryComputeNormal(_path[count - 1], _path[0], _direction, out _firstOutset))
                throw new InvalidOperationException("The sanitized contour contains a zero-length edge.");

            _firstOutset = Multiply(_firstOutset, outset);
            _firstPoint = _path[count - 1];
            _firstVertexIndex = _mesh.Positions.Count;
            _previousOutset = _firstOutset;
            _previousPoint = _firstPoint;
            _previousUmbraIndex = -1;

            AddInnerPoint(_firstPoint, umbraColor, umbraPolygon, out _previousUmbraIndex);

            if (!_transparent && clipUmbra)
            {
                var isOutside = ClipUmbraPoint(_mesh.Positions[_firstVertexIndex], _centroid, out var clipPoint);
                if (isOutside)
                    AddVertex(clipPoint, umbraColor);
                _previousUmbraOutside = isOutside;
                _firstUmbraOutside = isOutside;
            }

            AddVertex(Add(_firstPoint, _firstOutset), PenumbraColor);
            AddEdge(_path[0], _firstOutset, umbraColor, umbraPolygon, false, clipUmbra);

            for (var index = 1; index < count; index++)
            {
                if (!TryComputeNormal(_previousPoint, _path[index], _direction, out var normal))
                    throw new InvalidOperationException("The sanitized contour contains a zero-length edge.");
                normal = Multiply(normal, outset);
                AddArc(normal, outset, true);
                AddEdge(_path[index], normal, umbraColor, umbraPolygon, index == count - 1, clipUmbra);
            }

            if (_mesh.Indices.Count == 0 || _mesh.Positions.Count < 3)
                throw new InvalidOperationException("The convex shadow tessellator produced no triangles.");

            if (AddArc(_firstOutset, outset, false))
            {
                AppendTriangle(
                    _firstVertexIndex,
                    _mesh.Positions.Count - 1,
                    _firstUmbraOutside ? _firstVertexIndex + 2 : _firstVertexIndex + 1);
            }
            else
            {
                _mesh.Positions[_firstUmbraOutside ? _firstVertexIndex + 2 : _firstVertexIndex + 1] =
                    _mesh.Positions[^1];
            }

            return _mesh;
        }

        private ShadowMesh? BuildConcave(float inset, float outset)
        {
            inset = MathF.Min(inset, MathF.Min(
                MathF.Abs(_originalBounds.Width * 0.5f), MathF.Abs(_originalBounds.Height * 0.5f)));
            if (!SimplePolygonOffset.TryCreate(_path, inset, out var umbra, out var umbraIndices) ||
                !SimplePolygonOffset.TryCreate(_path, -outset, out var penumbra, out var penumbraIndices) ||
                umbra.Count == 0 || penumbra.Count == 0)
                return null;

            StitchConcaveRings(umbra, umbraIndices, penumbra, penumbraIndices);
            return _mesh.Indices.Count > 0 ? _mesh : null;
        }

        private void StitchConcaveRings(
            List<SKPoint> umbra,
            List<int> umbraIndices,
            List<SKPoint> penumbra,
            List<int> penumbraIndices)
        {
            var indexMap = new ushort[umbra.Count];
            var currentPenumbra = IndexOfMinimum(penumbraIndices);
            var currentUmbra = IndexOfMinimum(umbraIndices);
            var maximumPenumbraIndex = _path.Count - 1;
            var maximumUmbraIndex = _path.Count - 1;
            while (penumbraIndices[currentPenumbra] != umbraIndices[currentUmbra])
            {
                if (penumbraIndices[currentPenumbra] < umbraIndices[currentUmbra])
                {
                    penumbraIndices[currentPenumbra] += _path.Count;
                    maximumPenumbraIndex = penumbraIndices[currentPenumbra];
                    currentPenumbra = (currentPenumbra + 1) % penumbra.Count;
                }
                else
                {
                    umbraIndices[currentUmbra] += _path.Count;
                    maximumUmbraIndex = umbraIndices[currentUmbra];
                    currentUmbra = (currentUmbra + 1) % umbra.Count;
                }
            }

            AddVertex(penumbra[currentPenumbra], PenumbraColor);
            var previousPenumbraIndex = 0;
            AddVertex(umbra[currentUmbra], UmbraColor);
            _previousUmbraIndex = 1;
            indexMap[currentUmbra] = 1;
            var nextPenumbra = (currentPenumbra + 1) % penumbra.Count;
            var nextUmbra = (currentUmbra + 1) % umbra.Count;
            var guard = 0;

            while (penumbraIndices[nextPenumbra] <= maximumPenumbraIndex ||
                   umbraIndices[nextUmbra] <= maximumUmbraIndex)
            {
                if (++guard > (penumbra.Count + umbra.Count) * 4)
                    throw new InvalidOperationException("Concave shadow ring stitching did not converge.");

                if (umbraIndices[nextUmbra] == penumbraIndices[nextPenumbra])
                {
                    AddVertex(penumbra[nextPenumbra], PenumbraColor);
                    var currentPenumbraIndex = _mesh.Positions.Count - 1;
                    AddVertex(umbra[nextUmbra], UmbraColor);
                    var currentUmbraIndex = _mesh.Positions.Count - 1;
                    indexMap[nextUmbra] = checked((ushort)currentUmbraIndex);
                    AppendQuad(previousPenumbraIndex, currentPenumbraIndex, _previousUmbraIndex, currentUmbraIndex);
                    previousPenumbraIndex = currentPenumbraIndex;
                    penumbraIndices[currentPenumbra] += _path.Count;
                    currentPenumbra = nextPenumbra;
                    nextPenumbra = (currentPenumbra + 1) % penumbra.Count;
                    _previousUmbraIndex = currentUmbraIndex;
                    umbraIndices[currentUmbra] += _path.Count;
                    currentUmbra = nextUmbra;
                    nextUmbra = (currentUmbra + 1) % umbra.Count;
                }

                while (penumbraIndices[nextPenumbra] < umbraIndices[nextUmbra] &&
                       penumbraIndices[nextPenumbra] <= maximumPenumbraIndex)
                {
                    AddVertex(penumbra[nextPenumbra], PenumbraColor);
                    var currentPenumbraIndex = _mesh.Positions.Count - 1;
                    AppendTriangle(previousPenumbraIndex, currentPenumbraIndex, _previousUmbraIndex);
                    previousPenumbraIndex = currentPenumbraIndex;
                    penumbraIndices[currentPenumbra] += _path.Count;
                    currentPenumbra = nextPenumbra;
                    nextPenumbra = (currentPenumbra + 1) % penumbra.Count;
                }

                while (umbraIndices[nextUmbra] < penumbraIndices[nextPenumbra] &&
                       umbraIndices[nextUmbra] <= maximumUmbraIndex)
                {
                    AddVertex(umbra[nextUmbra], UmbraColor);
                    var currentUmbraIndex = _mesh.Positions.Count - 1;
                    indexMap[nextUmbra] = checked((ushort)currentUmbraIndex);
                    AppendTriangle(_previousUmbraIndex, previousPenumbraIndex, currentUmbraIndex);
                    _previousUmbraIndex = currentUmbraIndex;
                    umbraIndices[currentUmbra] += _path.Count;
                    currentUmbra = nextUmbra;
                    nextUmbra = (currentUmbra + 1) % umbra.Count;
                }
            }

            AddVertex(penumbra[nextPenumbra], PenumbraColor);
            var finalPenumbraIndex = _mesh.Positions.Count - 1;
            AddVertex(umbra[nextUmbra], UmbraColor);
            var finalUmbraIndex = _mesh.Positions.Count - 1;
            AppendQuad(previousPenumbraIndex, finalPenumbraIndex, _previousUmbraIndex, finalUmbraIndex);
            if (_transparent)
            {
                // Native stitching retains the rings and any emitted ears if triangulation
                // cannot finish; it does not replace the whole shadow with blur in this case.
                SimplePolygonTriangulator.TryAppend(umbra, indexMap, _mesh.Indices);
            }
        }

        private static int IndexOfMinimum(List<int> values)
        {
            var result = 0;
            for (var index = 1; index < values.Count; index++)
            {
                if (values[index] < values[result])
                    result = index;
            }
            return result;
        }

        private void ComputeClipVectorsAndTestCentroid()
        {
            _currentClipIndex = _clip.Count - 1;
            var firstEdge = Subtract(_clip[1], _clip[0]);
            _clipVectors.Add(firstEdge);

            var toCentroid = Subtract(_centroid, _clip[0]);
            var initialCross = Cross(firstEdge, toCentroid);
            var hiddenCentroid = true;

            for (var index = 1; index < _clip.Count; index++)
            {
                var edge = Subtract(_clip[(index + 1) % _clip.Count], _clip[index]);
                _clipVectors.Add(edge);
                toCentroid = Subtract(_centroid, _clip[index]);
                if (initialCross * Cross(edge, toCentroid) <= 0)
                    hiddenCentroid = false;
            }

            _transparent = _transparent || !hiddenCentroid;
        }

        private void AddEdge(
            SKPoint nextPoint,
            SKPoint nextNormal,
            SKColor umbraColor,
            List<SKPoint> umbraPolygon,
            bool lastEdge,
            bool clipUmbra)
        {
            int currentUmbraIndex;
            bool duplicate;
            if (lastEdge)
            {
                duplicate = false;
                currentUmbraIndex = _firstVertexIndex;
                _previousPoint = nextPoint;
            }
            else
            {
                duplicate = AddInnerPoint(nextPoint, umbraColor, umbraPolygon, out currentUmbraIndex);
            }

            var previousPenumbraIndex = duplicate || currentUmbraIndex == _firstVertexIndex
                ? _mesh.Positions.Count - 1
                : _mesh.Positions.Count - 2;

            if (!duplicate)
            {
                if (_transparent)
                {
                    AppendTriangle(0, _previousUmbraIndex, currentUmbraIndex);
                }
                else if (clipUmbra)
                {
                    var clipPoint = default(SKPoint);
                    var isOutside = lastEdge
                        ? _firstUmbraOutside
                        : ClipUmbraPoint(_mesh.Positions[currentUmbraIndex], _centroid, out clipPoint);

                    if (isOutside)
                    {
                        if (!lastEdge)
                            AddVertex(clipPoint, umbraColor);
                        AppendTriangle(_previousUmbraIndex, currentUmbraIndex, currentUmbraIndex + 1);
                        if (_previousUmbraOutside)
                            AppendTriangle(_previousUmbraIndex, currentUmbraIndex + 1, _previousUmbraIndex + 1);
                    }
                    else if (_previousUmbraOutside)
                    {
                        AppendTriangle(_previousUmbraIndex, currentUmbraIndex, _previousUmbraIndex + 1);
                    }

                    _previousUmbraOutside = isOutside;
                }
            }

            AddVertex(Add(nextPoint, nextNormal), PenumbraColor);
            if (!duplicate)
                AppendTriangle(_previousUmbraIndex, previousPenumbraIndex, currentUmbraIndex);
            AppendTriangle(previousPenumbraIndex, _mesh.Positions.Count - 1, currentUmbraIndex);

            _previousUmbraIndex = currentUmbraIndex;
            _previousOutset = nextNormal;
        }

        private bool ClipUmbraPoint(SKPoint umbraPoint, SKPoint centroid, out SKPoint clipPoint)
        {
            var segment = Subtract(centroid, umbraPoint);
            var start = _currentClipIndex;
            do
            {
                var delta = Subtract(umbraPoint, _clip[_currentClipIndex]);
                var denominator = Cross(_clipVectors[_currentClipIndex], segment);
                var tNumerator = Cross(delta, segment);
                if (NearlyEqualZero(denominator))
                {
                    if (NearlyEqualZero(tNumerator))
                    {
                        clipPoint = default;
                        return false;
                    }
                }
                else if (tNumerator >= 0 && tNumerator <= denominator)
                {
                    var sNumerator = Cross(delta, _clipVectors[_currentClipIndex]);
                    if (sNumerator >= 0 && sNumerator <= denominator)
                    {
                        clipPoint = Add(umbraPoint, Multiply(segment, sNumerator / denominator));
                        return true;
                    }
                }

                _currentClipIndex = (_currentClipIndex + 1) % _clip.Count;
            }
            while (_currentClipIndex != start);

            clipPoint = default;
            return false;
        }

        private bool AddInnerPoint(
            SKPoint pathPoint,
            SKColor umbraColor,
            List<SKPoint> umbraPolygon,
            out int currentUmbraIndex)
        {
            SKPoint umbraPoint;
            if (!_validUmbra)
            {
                umbraPoint = Add(pathPoint, Multiply(Subtract(_centroid, pathPoint), 0.95f));
            }
            else
            {
                umbraPoint = umbraPolygon[GetClosestUmbraIndex(pathPoint, umbraPolygon)];
            }

            _previousPoint = pathPoint;
            if (_previousUmbraIndex == -1 || !Duplicate(umbraPoint, _mesh.Positions[_previousUmbraIndex]))
            {
                if (_previousUmbraIndex >= 0 && Duplicate(umbraPoint, _mesh.Positions[_firstVertexIndex]))
                {
                    currentUmbraIndex = _firstVertexIndex;
                }
                else
                {
                    currentUmbraIndex = _mesh.Positions.Count;
                    AddVertex(umbraPoint, umbraColor);
                }

                return false;
            }

            currentUmbraIndex = _previousUmbraIndex;
            return true;
        }

        private int GetClosestUmbraIndex(SKPoint point, List<SKPoint> polygon)
        {
            var minimumDistance = DistanceSquared(point, polygon[_currentUmbraIndex]);
            var index = _currentUmbraIndex;
            var direction = 1;
            var next = (index + direction) % polygon.Count;
            var distance = DistanceSquared(point, polygon[next]);
            if (distance < minimumDistance)
            {
                index = next;
                minimumDistance = distance;
            }
            else
            {
                direction = polygon.Count - 1;
            }

            next = (index + direction) % polygon.Count;
            distance = DistanceSquared(point, polygon[next]);
            while (distance < minimumDistance)
            {
                index = next;
                minimumDistance = distance;
                next = (index + direction) % polygon.Count;
                distance = DistanceSquared(point, polygon[next]);
            }

            _currentUmbraIndex = index;
            return index;
        }

        private bool AddArc(SKPoint nextNormal, float offset, bool finishArc)
        {
            var cosine = Dot(_previousOutset, nextNormal);
            var sine = Cross(_previousOutset, nextNormal);
            if (!float.IsFinite(cosine) || !float.IsFinite(sine))
                return false;

            var theta = MathF.Atan2(sine, cosine);
            var floatingSteps = MathF.Abs(offset * theta * 0.25f);
            if (floatingSteps >= ushort.MaxValue)
                return false;
            var steps = (int)MathF.Floor(floatingSteps + 0.5f);
            var deltaTheta = steps > 0 ? theta / steps : 0;
            var rotationSin = MathF.Sin(deltaTheta);
            var rotationCos = MathF.Cos(deltaTheta);
            if (steps > 0 && (rotationSin == 0 || rotationCos == 1))
                return false;

            var previousNormal = _previousOutset;
            for (var index = 0; index < steps - 1; index++)
            {
                var currentNormal = new SKPoint(
                    previousNormal.X * rotationCos - previousNormal.Y * rotationSin,
                    previousNormal.Y * rotationCos + previousNormal.X * rotationSin);
                AddVertex(Add(_previousPoint, currentNormal), PenumbraColor);
                AppendTriangle(_previousUmbraIndex, _mesh.Positions.Count - 1, _mesh.Positions.Count - 2);
                previousNormal = currentNormal;
            }

            if (finishArc && steps > 0)
            {
                AddVertex(Add(_previousPoint, nextNormal), PenumbraColor);
                AppendTriangle(_previousUmbraIndex, _mesh.Positions.Count - 1, _mesh.Positions.Count - 2);
            }

            _previousOutset = nextNormal;
            return steps > 0;
        }

        private void AddVertex(SKPoint point, SKColor color)
        {
            if (_mesh.Positions.Count >= ushort.MaxValue)
                throw new NotSupportedException("The shadow mesh exceeds Skia's 16-bit index limit.");
            _mesh.Positions.Add(point);
            _mesh.Colors.Add(color);
        }

        private void AppendTriangle(int first, int second, int third)
        {
            if (first < 0 || second < 0 || third < 0 ||
                first >= _mesh.Positions.Count || second >= _mesh.Positions.Count || third >= _mesh.Positions.Count)
                throw new InvalidOperationException("The shadow tessellator generated an invalid triangle index.");
            _mesh.Indices.Add(checked((ushort)first));
            _mesh.Indices.Add(checked((ushort)second));
            _mesh.Indices.Add(checked((ushort)third));
        }

        private void AppendQuad(int first, int second, int third, int fourth)
        {
            AppendTriangle(first, second, third);
            AppendTriangle(third, second, fourth);
        }

        private static bool TryInsetConvexPolygon(
            List<SKPoint> polygon,
            float inset,
            out List<SKPoint> result)
        {
            result = [];
            if (polygon.Count < 3 || inset < -NearlyZero || !float.IsFinite(inset))
                return false;
            if (inset <= NearlyZero)
            {
                result.AddRange(polygon);
                return true;
            }

            var windingArea = SignedArea(polygon);
            if (MathF.Abs(windingArea) <= NearlyZero * NearlyZero)
                return false;
            var winding = windingArea > 0 ? 1 : -1;
            var edges = new InsetEdge[polygon.Count];
            for (var index = 0; index < edges.Length; index++)
                edges[index] = new InsetEdge();

            var previousIndex = polygon.Count - 1;
            for (var currentIndex = 0; currentIndex < polygon.Count; previousIndex = currentIndex++)
            {
                var nextIndex = (currentIndex + 1) % polygon.Count;
                var currentPoint = polygon[currentIndex];
                if (!float.IsFinite(currentPoint.X) || !float.IsFinite(currentPoint.Y))
                    return false;

                var previousVector = Subtract(currentPoint, polygon[previousIndex]);
                if (ComputeSide(polygon[previousIndex], previousVector, polygon[nextIndex]) * winding < 0)
                    return false;

                var vector = Subtract(polygon[nextIndex], currentPoint);
                var perpendicular = new SKPoint(-vector.Y, vector.X);
                if (!TrySetLength(perpendicular, inset * winding, out perpendicular))
                    return false;

                var edge = edges[currentIndex];
                edge.Previous = previousIndex;
                edge.Next = nextIndex;
                edge.Origin = Add(currentPoint, perpendicular);
                edge.Vector = vector;
                edge.Intersection = edge.Origin;
                edge.TValue = -float.MaxValue;
            }

            var head = 0;
            var current = head;
            var previous = edges[current].Previous;
            var insetVertexCount = polygon.Count;
            var iterations = 0u;
            var maximumIterations = checked((uint)(polygon.Count * polygon.Count));

            while (head >= 0 && previous != current)
            {
                if (++iterations > maximumIterations)
                    return false;

                var previousEdge = edges[previous];
                var currentEdge = edges[current];
                if (TryComputeSegmentIntersection(previousEdge, currentEdge, out var intersection, out var s, out var t))
                {
                    if (s < previousEdge.TValue)
                    {
                        var goBackTo = previousEdge.Previous;
                        RemoveInsetEdge(edges, previous, ref head);
                        insetVertexCount--;
                        previous = goBackTo;
                    }
                    else if (currentEdge.TValue > -float.MaxValue &&
                             EqualsWithinTolerance(intersection, currentEdge.Intersection, 1e-6f))
                    {
                        break;
                    }
                    else
                    {
                        currentEdge.Intersection = intersection;
                        currentEdge.TValue = t;
                        previous = current;
                        current = currentEdge.Next;
                    }
                }
                else
                {
                    var side = winding * ComputeSide(currentEdge.Origin, currentEdge.Vector, previousEdge.Origin);
                    if (side < 0 &&
                        side == winding * ComputeSide(
                            currentEdge.Origin,
                            currentEdge.Vector,
                            Add(previousEdge.Origin, previousEdge.Vector)))
                    {
                        var goBackTo = previousEdge.Previous;
                        RemoveInsetEdge(edges, previous, ref head);
                        insetVertexCount--;
                        previous = goBackTo;
                    }
                    else
                    {
                        var next = currentEdge.Next;
                        RemoveInsetEdge(edges, current, ref head);
                        insetVertexCount--;
                        current = next;
                    }
                }
            }

            if (head < 0)
                return false;

            const float cleanupTolerance = 0.01f;
            result.Capacity = Math.Max(result.Capacity, Math.Max(0, insetVertexCount));
            result.Add(edges[head].Intersection);
            current = edges[head].Next;
            while (current != head)
            {
                var intersection = edges[current].Intersection;
                if (!EqualsWithinTolerance(intersection, result[^1], cleanupTolerance))
                    result.Add(intersection);
                current = edges[current].Next;
            }

            if (result.Count > 1 && EqualsWithinTolerance(result[0], result[^1], cleanupTolerance))
                result.RemoveAt(result.Count - 1);
            return SimplePolygonSweep.IsConvex(result);
        }

        private static bool TryComputeSegmentIntersection(
            InsetEdge first,
            InsetEdge second,
            out SKPoint intersection,
            out float firstParameter,
            out float secondParameter)
        {
            const float crossTolerance = NearlyZero * NearlyZero;
            var firstVector = first.Vector;
            var secondVector = second.Vector;
            var delta = Subtract(second.Origin, first.Origin);
            var denominator = Cross(firstVector, secondVector);
            var denominatorPositive = denominator > 0;
            float firstNumerator;
            float secondNumerator;

            if (MathF.Abs(denominator) <= crossTolerance)
            {
                if (MathF.Abs(Cross(delta, firstVector)) > crossTolerance ||
                    MathF.Abs(Cross(delta, secondVector)) > crossTolerance)
                    return IntersectionFailure(out intersection, out firstParameter, out secondParameter);

                var firstLengthSquared = Dot(firstVector, firstVector);
                if (!IsUsableVector(firstVector, firstLengthSquared))
                {
                    var secondLengthSquared = Dot(secondVector, secondVector);
                    if (!IsUsableVector(secondVector, secondLengthSquared))
                    {
                        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y) ||
                            (delta.X == 0 && delta.Y == 0))
                        {
                            intersection = first.Origin;
                            firstParameter = 0;
                            secondParameter = 0;
                            return true;
                        }
                        return IntersectionFailure(out intersection, out firstParameter, out secondParameter);
                    }

                    secondNumerator = Dot(secondVector, Multiply(delta, -1));
                    denominator = secondLengthSquared;
                    if (OutsideInterval(secondNumerator, denominator, true))
                        return IntersectionFailure(out intersection, out firstParameter, out secondParameter);
                    firstNumerator = 0;
                }
                else
                {
                    firstNumerator = Dot(firstVector, delta);
                    denominator = firstLengthSquared;
                    secondNumerator = 0;
                    if (OutsideInterval(firstNumerator, denominator, true))
                    {
                        var secondLengthSquared = Dot(secondVector, secondVector);
                        if (!IsUsableVector(secondVector, secondLengthSquared))
                            return IntersectionFailure(out intersection, out firstParameter, out secondParameter);

                        var oldFirstNumerator = firstNumerator;
                        firstNumerator = Dot(firstVector, Add(delta, secondVector));
                        secondNumerator = denominator;
                        if (OutsideInterval(firstNumerator, denominator, true))
                        {
                            if (firstNumerator * oldFirstNumerator > 0)
                                return IntersectionFailure(out intersection, out firstParameter, out secondParameter);
                            firstNumerator = 0;
                            secondNumerator = Dot(secondVector, Multiply(delta, -1));
                            denominator = secondLengthSquared;
                        }
                    }
                }
            }
            else
            {
                firstNumerator = Cross(delta, secondVector);
                if (OutsideInterval(firstNumerator, denominator, denominatorPositive))
                    return IntersectionFailure(out intersection, out firstParameter, out secondParameter);
                secondNumerator = Cross(delta, firstVector);
                if (OutsideInterval(secondNumerator, denominator, denominatorPositive))
                    return IntersectionFailure(out intersection, out firstParameter, out secondParameter);
            }

            firstParameter = firstNumerator / denominator;
            secondParameter = secondNumerator / denominator;
            intersection = Add(first.Origin, Multiply(firstVector, firstParameter));
            return true;
        }

        private static bool IntersectionFailure(
            out SKPoint intersection,
            out float firstParameter,
            out float secondParameter)
        {
            intersection = default;
            firstParameter = default;
            secondParameter = default;
            return false;
        }

        private static bool OutsideInterval(float numerator, float denominator, bool denominatorPositive) =>
            denominatorPositive
                ? numerator < 0 || numerator > denominator
                : numerator > 0 || numerator < denominator;

        private static bool IsUsableVector(SKPoint vector, float lengthSquared) =>
            float.IsFinite(vector.X) && float.IsFinite(vector.Y) && lengthSquared != 0;

        private static int ComputeSide(SKPoint origin, SKPoint vector, SKPoint point)
        {
            var perpendicular = Cross(vector, Subtract(point, origin));
            const float crossTolerance = NearlyZero * NearlyZero;
            return MathF.Abs(perpendicular) <= crossTolerance ? 0 : perpendicular > 0 ? 1 : -1;
        }

        private static void RemoveInsetEdge(InsetEdge[] edges, int index, ref int head)
        {
            var edge = edges[index];
            edges[edge.Previous].Next = edge.Next;
            edges[edge.Next].Previous = edge.Previous;
            if (index == head)
                head = edge.Next == index ? -1 : edge.Next;
        }

        private sealed class InsetEdge
        {
            public SKPoint Origin;
            public SKPoint Vector;
            public SKPoint Intersection;
            public float TValue;
            public int Previous;
            public int Next;
        }

        private static float SignedArea(List<SKPoint> points)
        {
            var area = 0f;
            var origin = points[0];
            var previous = Subtract(points[1], origin);
            for (var index = 2; index < points.Count; index++)
            {
                var current = Subtract(points[index], origin);
                area += Cross(previous, current);
                previous = current;
            }
            return area;
        }

        private static bool TryComputeNormal(SKPoint first, SKPoint second, float direction, out SKPoint normal)
        {
            normal = new SKPoint((first.Y - second.Y) * direction, (second.X - first.X) * direction);
            return TrySetLength(normal, 1f, out normal);
        }

        // SkPoint::setLength deliberately performs its magnitude and scale calculations in double
        // even for ordinary finite float inputs. Keeping this behavior matters because the
        // resulting float normals are used directly as shadow-mesh vertex positions.
        private static bool TrySetLength(SKPoint point, float length, out SKPoint result)
        {
            var x = (double)point.X;
            var y = (double)point.Y;
            var magnitude = Math.Sqrt(x * x + y * y);
            var scale = length / magnitude;
            var resultX = (float)(point.X * scale);
            var resultY = (float)(point.Y * scale);
            if (!float.IsFinite(resultX) || !float.IsFinite(resultY) || (resultX == 0 && resultY == 0))
            {
                result = default;
                return false;
            }

            result = new SKPoint(resultX, resultY);
            return true;
        }

        private static float DistanceToSegmentSquared(SKPoint point, SKPoint first, SKPoint second)
        {
            var segment = Subtract(second, first);
            var toPoint = Subtract(point, first);
            var lengthSquared = Dot(segment, segment);
            var projection = Dot(segment, toPoint);
            if (projection <= 0)
                return Dot(toPoint, toPoint);
            if (projection > lengthSquared)
                return DistanceSquared(second, point);

            var determinant = Cross(segment, toPoint);
            var result = determinant / lengthSquared;
            result *= determinant;
            return float.IsFinite(result) ? result : Dot(toPoint, toPoint);
        }

        private static bool Duplicate(SKPoint first, SKPoint second) =>
            DistanceSquared(first, second) < (1f / 16f) * (1f / 16f);

        private static bool EqualsWithinTolerance(SKPoint first, SKPoint second, float tolerance) =>
            MathF.Abs(first.X - second.X) <= tolerance && MathF.Abs(first.Y - second.Y) <= tolerance;

        private static bool NearlyEqualZero(float value) => MathF.Abs(value) <= NearlyZero;
        private static float DistanceSquared(SKPoint first, SKPoint second) => Dot(Subtract(first, second), Subtract(first, second));
        private static float Dot(SKPoint first, SKPoint second) => first.X * second.X + first.Y * second.Y;
        private static float Cross(SKPoint first, SKPoint second) => first.X * second.Y - first.Y * second.X;
        private static SKPoint Add(SKPoint first, SKPoint second) => new(first.X + second.X, first.Y + second.Y);
        private static SKPoint Subtract(SKPoint first, SKPoint second) => new(first.X - second.X, first.Y - second.Y);
        private static SKPoint Multiply(SKPoint point, float value) => new(point.X * value, point.Y * value);
    }

    private sealed class PathPolygon
    {
        private const float NearlyZero = 1f / 4096f;
        private const float QuadraticTolerance = 0.2f;
        private const float QuadraticToleranceSquared = 0.2f * 0.2f;
        private const float ConicTolerance = 0.25f;

        private float _area;
        private float _lastArea;
        private float _lastCross;
        private SKPoint _centroid;

        private PathPolygon()
        {
        }

        public List<SKPoint> Points { get; } = [];
        public List<SKPoint> Clip { get; } = [];
        public bool IsConvex { get; private set; } = true;
        public float Area => _area;
        public SKPoint Centroid => _centroid;
        public PathPolygon Path => this;

        public static PathPolygon CreateAmbient(SKPath path, SKMatrix transform)
        {
            var result = new PathPolygon();
            result.Walk(path, point => transform.MapPoint(point), null);
            return result;
        }

        public static PathPolygon CreateSpot(
            SKPath path,
            SKMatrix localToDevice,
            float shadowScale,
            SKPoint shadowTranslation)
        {
            var result = new PathPolygon();
            var shadowTransform = SKMatrix.Concat(
                new SKMatrix(shadowScale, 0, shadowTranslation.X, 0, shadowScale, shadowTranslation.Y, 0, 0, 1),
                localToDevice);
            SKPoint ShadowTransform(SKPoint point)
            {
                return shadowTransform.MapPoint(point);
            }

            result.Walk(path, ShadowTransform, point => localToDevice.MapPoint(point));
            return result;
        }

        private void Walk(
            SKPath path,
            Func<SKPoint, SKPoint> pathTransform,
            Func<SKPoint, SKPoint>? clipTransform)
        {
            using var iterator = path.CreateIterator(true);
            Span<SKPoint> points = stackalloc SKPoint[4];
            var sawVerb = false;
            var sawClose = false;
            while (true)
            {
                var verb = iterator.Next(points);
                if (verb == SKPathVerb.Done)
                    break;
                if (sawClose)
                    throw new NotSupportedException("MaterialSkiaShadow supports one contour only.");

                switch (verb)
                {
                    case SKPathVerb.Move:
                        if (sawVerb)
                            throw new NotSupportedException("MaterialSkiaShadow supports one contour only.");
                        break;
                    case SKPathVerb.Line:
                        if (clipTransform is not null)
                            AddClip(clipTransform(points[1]));
                        HandleLine(pathTransform(points[1]));
                        break;
                    case SKPathVerb.Quad:
                        if (clipTransform is not null)
                        {
                            var first = clipTransform(points[0]);
                            var control = clipTransform(points[1]);
                            var last = clipTransform(points[2]);
                            AddClip(new SKPoint(
                                0.25f * first.X + 0.5f * control.X + 0.25f * last.X,
                                0.25f * first.Y + 0.5f * control.Y + 0.25f * last.Y));
                            AddClip(last);
                        }
                        HandleQuadratic(pathTransform(points[0]), pathTransform(points[1]), pathTransform(points[2]));
                        break;
                    case SKPathVerb.Conic:
                        var weight = iterator.ConicWeight();
                        if (clipTransform is not null)
                        {
                            var first = clipTransform(points[0]);
                            var control = clipTransform(points[1]);
                            var last = clipTransform(points[2]);
                            var reciprocal = 1f / (0.5f + 0.5f * weight);
                            AddClip(new SKPoint(
                                (0.25f * first.X + 0.5f * weight * control.X + 0.25f * last.X) * reciprocal,
                                (0.25f * first.Y + 0.5f * weight * control.Y + 0.25f * last.Y) * reciprocal));
                            AddClip(last);
                        }
                        HandleConic(pathTransform(points[0]), pathTransform(points[1]), pathTransform(points[2]), weight);
                        break;
                    case SKPathVerb.Cubic:
                        throw new NotSupportedException("Cubic shadow contours are outside MaterialSkiaShadow's first-stage contract.");
                    case SKPathVerb.Close:
                        sawClose = true;
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported Skia path verb '{verb}'.");
                }

                sawVerb = true;
            }

            Finish();
            if (Points.Count < 3 || !float.IsFinite(_area))
                throw new NotSupportedException(
                    $"MaterialSkiaShadow requires a finite contour; points={Points.Count}, area={_area}, convex={IsConvex}, " +
                    $"polygon=[{string.Join(", ", Points.Select(point => $"({point.X},{point.Y})"))}].");
        }

        private void HandleConic(SKPoint first, SKPoint control, SKPoint last, float weight)
        {
            var a = weight - 1f;
            var k = a / (4f * (2f + a));
            var x = k * (first.X - 2f * control.X + last.X);
            var y = k * (first.Y - 2f * control.Y + last.Y);
            var error = MathF.Sqrt(x * x + y * y);
            var power = 0;
            for (; power < 5; power++)
            {
                if (error <= ConicTolerance)
                    break;
                error *= 0.25f;
            }

            var quadratics = SKPath.ConvertConicToQuads(first, control, last, weight, power);
            var previous = quadratics[0];
            var count = 1 << power;
            for (var index = 0; index < count; index++)
            {
                var quadraticControl = quadratics[index * 2 + 1];
                var quadraticLast = index == count - 1 ? last : quadratics[index * 2 + 2];
                HandleQuadratic(previous, quadraticControl, quadraticLast);
                previous = quadraticLast;
            }
        }

        private void HandleQuadratic(SKPoint first, SKPoint control, SKPoint last)
        {
            // Skiko publishes Ganesh as an extension. SK_GANESH is therefore absent while core
            // SkShadowTessellator.cpp is compiled, selecting its coarse compatibility branch.
            HandleLine(control);
            HandleLine(last);
        }

        // GrPathUtils::quadraticPointCount uses Wang's formula to cap recursive subdivision.
        // This cap is observable at small shadow radii: allowing the recursion a generic large
        // budget makes rounded corners smoother than SkShadowTessellator's native mesh.
        private static int QuadraticPointCount(SKPoint first, SKPoint control, SKPoint last)
        {
            var precision = 1f / QuadraticTolerance;
            var x = first.X - 2f * control.X + last.X;
            var y = first.Y - 2f * control.Y + last.Y;
            // length_term_p2<2>(precision) = precision^2 / 16.
            var power4 = (x * x + y * y) * (precision * precision * (1f / 16f));
            var chopCount = NextLog16(power4);
            return 1 << Math.Min(chopCount, 10);
        }

        // Bit-identical translation of wangs_formula::nextlog2/nextlog16. Using MathF.Log2 and
        // Ceil changes exact-power-of-two and one-ULP boundary cases that affect vertex counts.
        private static int NextLog16(float value)
        {
            if (value <= 1f)
                return 0;

            var bits = BitConverter.SingleToUInt32Bits(value);
            bits += (1u << 23) - 1u;
            var exponent = (int)((bits >> 23) & 0xffu) - 127;
            var nextLog2 = exponent > 0 ? exponent : 0;
            return (nextLog2 + 3) >> 2;
        }

        private void GenerateQuadratic(SKPoint first, SKPoint control, SKPoint last, int pointsLeft)
        {
            if (pointsLeft < 2 || DistanceToSegmentSquared(control, first, last) < QuadraticToleranceSquared)
            {
                HandleLine(last);
                return;
            }

            var firstMid = Midpoint(first, control);
            var secondMid = Midpoint(control, last);
            var center = Midpoint(firstMid, secondMid);
            pointsLeft >>= 1;
            GenerateQuadratic(first, firstMid, center, pointsLeft);
            GenerateQuadratic(center, secondMid, last, pointsLeft);
        }

        private void HandleLine(SKPoint point)
        {
            var sanitized = new SKPoint(
                MathF.Floor(16f * point.X + 0.5f) * 0.0625f,
                MathF.Floor(16f * point.Y + 0.5f) * 0.0625f);

            if (Points.Count > 0 && !AccumulateCentroid(Points[^1], sanitized))
                return;

            if (Points.Count > 1 && !CheckConvexity(Points[^2], Points[^1], sanitized))
            {
                Points.RemoveAt(Points.Count - 1);
                if (Points.Count > 0 && Duplicate(Points[^1], sanitized))
                    Points.RemoveAt(Points.Count - 1);
            }

            Points.Add(sanitized);
        }

        private bool AccumulateCentroid(SKPoint current, SKPoint next)
        {
            if (Duplicate(current, next))
                return false;
            var first = Points[0];
            var firstVector = Subtract(current, first);
            var secondVector = Subtract(next, first);
            var quadArea = Cross(firstVector, secondVector);
            _centroid = new SKPoint(
                _centroid.X + (firstVector.X + secondVector.X) * quadArea,
                _centroid.Y + (firstVector.Y + secondVector.Y) * quadArea);
            _area += quadArea;
            if (quadArea * _lastArea < 0)
                IsConvex = false;
            if (quadArea != 0)
                _lastArea = quadArea;
            return true;
        }

        private bool CheckConvexity(SKPoint first, SKPoint second, SKPoint third)
        {
            var cross = Cross(Subtract(second, first), Subtract(third, second));
            if (NearlyEqualZero(cross))
                return false;
            if (_lastCross * cross < 0)
                IsConvex = false;
            if (cross != 0)
                _lastCross = cross;
            return true;
        }

        private void Finish()
        {
            if (Points.Count > 1 && !AccumulateCentroid(Points[^1], Points[0]))
                Points.RemoveAt(Points.Count - 1);

            if (Points.Count > 2)
            {
                _centroid = new SKPoint(
                    _centroid.X * (1f / (3f * _area)) + Points[0].X,
                    _centroid.Y * (1f / (3f * _area)) + Points[0].Y);
                if (!CheckConvexity(Points[^2], Points[^1], Points[0]))
                {
                    Points[0] = Points[^1];
                    Points.RemoveAt(Points.Count - 1);
                }
            }
        }

        private void AddClip(SKPoint point)
        {
            if (Clip.Count == 0 || !Duplicate(point, Clip[^1]))
                Clip.Add(point);
        }

        private static float DistanceToSegmentSquared(SKPoint point, SKPoint first, SKPoint second)
        {
            var segment = Subtract(second, first);
            var toPoint = Subtract(point, first);
            var lengthSquared = Dot(segment, segment);
            var projection = Dot(segment, toPoint);
            if (projection <= 0)
                return Dot(toPoint, toPoint);
            if (projection > lengthSquared)
            {
                var toEnd = Subtract(second, point);
                return Dot(toEnd, toEnd);
            }

            var determinant = Cross(segment, toPoint);
            var result = determinant / lengthSquared;
            result *= determinant;
            return float.IsFinite(result) ? result : Dot(toPoint, toPoint);
        }

        private static bool Duplicate(SKPoint first, SKPoint second)
        {
            var delta = Subtract(first, second);
            return Dot(delta, delta) < (1f / 16f) * (1f / 16f);
        }

        private static bool NearlyEqualZero(float value) => MathF.Abs(value) <= NearlyZero;
        private static float Dot(SKPoint first, SKPoint second) => first.X * second.X + first.Y * second.Y;
        private static float Cross(SKPoint first, SKPoint second) => first.X * second.Y - first.Y * second.X;
        private static SKPoint Subtract(SKPoint first, SKPoint second) => new(first.X - second.X, first.Y - second.Y);
        private static SKPoint Midpoint(SKPoint first, SKPoint second) => new(
            (float)(0.5 * ((double)first.X + second.X)),
            (float)(0.5 * ((double)first.Y + second.Y)));
    }
}
