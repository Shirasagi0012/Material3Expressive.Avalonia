using SkiaSharp;

namespace MaterialSkiaShadow.Internal;

/// <summary>
/// Scoped managed adaptation of the m150 SkOffsetSimplePolygon support used by shadow
/// tessellation. The intersection sweep is intentionally quadratic: Material contours contain
/// tens, not thousands, of vertices and this keeps the port independent of Skia's private trees.
/// </summary>
internal static class SimplePolygonOffset
{
    private const float NearlyZero = 1f / 4096f;
    private const float CrossTolerance = NearlyZero * NearlyZero;

    public static bool TryCreate(
        IReadOnlyList<SKPoint> input,
        float offset,
        out List<SKPoint> polygon,
        out List<int> polygonIndices)
    {
        polygon = [];
        polygonIndices = [];
        if (input.Count < 3 || input.Count >= ushort.MaxValue || !float.IsFinite(offset))
            return false;
        if (MathF.Abs(offset) <= NearlyZero)
        {
            polygon.AddRange(input);
            for (var index = 0; index < input.Count; index++)
                polygonIndices.Add(index);
            return true;
        }

        var winding = Winding(input);
        if (winding == 0 || !IsSimple(input))
            return false;

        var normals = new SKPoint[input.Count];
        for (var index = 0; index < input.Count; index++)
        {
            if (!TrySetLength(
                    new SKPoint(
                        input[index].Y - input[(index + 1) % input.Count].Y,
                        input[(index + 1) % input.Count].X - input[index].X),
                    offset * winding,
                    out normals[index]))
                return false;
        }

        List<Edge> edges = [];
        for (var current = 0; current < input.Count; current++)
        {
            var previous = (current + input.Count - 1) % input.Count;
            var next = (current + 1) % input.Count;
            if (IsReflex(input, winding, offset, previous, current, next))
            {
                if (!ComputeRadialSteps(normals[previous], normals[current], offset,
                        out var rotationSin, out var rotationCos, out var steps))
                    return false;

                var previousNormal = normals[previous];
                for (var step = 0; step < Math.Max(steps, 1); step++)
                {
                    var currentNormal = step < steps - 1
                        ? new SKPoint(
                            previousNormal.X * rotationCos - previousNormal.Y * rotationSin,
                            previousNormal.Y * rotationCos + previousNormal.X * rotationSin)
                        : normals[current];
                    AddEdge(
                        edges,
                        Add(input[current], previousNormal),
                        Add(input[current], currentNormal),
                        current,
                        current);
                    previousNormal = currentNormal;
                }
            }

            AddEdge(
                edges,
                Add(input[current], normals[current]),
                Add(input[next], normals[current]),
                current,
                next);
        }

        for (var index = 0; index < edges.Count; index++)
        {
            edges[index].Previous = (index + edges.Count - 1) % edges.Count;
            edges[index].Next = (index + 1) % edges.Count;
        }

        var head = 0;
        var currentEdge = head;
        var previousEdge = edges[currentEdge].Previous;
        var remaining = edges.Count;
        ulong iterations = 0;
        var maximumIterations = (ulong)edges.Count * (ulong)edges.Count;
        while (head >= 0 && previousEdge != currentEdge && remaining > 0)
        {
            if (++iterations > maximumIterations)
                return false;

            var previous = edges[previousEdge];
            var current = edges[currentEdge];
            if (TryCheckIntersection(previous, current, out var intersection, out var s, out var t))
            {
                if (s < previous.TValue)
                {
                    var goBack = previous.Previous;
                    Remove(edges, previousEdge, ref head);
                    remaining--;
                    previousEdge = goBack;
                }
                else if (current.TValue > -float.MaxValue &&
                         EqualsWithinTolerance(intersection, current.Intersection, 1e-6f))
                {
                    break;
                }
                else
                {
                    current.Intersection = intersection;
                    current.TValue = t;
                    current.Index = previous.End;
                    previousEdge = currentEdge;
                    currentEdge = current.Next;
                }
            }
            else
            {
                var previousPrevious = previous.Previous;
                var currentNext = current.Next;
                var distance0 = CrossingDistance(current, edges[previousPrevious]);
                var distance1 = CrossingDistance(previous, edges[currentNext]);
                if (distance0 < 0 && distance1 < 0)
                {
                    var previousEnd = Add(edges[previousPrevious].Origin, edges[previousPrevious].Vector);
                    var currentEnd = Add(current.Origin, current.Vector);
                    var previousSameContour = EqualsWithinTolerance(previousEnd, previous.Origin);
                    var currentSameContour = EqualsWithinTolerance(currentEnd, edges[currentNext].Origin);
                    if (currentSameContour && !previousSameContour)
                    {
                        Remove(edges, currentEdge, ref head);
                        currentEdge = currentNext;
                        remaining--;
                        continue;
                    }
                    if (previousSameContour && !currentSameContour)
                    {
                        Remove(edges, previousEdge, ref head);
                        previousEdge = previousPrevious;
                        remaining--;
                        continue;
                    }
                }

                if (distance0 < distance1)
                {
                    Remove(edges, previousEdge, ref head);
                    previousEdge = previousPrevious;
                }
                else
                {
                    Remove(edges, currentEdge, ref head);
                    currentEdge = currentNext;
                }
                remaining--;
            }
        }

        if (head < 0 || remaining == 0 || remaining >= ushort.MaxValue)
            return false;

        const float cleanupTolerance = 0.01f;
        polygon.Add(edges[head].Intersection);
        polygonIndices.Add(edges[head].Index);
        currentEdge = edges[head].Next;
        while (currentEdge != head)
        {
            var edge = edges[currentEdge];
            if (!EqualsWithinTolerance(edge.Intersection, polygon[^1], cleanupTolerance))
            {
                polygon.Add(edge.Intersection);
                polygonIndices.Add(edge.Index);
            }
            currentEdge = edge.Next;
        }
        if (polygon.Count > 1 && EqualsWithinTolerance(polygon[0], polygon[^1], cleanupTolerance))
        {
            polygon.RemoveAt(polygon.Count - 1);
            polygonIndices.RemoveAt(polygonIndices.Count - 1);
        }

        return Winding(polygon) * winding > 0 && IsSimple(polygon);
    }

    private static void AddEdge(List<Edge> edges, SKPoint first, SKPoint last, int start, int end) =>
        edges.Add(new Edge
        {
            Origin = first,
            Vector = Subtract(last, first),
            Intersection = first,
            TValue = -float.MaxValue,
            Index = start,
            End = end
        });

    private static bool IsReflex(
        IReadOnlyList<SKPoint> points,
        int winding,
        float offset,
        int previous,
        int current,
        int next) =>
        ComputeSide(points[previous], Subtract(points[current], points[previous]), points[next]) * winding * offset < 0;

    private static bool ComputeRadialSteps(
        SKPoint first,
        SKPoint second,
        float offset,
        out float rotationSin,
        out float rotationCos,
        out int steps)
    {
        var cosine = Dot(first, second);
        var sine = Cross(first, second);
        if (!float.IsFinite(cosine) || !float.IsFinite(sine))
        {
            rotationSin = rotationCos = default;
            steps = default;
            return false;
        }
        var theta = MathF.Atan2(sine, cosine);
        var floatingSteps = MathF.Abs(offset * theta * 0.25f);
        if (floatingSteps >= ushort.MaxValue)
        {
            rotationSin = rotationCos = default;
            steps = default;
            return false;
        }
        steps = (int)MathF.Floor(floatingSteps + 0.5f);
        var delta = steps > 0 ? theta / steps : 0;
        rotationSin = MathF.Sin(delta);
        rotationCos = MathF.Cos(delta);
        return steps <= 0 || (rotationSin != 0 && rotationCos != 1);
    }

    private static bool TryCheckIntersection(
        Edge first,
        Edge second,
        out SKPoint intersection,
        out float firstParameter,
        out float secondParameter)
    {
        if (first.End == second.Index)
        {
            var endpoint = Add(first.Origin, first.Vector);
            if (EqualsWithinTolerance(endpoint, second.Origin))
            {
                intersection = endpoint;
                firstParameter = 1;
                secondParameter = 0;
                return true;
            }
        }
        return TryIntersect(first.Origin, first.Vector, second.Origin, second.Vector,
            out intersection, out firstParameter, out secondParameter);
    }

    private static bool TryIntersect(
        SKPoint firstOrigin,
        SKPoint firstVector,
        SKPoint secondOrigin,
        SKPoint secondVector,
        out SKPoint intersection,
        out float firstParameter,
        out float secondParameter)
    {
        var delta = Subtract(secondOrigin, firstOrigin);
        var denominator = Cross(firstVector, secondVector);
        var denominatorPositive = denominator > 0;
        float firstNumerator;
        float secondNumerator;
        if (MathF.Abs(denominator) <= CrossTolerance)
        {
            if (MathF.Abs(Cross(delta, firstVector)) > CrossTolerance ||
                MathF.Abs(Cross(delta, secondVector)) > CrossTolerance)
                return Failure(out intersection, out firstParameter, out secondParameter);

            var firstLengthSquared = Dot(firstVector, firstVector);
            if (!Usable(firstVector, firstLengthSquared))
            {
                var secondLengthSquared = Dot(secondVector, secondVector);
                if (!Usable(secondVector, secondLengthSquared))
                {
                    if (MathF.Sqrt(Dot(delta, delta)) <= NearlyZero)
                    {
                        intersection = firstOrigin;
                        firstParameter = secondParameter = 0;
                        return true;
                    }
                    return Failure(out intersection, out firstParameter, out secondParameter);
                }
                secondNumerator = Dot(secondVector, Multiply(delta, -1));
                denominator = secondLengthSquared;
                if (Outside(secondNumerator, denominator, true))
                    return Failure(out intersection, out firstParameter, out secondParameter);
                firstNumerator = 0;
            }
            else
            {
                firstNumerator = Dot(firstVector, delta);
                denominator = firstLengthSquared;
                secondNumerator = 0;
                if (Outside(firstNumerator, denominator, true))
                {
                    var secondLengthSquared = Dot(secondVector, secondVector);
                    if (!Usable(secondVector, secondLengthSquared))
                        return Failure(out intersection, out firstParameter, out secondParameter);
                    var old = firstNumerator;
                    firstNumerator = Dot(firstVector, Add(delta, secondVector));
                    secondNumerator = denominator;
                    if (Outside(firstNumerator, denominator, true))
                    {
                        if (firstNumerator * old > 0)
                            return Failure(out intersection, out firstParameter, out secondParameter);
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
            secondNumerator = Cross(delta, firstVector);
            if (Outside(firstNumerator, denominator, denominatorPositive) ||
                Outside(secondNumerator, denominator, denominatorPositive))
                return Failure(out intersection, out firstParameter, out secondParameter);
        }

        firstParameter = firstNumerator / denominator;
        secondParameter = secondNumerator / denominator;
        intersection = Add(firstOrigin, Multiply(firstVector, firstParameter));
        return true;
    }

    private static float CrossingDistance(Edge first, Edge second)
    {
        var denominator = Cross(first.Vector, second.Vector);
        if (MathF.Abs(denominator) <= CrossTolerance)
            return float.MaxValue;
        var parameter = Cross(Subtract(second.Origin, first.Origin), second.Vector) / denominator;
        parameter = parameter < 0 ? -parameter : parameter - 1;
        return parameter * MathF.Abs(parameter) * Dot(first.Vector, first.Vector);
    }

    private static void Remove(List<Edge> edges, int index, ref int head)
    {
        var edge = edges[index];
        edges[edge.Previous].Next = edge.Next;
        edges[edge.Next].Previous = edge.Previous;
        if (index == head)
            head = edge.Next == index ? -1 : edge.Next;
    }

    private static bool IsSimple(IReadOnlyList<SKPoint> points)
    {
        if (points.Count < 3 || points.Any(point => !float.IsFinite(point.X) || !float.IsFinite(point.Y)))
            return false;
        for (var first = 0; first < points.Count; first++)
        {
            var firstNext = (first + 1) % points.Count;
            for (var second = first + 1; second < points.Count; second++)
            {
                var secondNext = (second + 1) % points.Count;
                if (first == second || firstNext == second || secondNext == first)
                    continue;
                if (SegmentsProperlyIntersect(points[first], points[firstNext], points[second], points[secondNext]))
                    return false;
            }
        }
        return true;
    }

    private static bool SegmentsProperlyIntersect(SKPoint a, SKPoint b, SKPoint c, SKPoint d)
    {
        var abC = ComputeSide(a, Subtract(b, a), c);
        var abD = ComputeSide(a, Subtract(b, a), d);
        var cdA = ComputeSide(c, Subtract(d, c), a);
        var cdB = ComputeSide(c, Subtract(d, c), b);
        return abC * abD < 0 && cdA * cdB < 0;
    }

    private static int Winding(IReadOnlyList<SKPoint> points)
    {
        if (points.Count < 3)
            return 0;
        var area = 0f;
        var previous = Subtract(points[1], points[0]);
        for (var index = 2; index < points.Count; index++)
        {
            var current = Subtract(points[index], points[0]);
            area += Cross(previous, current);
            previous = current;
        }
        return MathF.Abs(area) <= CrossTolerance ? 0 : area > 0 ? 1 : -1;
    }

    private static int ComputeSide(SKPoint origin, SKPoint vector, SKPoint point)
    {
        var value = Cross(vector, Subtract(point, origin));
        return MathF.Abs(value) <= CrossTolerance ? 0 : value > 0 ? 1 : -1;
    }

    private static bool TrySetLength(SKPoint point, float length, out SKPoint result)
    {
        var magnitude = Math.Sqrt((double)point.X * point.X + (double)point.Y * point.Y);
        var scale = length / magnitude;
        result = new SKPoint((float)(point.X * scale), (float)(point.Y * scale));
        return float.IsFinite(result.X) && float.IsFinite(result.Y) && (result.X != 0 || result.Y != 0);
    }

    private static bool Failure(out SKPoint point, out float first, out float second)
    {
        point = default;
        first = second = default;
        return false;
    }

    private static bool Outside(float numerator, float denominator, bool positive) =>
        positive ? numerator < 0 || numerator > denominator : numerator > 0 || numerator < denominator;
    private static bool Usable(SKPoint vector, float lengthSquared) =>
        float.IsFinite(vector.X) && float.IsFinite(vector.Y) && lengthSquared != 0;
    private static bool EqualsWithinTolerance(SKPoint first, SKPoint second, float tolerance = NearlyZero) =>
        MathF.Abs(first.X - second.X) <= tolerance && MathF.Abs(first.Y - second.Y) <= tolerance;
    private static float Dot(SKPoint first, SKPoint second) => first.X * second.X + first.Y * second.Y;
    private static float Cross(SKPoint first, SKPoint second) => first.X * second.Y - first.Y * second.X;
    private static SKPoint Add(SKPoint first, SKPoint second) => new(first.X + second.X, first.Y + second.Y);
    private static SKPoint Subtract(SKPoint first, SKPoint second) => new(first.X - second.X, first.Y - second.Y);
    private static SKPoint Multiply(SKPoint point, float value) => new(point.X * value, point.Y * value);

    private sealed class Edge
    {
        public int Previous;
        public int Next;
        public SKPoint Origin;
        public SKPoint Vector;
        public SKPoint Intersection;
        public float TValue;
        public int Index;
        public int End;
    }
}
