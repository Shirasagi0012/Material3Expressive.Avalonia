/*
 * Copyright 2024 The Android Open Source Project
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

// Ported from androidx\compose\material3\MaterialShape.kt by Shirasagi0012

using Avalonia;

namespace MaterialShapes;

public static class MaterialShapeCatalog
{
    private static readonly CornerRounding CornerRound15 = new(0.15);
    private static readonly CornerRounding CornerRound20 = new(0.2);
    private static readonly CornerRounding CornerRound30 = new(0.3);
    private static readonly CornerRounding CornerRound50 = new(0.5);
    private static readonly CornerRounding CornerRound100 = new(1);

    private static readonly Matrix RotateNeg45 = CreateRotationDegrees(-45);
    private static readonly Matrix RotateNeg90 = CreateRotationDegrees(-90);
    private static readonly Matrix RotateNeg135 = CreateRotationDegrees(-135);

    private static RoundedPolygon? _circle;
    private static RoundedPolygon? _square;
    private static RoundedPolygon? _slanted;
    private static RoundedPolygon? _arch;
    private static RoundedPolygon? _fan;
    private static RoundedPolygon? _arrow;
    private static RoundedPolygon? _semiCircle;
    private static RoundedPolygon? _oval;
    private static RoundedPolygon? _pill;
    private static RoundedPolygon? _triangle;
    private static RoundedPolygon? _diamond;
    private static RoundedPolygon? _clamShell;
    private static RoundedPolygon? _pentagon;
    private static RoundedPolygon? _gem;
    private static RoundedPolygon? _verySunny;
    private static RoundedPolygon? _sunny;
    private static RoundedPolygon? _cookie4Sided;
    private static RoundedPolygon? _cookie6Sided;
    private static RoundedPolygon? _cookie7Sided;
    private static RoundedPolygon? _cookie9Sided;
    private static RoundedPolygon? _cookie12Sided;
    private static RoundedPolygon? _ghostish;
    private static RoundedPolygon? _clover4Leaf;
    private static RoundedPolygon? _clover8Leaf;
    private static RoundedPolygon? _burst;
    private static RoundedPolygon? _softBurst;
    private static RoundedPolygon? _boom;
    private static RoundedPolygon? _softBoom;
    private static RoundedPolygon? _flower;
    private static RoundedPolygon? _puffy;
    private static RoundedPolygon? _puffyDiamond;
    private static RoundedPolygon? _pixelCircle;
    private static RoundedPolygon? _pixelTriangle;
    private static RoundedPolygon? _bun;
    private static RoundedPolygon? _heart;

    public static RoundedPolygon Circle => _circle ??= CirclePolygon().Normalized();
    public static RoundedPolygon Square => _square ??= SquarePolygon().Normalized();
    public static RoundedPolygon Slanted => _slanted ??= SlantedPolygon().Normalized();
    public static RoundedPolygon Arch => _arch ??= ArchPolygon().Normalized();
    public static RoundedPolygon Fan => _fan ??= FanPolygon().Normalized();
    public static RoundedPolygon Arrow => _arrow ??= ArrowPolygon().Normalized();
    public static RoundedPolygon SemiCircle => _semiCircle ??= SemiCirclePolygon().Normalized();
    public static RoundedPolygon Oval => _oval ??= OvalPolygon().Normalized();
    public static RoundedPolygon Pill => _pill ??= PillPolygon().Normalized();
    public static RoundedPolygon Triangle => _triangle ??= TrianglePolygon().Normalized();
    public static RoundedPolygon Diamond => _diamond ??= DiamondPolygon().Normalized();
    public static RoundedPolygon ClamShell => _clamShell ??= ClamShellPolygon().Normalized();
    public static RoundedPolygon Pentagon => _pentagon ??= PentagonPolygon().Normalized();
    public static RoundedPolygon Gem => _gem ??= GemPolygon().Normalized();
    public static RoundedPolygon Sunny => _sunny ??= SunnyPolygon().Normalized();
    public static RoundedPolygon VerySunny => _verySunny ??= VerySunnyPolygon().Normalized();
    public static RoundedPolygon Cookie4Sided => _cookie4Sided ??= Cookie4Polygon().Normalized();
    public static RoundedPolygon Cookie6Sided => _cookie6Sided ??= Cookie6Polygon().Normalized();
    public static RoundedPolygon Cookie7Sided => _cookie7Sided ??= Cookie7Polygon().Normalized();
    public static RoundedPolygon Cookie9Sided => _cookie9Sided ??= Cookie9Polygon().Normalized();
    public static RoundedPolygon Cookie12Sided => _cookie12Sided ??= Cookie12Polygon().Normalized();
    public static RoundedPolygon Ghostish => _ghostish ??= GhostishPolygon().Normalized();
    public static RoundedPolygon Clover4Leaf => _clover4Leaf ??= Clover4Polygon().Normalized();
    public static RoundedPolygon Clover8Leaf => _clover8Leaf ??= Clover8Polygon().Normalized();
    public static RoundedPolygon Burst => _burst ??= BurstPolygon().Normalized();
    public static RoundedPolygon SoftBurst => _softBurst ??= SoftBurstPolygon().Normalized();
    public static RoundedPolygon Boom => _boom ??= BoomPolygon().Normalized();
    public static RoundedPolygon SoftBoom => _softBoom ??= SoftBoomPolygon().Normalized();
    public static RoundedPolygon Flower => _flower ??= FlowerPolygon().Normalized();
    public static RoundedPolygon Puffy => _puffy ??= PuffyPolygon().Normalized();
    public static RoundedPolygon PuffyDiamond => _puffyDiamond ??= PuffyDiamondPolygon().Normalized();
    public static RoundedPolygon PixelCircle => _pixelCircle ??= PixelCirclePolygon().Normalized();
    public static RoundedPolygon PixelTriangle => _pixelTriangle ??= PixelTrianglePolygon().Normalized();
    public static RoundedPolygon Bun => _bun ??= BunPolygon().Normalized();
    public static RoundedPolygon Heart => _heart ??= HeartPolygon().Normalized();

    internal static RoundedPolygon CirclePolygon(int numVertices = 10)
    {
        return RoundedPolygon.CreateCircle(numVertices);
    }

    internal static RoundedPolygon SquarePolygon()
    {
        return RoundedPolygon.CreateRectangle(width: 1, height: 1, rounding: CornerRound30);
    }

    internal static RoundedPolygon SlantedPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.926, 0.970), new CornerRounding(0.189, 0.811)),
                new(new Point(-0.021, 0.967), new CornerRounding(0.187, 0.057))
            },
            2);
    }

    internal static RoundedPolygon ArchPolygon()
    {
        return new RoundedPolygon(
                4,
                perVertexRounding: new List<CornerRounding>
                {
                    CornerRound100, CornerRound100, CornerRound20, CornerRound20
                })
            .Transformed(RotateNeg135);
    }

    internal static RoundedPolygon FanPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(1.004, 1.000), new CornerRounding(0.148, 0.417)),
                new(new Point(0.000, 1.000), new CornerRounding(0.151)),
                new(new Point(0.000, -0.003), new CornerRounding(0.148)),
                new(new Point(0.978, 0.020), new CornerRounding(0.803))
            },
            1);
    }

    internal static RoundedPolygon ArrowPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, 0.892), new CornerRounding(0.313)),
                new(new Point(-0.216, 1.050), new CornerRounding(0.207)),
                new(new Point(0.499, -0.160), new CornerRounding(0.215, 1.000)),
                new(new Point(1.225, 1.060), new CornerRounding(0.211))
            },
            1);
    }

    internal static RoundedPolygon SemiCirclePolygon()
    {
        return RoundedPolygon.CreateRectangle(
            width: 1.6,
            height: 1,
            perVertexRounding: new List<CornerRounding>
            {
                CornerRound20, CornerRound20, CornerRound100, CornerRound100
            });
    }

    internal static RoundedPolygon OvalPolygon()
    {
        var m = new Matrix(1, 0, 0, 0.64, 0, 0);
        // MaterialShapes.oval() intentionally uses graphics-shapes' default 8-vertex circle.
        // MaterialShapes.circle() uses 10 vertices, but reusing that count here changes the
        // transformed oval's max bounds and makes LoadingIndicator about one percent too small.
        return RoundedPolygon.CreateCircle()
            .Transformed(m)
            .Transformed(RotateNeg45);
    }

    internal static RoundedPolygon PillPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.961, 0.039), new CornerRounding(0.426)),
                new(new Point(1.001, 0.428)),
                new(new Point(1.000, 0.609), new CornerRounding(1.000))
            },
            reps: 2,
            mirroring: true);
    }

    internal static RoundedPolygon TrianglePolygon()
    {
        return new RoundedPolygon(3, rounding: CornerRound20)
            .Transformed(RotateNeg90);
    }

    internal static RoundedPolygon DiamondPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, 1.096), new CornerRounding(0.151, 0.524)),
                new(new Point(0.040, 0.500), new CornerRounding(0.159))
            },
            2);
    }

    internal static RoundedPolygon ClamShellPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.171, 0.841), new CornerRounding(0.159)),
                new(new Point(-0.020, 0.500), new CornerRounding(0.140)),
                new(new Point(0.170, 0.159), new CornerRounding(0.159))
            },
            2);
    }

    internal static RoundedPolygon PentagonPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, -0.009), new CornerRounding(0.172)),
                new(new Point(1.030, 0.365), new CornerRounding(0.164)),
                new(new Point(0.828, 0.970), new CornerRounding(0.169))
            },
            reps: 1,
            mirroring: true);
    }

    internal static RoundedPolygon GemPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.499, 1.023), new CornerRounding(0.241, 0.778)),
                new(new Point(-0.005, 0.792), new CornerRounding(0.208)),
                new(new Point(0.073, 0.258), new CornerRounding(0.228)),
                new(new Point(0.433, -0.000), new CornerRounding(0.491))
            },
            1,
            mirroring: true);
    }

    internal static RoundedPolygon SunnyPolygon()
    {
        return RoundedPolygon.CreateStar(
            numVerticesPerRadius: 8,
            innerRadius: 0.8,
            rounding: CornerRound15);
    }

    internal static RoundedPolygon VerySunnyPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, 1.080), new CornerRounding(0.085)),
                new(new Point(0.358, 0.843), new CornerRounding(0.085))
            },
            8);
    }

    internal static RoundedPolygon Cookie4Polygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(1.237, 1.236), new CornerRounding(0.258)),
                new(new Point(0.500, 0.918), new CornerRounding(0.233))
            },
            4);
    }

    internal static RoundedPolygon Cookie6Polygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.723, 0.884), new CornerRounding(0.394)),
                new(new Point(0.500, 1.099), new CornerRounding(0.398))
            },
            6);
    }

    internal static RoundedPolygon Cookie7Polygon()
    {
        return RoundedPolygon.CreateStar(
                numVerticesPerRadius: 7,
                innerRadius: 0.75,
                rounding: CornerRound50)
            .Transformed(RotateNeg90);
    }

    internal static RoundedPolygon Cookie9Polygon()
    {
        return RoundedPolygon.CreateStar(
                numVerticesPerRadius: 9,
                innerRadius: 0.8,
                rounding: CornerRound50)
            .Transformed(RotateNeg90);
    }

    internal static RoundedPolygon Cookie12Polygon()
    {
        return RoundedPolygon.CreateStar(
                numVerticesPerRadius: 12,
                innerRadius: 0.8,
                rounding: CornerRound50)
            .Transformed(RotateNeg90);
    }

    internal static RoundedPolygon GhostishPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, 0.000), new CornerRounding(1.000)),
                new(new Point(1.000, 0.000), new CornerRounding(1.000)),
                new(new Point(1.000, 1.140), new CornerRounding(0.254, 0.106)),
                new(new Point(0.575, 0.906), new CornerRounding(0.253))
            },
            reps: 1,
            mirroring: true);
    }

    internal static RoundedPolygon Clover4Polygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, 0.074)),
                new(new Point(0.725, -0.099), new CornerRounding(0.476))
            },
            reps: 4,
            mirroring: true);
    }

    internal static RoundedPolygon Clover8Polygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, 0.036)),
                new(new Point(0.758, -0.101), new CornerRounding(0.209))
            },
            reps: 8);
    }

    internal static RoundedPolygon BurstPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, -0.006), new CornerRounding(0.006)),
                new(new Point(0.592, 0.158), new CornerRounding(0.006))
            },
            reps: 12);
    }

    internal static RoundedPolygon SoftBurstPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.193, 0.277), new CornerRounding(0.053)),
                new(new Point(0.176, 0.055), new CornerRounding(0.053))
            },
            reps: 10);
    }

    internal static RoundedPolygon BoomPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.457, 0.296), new CornerRounding(0.007)),
                new(new Point(0.500, -0.051), new CornerRounding(0.007))
            },
            reps: 15);
    }

    internal static RoundedPolygon SoftBoomPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.733, 0.454)),
                new(new Point(0.839, 0.437), new CornerRounding(0.532)),
                new(new Point(0.949, 0.449), new CornerRounding(0.439, 1.000)),
                new(new Point(0.998, 0.478), new CornerRounding(0.174))
            },
            reps: 16,
            mirroring: true);
    }

    internal static RoundedPolygon FlowerPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.370, 0.187)),
                new(new Point(0.416, 0.049), new CornerRounding(0.381)),
                new(new Point(0.479, 0.001), new CornerRounding(0.095))
            },
            reps: 8,
            mirroring: true);
    }

    internal static RoundedPolygon PuffyPolygon()
    {
        var m = new Matrix(1, 0, 0, 0.742, 0, 0);
        return CustomPolygon(
                new List<PointNRound>
                {
                    new(new Point(0.500, 0.053)),
                    new(new Point(0.545, -0.040), new CornerRounding(0.405)),
                    new(new Point(0.670, -0.035), new CornerRounding(0.426)),
                    new(new Point(0.717, 0.066), new CornerRounding(0.574)),
                    new(new Point(0.722, 0.128)),
                    new(new Point(0.777, 0.002), new CornerRounding(0.360)),
                    new(new Point(0.914, 0.149), new CornerRounding(0.660)),
                    new(new Point(0.926, 0.289), new CornerRounding(0.660)),
                    new(new Point(0.881, 0.346)),
                    new(new Point(0.940, 0.344), new CornerRounding(0.126)),
                    new(new Point(1.003, 0.437), new CornerRounding(0.255))
                },
                reps: 2,
                mirroring: true)
            .Transformed(m);
    }

    internal static RoundedPolygon PuffyDiamondPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.870, 0.130), new CornerRounding(0.146)),
                new(new Point(0.818, 0.357)),
                new(new Point(1.000, 0.332), new CornerRounding(0.853))
            },
            reps: 4,
            mirroring: true);
    }

    internal static RoundedPolygon PixelCirclePolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, 0.000)),
                new(new Point(0.704, 0.000)),
                new(new Point(0.704, 0.065)),
                new(new Point(0.843, 0.065)),
                new(new Point(0.843, 0.148)),
                new(new Point(0.926, 0.148)),
                new(new Point(0.926, 0.296)),
                new(new Point(1.000, 0.296))
            },
            reps: 2,
            mirroring: true);
    }

    internal static RoundedPolygon PixelTrianglePolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.110, 0.500)),
                new(new Point(0.113, 0.000)),
                new(new Point(0.287, 0.000)),
                new(new Point(0.287, 0.087)),
                new(new Point(0.421, 0.087)),
                new(new Point(0.421, 0.170)),
                new(new Point(0.560, 0.170)),
                new(new Point(0.560, 0.265)),
                new(new Point(0.674, 0.265)),
                new(new Point(0.675, 0.344)),
                new(new Point(0.789, 0.344)),
                new(new Point(0.789, 0.439)),
                new(new Point(0.888, 0.439))
            },
            reps: 1,
            mirroring: true);
    }

    internal static RoundedPolygon BunPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.796, 0.500)),
                new(new Point(0.853, 0.518), new CornerRounding(1)),
                new(new Point(0.992, 0.631), new CornerRounding(1)),
                new(new Point(0.968, 1.000), new CornerRounding(1))
            },
            reps: 2,
            mirroring: true);
    }

    internal static RoundedPolygon HeartPolygon()
    {
        return CustomPolygon(
            new List<PointNRound>
            {
                new(new Point(0.500, 0.268), new CornerRounding(0.016)),
                new(new Point(0.792, -0.066), new CornerRounding(0.958)),
                new(new Point(1.064, 0.276), new CornerRounding(1.000)),
                new(new Point(0.501, 0.946), new CornerRounding(0.129))
            },
            reps: 1,
            mirroring: true);
    }

    private readonly struct PointNRound(Point point, CornerRounding? rounding = null)
    {
        public Point O { get; } = point;
        public CornerRounding R { get; } = rounding ?? CornerRounding.Unrounded;
    }

    private static List<PointNRound> DoRepeat(
        IReadOnlyList<PointNRound> points,
        int reps,
        Point center,
        bool mirroring)
    {
        if (mirroring)
        {
            var angles = points
                .Select(p => AngleDegrees(p.O - center))
                .ToArray();
            var distances = points
                .Select(p =>
                {
                    var v = p.O - center;
                    return Math.Sqrt(v.X * v.X + v.Y * v.Y);
                })
                .ToArray();

            var actualReps = reps * 2;
            var sectionAngle = 360.0 / actualReps;
            var list = new List<PointNRound>();

            for (var rep = 0; rep < actualReps; rep++)
            {
                for (var index = 0; index < points.Count; index++)
                {
                    var i = rep % 2 == 0 ? index : points.Count - 1 - index;
                    if (i > 0 || rep % 2 == 0)
                    {
                        var angle = sectionAngle * rep +
                                    (rep % 2 == 0
                                        ? angles[i]
                                        : sectionAngle - angles[i] + 2 * angles[0]);
                        var rad = ToRadians(angle);
                        var x = Math.Cos(rad) * distances[i] + center.X;
                        var y = Math.Sin(rad) * distances[i] + center.Y;
                        list.Add(new PointNRound(new Point(x, y), points[i].R));
                    }
                }
            }

            return list;
        }

        var np = points.Count;
        var result = new List<PointNRound>(np * reps);
        for (var i = 0; i < np * reps; i++)
        {
            var basePoint = points[i % np];
            var angle = (i / np) * 360.0 / reps;
            var rotated = RotateDegrees(basePoint.O, angle, center);
            result.Add(new PointNRound(rotated, basePoint.R));
        }

        return result;
    }

    private static Point RotateDegrees(Point point, double angle, Point center)
    {
        var rad = ToRadians(angle);
        var off = point - center;
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        var x = off.X * cos - off.Y * sin + center.X;
        var y = off.X * sin + off.Y * cos + center.Y;
        return new Point(x, y);
    }

    private static double ToRadians(double degrees)
    {
        return degrees / 360.0 * 2.0 * Math.PI;
    }

    private static double AngleDegrees(Vector v)
    {
        return Math.Atan2(v.Y, v.X) * 180.0 / Math.PI;
    }

    private static RoundedPolygon CustomPolygon(
        IReadOnlyList<PointNRound> pnr,
        int reps,
        Point? center = null,
        bool mirroring = false)
    {
        var actualCenter = center ?? new Point(0.5, 0.5);
        var actualPoints = DoRepeat(pnr, reps, actualCenter, mirroring);
        var vertices = new Point[actualPoints.Count];
        var rounding = new List<CornerRounding>(actualPoints.Count);
        for (var i = 0; i < actualPoints.Count; i++)
        {
            vertices[i] = actualPoints[i].O;
            rounding.Add(actualPoints[i].R);
        }

        return new RoundedPolygon(
            vertices,
            CornerRounding.Unrounded,
            rounding,
            actualCenter);
    }

    private static Matrix CreateRotationDegrees(double degrees)
    {
        var rad = ToRadians(degrees);
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        return new Matrix(cos, sin, -sin, cos, 0, 0);
    }
}
