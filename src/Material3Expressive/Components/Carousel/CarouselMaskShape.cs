using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// CarouselItemScope.rememberMaskShape / MultiAspectCarousel.MaskShape. The owner decides
// when to replace the base shape; mask geometry still follows size and layout direction.
internal sealed class CarouselMaskShape(Action invalidate)
{
    private MaterialShape? _shape;
    private CornerRadius _corners;
    private bool _initialized;
    private PathGeometry? _path;
    private (Rect Rect, FlowDirection Direction, double Density, Size? EmptyFallback) _key;

    internal bool HasShape => _shape is not null;
    internal CornerRadius Corners => _corners;

    internal void Configure(MaterialShape? shape, CornerRadius corners, bool remember)
    {
        if (_initialized && (remember || ReferenceEquals(_shape, shape) && _corners == corners)) return;
        Reset();
        _initialized = true;
        _shape = shape;
        _corners = corners;
        if (_shape is not null) _shape.PropertyChanged += ShapeChanged;
    }

    internal PathGeometry Geometry(Rect rect, FlowDirection direction, double density, Size? emptyFallback = null)
    {
        var key = (rect, direction, density, emptyFallback);
        if (_path is not null && _key == key) return _path;
        var source = _shape?.CreateOutline(rect.Size, direction, density)
            ?? RoundedRectangleGeometry.Create(new Rect(rect.Size), _corners);
        // Snapshot the factory result: a custom shape may reuse and mutate its own path.
        _path = source.Figures is { Count: > 0 } ? PathGeometry.Parse(source.ToString()) : new PathGeometry();
        // addOutline adds the source contours to a new NonZero path. It does not copy
        // Outline.Generic's fill type; even-odd holes can therefore become filled masks.
        _path.FillRule = FillRule.NonZero;
        _path.Transform = new MatrixTransform((source.Transform?.Value ?? Matrix.Identity)
            * Matrix.CreateTranslation(rect.X, rect.Y));
        if (_path.Figures is not { Count: > 0 } && emptyFallback is { } size)
            _path = RoundedRectangleGeometry.Create(new Rect(size), default);
        _key = key;
        return _path;
    }

    private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        _path = null;
        invalidate();
    }

    internal void Reset()
    {
        if (_shape is not null) _shape.PropertyChanged -= ShapeChanged;
        _shape = null;
        _path = null;
        _initialized = false;
    }
}
