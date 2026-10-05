using Avalonia;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class VisibilityThresholdsTest
{
    [Fact]
    public void For_ReturnsTypedConstants()
    {
        Assert.Equal(VisibilityThresholds.Double, VisibilityThresholds.For(new DoubleConverter()));
        Assert.Equal(VisibilityThresholds.Float, VisibilityThresholds.For(new FloatConverter()));
        Assert.Equal(VisibilityThresholds.Int32, VisibilityThresholds.For(new Int32Converter()));
        Assert.Equal(VisibilityThresholds.Point, VisibilityThresholds.For(new PointConverter()));
        Assert.Equal(VisibilityThresholds.Vector, VisibilityThresholds.For(new VectorConverter()));
        Assert.Equal(VisibilityThresholds.Size, VisibilityThresholds.For(new SizeConverter()));
        Assert.Equal(VisibilityThresholds.Rect, VisibilityThresholds.For(new RectConverter()));
        Assert.Equal(VisibilityThresholds.Thickness, VisibilityThresholds.For(new ThicknessConverter()));
        Assert.Equal(VisibilityThresholds.CornerRadius, VisibilityThresholds.For(new CornerRadiusConverter()));
    }

    [Fact]
    public void For_LooksUpByConverterTypeNotInstance()
    {
        Assert.Equal(
            VisibilityThresholds.For(new PointConverter()),
            VisibilityThresholds.For(new PointConverter()));
    }

    [Fact]
    public void For_FillsEveryComponent()
    {
        AssertFilled(new DoubleConverter(), VisibilityThresholds.Double, VisibilityThresholds.Default);
        AssertFilled(new FloatConverter(), VisibilityThresholds.Float, VisibilityThresholds.Default);
        AssertFilled(new Int32Converter(), VisibilityThresholds.Int32, VisibilityThresholds.Int32);
        AssertFilled(new PointConverter(), VisibilityThresholds.Point, VisibilityThresholds.Dip);
        AssertFilled(new VectorConverter(), VisibilityThresholds.Vector, VisibilityThresholds.Dip);
        AssertFilled(new SizeConverter(), VisibilityThresholds.Size, VisibilityThresholds.Dip);
        AssertFilled(new RectConverter(), VisibilityThresholds.Rect, VisibilityThresholds.Dip);
        AssertFilled(new ThicknessConverter(), VisibilityThresholds.Thickness, VisibilityThresholds.Dip);
        AssertFilled(new CornerRadiusConverter(), VisibilityThresholds.CornerRadius, VisibilityThresholds.Dip);
    }

    [Fact]
    public void For_ColorAndUnknownConverters_ReturnDefault()
    {
        Assert.Equal(default, VisibilityThresholds.For(new ColorConverter()));
        Assert.Equal(0.0, VisibilityThresholds.For(new UnknownDoubleConverter()));
    }

    [Fact]
    public void InterruptionSpring_UsesTheTable()
    {
        Assert.Equal(VisibilityThresholds.Double,
            TransitionDefaults.InterruptionSpring(new DoubleConverter()).VisibilityThreshold);
        Assert.Equal(VisibilityThresholds.Float,
            TransitionDefaults.InterruptionSpring(new FloatConverter()).VisibilityThreshold);
        Assert.Equal(VisibilityThresholds.Int32,
            TransitionDefaults.InterruptionSpring(new Int32Converter()).VisibilityThreshold);
        Assert.Equal(VisibilityThresholds.Point,
            TransitionDefaults.InterruptionSpring(new PointConverter()).VisibilityThreshold);
        Assert.Equal(VisibilityThresholds.Vector,
            TransitionDefaults.InterruptionSpring(new VectorConverter()).VisibilityThreshold);
        Assert.Equal(VisibilityThresholds.Size,
            TransitionDefaults.InterruptionSpring(new SizeConverter()).VisibilityThreshold);
        Assert.Equal(VisibilityThresholds.Rect,
            TransitionDefaults.InterruptionSpring(new RectConverter()).VisibilityThreshold);
        Assert.Equal(VisibilityThresholds.Thickness,
            TransitionDefaults.InterruptionSpring(new ThicknessConverter()).VisibilityThreshold);
        Assert.Equal(VisibilityThresholds.CornerRadius,
            TransitionDefaults.InterruptionSpring(new CornerRadiusConverter()).VisibilityThreshold);
        Assert.Equal(default,
            TransitionDefaults.InterruptionSpring(new ColorConverter()).VisibilityThreshold);
        Assert.Equal(0.0,
            TransitionDefaults.InterruptionSpring(new UnknownDoubleConverter()).VisibilityThreshold);
    }

    [Fact]
    public void DipThreshold_ShortensPointSpringVersusDefault()
    {
        var converter = new PointConverter();
        var from = new Point(0, 0);
        var to = new Point(100, 100);
        var tight = new TargetBasedAnimation<Point>(new SpringSpec<Point>(), converter, from, to);
        var loose = new TargetBasedAnimation<Point>(
            new SpringSpec<Point>(visibilityThreshold: VisibilityThresholds.Point), converter, from, to);

        Assert.True(loose.Duration < tight.Duration);
    }

    [Fact]
    public void ThicknessDipThreshold_ShortensSpringVersusDefault()
    {
        var converter = new ThicknessConverter();
        var from = new Thickness(0);
        var to = new Thickness(100);
        var tight = new TargetBasedAnimation<Thickness>(new SpringSpec<Thickness>(), converter, from, to);
        var loose = new TargetBasedAnimation<Thickness>(
            new SpringSpec<Thickness>(visibilityThreshold: VisibilityThresholds.Thickness), converter, from, to);

        Assert.True(loose.Duration < tight.Duration);
    }

    [Fact]
    public void ColorForDefault_MatchesUntypedSpringDuration()
    {
        var converter = new ColorConverter();
        var from = Colors.Red;
        var to = Colors.Blue;
        var viaFor = new TargetBasedAnimation<Color>(
            new SpringSpec<Color>(visibilityThreshold: VisibilityThresholds.For(converter)),
            converter, from, to);
        var untyped = new TargetBasedAnimation<Color>(new SpringSpec<Color>(), converter, from, to);

        Assert.Equal(untyped.Duration, viaFor.Duration);
    }

    [Fact]
    public void RejectsNullConverter()
    {
        Assert.Throws<ArgumentNullException>(() => VisibilityThresholds.For<double>(null!));
    }

    private static void AssertFilled<T>(ITwoWayConverter<T> converter, T value, double perComponent)
    {
        var vector = new double[converter.Dimension];
        converter.To(value, vector);
        Assert.All(vector, component => Assert.Equal(perComponent, component, 5));
    }

    private sealed class UnknownDoubleConverter : ITwoWayConverter<double>
    {
        public int Dimension => 1;

        public void To(double value, Span<double> destination) => destination[0] = value;

        public double From(ReadOnlySpan<double> vector) => vector[0];
    }
}
