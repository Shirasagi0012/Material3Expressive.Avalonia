using System;
using System.Collections.Generic;
using System.Threading;

namespace Material3Expressive.Controls;

/// <summary>
/// Test-only owner for every <see cref="MotionGroup" /> created while the scope is active.
/// </summary>
/// <remarks>
/// The visual-parity host enters this scope before it constructs a scene. That makes initial,
/// templated, and trigger-created motion groups manual before they can request a host frame. The
/// type is internal and exposed only to the repository's friend test assemblies.
/// </remarks>
internal sealed class MotionTestClockScope : IDisposable
{
    private static readonly AsyncLocal<MotionTestClockScope?> CurrentScope = new();

    private readonly List<MotionGroup> _groups = [];
    private readonly HashSet<MotionGroup> _knownGroups = [];
    private bool _disposed;

    public MotionTestClockScope()
    {
        if (CurrentScope.Value is not null)
            throw new InvalidOperationException("A Material motion test-clock scope is already active.");

        CurrentScope.Value = this;
    }

    internal int GroupCount => _groups.Count;

    internal static void Register(MotionGroup group)
    {
        CurrentScope.Value?.Add(group);
    }

    internal void AdvanceBy(TimeSpan delta)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (delta < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delta), "Animation time cannot move backwards.");

        // A tick can create another motion group. Indexing lets that group consume the same
        // logical frame, matching a frame clock where all registered callbacks see one timestamp.
        for (var index = 0; index < _groups.Count; index++)
            _groups[index].AdvanceBy(delta);
    }

    internal void SettleAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        for (var index = 0; index < _groups.Count; index++)
            _groups[index].SettleAll();
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

    private void Add(MotionGroup group)
    {
        if (!_knownGroups.Add(group))
            return;

        group.UseManualClock();
        _groups.Add(group);
    }
}
