using Avalonia;
using static MaterialShapes.Test.TestUtils;

namespace MaterialShapes.Test;

public class MorphTest
{
    private const double Epsilon = 1e-4;

    private readonly RoundedPolygon _poly1 = new RoundedPolygon(3, center: new Point(0.5, 0.5));
    private readonly RoundedPolygon _poly2 = new RoundedPolygon(4, center: new Point(0.5, 0.5));

    [Fact]
    public void CubicsTest()
    {
        var morph11 = new Morph(_poly1, _poly1);
        var p1Cubics = _poly1.Cubics;
        var cubics11 = morph11.AsCubics(0d);

        Assert.True(cubics11.Count > 0);

        foreach (var morphCubic in cubics11)
        {
            var matched = false;
            foreach (var p1Cubic in p1Cubics)
                if (CubicsEqualish(morphCubic, p1Cubic))
                {
                    matched = true;
                    break;
                }

            Assert.True(matched);
        }
    }
}
