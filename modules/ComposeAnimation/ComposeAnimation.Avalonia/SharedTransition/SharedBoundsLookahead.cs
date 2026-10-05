using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace ComposeAnimation;

/// <summary>Scale destination content, or remeasure live content in a cooperative lookahead domain.</summary>
public enum SharedBoundsResizeMode { ScaleToBounds, RemeasureToBounds }

public partial class SharedBounds : ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost, ILookaheadApproach,
    ILookaheadEntryParticipant
{
    public static readonly StyledProperty<SharedBoundsResizeMode> ResizeModeProperty =
        AvaloniaProperty.Register<SharedBounds, SharedBoundsResizeMode>(nameof(ResizeMode),
            validate: mode => Enum.IsDefined(mode));
    /// <summary>RemeasureToBounds uses the enclosing lookahead domain, or creates a local domain at
    /// this endpoint. The parent retains the endpoint's target content size while the live subtree flies.</summary>
    public SharedBoundsResizeMode ResizeMode { get => GetValue(ResizeModeProperty); set => SetValue(ResizeModeProperty, value); }
    private SharedTransitionMotion? _flight;
    private Rect _flightBounds;
    private LookaheadContext? _flightContext;
    private long _flightSnapshotVersion = -1;
    private long _flightGeneration = -1;
    private sealed record TargetData(LookaheadConstraints Constraints);
    private LookaheadContext? Context => LookaheadScope.FindParticipatingContext(this);
    internal bool Remeasures => ResizeMode == SharedBoundsResizeMode.RemeasureToBounds;
    internal bool IsTargetLayoutPending => Remeasures && Context?.IsTargetDirty == true;
    internal Rect? FlightBounds => _flight is null ? null : _flightBounds;

    static SharedBounds() => AffectsMeasure<SharedBounds>(ResizeModeProperty);

    void ILookaheadEntryParticipant.EnsureLookaheadEntry()
    {
        if (!Remeasures) return;
        (_lookaheadEntry ??= new LookaheadEntry(this)).EnsureCore();
    }

    private void ValidateLookahead()
    {
        ValidateCoordinateChain();
        if (!ReferenceEquals(Template, DefaultSharedTemplate) || ContentTemplate is not null ||
            Content is not Control or not ILookaheadConstrainedLayoutNode || Padding != default || BorderThickness != default ||
            HorizontalContentAlignment != HorizontalAlignment.Stretch || VerticalContentAlignment != VerticalAlignment.Stretch)
            throw new NotSupportedException("SharedBounds lookahead requires its default template, direct full-constraint Control content, " +
                "stretch content alignment, and no ContentTemplate/Padding/BorderThickness. Put layout decoration in cooperative content.");
    }

    private void ValidateCoordinateChain()
    {
        for (Visual? visual = this; visual is not null && visual != Context?.CoordinateReference; visual = visual.GetVisualParent())
            if (visual.HasMirrorTransform || visual.RenderTransform?.Value is { } transform && transform != Matrix.Identity)
                throw new NotSupportedException("SharedBounds lookahead does not support render transforms or mirroring between the endpoint and its Context.");
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Content is Control child ? [child] : [];
    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => MeasureTarget(builder, LookaheadConstraints.AtMost(available).ApplyTo(this));
    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => MeasureTarget(builder, constraints);
    private Size MeasureTarget(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
    {
        ValidateLookahead();
        builder.SetLayoutData(this, new TargetData(constraints));
        return constraints.Constrain(builder.Measure((Control)Content!, constraints));
    }
    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        => builder.Place((Control)Content!, bounds);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Context is null)
        {
            return base.MeasureOverride(availableSize);
        }
        return MeasureApproach(LookaheadConstraints.AtMost(availableSize).ApplyTo(this));
    }
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints) => MeasureApproach(constraints);
    private Size MeasureApproach(LookaheadConstraints constraints)
    {
        // A full-constraint request can also reach ScaleToBounds outside a lookahead domain.
        // Keep the ordinary ContentControl presentation there.
        if (Context is null) return MeasureOverride(constraints.MaxSize);
        try
        {
            ValidateLookahead();
            var target = LookaheadRoot.TargetFor(this) ?? throw new InvalidOperationException("SharedBounds needs a complete target layout.");
            var data = (TargetData)target.LayoutData!;
            ApplyTemplate();
            var requested = _flight is null ? data.Constraints : LookaheadConstraints.Fixed(_flightBounds.Size);
            LookaheadMeasure.Measure(Suppress!, requested);
            // ContentSize placeholder: target geometry is never fed from the in-flight measure.
            return constraints.Constrain(target.MeasuredSize);
        }
        catch (Exception error) { FailFlight(error); throw; }
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Context is not { } root) return base.ArrangeOverride(finalSize);
        try
        {
            ValidateCoordinateChain();
            var slot = new Rect(finalSize);
            if (_flight is not null)
            {
                var rootOrigin = SharedLayoutBounds.LookaheadTranslation(root.CoordinateReference, SharedHost!)
                    ?? throw new InvalidOperationException("SharedBounds lost its host coordinate space.");
                var origin = LookaheadPlacement.RootPosition(this);
                slot = new Rect(_flightBounds.TopLeft - new Vector(rootOrigin.X + origin.X, rootOrigin.Y + origin.Y), _flightBounds.Size);
            }
            LookaheadPlacement.ArrangeChild(this, Suppress!, slot);
            return finalSize;
        }
        catch (Exception error) { FailFlight(error); throw; }
    }
    protected override Size MeasureCore(Size availableSize)
    {
        if (Remeasures && Context is null or { IsLocalEntry: true })
            return (_lookaheadEntry ??= new LookaheadEntry(this))
                .MeasureCore(availableSize, () => base.MeasureCore(availableSize));
        return LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired)
            ? desired : base.MeasureCore(availableSize);
    }
    protected override void ArrangeCore(Rect finalRect)
    {
        if (Remeasures && Context is { IsLocalEntry: true } &&
            _lookaheadEntry!.TryArrangeCore(finalRect, ArrangeOverride, out var localBounds)) Bounds = localBounds;
        else if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _lookaheadEntry?.Dispose();
        _lookaheadEntry = null;
        LookaheadMeasure.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty)
            _lookaheadEntry?.VisualParentChanged();
        if (change.Property == ResizeModeProperty)
        {
            _flight?.Session.Cancel(SharedSessionEndReason.Cancelled);
            if (!Remeasures)
            {
                _lookaheadEntry?.Dispose();
                _lookaheadEntry = null;
            }
        }
    }
    internal override Rect? ReadLayoutBounds()
    {
        if (Context is not { } root) return Remeasures ? null : base.ReadLayoutBounds();
        if (SharedHost is null || root.IsTargetDirty ||
            !root.Owner.IsMeasureValid || !root.Owner.IsArrangeValid ||
            LookaheadRoot.TargetFor(this) is not { } target) return null;
        ValidateCoordinateChain();
        var origin = SharedLayoutBounds.LookaheadTranslation(root.CoordinateReference, SharedHost);
        if (origin is null) return null;
        var position = LookaheadCoordinates.TargetPosition(this);
        return SharedLayoutBounds.Valid(new Rect(position + new Vector(origin.Value.X, origin.Value.Y), target.Bounds.Size));
    }
    internal void SetFlight(SharedTransitionMotion motion, Rect bounds)
    {
        if (!Remeasures) return;
        var context = Context;
        var snapshot = context?.Snapshot;
        if (context is null || !context.Contains(this) || SharedHost is null ||
            TopLevel.GetTopLevel(this) is not { } top || top != TopLevel.GetTopLevel(SharedHost))
            throw new InvalidOperationException("RemeasureToBounds requires a complete cooperative target.");
        if (snapshot is null || !snapshot.Results.ContainsKey(this))
        {
            // A target edit may invalidate the published snapshot between animation sampling and
            // the next layout pass. The existing flight can keep drawing its last committed target;
            // a new flight or domain must wait for a complete target.
            if (_flight != motion || !ReferenceEquals(context, _flightContext))
                throw new InvalidOperationException("RemeasureToBounds requires a complete cooperative target.");
        }
        var normalized = new Rect(bounds.TopLeft, new Size(Math.Max(0, bounds.Width), Math.Max(0, bounds.Height)));
        if (!double.IsFinite(normalized.X) || !double.IsFinite(normalized.Y) ||
            !double.IsFinite(normalized.Width) || !double.IsFinite(normalized.Height) ||
            !double.IsFinite(normalized.Right) || !double.IsFinite(normalized.Bottom))
            throw new InvalidOperationException("Shared bounds must be finite.");
        _flightContext = context;
        if (snapshot is not null) _flightSnapshotVersion = snapshot.Version;
        _flightGeneration = motion.Session.Generation;
        if (_flight == motion && _flightBounds == normalized) return;
        var sizeChanged = _flight is null || _flightBounds.Size != normalized.Size;
        _flight = motion; _flightBounds = normalized;
        if (sizeChanged) InvalidateMeasure(); else InvalidateArrange();
        context.InvalidateApproachPlacement();
    }
    internal void ClearFlight(SharedTransitionMotion motion)
    {
        if (_flight != motion) return;
        _flight = null;
        _flightContext = null;
        _flightSnapshotVersion = -1;
        _flightGeneration = -1;
        InvalidateMeasure();
        Context?.InvalidateApproachPlacement();
    }
    internal bool IsFlightLayoutReady(SharedTransitionMotion motion, SharedTransitionHandoff? handoff = null)
    {
        if (!Remeasures) return true;
        var context = Context;
        if (_flight != motion || motion.Session.IsClosed || motion.Session.Generation != _flightGeneration ||
            context is null || !ReferenceEquals(context, _flightContext) ||
            Capture is not { IsMeasureValid: true, IsArrangeValid: true } capture || SharedHost is null) return false;
        // Drawing may continue against the last committed target while a new target is dirty.
        // Final handoff is stricter: it belongs to this session/domain and exact target generation.
        if (handoff is not null && (context.IsTargetDirty || context.Snapshot is not { } snapshot ||
            snapshot.Version != _flightSnapshotVersion || !snapshot.Results.ContainsKey(this))) return false;
        // A custom bounds spec can temporarily collapse an axis. Zero-sized flight geometry
        // is valid, unlike a zero-sized destination offered to the matching coordinator.
        var origin = SharedLayoutBounds.LookaheadTranslation(capture, SharedHost);
        Rect? actual = origin is { } point ? new Rect(point, capture.Bounds.Size) : null;
        // Context origins outside the cooperative tree may have just committed. Revisit actual
        // placement once with that origin before rendering/handoff; never rebuild the target.
        if (actual is not { } rect || Math.Abs(rect.X - _flightBounds.X) > 0.000001 ||
            Math.Abs(rect.Y - _flightBounds.Y) > 0.000001 || Math.Abs(rect.Width - _flightBounds.Width) > 0.000001 ||
            Math.Abs(rect.Height - _flightBounds.Height) > 0.000001)
        {
            InvalidateArrange(); Context?.InvalidateApproachPlacement();
            return false;
        }
        return true;
    }
    internal bool IsFlightTargetCurrent(SharedTransitionMotion motion)
    {
        if (!Remeasures) return true;
        var context = Context;
        return _flight == motion && motion.Session.Generation == _flightGeneration &&
            context is not null && ReferenceEquals(context, _flightContext) && !context.IsTargetDirty &&
            context.Snapshot is { } snapshot && snapshot.Version == _flightSnapshotVersion &&
            snapshot.Results.ContainsKey(this);
    }
    void ILookaheadApproach.ResetApproach() => _flight?.Session.Cancel(SharedSessionEndReason.Cancelled);
    private void FailFlight(Exception error)
    {
        SharedHost?.Scope?.ReportSessionError(error);
        _flight?.Session.Cancel(SharedSessionEndReason.Failed);
        InvalidateMeasure(); InvalidateArrange();
    }
}
