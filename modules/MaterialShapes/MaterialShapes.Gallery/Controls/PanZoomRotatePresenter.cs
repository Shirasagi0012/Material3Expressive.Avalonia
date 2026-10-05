using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace MaterialShapes.Gallery.Controls;

public sealed class PanZoomRotatePresenter : ContentControl
{
    private Point _lastPoint;
    private bool _isPanning;
    private bool _isRotating;

    private double _scale = 1;
    private double _rotation;
    private Vector _offset;

    static PanZoomRotatePresenter()
    {
        ContentProperty.Changed.AddClassHandler<PanZoomRotatePresenter>((x, args) => x.UpdateTransform());
    }

    public PanZoomRotatePresenter()
    {
        ClipToBounds = true;
    }

    public void Reset()
    {
        _scale = 1;
        _rotation = 0;
        _offset = default;
        UpdateTransform();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _lastPoint = e.GetPosition(this);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            e.Pointer.Capture(this);
        }
        else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            _isRotating = true;
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
        _isRotating = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var current = e.GetPosition(this);
        var delta = current - _lastPoint;
        _lastPoint = current;

        if (_isPanning)
        {
            _offset += delta;
            UpdateTransform();
        }
        else if (_isRotating)
        {
            _rotation += delta.X * 0.5;
            UpdateTransform();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var delta = e.Delta.Y;
        if (Math.Abs(delta) < double.Epsilon)
            return;

        var factor = delta > 0 ? 1.1 : 0.9;
        _scale = Math.Clamp(_scale * factor, 0.1, 20);
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (Content is not Control child)
            return;

        child.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(_scale, _scale),
                new RotateTransform(_rotation),
                new TranslateTransform(_offset.X, _offset.Y)
            }
        };
        child.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
    }
}
