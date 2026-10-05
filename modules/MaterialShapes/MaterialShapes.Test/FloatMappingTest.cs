namespace MaterialShapes.Test;

using static TestUtils;

public class FloatMappingTest
{
    [Fact]
    public void IdentityMappingTest()
    {
        ValidateMapping(DoubleMapper.Identity, x => x);
    }

    [Fact]
    public void SimpleMappingTest()
    {
        ValidateMapping(
            new DoubleMapper((0d, 0d), (0.5d, 0.25d)),
            x => x < 0.5d ? x / 2d : (3d * x - 1d) / 2d);
    }

    [Fact]
    public void TargetWrapsTest()
    {
        ValidateMapping(
            new DoubleMapper((0d, 0.5d), (0.1d, 0.6d)),
            x => (x + 0.5d) % 1d);
    }

    [Fact]
    public void SourceWrapsTest()
    {
        ValidateMapping(
            new DoubleMapper((0.5d, 0d), (0.1d, 0.6d)),
            x => (x + 0.5d) % 1d);
    }

    [Fact]
    public void BothWrapTest()
    {
        ValidateMapping(
            new DoubleMapper((0.5d, 0.5d), (0.75d, 0.75d), (0.1d, 0.1d), (0.49d, 0.49d)),
            x => x);
    }

    [Fact]
    public void MultiplePointTest()
    {
        ValidateMapping(
            new DoubleMapper((0.4d, 0.2d), (0.5d, 0.22d), (0d, 0.8d)),
            x =>
            {
                if (x < 0.4d)
                    return (0.8d + x) % 1d;
                if (x < 0.5d)
                    return 0.2d + (x - 0.4d) / 5d;
                return 0.22d + (x - 0.5d) * 1.16d;
            });
    }

    [Fact]
    public void TargetDoubleWrapThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new DoubleMapper((0.0d, 0.0d), (0.3d, 0.6d), (0.6d, 0.3d), (0.9d, 0.9d)));
    }

    [Fact]
    public void SourceDoubleWrapThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            new DoubleMapper((0.0d, 0.0d), (0.6d, 0.3d), (0.3d, 0.6d), (0.9d, 0.9d)));
    }

    private static void ValidateMapping(DoubleMapper mapper, Func<double, double> expectedFunction)
    {
        for (var i = 0; i <= 9999; i++)
        {
            var source = i / 10000d;
            var target = expectedFunction(source);

            AssertEqualish(target, mapper.Map(source));
            AssertEqualish(source, mapper.MapBack(target));
        }
    }
}