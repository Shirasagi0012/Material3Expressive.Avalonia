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

// Ported from androidx\graphics\graphics-shapes\src\commonMain\kotlin\androidx\graphics\shapes\Shapes.kt by Shirasagi0012

using Avalonia;

namespace MaterialShapes;

public partial class RoundedPolygon
{
    public static RoundedPolygon CreateCircle(
        int numVertices = 8,
        double radius = 1D,
        Point center = default
    )
    {
        if (numVertices < 3)
            throw new ArgumentException("Circle must have at least three vertices");

        // Half of the angle between two adjacent vertices on the polygon
        var theta = Math.PI / numVertices;
        // Radius of the underlying RoundedPolygon object given the desired radius of the circle
        var polygonRadius = radius / Math.Cos(theta);

        return new RoundedPolygon(
            numVertices,
            polygonRadius,
            center,
            new CornerRounding(radius)
        );
    }

    public static RoundedPolygon CreateRectangle(
        double width = 2D,
        double height = 2D,
        CornerRounding? rounding = null,
        List<CornerRounding>? perVertexRounding = null,
        Point center = default
    )
    {
        var halfWidth = width / 2D;
        var halfHeight = height / 2D;

        var topLeft = new Point(center.X - halfWidth, center.Y - halfHeight);
        var topRight = new Point(center.X + halfWidth, center.Y - halfHeight);
        var bottomRight = new Point(center.X + halfWidth, center.Y + halfHeight);
        var bottomLeft = new Point(center.X - halfWidth, center.Y + halfHeight);

        return new RoundedPolygon(
            [bottomRight, bottomLeft, topLeft, topRight],
            rounding ?? CornerRounding.Unrounded,
            perVertexRounding,
            center
        );
    }

    public static RoundedPolygon CreateStar(
        int numVerticesPerRadius,
        double radius = 1D,
        double innerRadius = 0.5D,
        CornerRounding? rounding = null,
        CornerRounding? innerRounding = null,
        List<CornerRounding>? perVertexRounding = null,
        Point center = default
    )
    {
        if (radius <= 0d || innerRadius <= 0d) throw new ArgumentException("Star radii must both be greater than 0");

        if (innerRadius >= radius) throw new ArgumentException("innerRadius must be less than radius");

        var pvRounding = perVertexRounding;
        var roundingValue = rounding ?? CornerRounding.Unrounded;

        // If no per-vertex rounding supplied and caller asked for inner rounding,
        // create per-vertex rounding list based on supplied outer/inner rounding parameters
        if (pvRounding is null && innerRounding is { })
            pvRounding = Enumerable.Range(0, numVerticesPerRadius)
                .SelectMany(_ => new[] { roundingValue, innerRounding.Value })
                .ToList();

        // Star polygon is just a polygon with all vertices supplied (where we generate
        // those vertices to be on the inner/outer radii)
        return new RoundedPolygon(
            StarVerticesFromNumVerts(numVerticesPerRadius, radius, innerRadius, center),
            roundingValue,
            pvRounding,
            center
        );
    }

    private static Point[] StarVerticesFromNumVerts(
        int numVerticesPerRadius,
        double radius,
        double innerRadius,
        Point center
    )
    {
        var result = new Point[numVerticesPerRadius * 2];
        var arrayIndex = 0;
        for (var i = 0; i < numVerticesPerRadius; i++)
        {
            var vertex = Utils.RadialToCartesian(radius, Math.PI / numVerticesPerRadius * 2 * i);
            result[arrayIndex++] = new Point(center.X + vertex.X, center.Y + vertex.Y);

            vertex = Utils.RadialToCartesian(innerRadius, Math.PI / numVerticesPerRadius * (2 * i + 1));
            result[arrayIndex++] = new Point(center.X + vertex.X, center.Y + vertex.Y);
        }

        return result;
    }

    public static RoundedPolygon CreatePill(
        double width = 2D,
        double height = 1D,
        double smoothing = 0D,
        Point center = default
    )
    {
        if (width <= 0d || height <= 0d)
            throw new ArgumentException("Pill shapes must have positive width and height");

        var wHalf = width / 2d;
        var hHalf = height / 2d;

        var vertices = new[]
        {
            new Point(wHalf + center.X, hHalf + center.Y),
            new Point(-wHalf + center.X, hHalf + center.Y),
            new Point(-wHalf + center.X, -hHalf + center.Y),
            new Point(wHalf + center.X, -hHalf + center.Y)
        };

        return new RoundedPolygon(
            vertices,
            new CornerRounding(Math.Min(wHalf, hHalf), smoothing),
            null,
            center);
    }

    public static RoundedPolygon CreatePillStar(
        double width = 2D,
        double height = 1D,
        int numVerticesPerRadius = 8,
        double innerRadiusRatio = 0.5,
        CornerRounding? rounding = null,
        CornerRounding? innerRounding = null,
        List<CornerRounding>? perVertexRounding = null,
        double vertexSpacing = 0.5,
        double startLocation = 0.0,
        Point center = default
    )
    {
        if (width <= 0d || height <= 0d)
            throw new ArgumentException("Pill shapes must have positive width and height");

        if (innerRadiusRatio <= 0d || innerRadiusRatio > 1d)
            throw new ArgumentException("innerRadius must be between 0 and 1");

        var pvRounding = perVertexRounding;
        var roundingValue = rounding ?? CornerRounding.Unrounded;

        // If no per-vertex rounding supplied and caller asked for inner rounding,
        // create per-vertex rounding list based on supplied outer/inner rounding parameters
        if (pvRounding is null && innerRounding is { })
            pvRounding = Enumerable.Range(0, numVerticesPerRadius)
                .SelectMany(_ => new[] { roundingValue, innerRounding.Value })
                .ToList();

        return new RoundedPolygon(
            PillStarVerticesFromNumVerts(
                numVerticesPerRadius,
                width,
                height,
                innerRadiusRatio,
                vertexSpacing,
                startLocation,
                center),
            roundingValue,
            pvRounding,
            center);
    }

    private static Point[] PillStarVerticesFromNumVerts(
        int numVerticesPerRadius,
        double width,
        double height,
        double innerRadius,
        double vertexSpacing,
        double startLocation,
        Point center
    )
    {
        // The general approach here is to get the perimeter of the underlying pill outline,
        // then the t value for each vertex as we walk that perimeter. This tells us where
        // on the outline to place that vertex, then we figure out where to place the vertex
        // depending on which "section" it is in. The possible sections are the vertical edges
        // on the sides, the circular sections on all four corners, or the horizontal edges
        // on the top and bottom. Note that either the vertical or horizontal edges will be
        // of length zero (whichever dimension is smaller gets only circular curvature for the
        // pill shape).

        var endcapRadius = Math.Min(width, height);

        var vSegLen = Math.Max(0d, height - width);
        var hSegLen = Math.Max(0d, width - height);
        var vSegHalf = vSegLen / 2d;
        var hSegHalf = hSegLen / 2d;

        // vertexSpacing is used to position the vertices on the end caps. The caller has the choice
        // of spacing the inner (0) or outer (1) vertices like those along the edges, causing the
        // other vertices to be either further apart (0) or closer (1). The default is .5, which
        // averages things. The magnitude of the inner and rounding parameters may cause the caller
        // to want a different value.
        var circlePerimeter = 2d * Math.PI * endcapRadius * Interpolate(innerRadius, 1d, vertexSpacing);

        // perimeter is circle perimeter plus horizontal and vertical sections of inner rectangle,
        // whether either (or even both) might be of length zero.
        var perimeter = 2d * hSegLen + 2d * vSegLen + circlePerimeter;

        // The sections array holds the t start values of that part of the outline. We use these to
        // determine which section a given vertex lies in, based on its t value, as well as where
        // in that section it lies.
        var sections = new double[11];
        sections[0] = 0d;
        sections[1] = vSegLen / 2d;
        sections[2] = sections[1] + circlePerimeter / 4d;
        sections[3] = sections[2] + hSegLen;
        sections[4] = sections[3] + circlePerimeter / 4d;
        sections[5] = sections[4] + vSegLen;
        sections[6] = sections[5] + circlePerimeter / 4d;
        sections[7] = sections[6] + hSegLen;
        sections[8] = sections[7] + circlePerimeter / 4d;
        sections[9] = sections[8] + vSegLen / 2d;
        sections[10] = perimeter;

        // "t" is the length along the entire pill outline for a given vertex. With vertices spaced
        // evenly along this contour, we can determine for any vertex where it should lie.
        var tPerVertex = perimeter / (2d * numVerticesPerRadius);

        // separate iteration for inner vs outer, unlike the other shapes, because
        // the vertices can lie in different quadrants so each needs their own calculation
        var inner = false;

        // Increment section index as we walk around the pill contour with our increasing t values
        var currSecIndex = 0;

        // secStart/End are used to determine how far along a given vertex is in the section
        // in which it lands
        var secStart = 0d;
        var secEnd = sections[1];

        // t value is used to place each vertex. 0 is on the positive x axis,
        // moving into section 0 to begin with. startLocation, a value from 0 to 1, varies the location
        // anywhere on the perimeter of the shape
        var t = startLocation * perimeter;

        // The list of vertices to be returned
        var result = new Point[numVerticesPerRadius * 2];
        var vertexIndex = 0;

        var rectBR = new Point(hSegHalf, vSegHalf);
        var rectBL = new Point(-hSegHalf, vSegHalf);
        var rectTL = new Point(-hSegHalf, -vSegHalf);
        var rectTR = new Point(hSegHalf, -vSegHalf);

        // Each iteration through this loop uses the next t value as we walk around the shape
        for (var i = 0; i < numVerticesPerRadius * 2; i++)
        {
            // t could start (and end) after 0; extra boundedT logic makes sure it does the right
            // thing when crossing the boundar past 0 again

            var boundedT = t % perimeter;
            if (boundedT < secStart)
            {
                currSecIndex = 0;
                secStart = sections[0];
                secEnd = sections[1];
            }

            while (boundedT >= sections[(currSecIndex + 1) % sections.Length])
            {
                currSecIndex = (currSecIndex + 1) % sections.Length;
                secStart = sections[currSecIndex];
                secEnd = sections[(currSecIndex + 1) % sections.Length];
            }

            var tInSection = boundedT - secStart;
            var tProportion = secEnd - secStart <= Utils.DistanceEpsilon ? 0d : tInSection / (secEnd - secStart);

            var currRadius = inner ? endcapRadius * innerRadius : endcapRadius;

            var vertex = currSecIndex switch
            {
                0 => new Point(currRadius, tProportion * vSegHalf),
                1 => Utils.RadialToCartesian(currRadius, tProportion * (Math.PI / 2d)) + rectBR,
                2 => new Point(hSegHalf - tProportion * hSegLen, currRadius),
                3 => Utils.RadialToCartesian(currRadius, Math.PI / 2d + tProportion * (Math.PI / 2d)) + rectBL,
                4 => new Point(-currRadius, vSegHalf - tProportion * vSegLen),
                5 => Utils.RadialToCartesian(currRadius, Math.PI + tProportion * (Math.PI / 2d)) + rectTL,
                6 => new Point(-hSegHalf + tProportion * hSegLen, -currRadius),
                7 => Utils.RadialToCartesian(currRadius, Math.PI * 1.5d + tProportion * (Math.PI / 2d)) + rectTR,
                // 8
                _ => new Point(currRadius, -vSegHalf + tProportion * vSegHalf)
            };

            result[vertexIndex++] = new Point(vertex.X + center.X, vertex.Y + center.Y);

            t += tPerVertex;
            inner = !inner;
        }

        return result;
    }

    private static double Interpolate(double from, double to, double t)
    {
        return from + (to - from) * t;
    }
}