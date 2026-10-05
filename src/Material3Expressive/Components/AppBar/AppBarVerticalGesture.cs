using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Platform;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

// Native pointer recognition/capture, with velocity kept until the nested fling protocol completes.
// Unlike ScrollGestureRecognizer, recognition does not stop at the content's scroll boundaries.
internal sealed class AppBarVerticalGesture(
    Func<bool> enabled,
    Action pressed,
    Func<float, int, Vector> scroll,
    Action<float, int, bool> stopped,
    bool directDrag = false) : GestureRecognizer
{
    internal Func<float, int, Vector> Scroll { get; set; } = scroll;

    private IPointer? _pointer;
    private Point _start, _previous;
    private bool _dragging;
    private int _id;
    private readonly VelocityTracker _velocity = new();

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (!enabled() || !directDrag && e.Pointer.Type is not (PointerType.Touch or PointerType.Pen))
            return;

        Cancel();
        pressed();
        _pointer = e.Pointer;
        _id = ScrollGestureEventArgs.GetNextFreeId();
        _start = _previous = e.GetPosition(TopLevel.GetTopLevel((Visual)Target!));
        _velocity.Reset();
        _velocity.Add(e.Timestamp, (float)_start.Y);
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        if (_pointer != e.Pointer)
            return;

        var point = e.GetPosition(TopLevel.GetTopLevel((Visual)Target!));
        _velocity.Add(e.Timestamp, (float)point.Y);
        if (!_dragging)
        {
            var distance = directDrag
                ? e.Pointer.Type == PointerType.Mouse ? .125 : 18
                : (Application.Current?.PlatformSettings?.GetTapSize(e.Pointer.Type).Height ?? 10) / 2;
            var delta = point - _start;
            if (Math.Abs(delta.Y) <= distance || Math.Abs(delta.Y) < Math.Abs(delta.X))
                return;

            _dragging = true;
            _previous = new Point(_previous.X, _previous.Y + Math.Sign(delta.Y) * distance);
            Capture(e.Pointer);
        }

        Scroll((float)(point.Y - _previous.Y), _id);
        _previous = point;
        e.Handled = true;
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer)
            return;

        _pointer = null;
        if (!_dragging)
            return;

        _dragging = false;
        e.Handled = true;
        stopped(_velocity.Calculate(e.Timestamp), _id, false);
    }

    protected override void PointerCaptureLost(IPointer pointer)
    {
        if (_pointer == pointer)
            Cancel();
    }

    internal void Cancel()
    {
        _pointer = null;
        if (_dragging)
        {
            _dragging = false;
            stopped(0, _id, true);
        }

        _velocity.Reset();
    }
}
