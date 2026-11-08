using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

internal sealed partial class TextFieldLayout
{
    private MaterialShape? _observedShape;
    private PathGeometry? _shapePath, _indicatorShapePath;
    private (Size Size, FlowDirection Direction, double Density) _shapeKey;
    private CornerRadius ShapeCorners => _observedShape is RoundedCornerShape round
        ? CornerShape.Normalize(CornerShape.Rounded.From(round).Resolve(_containerBounds.Size), _containerBounds.Size)
        : _observedShape is CutCornerShape { IsRectangle: true } ? default : _owner?.CornerRadius ?? default;

    private void ObserveShape()
    {
        if (_observedShape is not null) _observedShape.PropertyChanged -= ShapeChanged;
        _observedShape = _owner is null ? null : MaterialTextField.GetShape(_owner);
        if (_observedShape is not null) _observedShape.PropertyChanged += ShapeChanged;
        _shapePath = _indicatorShapePath = null;
    }

    private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        _shapePath = _indicatorShapePath = null;
        InvalidateDecoration();
    }

    private void UpdateShape()
    {
        if (_observedShape is null || _containerBounds.Width <= 0 || _containerBounds.Height <= 0)
        { _shapePath = _indicatorShapePath = null; return; }
        var key = (_containerBounds.Size, FlowDirection, Scale);
        if (_shapePath is not null && _shapeKey == key) return;
        var source = _observedShape.CreateOutline(_containerBounds.Size, FlowDirection, Scale);
        _shapePath = source.Figures is { Count: > 0 } ? PathGeometry.Parse(source.ToString()) : new PathGeometry();
        _shapePath.FillRule = source.FillRule;
        var matrix = source.Transform?.Value ?? Matrix.Identity;
        // Factories use physical coordinates. Cancel Avalonia's RTL tree mirror on the
        // path, while retaining native logical placement for the editor and all slots.
        if (FlowDirection == FlowDirection.RightToLeft)
            matrix *= new Matrix(-1, 0, 0, 1, _containerBounds.Width, 0);
        _shapePath.Transform = new MatrixTransform(matrix);
        _indicatorShapePath = source.Figures is { Count: > 0 } ? PathGeometry.Parse(source.ToString()) : new PathGeometry();
        _indicatorShapePath.FillRule = FillRule.NonZero;
        _indicatorShapePath.Transform = new MatrixTransform(matrix);
        _shapeKey = key;
    }
}
