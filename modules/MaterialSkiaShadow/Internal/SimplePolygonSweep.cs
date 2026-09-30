/*
 * Copyright 2017 Google LLC
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE.Skia.txt file.
 * Managed adaptation of SkPolyUtils::SkIsSimplePolygon and its sweep-line tree.
 */

using SkiaSharp;

namespace MaterialSkiaShadow.Internal;

internal static class SimplePolygonSweep
{
    private const float CrossTolerance = 1f / (4096f * 4096f);

    public static bool IsConvex(IReadOnlyList<SKPoint> polygon)
    {
        if (polygon.Count < 3)
            return false;
        var lastCross = 0f;
        var xChanges = 0;
        var yChanges = 0;
        var current = 0;
        var next = 1;
        var previousVector = Subtract(polygon[current], polygon[^1]);
        var lastX = previousVector.X;
        var lastY = previousVector.Y;
        var nextVector = Subtract(polygon[next], polygon[current]);
        for (var index = 0; index < polygon.Count; index++)
        {
            if (!float.IsFinite(polygon[index].X) || !float.IsFinite(polygon[index].Y))
                return false;
            var cross = Cross(previousVector, nextVector);
            if (lastCross * cross < 0)
                return false;
            if (cross != 0)
                lastCross = cross;
            if (lastX * nextVector.X < 0)
                xChanges++;
            if (lastY * nextVector.Y < 0)
                yChanges++;
            if (xChanges > 2 || yChanges > 2)
                return false;
            current = next;
            next = (current + 1) % polygon.Count;
            if (nextVector.X != 0)
                lastX = nextVector.X;
            if (nextVector.Y != 0)
                lastY = nextVector.Y;
            previousVector = nextVector;
            nextVector = Subtract(polygon[next], polygon[current]);
        }
        return true;
    }

    public static bool IsSimple(IReadOnlyList<SKPoint> polygon)
    {
        if (polygon.Count < 3)
            return false;
        if (IsConvex(polygon))
            return true;
        if (polygon.Count > 2048)
            return false;

        // Preserve SkTDPQueue's binary heap and tie ordering. Sorting the vertices, or using
        // an all-pairs crossing test, changes rejection of nearly collinear offset contours.
        var heap = new List<int>(polygon.Count);
        for (var index = 0; index < polygon.Count; index++)
        {
            if (!float.IsFinite(polygon[index].X) || !float.IsFinite(polygon[index].Y) ||
                polygon[(index + polygon.Count - 1) % polygon.Count] == polygon[(index + 1) % polygon.Count])
                return false;
            heap.Add(index);
            var child = heap.Count - 1;
            while (child > 0)
            {
                var parent = (child - 1) >> 1;
                if (!Left(polygon[heap[child]], polygon[heap[parent]]))
                    break;
                (heap[child], heap[parent]) = (heap[parent], heap[child]);
                child = parent;
            }
        }

        var sweep = new EdgeList(polygon.Count);
        while (heap.Count > 0)
        {
            var index = heap[0];
            var previous = (index + polygon.Count - 1) % polygon.Count;
            var next = (index + 1) % polygon.Count;
            var previousLeft = Left(polygon[previous], polygon[index]);
            var nextLeft = Left(polygon[next], polygon[index]);
            if (!previousLeft && !nextLeft)
            {
                if (!sweep.Insert(polygon[index], polygon[previous], index, previous) ||
                    !sweep.Insert(polygon[index], polygon[next], index, next))
                    return false;
            }
            else if (previousLeft && nextLeft)
            {
                if (!sweep.Remove(polygon[previous], polygon[index], previous, index) ||
                    !sweep.Remove(polygon[next], polygon[index], next, index))
                    return false;
            }
            else if (previousLeft)
            {
                if (!sweep.Replace(polygon[previous], polygon[index], polygon[next], previous, index, next))
                    return false;
            }
            else if (!sweep.Replace(polygon[next], polygon[index], polygon[previous], next, index, previous))
            {
                return false;
            }

            heap[0] = heap[^1];
            heap.RemoveAt(heap.Count - 1);
            var parent = 0;
            while (2 * parent + 1 < heap.Count)
            {
                var child = 2 * parent + 1;
                if (child + 1 < heap.Count && Left(polygon[heap[child + 1]], polygon[heap[child]]))
                    child++;
                if (!Left(polygon[heap[child]], polygon[heap[parent]]))
                    break;
                (heap[child], heap[parent]) = (heap[parent], heap[child]);
                parent = child;
            }
        }
        return true;
    }

    private static bool Left(SKPoint first, SKPoint second) =>
        first.X < second.X || (!(first.X > second.X) && first.Y > second.Y);
    private static SKPoint Subtract(SKPoint first, SKPoint second) => new(first.X - second.X, first.Y - second.Y);
    private static SKPoint Add(SKPoint first, SKPoint second) => new(first.X + second.X, first.Y + second.Y);
    private static float Cross(SKPoint first, SKPoint second) => first.X * second.Y - first.Y * second.X;
    private static int Side(SKPoint origin, SKPoint vector, SKPoint point)
    {
        var cross = Cross(vector, Subtract(point, origin));
        return MathF.Abs(cross) <= CrossTolerance ? 0 : cross > 0 ? 1 : -1;
    }

    private sealed class Edge
    {
        public SKPoint Origin;
        public SKPoint Vector;
        public int Index0;
        public int Index1;
        public readonly Edge?[] Children = new Edge?[2];
        public Edge? Above;
        public Edge? Below;
        public bool Red;

        public bool Intersects(SKPoint origin, SKPoint vector, int index0, int index1)
        {
            if (Index0 == index0 || Index1 == index0 || Index0 == index1 || Index1 == index1)
                return false;
            var end = Add(Origin, Vector);
            var otherEnd = Add(origin, vector);
            if (Origin.X < origin.X)
                return otherEnd.X < end.X
                    ? Side(Origin, Vector, origin) * Side(Origin, Vector, otherEnd) < 0
                    : Side(Origin, Vector, origin) * Side(origin, vector, end) > 0;
            return end.X < otherEnd.X
                ? Side(origin, vector, Origin) * Side(origin, vector, end) < 0
                : Side(origin, vector, Origin) * Side(Origin, Vector, otherEnd) > 0;
        }

        public bool Intersects(Edge edge) => Intersects(edge.Origin, edge.Vector, edge.Index0, edge.Index1);
    }

    private sealed class EdgeList(int maximumEdges)
    {
        private readonly Edge _head = new();
        private int _allocated;

        public bool Insert(SKPoint origin, SKPoint end, int index0, int index1)
        {
            var vector = Subtract(end, origin);
            if (!float.IsFinite(vector.X) || !float.IsFinite(vector.Y))
                return false;
            if (_head.Children[1] is null)
            {
                _head.Children[1] = Allocate(origin, vector, index0, index1);
                if (_head.Children[1] is null)
                    return false;
                _head.Children[1]!.Red = false;
                return true;
            }

            var top = _head;
            Edge? grandparent = null;
            Edge? parent = null;
            var current = top.Children[1];
            var direction = 0;
            var last = 0;
            Edge? predecessor = null;
            Edge? successor = null;
            while (true)
            {
                if (current is null)
                {
                    if (predecessor?.Intersects(origin, vector, index0, index1) == true ||
                        successor?.Intersects(origin, vector, index0, index1) == true)
                        return false;
                    current = parent!.Children[direction] = Allocate(origin, vector, index0, index1);
                    if (current is null)
                        return false;
                    current.Above = predecessor;
                    current.Below = successor;
                    if (predecessor is not null)
                    {
                        if (predecessor.Origin == origin && predecessor.Vector == vector)
                            return false;
                        predecessor.Below = current;
                    }
                    if (successor is not null)
                    {
                        if (successor.Origin == origin && successor.Vector == vector)
                            return false;
                        successor.Above = current;
                    }
                    if (IsRed(parent))
                    {
                        var direction2 = top.Children[1] == grandparent ? 1 : 0;
                        top.Children[direction2] = current == parent.Children[last]
                            ? Rotate(grandparent!, 1 - last) : DoubleRotate(grandparent!, 1 - last);
                    }
                    break;
                }
                if (IsRed(current.Children[0]) && IsRed(current.Children[1]))
                {
                    current.Red = true;
                    current.Children[0]!.Red = false;
                    current.Children[1]!.Red = false;
                    if (IsRed(parent))
                    {
                        var direction2 = top.Children[1] == grandparent ? 1 : 0;
                        top.Children[direction2] = current == parent!.Children[last]
                            ? Rotate(grandparent!, 1 - last) : DoubleRotate(grandparent!, 1 - last);
                    }
                }
                last = direction;
                var side = Side(current.Origin, current.Vector, current.Index0 == index0 ? end : origin);
                if (side == 0)
                    return false;
                direction = side < 0 ? 1 : 0;
                if (direction == 0)
                    successor = current;
                else
                    predecessor = current;
                if (grandparent is not null)
                    top = grandparent;
                grandparent = parent;
                parent = current;
                current = current.Children[direction];
            }
            _head.Children[1]!.Red = false;
            return true;
        }

        public bool Replace(SKPoint origin, SKPoint end, SKPoint nextEnd, int index0, int index1, int index2)
        {
            var current = _head.Children[1];
            Edge? found = null;
            while (current is not null)
            {
                if (current.Index0 == index0 && current.Index1 == index1)
                {
                    found = current;
                    break;
                }
                var side = Side(current.Origin, current.Vector, current.Index1 == index1 ? origin : end);
                if (side == 0)
                    return false;
                current = current.Children[side < 0 ? 1 : 0];
            }
            if (found is null)
                return false;
            var vector = Subtract(nextEnd, end);
            var predecessor = found.Above;
            var successor = found.Below;
            if (predecessor is not null && (predecessor.Intersects(found) || predecessor.Intersects(end, vector, index1, index2)) ||
                successor is not null && (successor.Intersects(found) || successor.Intersects(end, vector, index1, index2)))
                return false;
            found.Origin = end;
            found.Vector = vector;
            found.Index0 = index1;
            found.Index1 = index2;
            return true;
        }

        public bool Remove(SKPoint origin, SKPoint end, int index0, int index1)
        {
            if (_head.Children[1] is null)
                return false;
            var current = _head;
            Edge? parent = null;
            Edge? grandparent = null;
            Edge? found = null;
            var direction = 1;
            while (current.Children[direction] is not null)
            {
                var last = direction;
                grandparent = parent;
                parent = current;
                current = current.Children[direction]!;
                if (current.Index0 == index0 && current.Index1 == index1)
                {
                    found = current;
                    direction = 0;
                }
                else
                {
                    var side = Side(current.Origin, current.Vector, current.Index1 == index1 ? origin : end);
                    if (side == 0)
                        return false;
                    direction = side < 0 ? 1 : 0;
                }
                if (!IsRed(current) && !IsRed(current.Children[direction]))
                {
                    if (IsRed(current.Children[1 - direction]))
                    {
                        parent = parent.Children[last] = Rotate(current, direction);
                    }
                    else
                    {
                        var sibling = parent.Children[1 - last];
                        if (sibling is not null)
                        {
                            if (!IsRed(sibling.Children[1 - last]) && !IsRed(sibling.Children[last]))
                            {
                                parent.Red = false;
                                sibling.Red = true;
                                current.Red = true;
                            }
                            else
                            {
                                var direction2 = grandparent!.Children[1] == parent ? 1 : 0;
                                var rotated = grandparent.Children[direction2] = IsRed(sibling.Children[last])
                                    ? DoubleRotate(parent, last) : Rotate(parent, last);
                                current.Red = rotated.Red = true;
                                rotated.Children[0]!.Red = false;
                                rotated.Children[1]!.Red = false;
                            }
                        }
                    }
                }
            }
            if (found is not null)
            {
                var predecessor = found.Above;
                var successor = found.Below;
                if (predecessor?.Intersects(found) == true || successor?.Intersects(found) == true)
                    return false;
                if (found != current)
                {
                    found.Origin = current.Origin;
                    found.Vector = current.Vector;
                    found.Index0 = current.Index0;
                    found.Index1 = current.Index1;
                    found.Above = current.Above;
                    predecessor = found.Above;
                }
                else if (successor is not null)
                {
                    successor.Above = predecessor;
                }
                if (predecessor is not null)
                    predecessor.Below = current.Below;
                parent!.Children[parent.Children[1] == current ? 1 : 0] = current.Children[current.Children[0] is null ? 1 : 0];
            }
            if (_head.Children[1] is not null)
                _head.Children[1]!.Red = false;
            return true;
        }

        private Edge? Allocate(SKPoint origin, SKPoint vector, int index0, int index1) =>
            _allocated++ >= maximumEdges ? null : new Edge
            {
                Origin = origin, Vector = vector, Index0 = index0, Index1 = index1, Red = true
            };

        private static bool IsRed(Edge? edge) => edge?.Red == true;
        private static Edge Rotate(Edge node, int direction)
        {
            var next = node.Children[1 - direction]!;
            node.Children[1 - direction] = next.Children[direction];
            next.Children[direction] = node;
            node.Red = true;
            next.Red = false;
            return next;
        }
        private static Edge DoubleRotate(Edge node, int direction)
        {
            node.Children[1 - direction] = Rotate(node.Children[1 - direction]!, 1 - direction);
            return Rotate(node, direction);
        }
    }
}
