using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3Expressive.Controls;

/// <summary>Live geometry lease held by a short-lived interaction presenter.</summary>
internal sealed class MaterialInteractionPresentationLease : IDisposable
{
    private readonly MaterialInteractionController _controller;
    private readonly Control _presenter;
    private bool _disposed;

    internal MaterialInteractionPresentationLease(
        MaterialInteractionController controller,
        Control presenter)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        Geometry = controller.DefaultGeometry;
    }

    internal Control Presenter => _presenter;
    internal bool IsDisposed => _disposed;
    internal MaterialInteractionGeometry Geometry { get; private set; }
    internal event Action? Disposed;

    internal void UpdateGeometry(in MaterialInteractionGeometry geometry)
    {
        if (_disposed)
            return;

        Geometry = geometry;
        _controller.SetPresentationGeometry(this, geometry);
    }

    internal void DrawLayers(
        DrawingContext context,
        IBrush? brush,
        CornerRadius cornerRadius)
    {
        if (!_disposed)
            _controller.DrawLayers(context, brush, cornerRadius);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _controller.ClearPresentation(this);
        Disposed?.Invoke();
        Disposed = null;
    }
}
