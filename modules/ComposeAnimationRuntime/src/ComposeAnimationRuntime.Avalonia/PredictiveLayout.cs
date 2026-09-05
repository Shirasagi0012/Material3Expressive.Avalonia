using Avalonia;
using Avalonia.Controls;

namespace ComposeAnimationRuntime.Avalonia;

public readonly record struct PredictiveLayoutSnapshot(Size DesiredSize, Rect Bounds)
{
    public static PredictiveLayoutSnapshot Capture(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return new PredictiveLayoutSnapshot(control.DesiredSize, control.Bounds);
    }
}

public sealed class PredictiveLayoutController : IDisposable
{
    private readonly Control _control;
    private bool _disposed;
    private PredictiveLayoutSnapshot _last;
    private bool _hasLast;

    public PredictiveLayoutController(Control control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _control.LayoutUpdated += OnLayoutUpdated;
    }

    public PredictiveLayoutSnapshot Current => PredictiveLayoutSnapshot.Capture(_control);
    public event EventHandler<PredictiveLayoutSnapshot>? SnapshotChanged;

    public PredictiveLayoutSnapshot Capture()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var snapshot = Current;
        _last = snapshot;
        _hasLast = true;
        SnapshotChanged?.Invoke(this, snapshot);
        return snapshot;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _control.LayoutUpdated -= OnLayoutUpdated;
        SnapshotChanged = null;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_disposed) return;
        var snapshot = Current;
        if (_hasLast && snapshot == _last) return;
        _last = snapshot;
        _hasLast = true;
        SnapshotChanged?.Invoke(this, snapshot);
    }
}
