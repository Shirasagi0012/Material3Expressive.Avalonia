using System;

namespace Material3Expressive.Controls;

/// <summary>Disposable subscription to an owner's interaction state and frame stream.</summary>
internal sealed class MaterialInteractionObserverLease : IDisposable
{
    private readonly MaterialInteractionController _controller;
    private readonly IMaterialInteractionObserver _observer;
    private bool _disposed;

    internal MaterialInteractionObserverLease(
        MaterialInteractionController controller,
        IMaterialInteractionObserver observer)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
    }

    internal bool IsDisposed => _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _controller.RemoveObserver(_observer);
    }
}
