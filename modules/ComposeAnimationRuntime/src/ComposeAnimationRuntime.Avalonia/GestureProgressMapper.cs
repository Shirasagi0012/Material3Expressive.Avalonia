using System.Numerics;
using Avalonia;

namespace ComposeAnimationRuntime.Avalonia;

public interface IProgressMapper
{
    double Map(Vector2 position, Size viewport);
}

public sealed class HorizontalProgressMapper : IProgressMapper
{
    public double Map(Vector2 position, Size viewport)
        => viewport.Width <= 0 ? 0 : Math.Clamp(position.X / viewport.Width, 0, 1);
}

public sealed class VerticalProgressMapper : IProgressMapper
{
    public double Map(Vector2 position, Size viewport)
        => viewport.Height <= 0 ? 0 : Math.Clamp(position.Y / viewport.Height, 0, 1);
}

public sealed class AxisProgressMapper(bool horizontal = true) : IProgressMapper
{
    private static readonly HorizontalProgressMapper Horizontal = new();
    private static readonly VerticalProgressMapper Vertical = new();
    public double Map(Vector2 position, Size viewport)
        => horizontal
            ? Horizontal.Map(position, viewport)
            : Vertical.Map(position, viewport);
}

public sealed class CustomProgressMapper(Func<Vector2, Size, double> map) : IProgressMapper
{
    private readonly Func<Vector2, Size, double> _map = map ?? throw new ArgumentNullException(nameof(map));
    public double Map(Vector2 position, Size viewport) => Math.Clamp(_map(position, viewport), 0, 1);
}
