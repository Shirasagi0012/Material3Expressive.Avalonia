using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System.Runtime.CompilerServices;

namespace ComposeAnimation;

/// <summary>Prepares an opt-in entry before the full-constraint bridge captures its domain key.</summary>
internal interface ILookaheadEntryParticipant
{
    void EnsureLookaheadEntry();
}

/// <summary>Native-layout entry for a cooperative node when no containing domain owns it.</summary>
internal sealed class LookaheadEntry
{
    private static readonly ConditionalWeakTable<Control, LookaheadEntry> Entries = new();
    private readonly Control owner;
    private readonly bool _createsLocalDomain;
    private LookaheadContext? _local;
    private Size _available;
    private LookaheadConstraints? _requested;
    private bool _approachMeasurePending;
    private long _approachVersion = -1;
    private Size _approachDesired;
    private LookaheadConstraints? _approachEffective;

    internal LookaheadEntry(Control owner, bool createsLocalDomain = true)
    {
        this.owner = owner;
        _createsLocalDomain = createsLocalDomain;
        Entries.Add(owner, this);
    }

    internal static void Ensure(Control control)
    {
        if (control is ILookaheadEntryParticipant participant)
        {
            participant.EnsureLookaheadEntry();
            return;
        }
        if (!Entries.TryGetValue(control, out _) && control is ILookaheadLayoutPolicy)
            LookaheadLayout.Register(control);
        if (Entries.TryGetValue(control, out var entry)) entry.EnsureCore();
    }

    internal Size MeasureCore(Size available, Func<Size> nativeMeasure)
    {
        EnsureCore();
        if (_local is not { } local)
            return LookaheadMeasure.TryMeasureCore(owner, available, out var desired) ? desired : nativeMeasure();
        if (!owner.IsVisible) return default;

        _available = available;
        _requested = LookaheadMeasure.PendingConstraints(owner, available);
        _approachMeasurePending = true;
        var targetDesired = _requested is { } full ? local.PrepareLocal(full) : local.PrepareLocal(available);
        if (_requested is not null) LookaheadMeasure.AcceptPrepared(owner, local.LocalMeasurement);
        return targetDesired;
    }

    internal bool TryArrangeCore(Rect slot, Func<Size, Size> arrange, out Rect bounds)
    {
        if (_local is not { } local)
            return LookaheadMeasure.TryArrangeCore(owner, slot, arrange, out bounds);

        bounds = owner.Bounds;
        if (!owner.IsVisible || !local.CommitLocal(slot)) return true;
        var target = local.Snapshot!.Results[owner].Bounds;
        if (_approachMeasurePending || _approachVersion != local.Snapshot.Version)
        {
            Size desired = default;
            LookaheadConstraints? effective = null;
            local.RunActual(() =>
            {
                if (_requested is not null)
                {
                    if (!LookaheadMeasure.TryMeasureCore(owner, _available, out desired))
                        throw new InvalidOperationException("The local full-constraint request expired before placement.");
                    effective = LookaheadMeasure.ActualConstraints(owner);
                }
                else
                {
                    var margin = LookaheadMeasure.Margin(owner);
                    var incoming = LookaheadConstraints.AtMost(_available);
                    effective = incoming.Deflate(margin).ApplyTo(owner);
                    var measured = ((ILookaheadConstrainedLayoutNode)owner).MeasureApproach(effective.Value);
                    desired = LookaheadMeasure.Desired(owner, measured, effective.Value, incoming, margin);
                }
            });
            _approachDesired = desired;
            _approachEffective = effective;
            _approachVersion = local.Snapshot.Version;
            _approachMeasurePending = false;
        }

        var actualBounds = LookaheadLayoutBuilder.ResolveBounds(owner, slot, _approachDesired, default, _approachEffective);
        if (owner is ILookaheadPlacementApproach { AppliesPositionApproach: true } approach)
            actualBounds = new Rect(approach.ApproachPosition(local, target.TopLeft), actualBounds.Size);
        local.RunActual(() => LookaheadPlacement.WithOrigin(owner, local, actualBounds.TopLeft, () =>
        {
            var actual = arrange(actualBounds.Size);
            if (actual != actualBounds.Size)
                throw new InvalidOperationException("Lookahead ArrangeOverride must return its supplied finalSize.");
        }));
        bounds = actualBounds;
        return true;
    }

    internal void VisualParentChanged()
    {
        if (_local is null) return;
        Release();
        if (owner is ILookaheadApproach approach) approach.ResetApproach();
    }

    internal void Detach()
    {
        LookaheadMeasure.Forget(owner);
        Release();
    }

    internal void Dispose()
    {
        Entries.Remove(owner);
        Detach();
        if (owner is ILookaheadApproach approach) approach.ResetApproach();
    }

    internal void EnsureCore()
    {
        if (!_createsLocalDomain) return;
        if (_local is not null)
        {
            var parentContext = owner.GetVisualParent() is { } parent
                ? LookaheadEnvironment.GetContext(parent) : null;
            if (parentContext is null || parentContext == _local || !parentContext.Contains(owner)) return;
            Release();
            if (owner is ILookaheadApproach approach) approach.ResetApproach();
        }
        var inherited = LookaheadScope.FindContext(owner);
        if (inherited?.Contains(owner) == true) return;
        _local = new LookaheadContext(owner, () => owner, localEntry: true);
        LookaheadEnvironment.SetContext(owner, _local);
        _local.Attach();
    }

    private void Release()
    {
        if (_local is not { } local) return;
        local.Detach();
        if (LookaheadEnvironment.GetContext(owner) == local)
            owner.ClearValue(LookaheadEnvironment.ContextProperty);
        _local = null;
        _requested = null;
        _approachMeasurePending = false;
        _approachVersion = -1;
    }
}
