/*
 * Copyright 2017 Google LLC
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE.Skia.txt file.
 * Managed adaptation of SkPolyUtils::SkTriangulateSimplePolygon and ReflexHash.
 */

using SkiaSharp;

namespace MaterialSkiaShadow.Internal;

internal static class SimplePolygonTriangulator
{
    public static bool TryAppend(IReadOnlyList<SKPoint> points, ushort[] indexMap, List<ushort> indices)
    {
        if (points.Count < 3 || points.Count >= ushort.MaxValue)
            return false;
        var bounds = new SKRect(points[0].X, points[0].Y, points[0].X, points[0].Y);
        var area = 0f;
        var previousVector = Subtract(points[1], points[0]);
        for (var index = 0; index < points.Count; index++)
        {
            var point = points[index];
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                return false;
            bounds.Left = MathF.Min(bounds.Left, point.X);
            bounds.Top = MathF.Min(bounds.Top, point.Y);
            bounds.Right = MathF.Max(bounds.Right, point.X);
            bounds.Bottom = MathF.Max(bounds.Bottom, point.Y);
            if (index >= 2)
            {
                var vector = Subtract(point, points[0]);
                area += Cross(previousVector, vector);
                previousVector = vector;
            }
        }
        const float nearlyZero = 1f / 4096f;
        const float crossTolerance = nearlyZero * nearlyZero;
        if (MathF.Abs(area) <= crossTolerance)
            return false;
        var winding = area > 0 ? 1 : -1;
        if (!float.IsFinite(bounds.Width) || !float.IsFinite(bounds.Height))
            return false;
        var horizontal = MathF.Sqrt(points.Count * bounds.Width / bounds.Height);
        if (!float.IsFinite(horizontal))
            return false;
        var horizontalCount = (int)Math.Clamp(MathF.Floor(horizontal + 0.5f), 1, points.Count);
        var verticalCount = points.Count / horizontalCount;
        var conversion = new SKPoint(
            (horizontalCount - 0.001f) / bounds.Width,
            (verticalCount - 0.001f) / bounds.Height);
        if (!float.IsFinite(conversion.X) || !float.IsFinite(conversion.Y))
            return false;

        var previous = new int[points.Count];
        var next = new int[points.Count];
        var reflex = new bool[points.Count];
        var cells = new LinkedList<int>[horizontalCount * verticalCount];
        for (var cell = 0; cell < cells.Length; cell++)
            cells[cell] = new LinkedList<int>();
        var convex = new LinkedList<int>();
        var nodes = new LinkedListNode<int>[points.Count];
        int Hash(int index) =>
            (int)((points[index].Y - bounds.Top) * conversion.Y) * horizontalCount +
            (int)((points[index].X - bounds.Left) * conversion.X);
        for (var index = 0; index < points.Count; index++)
        {
            previous[index] = (index + points.Count - 1) % points.Count;
            next[index] = (index + 1) % points.Count;
            reflex[index] = winding * Cross(
                Subtract(points[index], points[previous[index]]),
                Subtract(points[next[index]], points[index])) <= crossTolerance;
            nodes[index] = new LinkedListNode<int>(index);
        }
        for (var index = 0; index < points.Count; index++)
        {
            if (reflex[index])
                cells[Hash(index)].AddLast(nodes[index]);
            else if (reflex[previous[index]] || reflex[next[index]])
                convex.AddFirst(nodes[index]);
            else
                convex.AddLast(nodes[index]);
        }

        void Reclassify(int index)
        {
            if (reflex[index] && winding * Cross(
                    Subtract(points[index], points[previous[index]]),
                    Subtract(points[next[index]], points[index])) > crossTolerance)
            {
                reflex[index] = false;
                cells[Hash(index)].Remove(nodes[index]);
                convex.AddLast(nodes[index]);
            }
        }

        var remaining = points.Count;
        while (remaining > 3)
        {
            var ear = -1;
            foreach (var index in convex)
            {
                var first = previous[index];
                var third = next[index];
                var a = points[first];
                var b = points[index];
                var c = points[third];
                var left = MathF.Min(a.X, MathF.Min(b.X, c.X));
                var top = MathF.Min(a.Y, MathF.Min(b.Y, c.Y));
                var right = MathF.Max(a.X, MathF.Max(b.X, c.X));
                var bottom = MathF.Max(a.Y, MathF.Max(b.Y, c.Y));
                var failed = false;
                for (var row = (int)((top - bounds.Top) * conversion.Y);
                     row <= (int)((bottom - bounds.Top) * conversion.Y) && !failed; row++)
                {
                    for (var column = (int)((left - bounds.Left) * conversion.X);
                         column <= (int)((right - bounds.Left) * conversion.X) && !failed; column++)
                    {
                        foreach (var candidate in cells[row * horizontalCount + column])
                        {
                            if (candidate != first && candidate != third && PointInTriangle(a, b, c, points[candidate]))
                            {
                                failed = true;
                                break;
                            }
                        }
                    }
                }
                if (!failed)
                {
                    ear = index;
                    break;
                }
            }
            if (ear < 0)
                return false;
            indices.Add(indexMap[previous[ear]]);
            indices.Add(indexMap[ear]);
            indices.Add(indexMap[next[ear]]);
            convex.Remove(nodes[ear]);
            remaining--;
            next[previous[ear]] = next[ear];
            Reclassify(previous[ear]);
            previous[next[ear]] = previous[ear];
            Reclassify(next[ear]);
        }
        foreach (var index in convex)
            indices.Add(indexMap[index]);
        return true;
    }

    private static bool PointInTriangle(SKPoint first, SKPoint second, SKPoint third, SKPoint point)
    {
        var v0 = Subtract(second, first);
        var v1 = Subtract(third, second);
        var normal = Cross(v0, v1);
        const float tolerance = 1f / 4096f;
        return normal * Cross(v0, Subtract(point, first)) >= tolerance &&
               normal * Cross(v1, Subtract(point, second)) >= tolerance &&
               normal * Cross(Subtract(first, third), Subtract(point, third)) >= tolerance;
    }

    private static SKPoint Subtract(SKPoint first, SKPoint second) => new(first.X - second.X, first.Y - second.Y);
    private static float Cross(SKPoint first, SKPoint second) => first.X * second.Y - first.Y * second.X;
}
