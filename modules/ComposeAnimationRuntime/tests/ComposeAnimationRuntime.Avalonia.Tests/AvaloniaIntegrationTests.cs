using Avalonia;
using ComposeAnimationRuntime.Avalonia;
using Xunit;

namespace ComposeAnimationRuntime.Avalonia.Tests;

public sealed class AvaloniaIntegrationTests
{
    [Fact]
    public void PointConverterRoundTrips()
    {
        var converter = new PointVectorConverter();
        var value = new Point(12.5, -4.25);
        Assert.Equal(value, converter.ConvertFromVector(converter.ConvertToVector(value)));
    }

    [Fact]
    public void BindingCanBeDisposed()
    {
        var owner = new TestObject();
        var converter = new DoubleVectorConverter();
        var animation = new ComposeAnimationRuntime.Animatable<double>(0, converter);
        using (MotionBinding.Bind(owner, TestObject.ValueProperty, animation))
        {
            animation.SnapTo(1);
            Assert.Equal(1, owner.Value);
        }

        animation.SnapTo(2);
        Assert.Equal(1, owner.Value);
    }

    private sealed class TestObject : AvaloniaObject
    {
        public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<TestObject, double>(nameof(Value));
        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    }
}
