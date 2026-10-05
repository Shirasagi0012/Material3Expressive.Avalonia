using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia;
using MaterialShapes;

namespace MaterialShapes.Gallery.Models;

public sealed partial class ShapeItem(
    string name,
    Func<RoundedPolygon> shapeGen,
    Func<string> shapeDetailsProvider,
    bool usesSides = true,
    bool usesInnerRatio = true,
    bool usesRoundness = true,
    bool usesInnerParameters = true
)
{
    public string Name { get; } = name;
    public Func<RoundedPolygon> ShapeGen { get; } = shapeGen;
    public Func<string> ShapeDetailsProvider { get; } = shapeDetailsProvider;
    public bool UsesSides { get; } = usesSides;
    public bool UsesInnerRatio { get; } = usesInnerRatio;
    public bool UsesRoundness { get; } = usesRoundness;
    public bool UsesInnerParameters { get; } = usesInnerParameters;
}

public sealed partial class ShapeParameters : ObservableObject
{
    public enum ShapeId
    {
        Star,
        Polygon,
        Triangle,
        Blob,
        CornerSE
    }

    [ObservableProperty] private int _shapeIndex;
    [ObservableProperty] private double _sides = 5;
    [ObservableProperty] private double _innerRadius = 0.5;
    [ObservableProperty] private double _roundness;
    [ObservableProperty] private double _smooth;
    [ObservableProperty] private double _innerRoundness;
    [ObservableProperty] private double _innerSmooth;
    [ObservableProperty] private double _rotation;

    public ShapeParameters(
        int sides = 5,
        double innerRadius = 0.5,
        double roundness = 0,
        double smooth = 0,
        double innerRoundness = 0,
        double innerSmooth = 0,
        double rotation = 0,
        ShapeId shapeId = ShapeId.Polygon)
    {
        _sides = sides;
        _innerRadius = innerRadius;
        _roundness = roundness;
        _smooth = smooth;
        _innerRoundness = innerRoundness;
        _innerSmooth = innerSmooth;
        _rotation = rotation;
        _shapeIndex = (int)shapeId;
        _shapes = BuildShapes();
    }

    public ShapeParameters Copy()
    {
        return new ShapeParameters(
            (int)Math.Round(Sides),
            InnerRadius,
            Roundness,
            Smooth,
            InnerRoundness,
            InnerSmooth,
            Rotation,
            (ShapeId)ShapeIndex);
    }

    public IReadOnlyList<ShapeItem> Shapes => _shapes;

    private readonly List<ShapeItem> _shapes;

    public ShapeItem SelectedShape => _shapes[ShapeIndex];

    public string ShapeDetails => SelectedShape.ShapeDetailsProvider();

    public RoundedPolygon GenerateShape(bool autoSize = true)
    {
        var polygon = SelectedShape.ShapeGen();

        if (!autoSize && Rotation == 0)
            return polygon;

        var bounds = polygon.CalculateBounds();
        var center = new Point((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
        var scale = 2.0 / Math.Max(bounds.Width, bounds.Height);
        var radians = ShapeUtilities.ToRadians(Rotation);
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);

        return polygon.Transformed(p =>
        {
            var x = p.X;
            var y = p.Y;
            if (autoSize)
            {
                x -= center.X;
                y -= center.Y;
                x *= scale;
                y *= scale;
            }

            if (Rotation != 0)
            {
                var rx = x * cos - y * sin;
                var ry = x * sin + y * cos;
                x = rx;
                y = ry;
            }

            return new Point(x, y);
        });
    }

    public string ShapeDescription(
        string? id = null,
        int? numVerts = null,
        int? sides = null,
        double? innerRadius = null,
        double? roundness = null,
        double? innerRoundness = null,
        double? smooth = null,
        double? innerSmooth = null,
        double? rotation = null,
        string? code = null)
    {
        var sb = new System.Text.StringBuilder("ShapeParameters:\n");
        if (id != null) sb.Append($"shapeId = {id}, ");
        if (numVerts != null) sb.Append($"numVertices = {numVerts}, ");
        if (sides != null) sb.Append($"sides = {sides}, ");
        if (innerRadius != null) sb.Append($"innerRadius = {innerRadius}, ");
        if (roundness != null) sb.Append($"roundness = {roundness}, ");
        if (innerRoundness != null) sb.Append($"innerRoundness = {innerRoundness}, ");
        if (smooth != null) sb.Append($"smoothness = {smooth}, ");
        if (innerSmooth != null) sb.Append($"innerSmooth = {innerSmooth}, ");
        if (rotation != null) sb.Append($"rotation = {rotation}, ");
        if (numVerts != null) sb.Append($"numVerts = {numVerts}, ");
        if (code != null)
        {
            sb.Append("\nCode:\n");
            sb.Append(code);
        }
        return sb.ToString();
    }

    partial void OnShapeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedShape));
        OnPropertyChanged(nameof(ShapeDetails));
    }

    partial void OnSidesChanged(double value)
    {
        OnPropertyChanged(nameof(ShapeDetails));
    }

    partial void OnInnerRadiusChanged(double value)
    {
        OnPropertyChanged(nameof(ShapeDetails));
    }

    partial void OnRoundnessChanged(double value)
    {
        OnPropertyChanged(nameof(ShapeDetails));
    }

    partial void OnSmoothChanged(double value)
    {
        OnPropertyChanged(nameof(ShapeDetails));
    }

    partial void OnInnerRoundnessChanged(double value)
    {
        OnPropertyChanged(nameof(ShapeDetails));
    }

    partial void OnInnerSmoothChanged(double value)
    {
        OnPropertyChanged(nameof(ShapeDetails));
    }

    partial void OnRotationChanged(double value)
    {
        OnPropertyChanged(nameof(ShapeDetails));
    }

    private static Point[] SquarePoints()
    {
        return
        [
            new Point(1, 1),
            new Point(-1, 1),
            new Point(-1, -1),
            new Point(1, -1)
        ];
    }

    public ShapeParameters() : this(shapeId: ShapeId.Polygon)
    {
    }

    private List<ShapeItem> BuildShapes()
    {
        return
        [
            new ShapeItem(
                "Star",
                () => RoundedPolygon.CreateStar(
                    numVerticesPerRadius: (int)Math.Round(Sides),
                    innerRadius: InnerRadius,
                    rounding: new CornerRounding(Roundness, Smooth),
                    innerRounding: new CornerRounding(InnerRoundness, InnerSmooth)),
                () => ShapeDescription(
                    id: "Star",
                    sides: (int)Math.Round(Sides),
                    innerRadius: InnerRadius,
                    roundness: Roundness,
                    smooth: Smooth,
                    innerRoundness: InnerRoundness,
                    innerSmooth: InnerSmooth,
                    rotation: Rotation,
                    code: $"RoundedPolygon.CreateStar(numVerticesPerRadius = {Sides}, " +
                          $"innerRadius = {InnerRadius}, rounding = new CornerRounding({Roundness}, {Smooth}), " +
                          $"innerRounding = new CornerRounding({InnerRoundness}, {InnerSmooth}))")
            ),
            new ShapeItem(
                "Polygon",
                () => new RoundedPolygon(
                    numVertices: (int)Math.Round(Sides),
                    rounding: new CornerRounding(Roundness, Smooth)),
                () => ShapeDescription(
                    id: "Polygon",
                    sides: (int)Math.Round(Sides),
                    roundness: Roundness,
                    smooth: Smooth,
                    rotation: Rotation,
                    code: $"new RoundedPolygon(numVertices: {(int)Math.Round(Sides)}, " +
                          $"rounding: new CornerRounding({Roundness}, {Smooth}))"),
                usesInnerRatio: false,
                usesInnerParameters: false
            ),
            new ShapeItem(
                "Triangle",
                () =>
                {
                    var points = new[]
                    {
                        ShapeUtilities.RadialToCartesian(1, ShapeUtilities.ToRadians(270)),
                        ShapeUtilities.RadialToCartesian(1, ShapeUtilities.ToRadians(30)),
                        ShapeUtilities.RadialToCartesian(InnerRadius, ShapeUtilities.ToRadians(90)),
                        ShapeUtilities.RadialToCartesian(1, ShapeUtilities.ToRadians(150))
                    };
                    return new RoundedPolygon(
                        points,
                        new CornerRounding(Roundness, Smooth),
                        center: default);
                },
                () => ShapeDescription(
                    id: "Triangle",
                    innerRadius: InnerRadius,
                    smooth: Smooth,
                    rotation: Rotation,
                    code: "var points = new[] {\n" +
                          "    RadialToCartesian(1, ToRadians(270)),\n" +
                          "    RadialToCartesian(1, ToRadians(30)),\n" +
                          "    RadialToCartesian(innerRadius, ToRadians(90)),\n" +
                          "    RadialToCartesian(1, ToRadians(150))\n" +
                          "};\n" +
                          "new RoundedPolygon(points, new CornerRounding(roundness, smooth))"),
                usesSides: false,
                usesInnerParameters: false
            ),
            new ShapeItem(
                "Blob",
                () =>
                {
                    var sx = Math.Max(InnerRadius, 0.1);
                    var sy = Math.Max(Roundness, 0.1);
                    var verts = new[]
                    {
                        new Point(-sx, -sy),
                        new Point(sx, -sy),
                        new Point(sx, sy),
                        new Point(-sx, sy)
                    };
                    return new RoundedPolygon(
                        verts,
                        new CornerRounding(Math.Min(sx, sy), Smooth),
                        center: default);
                },
                () => ShapeDescription(
                    id: "Blob",
                    roundness: Roundness,
                    smooth: Smooth,
                    rotation: Rotation,
                    code: "var sx = Math.Max(innerRadius, 0.1);\n" +
                          "var sy = Math.Max(roundness, 0.1);\n" +
                          "var verts = new[] { new Point(-sx, -sy), new Point(sx, -sy), " +
                          "new Point(sx, sy), new Point(-sx, sy) };\n" +
                          "new RoundedPolygon(verts, new CornerRounding(Math.Min(sx, sy), smooth))"),
                usesSides: false,
                usesInnerParameters: false
            ),
            new ShapeItem(
                "CornerSE",
                () => new RoundedPolygon(
                    SquarePoints(),
                    perVertexRounding:
                    [
                        new CornerRounding(Roundness, Smooth),
                        new CornerRounding(1),
                        new CornerRounding(1),
                        new CornerRounding(1)
                    ],
                    center: default),
                () => ShapeDescription(
                    id: "cornerSE",
                    roundness: Roundness,
                    smooth: Smooth,
                    rotation: Rotation,
                    code: "new RoundedPolygon(points, perVertexRounding: new[] {" +
                          $"new CornerRounding({Roundness}, {Smooth}), new CornerRounding(1), " +
                          "new CornerRounding(1), new CornerRounding(1) })"),
                usesSides: false,
                usesInnerRatio: false,
                usesInnerParameters: false
            ),
            new ShapeItem(
                "Circle",
                () => RoundedPolygon.CreateCircle((int)Math.Round(Sides)),
                () => ShapeDescription(
                    id: "Circle",
                    roundness: Roundness,
                    smooth: Smooth,
                    rotation: Rotation,
                    code: $"RoundedPolygon.CreateCircle({Sides})"),
                usesSides: true,
                usesInnerRatio: false,
                usesInnerParameters: false
            ),
            new ShapeItem(
                "Rectangle",
                () => RoundedPolygon.CreateRectangle(
                    width: 4,
                    height: 2,
                    rounding: new CornerRounding(Roundness, Smooth)),
                () => ShapeDescription(
                    id: "Rectangle",
                    numVerts: 4,
                    roundness: Roundness,
                    smooth: Smooth,
                    rotation: Rotation,
                    code: $"RoundedPolygon.CreateRectangle(width: 4, height: 2, rounding: " +
                          $"new CornerRounding({Roundness}, {Smooth}))"),
                usesSides: false,
                usesInnerRatio: false,
                usesInnerParameters: false
            )
        ];
    }
}
