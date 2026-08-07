using System;
using System.Collections.Generic;
using System.Threading;

namespace Material3Expressive.Controls;

/// <summary>
/// Test-only owner for every <see cref="MaterialMotionCoordinator" /> created while the scope is active.
/// </summary>
/// <remarks>
/// The visual-parity host enters this scope before it constructs a scene. That makes initial,
/// templated, and trigger-created coordinators manual before they can request a host frame. The
/// type is internal and exposed only to the repository's friend test assemblies.
/// </remarks>
internal sealed class MotionTestClockScope : IDisposable
{
    private static readonly AsyncLocal<MotionTestClockScope?> CurrentScope = new();

    private readonly List<MaterialMotionCoordinator> _coordinators = [];
    private readonly HashSet<MaterialMotionCoordinator> _knownCoordinators = [];
    private bool _disposed;

    public MotionTestClockScope()
    {
        if (CurrentScope.Value is not null)
            throw new InvalidOperationException("A Material motion test-clock scope is already active.");

        CurrentScope.Value = this;
    }

    internal int CoordinatorCount => _coordinators.Count;

    internal static void Register(MaterialMotionCoordinator coordinator)
    {
        CurrentScope.Value?.Add(coordinator);
    }

    internal void AdvanceBy(TimeSpan delta)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta), "Animation time cannot move backwards.");

        // A tick can create another coordinator. Indexing lets that coordinator consume the same
        // logical frame, matching a frame clock where all registered callbacks see one timestamp.
        for (var index = 0; index < _coordinators.Count; index++)
            _coordinators[index].AdvanceBy(delta);
    }

    internal void SettleAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        for (var index = 0; index < _coordinators.Count; index++)
            _coordinators[index].SettleAll();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (!ReferenceEquals(CurrentScope.Value, this))
            throw new InvalidOperationException("Material motion test-clock scopes must be disposed in activation order.");

        CurrentScope.Value = null;
        _disposed = true;
    }

    private void Add(MaterialMotionCoordinator coordinator)
    {
        if (!_knownCoordinators.Add(coordinator))
            return;

        coordinator.UseManualClock();
        _coordinators.Add(coordinator);
    }
}
