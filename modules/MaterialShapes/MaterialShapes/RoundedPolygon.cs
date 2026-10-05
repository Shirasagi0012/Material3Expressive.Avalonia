/*
 * Copyright 2022 The Android Open Source Project
 * Copyright 2026 @Shirasagi0012
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// Ported from androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\RoundedPolygon.kt by Shirasagi0012

using System.Text;
using System.Collections.Immutable;
using Avalonia;

namespace MaterialShapes;

public sealed partial class RoundedPolygon : IEquatable<RoundedPolygon>
{
    public Point Center { get; }

    private readonly ImmutableArray<CubicBezier> _cubics;
    private readonly ImmutableArray<Feature> _features;

    public double CenterX => Center.X;
    public double CenterY => Center.Y;

    public IReadOnlyList<CubicBezier> Cubics => _cubics;
    public IReadOnlyList<Feature> Features => _features;

    private ImmutableArray<CubicBezier> BuildCubicsList()
    {
        // The first/last mechanism here ensures that the final anchor point in the shape
        // exactly matches the first anchor point. There can be rendering artifacts introduced
        // by those points being slightly off, even by much less than a pixel
        var cubics = new List<CubicBezier>();

        CubicBezier? firstCubic = null;
        CubicBezier? lastCubic = null;

        List<CubicBezier>? firstFeatureSplitStart = null;
        List<CubicBezier>? firstFeatureSplitEnd = null;

        if (_features.Length > 0 && _features[0].Cubics.Count == 3)
        {
            var centerCubic = _features[0].Cubics[1];
            var (start, end) = centerCubic.Split(0.5);
            firstFeatureSplitStart = [_features[0].Cubics[0], start];
            firstFeatureSplitEnd = [end, _features[0].Cubics[2]];
        }

        // Iterating one past the features list size allows us to insert the initial split cubic if it exists.
        for (var i = 0; i <= _features.Length; i++)
        {
            IReadOnlyList<CubicBezier> featureCubics;

            if (i == 0 && firstFeatureSplitEnd is { })
            {
                featureCubics = firstFeatureSplitEnd;
            }
            else if (i == _features.Length)
            {
                if (firstFeatureSplitStart is { })
                    featureCubics = firstFeatureSplitStart;
                else
                    break;
            }
            else
            {
                featureCubics = _features[i].Cubics;
            }

            foreach (var cubic in featureCubics)
                // Skip zero-length curves; they add nothing and can trigger rendering artifacts.
                if (!cubic.IsZeroLength)
                {
                    if (lastCubic is { })
                        cubics.Add(lastCubic.Value);

                    lastCubic = cubic;
                    firstCubic ??= cubic;
                }
                else
                {
                    // Dropping several zero-ish length curves in a row can lead to enough discontinuity
                    // to throw an exception later, even though the distances are quite small.
                    // Account for that by making the last cubic use the latest anchor point, always.
                    if (lastCubic is null)
                        continue;

                    var updated = new CubicBezier(lastCubic.Value)
                    {
                        Anchor1 = cubic.Anchor1
                    };
                    lastCubic = updated;
                }
        }

        if (lastCubic is { } && firstCubic is { })
        {
            var last = lastCubic.Value;
            var first = firstCubic.Value;

            cubics.Add(new CubicBezier(
                last.Anchor0,
                last.Control0,
                last.Control1,
                first.Anchor0
            ));
        }
        else
        {
            // Empty / 0-sized polygon.
            cubics.Add(new CubicBezier(
                Center,
                Center,
                Center,
                Center));
        }

        // TODO (perf): Use ImmutableArray.Builder to avoid copy.
        return cubics.ToImmutableArray();
    }

    public RoundedPolygon(IReadOnlyList<Feature> features, Point? center = null)
    {
        var featuresArray = features.ToImmutableArray();
        if (center == null)
        {
            if (featuresArray.Length < 2)
                throw new ArgumentException("Polygons must have at least 2 features", nameof(features));

            var vertices = new List<double>(featuresArray.Length * 2);
            foreach (var feature in featuresArray)
            foreach (var cubic in feature.Cubics)
            {
                vertices.Add(cubic.Anchor0.X);
                vertices.Add(cubic.Anchor0.Y);
            }

            center = CalculateCenter(vertices.ToArray());
        }
        
        Center = center.Value;
        _features = featuresArray;
        _cubics = BuildCubicsList();

        if (_cubics.Length == 0)
            throw new ArgumentException("RoundedPolygon cannot be empty.");

        var prevCubic = _cubics[^1];
        foreach (var cubic in _cubics)
        {
            if (Math.Abs(cubic.Anchor0.X - prevCubic.Anchor1.X) > Utils.DistanceEpsilon ||
                Math.Abs(cubic.Anchor0.Y - prevCubic.Anchor1.Y) > Utils.DistanceEpsilon)
                throw new ArgumentException(
                    "RoundedPolygon must be contiguous, with the anchor points of all curves " +
                    "matching the anchor points of the preceding and succeeding cubics");

            prevCubic = cubic;
        }
    }

    private RoundedPolygon((ImmutableArray<Feature> features, Point center) built) : this(built.features, built.center)
    {
    }

    public RoundedPolygon(
        int numVertices,
        double radius = 1,
        Point center = default,
        CornerRounding rounding = default,
        IReadOnlyList<CornerRounding>? perVertexRounding = null
    )
        : this(BuildFromVertexCount(numVertices, radius, center, rounding, perVertexRounding))
    {
    }

    public RoundedPolygon(
        Point[] vertices,
        CornerRounding rounding = default,
        IReadOnlyList<CornerRounding>? perVertexRounding = null,
        Point? center = null
    )
        : this(BuildFromVertices(vertices, rounding, perVertexRounding, center))
    {
    }

    public RoundedPolygon(RoundedPolygon source) : this(source._features, source.Center)
    {
    }


    private static (ImmutableArray<Feature> features, Point center) BuildFromVertices(
        Point[] vertices,
        CornerRounding rounding,
        IReadOnlyList<CornerRounding>? perVertexRounding,
        Point? center
    )
    {
        if (vertices.Length < 3)
            throw new ArgumentException("Polygons must have at least 3 vertices", nameof(vertices));

        if (perVertexRounding is { } && perVertexRounding.Count != vertices.Length)
            throw new ArgumentException(
                "perVertexRounding list should be either null or the same size as the number of vertices (vertices.Length)",
                nameof(perVertexRounding));

        var corners = new List<List<CubicBezier>>();
        var n = vertices.Length;
        var roundedCorners = new List<RoundedCorner>(n);
        for (var i = 0; i < n; i++)
        {
            var vtxRounding = perVertexRounding is null ? rounding : perVertexRounding[i];
            var prevIndex = (i + n - 1) % n;
            var nextIndex = (i + 1) % n;
            roundedCorners.Add(
                new RoundedCorner(
                    vertices[prevIndex],
                    vertices[i],
                    vertices[nextIndex],
                    vtxRounding));
        }

        var cutAdjusts = new (double roundCutRatio, double cutRatio)[n];
        for (var ix = 0; ix < n; ix++)
        {
            var expectedRoundCut = roundedCorners[ix].ExpectedRoundCut + roundedCorners[(ix + 1) % n].ExpectedRoundCut;
            var expectedCut = roundedCorners[ix].ExpectedCut + roundedCorners[(ix + 1) % n].ExpectedCut;

            var vtx = vertices[ix];
            var nextVtx = vertices[(ix + 1) % n];
            var sideSize = Utils.Distance(vtx.X - nextVtx.X, vtx.Y - nextVtx.Y);

            if (expectedRoundCut > sideSize)
                cutAdjusts[ix] = (sideSize / expectedRoundCut, 0);
            else if (expectedCut > sideSize)
                cutAdjusts[ix] = (1, (sideSize - expectedRoundCut) / (expectedCut - expectedRoundCut));
            else
                cutAdjusts[ix] = (1, 1);
        }

        for (var i = 0; i < n; i++)
        {
            var allowedCuts = new double[2];
            for (var delta = 0; delta <= 1; delta++)
            {
                var (roundCutRatio, cutRatio) = cutAdjusts[(i + n - 1 + delta) % n];
                allowedCuts[delta] =
                    roundedCorners[i].ExpectedRoundCut * roundCutRatio +
                    (roundedCorners[i].ExpectedCut - roundedCorners[i].ExpectedRoundCut) * cutRatio;
            }

            corners.Add(roundedCorners[i].GetCubics(allowedCuts[0], allowedCuts[1]));
        }

        var tempFeatures = new List<Feature>(n * 2);
        for (var i = 0; i < n; i++)
        {
            var prevVtxIndex = (i + n - 1) % n;
            var nextVtxIndex = (i + 1) % n;

            var currVertex = vertices[i];
            var prevVertex = vertices[prevVtxIndex];
            var nextVertex = vertices[nextVtxIndex];

            var convex = Utils.Convex(prevVertex, currVertex, nextVertex);
            tempFeatures.Add(new CornerFeature(corners[i], convex));

            tempFeatures.Add(new EdgeFeature([
                CubicBezier.StraightLine(corners[i].Last().Anchor1, corners[(i + 1) % n].First().Anchor0)
            ]));
        }

        var vertexArray = new double[vertices.Length * 2];
        for (var i = 0; i < vertices.Length; i++)
        {
            vertexArray[i * 2] = vertices[i].X;
            vertexArray[i * 2 + 1] = vertices[i].Y;
        }

        return (tempFeatures.ToImmutableArray(), center ?? CalculateCenter(vertexArray));
    }

    private static Point CalculateCenter(double[] vertices)
    {
        double cumulativeX = 0;
        double cumulativeY = 0;
        var index = 0;
        while (index < vertices.Length)
        {
            cumulativeX += vertices[index++];
            cumulativeY += vertices[index++];
        }

        var count = vertices.Length / 2.0;
        return new Point(cumulativeX / count, cumulativeY / count);
    }

    public RoundedPolygon Transformed(Func<Point, Point> f)
    {
        var center = f(Center);
        var features = _features.Select(x => x.Transformed(f)).ToImmutableArray();
        return new RoundedPolygon(features, center);
    }

    public RoundedPolygon Transformed(Matrix matrix)
    {
        return Transformed(matrix.Transform);
    }

    public RoundedPolygon Normalized()
    {
        var bounds = CalculateBounds();
        var side = Math.Max(bounds.Width, bounds.Height);
        var offsetX = (side - bounds.Width) / 2 - bounds.X;
        var offsetY = (side - bounds.Height) / 2 - bounds.Y;
        return Transformed(p => new Point(
            (p.X + offsetX) / side,
            (p.Y + offsetY) / side
        ));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("[RoundedPolygon. Cubics = ");
        sb.Append(String.Join(", ", _cubics));
        sb.Append(" || Features = ");
        sb.Append(String.Join(", ", _features));
        sb.Append(" || Center = (");
        sb.Append(CenterX);
        sb.Append(", ");
        sb.Append(CenterY);
        sb.Append(")]");
        return sb.ToString();
    }


    public Rect CalculateMaxBounds()
    {
        double maxDistSquared = 0;
        foreach (var cubic in _cubics)
        {
            var anchorDistance = Utils.DistanceSquared(cubic.Anchor0 - Center);
            var middlePoint = cubic.PointOnCurve(0.5);
            var middleDistance = Utils.DistanceSquared(middlePoint - Center);
            maxDistSquared = Math.Max(maxDistSquared, Math.Max(anchorDistance, middleDistance));
        }

        var distance = Math.Sqrt(maxDistSquared);

        return new Rect(
            Center.X - distance,
            Center.Y - distance,
            distance * 2,
            distance * 2);
    }


    public Rect CalculateBounds(bool approximate = true)
    {
        Rect bounds;
        var minX = Double.MaxValue;
        var minY = Double.MaxValue;
        var maxX = Double.MinValue;
        var maxY = Double.MinValue;
        foreach (var cubic in _cubics)
        {
            bounds = cubic.CalculateBounds(approximate);
            minX = Math.Min(minX, bounds.Left);
            minY = Math.Min(minY, bounds.Top);
            maxX = Math.Max(maxX, bounds.Right);
            maxY = Math.Max(maxY, bounds.Bottom);
        }

        return new Rect(
            minX,
            minY,
            maxX - minX,
            maxY - minY);
    }

    public bool Equals(RoundedPolygon? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return _features.SequenceEqual(other._features);
    }

    public override bool Equals(object? obj)
    {
        return obj is RoundedPolygon other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var feature in _features)
            hash.Add(feature);
        return hash.ToHashCode();
    }

    private static (ImmutableArray<Feature> features, Point center) BuildFromVertexCount(
        int numVertices,
        double radius,
        Point center,
        CornerRounding rounding,
        IReadOnlyList<CornerRounding>? perVertexRounding
    )
    {
        if (numVertices < 3)
            throw new ArgumentOutOfRangeException(nameof(numVertices), "Polygons must have at least 3 vertices.");

        return BuildFromVertices(
            VerticesFromNumVerts(numVertices, radius, center),
            rounding,
            perVertexRounding,
            center);
    }


    private static Point[] VerticesFromNumVerts(int numVertices, double radius, Point center)
    {
        var result = new Point[numVertices];
        var arrayIndex = 0;
        for (var i = 0; i < numVertices; i++)
        {
            var angle = Math.PI / numVertices * 2 * i;
            var vertex = Utils.RadialToCartesian(radius, angle) + center;
            result[arrayIndex++] = vertex;
        }

        return result;
    }
}

file sealed class RoundedCorner
{
    private readonly Point _p0;
    private readonly Point _p1;
    private readonly Point _p2;
    private readonly CornerRounding _rounding;

    private readonly Point _d1;
    private readonly Point _d2;
    private readonly double _cornerRadius;
    private readonly double _smoothing;
    private readonly double _cosAngle;
    private readonly double _sinAngle;

    internal double ExpectedRoundCut { get; }
    internal double ExpectedCut => (1 + _smoothing) * ExpectedRoundCut;

    private Point _center;

    internal RoundedCorner(Point p0, Point p1, Point p2, CornerRounding rounding)
    {
        _p0 = p0;
        _p1 = p1;
        _p2 = p2;
        _rounding = rounding;

        var v01 = p0 - p1;
        var v21 = p2 - p1;
        var d01 = v01.GetDistance();
        var d21 = v21.GetDistance();

        if (d01 > 0 && d21 > 0)
        {
            _d1 = v01 / d01;
            _d2 = v21 / d21;
            _cornerRadius = rounding.Radius;
            _smoothing = rounding.Smoothing;

            _cosAngle = _d1.DotProduct(_d2);
            _sinAngle = Math.Sqrt(Math.Max(0, 1 - Utils.Square(_cosAngle)));

            ExpectedRoundCut =
                _sinAngle > 1e-3
                    ? _cornerRadius * (_cosAngle + 1) / _sinAngle
                    : 0;
        }
        else
        {
            _d1 = default;
            _d2 = default;
            _cornerRadius = 0;
            _smoothing = 0;
            _cosAngle = 0;
            _sinAngle = 0;
            ExpectedRoundCut = 0;
        }

        _center = default;
    }

    internal List<CubicBezier> GetCubics(double allowedCut0, double allowedCut1)
    {
        var allowedCut = Math.Min(allowedCut0, allowedCut1);
        if (ExpectedRoundCut < Utils.DistanceEpsilon ||
            allowedCut < Utils.DistanceEpsilon ||
            _cornerRadius < Utils.DistanceEpsilon)
        {
            _center = _p1;
            return [CubicBezier.StraightLine(_p1, _p1)];
        }

        var actualRoundCut = Math.Min(allowedCut, ExpectedRoundCut);
        var actualSmoothing0 = CalculateActualSmoothingValue(allowedCut0);
        var actualSmoothing1 = CalculateActualSmoothingValue(allowedCut1);

        var actualR = _cornerRadius * actualRoundCut / ExpectedRoundCut;
        var centerDistance = Math.Sqrt(Utils.Square(actualR) + Utils.Square(actualRoundCut));

        _center = _p1 + ((_d1 + _d2) / 2).GetDirection() * centerDistance;

        var circleIntersection0 = _p1 + _d1 * actualRoundCut;
        var circleIntersection2 = _p1 + _d2 * actualRoundCut;

        var flanking0 =
            ComputeFlankingCurve(
                actualRoundCut,
                actualSmoothing0,
                _p1,
                _p0,
                circleIntersection0,
                circleIntersection2,
                _center,
                actualR);

        var flanking2 =
            ComputeFlankingCurve(
                    actualRoundCut,
                    actualSmoothing1,
                    _p1,
                    _p2,
                    circleIntersection2,
                    circleIntersection0,
                    _center,
                    actualR)
                .Reversed();

        return
        [
            flanking0,
            CubicBezier.CircularArc(_center, flanking0.Anchor1, flanking2.Anchor0),
            flanking2
        ];
    }

    private double CalculateActualSmoothingValue(double allowedCut)
    {
        if (allowedCut > ExpectedCut)
            return _smoothing;
        if (allowedCut > ExpectedRoundCut)
            return _smoothing * (allowedCut - ExpectedRoundCut) / (ExpectedCut - ExpectedRoundCut);
        return 0;
    }

    private static CubicBezier ComputeFlankingCurve(
        double actualRoundCut,
        double actualSmoothingValues,
        Point corner,
        Point sideStart,
        Point circleSegmentIntersection,
        Point otherCircleSegmentIntersection,
        Point circleCenter,
        double actualR
    )
    {
        var sideDirection = (sideStart - corner).GetDirection();
        var curveStart = corner + sideDirection * actualRoundCut * (1 + actualSmoothingValues);

        var p =
            Utils.Interpolate(
                circleSegmentIntersection,
                new Point(
                    (circleSegmentIntersection.X + otherCircleSegmentIntersection.X) / 2,
                    (circleSegmentIntersection.Y + otherCircleSegmentIntersection.Y) / 2),
                actualSmoothingValues);

        var curveEnd = circleCenter + Utils.DirectionVector(p.X - circleCenter.X, p.Y - circleCenter.Y) * actualR;

        var circleTangent = (curveEnd - circleCenter).Rotate90();
        var anchorEnd = LineIntersection(sideStart, sideDirection, curveEnd, circleTangent) ??
                        circleSegmentIntersection;

        var anchorStart = new Point(
            (curveStart.X + anchorEnd.X * 2) / 3,
            (curveStart.Y + anchorEnd.Y * 2) / 3);

        return new CubicBezier(curveStart, anchorStart, anchorEnd, curveEnd);
    }

    private static Point? LineIntersection(Point p0, Point d0, Point p1, Point d1)
    {
        var rotatedD1 = d1.Rotate90();
        var den = d0.DotProduct(rotatedD1);
        if (Math.Abs(den) < Utils.DistanceEpsilon)
            return null;

        var num = (p1 - p0).DotProduct(rotatedD1);
        if (Math.Abs(den) < Utils.DistanceEpsilon * Math.Abs(num))
            return null;

        var k = num / den;
        return p0 + d0 * k;
    }
}
