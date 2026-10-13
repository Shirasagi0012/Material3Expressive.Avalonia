using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>Stable domain identity and target coordinator, independent of its layout entry control.</summary>
internal sealed partial class LookaheadContext
{
    private readonly Func<Control?> _content;
    internal LookaheadContext(Control owner, Func<Control?> content, bool localEntry = false)
    {
        Owner = owner;
        _content = content;
        IsLocalEntry = localEntry;
    }

    internal Control Owner { get; }
    internal bool IsLocalEntry { get; }
    internal Control CoordinateReference => IsLocalEntry ? Owner.GetVisualParent() as Control ?? Owner : Owner;
    internal bool Contains(Control control) => control == Owner || _visited.Contains(control);
    internal long TargetInputRevision { get; private set; }
    internal long MembershipEpoch { get; private set; }
    internal bool IsTargetDirty => _targetDirty;
    private Control? Child => _content();

    private readonly List<Control> _observed = [];
    private readonly List<INotifyCollectionChanged> _collections = [];
    private readonly HashSet<Control> _visited = [];
    private bool _targetDirty = true;
    private long _version;
    private bool _attached;
    private double _scale;
    private bool _movingContent;
    private bool _moveLayoutAttempted;
    private bool _approachPlacementPending;
    private bool _actualPass;
    private bool _building;
    private bool _checkingDependencies;
    private bool _buildInvalidated;
    private bool _publishingTarget;
    private LookaheadLayoutBuilder? _committedMeasurements;
    private LookaheadLayoutSnapshot? _committedMeasurementsSnapshot;
    private long _committedMeasurementsMembershipEpoch = -1;
    // Deterministic work counter for regression tests; no per-frame public diagnostics API.
    internal long ApproachInvalidationPassCount { get; private set; }

    /// <summary>Last complete target layout; null before layout or after target invalidation.</summary>
    public LookaheadLayoutSnapshot? Snapshot { get; private set; }
    /// <summary>Raised after a complete target measure/placement and before actual child layout.</summary>
    public event EventHandler? TargetLayoutUpdated;
    internal event Action? TargetInvalidated;

    /// <summary>
    /// Experimental synchronous tree edit preserving initialized AnimatedBounds instances in content's
    /// visual subtree. Update the actual parent collections inside move; content and retained nodes must
    /// remain in this attached domain with the same coordinate reference. Do not await, pump frames/layout,
    /// or nest MoveContent in this domain. Failures release retained motion; tree edits are not rolled back.
    /// Ordinary removal (outside MoveContent) releases animations at the next scope drain, or resumes them
    /// there when the control is re-attached in the same pass with an unchanged motion-frame basis.
    /// This is not keyed content or exit retention.
    /// </summary>
    public void MoveContent(Control content, Action move)
    {
        Owner.VerifyAccess();
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(move);
        if (_movingContent) throw new InvalidOperationException("MoveContent cannot be nested in the same domain.");
        if (!_attached || content == Owner || LookaheadScope.FindParticipatingContext(content) != this)
            throw new InvalidOperationException("MoveContent requires content in this attached domain.");
        if (Attribute.IsDefined(move.Method, typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute)))
            throw new ArgumentException("MoveContent requires a synchronous callback, not async void.", nameof(move));
        var topLevel = TopLevel.GetTopLevel(Owner);
        var states = new List<ILookaheadMoveState>();
        var committed = false;
        // No scope drain while the move is open (its basis is uncommitted); re-armed on dispose.
        var drainGate = AnimationScope.BeginMove(Owner);
        _movingContent = true;
        _moveLayoutAttempted = false;
        try
        {
            foreach (var node in new Visual[] { content }.Concat(content.GetVisualDescendants()).OfType<ILookaheadMoveParticipant>())
                if (node.CaptureMove(this) is { } state) states.Add(state);
            move();
            if (_moveLayoutAttempted || !_attached || TopLevel.GetTopLevel(Owner) != topLevel ||
                LookaheadScope.FindParticipatingContext(content) != this ||
                states.Any(state => !state.CanCommit()))
                throw new InvalidOperationException("MoveContent must finish in the same domain/reference without running layout or animation frames. Tree edits are not rolled back.");
            foreach (var state in states) state.Commit();
            committed = true;
        }
        finally
        {
            if (!committed)
                foreach (var state in states) state.Cancel();
            _movingContent = false;
            drainGate?.Dispose();
            InvalidateTargetLayout();
            InvalidateApproachPlacement();
        }
    }

    /// <summary>Invalidates the target after external layout-policy inputs change.</summary>
    public void InvalidateTargetLayout()
    {
        Owner.VerifyAccess();
        TargetInputRevision++;
        _targetDirty = true;
        if (_building) _buildInvalidated = true;
        // Mid-pass invalidation (e.g. template realization changing inherited inputs) must not
        // pull the snapshot from under the running approach pass; the scheduled repass rebuilds it.
        else if (!_actualPass)
        {
            Snapshot = null;
            _committedMeasurements = null;
            _committedMeasurementsSnapshot = null;
            _committedMeasurementsMembershipEpoch = -1;
        }
        TargetInvalidated?.Invoke();
        Owner.InvalidateMeasure();
    }

    internal void Attach()
    {
        _attached = true;
        InvalidateTargetLayout();
    }

    internal void Detach()
    {
        _attached = false;
        Unsubscribe();
        InvalidateTargetLayout();
        // Detach/migration ends membership even when triggered from a running actual callback.
        // The pass must not retain unpublished candidate measurements after ownership is gone.
        _committedMeasurements = null;
        _committedMeasurementsSnapshot = null;
        _committedMeasurementsMembershipEpoch = -1;
    }

    internal Size MeasureLegacy(Size availableSize)
    {
        EnsureTarget(availableSize);
        if (Snapshot is null) return availableSize;
        _actualPass = true;
        try
        {
            if (Child is { } child) LookaheadMeasure.Measure(child, availableSize);
        }
        finally { _actualPass = false; }
        return availableSize;
    }

    internal Size ArrangeLegacy(Size finalSize)
    {
        // Clear before callbacks: an invalidation during this pass must request another pass.
        _approachPlacementPending = false;
        EnsureTarget(finalSize);
        if (Snapshot is not null)
        {
            _actualPass = true;
            try
            {
                if (Child is { } child) LookaheadPlacement.ArrangeChild(Owner, child, new Rect(finalSize));
            }
            finally { _actualPass = false; }
        }
        return finalSize;
    }

    private void EnsureTarget(Size viewport)
    {
        VerifyLayoutAllowed();
        if (!double.IsFinite(viewport.Width) || !double.IsFinite(viewport.Height))
            throw new NotSupportedException("LookaheadRoot requires a finite viewport. Set Width/Height on axes unconstrained by its parent.");
        var scale = LayoutHelper.GetLayoutScale(Owner);
        if (!_targetDirty && Snapshot?.Viewport == viewport && _scale == scale)
        {
            _checkingDependencies = true;
            try
            {
                if (!HasStaleBoundaryContent() && !_targetDirty) return;
            }
            catch
            {
                FailTarget();
                throw;
            }
            finally { _checkingDependencies = false; }
        }
        Unsubscribe();
        if (Child is { } child) Observe(child);
        var inputs = CaptureBoundaryInputs();
        var builder = new LookaheadLayoutBuilder(this);
        _building = true;
        _buildInvalidated = false;
        try
        {
            if (Child is { } content)
            {
                builder.Measure(content, viewport);
                builder.Place(content, new Rect(viewport));
            }
            if (IsBuildStale(inputs))
            {
                _targetDirty = true;
                Owner.InvalidateMeasure();
                Owner.InvalidateArrange();
                return;
            }
        }
        catch
        {
            // A failed target policy must not leave previous animations running against a
            // missing/partial destination, or Avalonia's optimistic measure cache marked valid.
            FailTarget();
            throw;
        }
        finally { _building = false; }
        Snapshot = builder.Build(++_version, viewport);
        _committedMeasurements = builder;
        _committedMeasurementsSnapshot = Snapshot;
        _committedMeasurementsMembershipEpoch = MembershipEpoch;
        _scale = scale;
        _targetDirty = false;
        // New targets/constraints must reach nested approach nodes even when their outer
        // ordinary measure constraints did not change. Animation-only invalidation skips this.
        foreach (var control in _observed)
        {
            if (control is ILookaheadApproach approach &&
                (!control.IsVisible || !Snapshot.Results.ContainsKey(control))) approach.ResetApproach();
            control.InvalidateMeasure();
        }
        PublishTarget(Snapshot);
    }

    internal bool IsBuildingTarget => _building;

    private void PublishTarget(LookaheadLayoutSnapshot snapshot)
    {
        if (_publishingTarget)
            throw new InvalidOperationException("Target-layout notification cannot be re-entered.");
        _publishingTarget = true;
        var previousActual = _actualPass;
        _actualPass = true;
        try
        {
            TargetLayoutUpdated?.Invoke(this, EventArgs.Empty);
            foreach (var participant in snapshot.Results.Keys)
            {
                if (participant is ILookaheadLayoutNode)
                    participant.RaiseEvent(new LookaheadTargetLayoutEventArgs(snapshot));
            }
        }
        catch (Exception error)
        {
            // A notification is user code. Its failure must not leave a clean generation or
            // animated participants waiting for frames against a failed layout operation.
            try { FailTarget(); }
            catch (Exception cleanup) { error.Data["LookaheadCleanupException"] = cleanup; }
            throw;
        }
        finally
        {
            _actualPass = previousActual;
            _publishingTarget = false;
        }
    }

    private void FailTarget()
    {
        _targetDirty = true;
        Snapshot = null;
        _committedMeasurements = null;
        _committedMeasurementsSnapshot = null;
        _committedMeasurementsMembershipEpoch = -1;
        _candidate = null;
        _candidateRevision = -1;
        _candidateCommitted = false;
        List<Exception>? failures = null;
        // Cleanup cannot stop at one throwing property listener or session callback: another
        // participant may still own a frame subscription or a retained shared presentation.
        foreach (var handler in TargetInvalidated?.GetInvocationList() ?? [])
        {
            try { ((Action)handler)(); }
            catch (Exception error) { (failures ??= []).Add(error); }
        }
        foreach (var approach in _observed.OfType<ILookaheadApproach>().ToArray())
        {
            try { approach.ResetApproach(); }
            catch (Exception error) { (failures ??= []).Add(error); }
        }
        Owner.InvalidateMeasure();
        Owner.InvalidateArrange();
        if (failures is not null) throw new AggregateException("Lookahead target cleanup failed.", failures);
    }

    /// <summary>
    /// Rebuild the destination without dropping the published snapshot. Used when native
    /// boundary content changed during an actual pass that still needs the last target.
    /// </summary>
    internal void RequestTargetRebuild()
    {
        Owner.VerifyAccess();
        TargetInputRevision++;
        _targetDirty = true;
        if (_building) _buildInvalidated = true;
        TargetInvalidated?.Invoke();
        Owner.InvalidateMeasure();
    }

    internal void VerifyLayoutAllowed()
    {
        if (_movingContent)
        {
            _moveLayoutAttempted = true;
            throw new InvalidOperationException("Layout cannot run during MoveContent; finish the synchronous tree edit first.");
        }
        if (_building || _checkingDependencies || _publishingTarget)
            throw new InvalidOperationException("A lookahead domain cannot re-enter layout during target measurement, dependency checks, or target notification.");
    }

    private void Observe(Control control)
    {
        if (!_visited.Add(control))
            throw new InvalidOperationException("LookaheadChildren must form a tree: a control was registered more than once.");
        _observed.Add(control);
        if (_attached) control.PropertyChanged += InputChanged;
        if (_attached && control is ILookaheadMotionFrameProvider motion)
            motion.MotionFrameChanged += MotionFrameChanged;
        if (control is ILookaheadLayoutNode node)
        {
            if (_attached && control is ILookaheadTargetInvalidationSource source)
                source.TargetMeasureInvalidated += TargetInputChanged;
            if (_attached && node.LookaheadChildrenChanged is { } collection)
            {
                collection.CollectionChanged += ChildrenChanged;
                _collections.Add(collection);
            }
            foreach (var child in node.LookaheadChildren) Observe(child);
        }
    }

    private bool HasStaleBoundaryContent()
    {
        var snapshot = Snapshot;
        if (snapshot is null) return false;
        foreach (var boundary in _observed.OfType<LookaheadBoundary>())
            if (boundary.IsNativeContentStale(snapshot)) return true;
        return false;
    }

    private (LookaheadBoundary Boundary, long Revision, Control? Child)[] CaptureBoundaryInputs()
    {
        var inputs = new (LookaheadBoundary, long, Control?)[_observed.OfType<LookaheadBoundary>().Count()];
        var index = 0;
        foreach (var boundary in _observed.OfType<LookaheadBoundary>())
            inputs[index++] = (boundary, boundary.InputRevision, boundary.Child);
        return inputs;
    }

    private bool IsBuildStale((LookaheadBoundary Boundary, long Revision, Control? Child)[] start)
    {
        if (_buildInvalidated) return true;
        var current = _observed.OfType<LookaheadBoundary>().ToList();
        if (current.Count != start.Length) return true;
        for (var i = 0; i < start.Length; i++)
        {
            var boundary = current[i];
            if (!ReferenceEquals(start[i].Boundary, boundary) ||
                start[i].Revision != boundary.InputRevision ||
                !ReferenceEquals(start[i].Child, boundary.Child))
                return true;
        }
        return false;
    }

    private void InputChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        var p = e.Property;
        if (p == Control.WidthProperty || p == Control.HeightProperty || p == Control.MinWidthProperty || p == Control.MaxWidthProperty ||
            p == Control.MinHeightProperty || p == Control.MaxHeightProperty || p == Control.MarginProperty || p == Control.IsVisibleProperty ||
            p == Control.HorizontalAlignmentProperty || p == Control.VerticalAlignmentProperty || p == Control.UseLayoutRoundingProperty ||
            p == Control.FlowDirectionProperty || p == Decorator.ChildProperty || p == ContentControl.ContentProperty || p == ContentControl.ContentTemplateProperty ||
            p == Avalonia.Controls.Primitives.TemplatedControl.TemplateProperty ||
            p == ContentControl.PaddingProperty || p == Decorator.PaddingProperty || p == ContentControl.BorderThicknessProperty ||
            p == ContentControl.HorizontalContentAlignmentProperty || p == ContentControl.VerticalContentAlignmentProperty ||
            sender is ILookaheadLayoutNode node && node.LookaheadProperties.Contains(p))
            InvalidateTargetLayout();
    }

    private void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateTargetLayout();

    private void TargetInputChanged(object? sender, EventArgs e) => InvalidateTargetLayout();

    private void MotionFrameChanged(object? sender, EventArgs e)
    {
        Owner.VerifyAccess();
        foreach (var observer in _observed.OfType<ILookaheadMotionFrameObserver>()) observer.InvalidateMotionFrame();
        InvalidateApproachPlacement();
    }

    /// <summary>
    /// Invalidates actual placement throughout this scope without invalidating target layout.
    /// Use when an approach sample changes descendant origins, even with unchanged local slots.
    /// Repeated requests before the next entry arrange share one traversal. Requests during
    /// placement still invalidate the following pass; hosts must continue arranging descendants.
    /// </summary>
    public void InvalidateApproachPlacement()
    {
        Owner.VerifyAccess();
        if (_approachPlacementPending) return;
        _approachPlacementPending = true;
        ApproachInvalidationPassCount++;
        Owner.InvalidateArrange();
        foreach (var control in _observed) control.InvalidateArrange();
    }

    private void Unsubscribe()
    {
        MembershipEpoch++;
        _approachPlacementPending = false;
        foreach (var control in _observed)
        {
            control.PropertyChanged -= InputChanged;
            if (control is ILookaheadMotionFrameProvider motion) motion.MotionFrameChanged -= MotionFrameChanged;
            if (control is ILookaheadTargetInvalidationSource source)
                source.TargetMeasureInvalidated -= TargetInputChanged;
        }
        foreach (var collection in _collections) collection.CollectionChanged -= ChildrenChanged;
        _observed.Clear();
        _visited.Clear();
        _collections.Clear();
    }
}
