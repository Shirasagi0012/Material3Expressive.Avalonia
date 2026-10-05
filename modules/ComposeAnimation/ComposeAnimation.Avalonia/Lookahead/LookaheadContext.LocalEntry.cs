using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ComposeAnimation;

internal sealed partial class LookaheadContext
{
    private LookaheadLayoutBuilder? _candidate;
    private Size _candidateAvailable;
    private LookaheadConstraints? _candidateConstraints;
    private long _candidateRevision = -1;
    private Rect? _committedSlot;
    private bool _candidateCommitted;

    internal LookaheadLayoutResult? LocalMeasurement => _candidate?.ResultFor(Owner);

    /// <summary>Only pure target measurement is performed here; position is supplied by native Arrange.</summary>
    internal Size PrepareLocal(Size available)
        => PrepareLocalCore(available, null);

    internal Size PrepareLocal(LookaheadConstraints constraints)
        => PrepareLocalCore(constraints.MaxSize, constraints);

    private Size PrepareLocalCore(Size available, LookaheadConstraints? constraints)
    {
        VerifyLayoutAllowed();
        if (!IsLocalEntry) throw new InvalidOperationException("This domain has an explicit viewport entry.");
        var scale = LayoutHelper.GetLayoutScale(Owner);
        if (_candidate is not null && _candidateRevision == TargetInputRevision &&
            _candidateAvailable == available && _candidateConstraints == constraints && _scale == scale)
        {
            _checkingDependencies = true;
            try
            {
                if (!HasStaleBoundaryContent() && _candidateRevision == TargetInputRevision)
                    return _candidate.Desired(Owner);
            }
            catch { FailTarget(); throw; }
            finally { _checkingDependencies = false; }
        }

        Unsubscribe();
        Observe(Owner);
        var inputs = CaptureBoundaryInputs();
        var builder = new LookaheadLayoutBuilder(this);
        var revision = TargetInputRevision;
        _building = true;
        _buildInvalidated = false;
        try
        {
            var desired = constraints is { } full
                ? builder.Measure(Owner, full)
                : builder.Measure(Owner, available);
            _candidate = builder;
            _candidateAvailable = available;
            _candidateConstraints = constraints;
            _candidateRevision = revision;
            _candidateCommitted = false;
            _scale = scale;
            _targetDirty = true;
            Snapshot = null;
            TargetInvalidated?.Invoke();
            if (IsBuildStale(inputs) || revision != TargetInputRevision)
            {
                _candidateRevision = -1;
                Owner.InvalidateMeasure();
            }
            return desired;
        }
        catch { _candidate = null; FailTarget(); throw; }
        finally { _building = false; }
    }

    /// <summary>Completes placement and publishes one immutable generation, never a partial rectangle.</summary>
    internal bool CommitLocal(Rect nativeSlot)
    {
        VerifyLayoutAllowed();
        _approachPlacementPending = false;
        if (_candidate is null || _candidateRevision != TargetInputRevision)
        {
            Owner.InvalidateMeasure();
            Owner.InvalidateArrange();
            return false;
        }
        if (_candidateCommitted && _committedSlot == nativeSlot && !_targetDirty) return true;
        var inputs = CaptureBoundaryInputs();
        _building = true;
        _buildInvalidated = false;
        try
        {
            _candidate.Place(Owner, nativeSlot);
            if (IsBuildStale(inputs) || _candidateRevision != TargetInputRevision)
            {
                Owner.InvalidateMeasure();
                Owner.InvalidateArrange();
                return false;
            }
        }
        catch { FailTarget(); throw; }
        finally { _building = false; }

        Snapshot = _candidate.Build(++_version, nativeSlot.Size);
        _committedMeasurements = _candidate;
        _committedMeasurementsSnapshot = Snapshot;
        _committedMeasurementsMembershipEpoch = MembershipEpoch;
        _candidateCommitted = true;
        _committedSlot = nativeSlot;
        _targetDirty = false;
        foreach (var control in _observed)
        {
            if (control is ILookaheadApproach approach &&
                (!control.IsVisible || !Snapshot.Results.ContainsKey(control))) approach.ResetApproach();
            // The entry is already in Arrange. Its native DesiredSize is the prepared target.
            if (control != Owner) control.InvalidateMeasure();
        }
        PublishTarget(Snapshot);
        return true;
    }

    internal bool TryPreparationMeasure(Control control, out LookaheadLayoutResult result)
    {
        result = null!;
        if (IsLocalEntry && !_actualPass && !_candidateCommitted &&
            _candidateRevision == TargetInputRevision && _candidate?.ResultFor(control) is { } prepared)
        {
            result = prepared;
            return true;
        }
        // A placement policy may measure a child but omit it from this final-size selection.
        // Its committed candidate size remains available only to the running actual measure;
        // no target rectangle is published and no bounds animation is initialized.
        if (_actualPass && Snapshot is { } snapshot &&
            ReferenceEquals(snapshot, _committedMeasurementsSnapshot) &&
            MembershipEpoch == _committedMeasurementsMembershipEpoch && Contains(control) &&
            !snapshot.Results.ContainsKey(control) &&
            _committedMeasurements?.ResultFor(control) is { } unplaced)
        {
            result = unplaced;
            return true;
        }
        return false;
    }

    internal void RunActual(Action action)
    {
        var previous = _actualPass;
        _actualPass = true;
        try { action(); }
        finally { _actualPass = previous; }
    }
}
