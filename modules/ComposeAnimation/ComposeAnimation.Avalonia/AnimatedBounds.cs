using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Experimental cooperative animateBounds: animates complete rectangles and remeasures live
/// content. Creates a local lookahead domain when no participating parent owns it.
/// </summary>
/// <remarks>
/// Outer and inner rectangle states surround TargetSize, Padding and IntermediateLayout, in that
/// order. The child must support full constraints and receives fixed animated
/// child constraints. TargetSize includes padding; native Width/Height
/// remain per-pass constraints unless an incoming full range overrides them.
/// No clipping is added. The default TargetSize mode keeps the native layout slot stable;
/// CurrentBounds describes the animated presentation. Detach, hide and domain changes reset motion.
/// ReferenceElement selects a participating ancestor; motion-frame contributions follow directly by default.
/// Lookahead.MoveContent preserves same-instance motion through synchronous same-domain parent changes.
/// </remarks>
public sealed class AnimatedBounds : Control, ILookaheadConstrainedLayoutNode, ILookaheadPlacementHost,
    ILookaheadPlacementApproach, ILookaheadMotionFrameObserver, ILookaheadMoveParticipant, ILookaheadEntryParticipant
{
    public static readonly StyledProperty<Control?> ChildProperty = Decorator.ChildProperty.AddOwner<AnimatedBounds>();
    public static readonly StyledProperty<Thickness> PaddingProperty = Decorator.PaddingProperty.AddOwner<AnimatedBounds>();
    [Content]
    public Control? Child { get => GetValue(ChildProperty); set => SetValue(ChildProperty, value); }
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    public static readonly DirectProperty<AnimatedBounds, Rect?> CurrentBoundsProperty =
        AvaloniaProperty.RegisterDirect<AnimatedBounds, Rect?>(nameof(CurrentBounds), value => value.CurrentBounds);
    public static readonly DirectProperty<AnimatedBounds, Rect?> TargetBoundsProperty =
        AvaloniaProperty.RegisterDirect<AnimatedBounds, Rect?>(nameof(TargetBounds), value => value.TargetBounds);
    public static readonly DirectProperty<AnimatedBounds, bool> IsRunningProperty =
        AvaloniaProperty.RegisterDirect<AnimatedBounds, bool>(nameof(IsRunning), value => value.IsRunning);
    private Rect? _currentBounds;
    private Rect? _targetBounds;
    private bool _isRunning;
    public Rect? CurrentBounds => _currentBounds;
    public Rect? TargetBounds => LookaheadCoordinates.TryGetTargetBounds(this, out var bounds,
        ReferenceElement, includeMotionFrameOfReference: true) ? bounds : null;

    public static readonly StyledProperty<BoundsTransform> BoundsTransformProperty =
        AvaloniaProperty.Register<AnimatedBounds, BoundsTransform>(nameof(BoundsTransform),
            new BoundsTransform(), validate: value => value is not null);
    public static readonly StyledProperty<Size?> TargetSizeProperty =
        AvaloniaProperty.Register<AnimatedBounds, Size?>(nameof(TargetSize), validate: size => size is null ||
            double.IsFinite(size.Value.Width) && double.IsFinite(size.Value.Height) &&
            size.Value.Width >= 0 && size.Value.Height >= 0);
    public static readonly StyledProperty<BoundsLayout> IntermediateLayoutProperty =
        AvaloniaProperty.Register<AnimatedBounds, BoundsLayout>(nameof(IntermediateLayout),
            BoundsLayout.Empty, validate: value => value is not null);
    public static readonly StyledProperty<Control?> ReferenceElementProperty =
        AvaloniaProperty.Register<AnimatedBounds, Control?>(nameof(ReferenceElement));
    /// <summary>Compatibility alias for ReferenceElementProperty.</summary>
    public static readonly StyledProperty<Control?> ReferenceScopeProperty = ReferenceElementProperty;
    public static readonly StyledProperty<BoundsParentLayoutMode> ParentLayoutModeProperty =
        AvaloniaProperty.Register<AnimatedBounds, BoundsParentLayoutMode>(nameof(ParentLayoutMode),
            BoundsParentLayoutMode.TargetSize, validate: value => Enum.IsDefined(value));
    public static readonly StyledProperty<bool> AnimateMotionFrameOfReferenceProperty =
        AvaloniaProperty.Register<AnimatedBounds, bool>(nameof(AnimateMotionFrameOfReference));

    /// <summary>Coordinate ancestor, defaulting to the domain reference. Changing it resets both animations.</summary>
    public Control? ReferenceElement { get => GetValue(ReferenceElementProperty); set => SetValue(ReferenceElementProperty, value); }
    /// <summary>Compatibility alias for ReferenceElement.</summary>
    public Control? ReferenceScope { get => GetValue(ReferenceScopeProperty); set => SetValue(ReferenceScopeProperty, value); }
    /// <summary>TargetSize reserves destination space. AnimatedSize requires a participating parent.</summary>
    public BoundsParentLayoutMode ParentLayoutMode { get => GetValue(ParentLayoutModeProperty); set => SetValue(ParentLayoutModeProperty, value); }
    /// <summary>Include continuous motion (such as scroll) in animation targets. Changing this resets motion.</summary>
    public bool AnimateMotionFrameOfReference
    {
        get => GetValue(AnimateMotionFrameOfReferenceProperty);
        set => SetValue(AnimateMotionFrameOfReferenceProperty, value);
    }

    public BoundsTransform BoundsTransform
    {
        get => GetValue(BoundsTransformProperty);
        set => SetValue(BoundsTransformProperty, value);
    }
    public Size? TargetSize { get => GetValue(TargetSizeProperty); set => SetValue(TargetSizeProperty, value); }
    public BoundsLayout IntermediateLayout
    {
        get => GetValue(IntermediateLayoutProperty);
        set => SetValue(IntermediateLayoutProperty, value);
    }
    public bool IsRunning => _isRunning;

    private readonly BoundsAnimationState _outer;
    private readonly BoundsAnimationState _inner;
    private readonly LookaheadPresentationHost _presentation;
    private LookaheadContext? _localContext;
    private LookaheadConstraints _presentationConstraints;
    private bool _updatingPresentation;
    private long _presentedVersion = -1;
    private Rect _nativeBounds;
    internal Control Presentation => _presentation;
    private LookaheadContext? _context;
    private Control? _reference;
    private ObservedClock? _clock;
    private IDisposable? _motionRegistration;
    private SharedAnimationFrameClock? _sharedClock;
    private Rect _outerSample;
    private Rect _innerSample;
    private long _lifecycle;
    private TargetLayout? _immediateLayout;
    private TargetLayout? _targetLayout;
    private TargetLayout? _approachLayout;
    private LookaheadConstraints? _lastInnerConstraints;
    private MoveState? _pendingMove;
    internal bool RetainsAnimationAttachment => _pendingMove is not null;
    private Vector _motionBasis;
    private sealed record TargetLayout(BoundsLayoutResult Layout, LookaheadConstraints InnerConstraints,
        Size InnerSize, Rect InnerBounds);

    static AnimatedBounds() => AffectsMeasure<AnimatedBounds>(TargetSizeProperty, IntermediateLayoutProperty,
        ChildProperty, PaddingProperty, ParentLayoutModeProperty);

    public AnimatedBounds()
    {
        _outer = new BoundsAnimationState(SampleChanged, error => VisualAnimationScope.ReportError(this, error));
        _inner = new BoundsAnimationState(SampleChanged, error => VisualAnimationScope.ReportError(this, error));
        _presentation = new LookaheadPresentationHost(this, MeasurePresentation, ArrangeApproach, ContentInvalidated);
        _presentation.UseLayoutRounding = false;
        VisualChildren.Add(_presentation);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_outer is null) return;
        if (change.Property == VisualParentProperty && _localContext is not null)
        {
            ReleaseLocalEntry();
            Release();
            BindContext(null);
        }
        if (change.Property == IsVisibleProperty && !IsVisible) Release();
        if (change.Property == ChildProperty)
        {
            _presentation.SetChild(null);
            if (change.OldValue is Control oldChild)
            {
                ((ISetLogicalParent)oldChild).SetParent(null);
                LogicalChildren.Remove(oldChild);
                LookaheadMeasure.Forget(oldChild);
            }
            if (change.NewValue is Control newChild)
            {
                ((ISetLogicalParent)newChild).SetParent(this);
                LogicalChildren.Add(newChild);
                _presentation.SetChild(newChild);
            }
            _presentation.InvalidateApproachMeasure();
        }
        if (change.Property == BoundsTransformProperty)
        {
            _outer.RetryFailedTarget();
            _inner.RetryFailedTarget();
            InvalidateMeasure();
        }
        else if (change.Property == ReferenceScopeProperty || change.Property == AnimateMotionFrameOfReferenceProperty)
        {
            Release();
            InvalidateMeasure();
            _context?.InvalidateApproachPlacement();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
        => MeasureApproach(LookaheadConstraints.AtMost(availableSize).ApplyTo(this));

    private Size MeasureApproach(LookaheadConstraints constraints)
    {
        _context?.VerifyLayoutAllowed();
        VerifyParentLayoutMode();
        try
        {
            _presentationConstraints = constraints;
            _updatingPresentation = true;
            _presentation.InvalidateApproachMeasure();
            _presentation.Measure(constraints.MaxSize);
            return _presentation.DesiredSize;
        }
        catch
        {
            // Layout-policy/child failures are structural errors, not animation-spec errors.
            // Stop frame requests and invalidate Avalonia's optimistic Measure cache before rethrowing.
            Release();
            InvalidateMeasure();
            InvalidateArrange();
            throw;
        }
        finally { _updatingPresentation = false; }
    }

    private Size MeasurePresentation(Size available)
    {
        if (LookaheadScope.FindContext(this)?.TryPreparationMeasure(this, out var prepared) == true)
        {
            if (prepared.LayoutData is TargetLayout layout && Child is { } child)
                LookaheadMeasure.Measure(child, layout.InnerConstraints);
            return prepared.MeasuredSize;
        }
        var result = default(Size);
        if (_localContext is { } local)
        {
            local.RunActual(() => result = MeasureApproachCore(_presentationConstraints));
            return local.LocalMeasurement?.MeasuredSize ?? result;
        }
        result = MeasureApproachCore(_presentationConstraints);
        return ParentLayoutMode == BoundsParentLayoutMode.TargetSize && LookaheadRoot.TargetFor(this) is { } target
            ? _presentationConstraints.Constrain(target.MeasuredSize) : result;
    }

    private void BindContext(LookaheadContext? context)
    {
        if (_context == context) return;
        if (_context is not null) _context.TargetInvalidated -= ClearPublishedTarget;
        _context = context;
        if (_context is not null) _context.TargetInvalidated += ClearPublishedTarget;
    }

    private void ClearPublishedTarget() => SetAndRaise(TargetBoundsProperty, ref _targetBounds, null);

    private void ContentInvalidated()
    {
        if (_updatingPresentation) return;
        // Target inputs are subscribed by the coordinator. A descendant approach changing
        // DesiredSize only requires presentation work, not a new target generation.
        if (ParentLayoutMode == BoundsParentLayoutMode.AnimatedSize || _context?.IsTargetDirty != false) InvalidateMeasure();
        InvalidateArrange();
    }

    private Size MeasureApproachCore(LookaheadConstraints constraints)
    {
        _context?.VerifyLayoutAllowed();
        var root = LookaheadScope.FindContext(this);
        var target = LookaheadRoot.TargetFor(this);
        if (root is null)
        {
            Release();
            BindContext(null);
            _immediateLayout = MeasureLayout(constraints, (child, incoming) =>
                child is ILookaheadConstrainedLayoutNode
                    ? LookaheadMeasure.Measure(child, incoming) : LookaheadMeasure.Measure(child, incoming.MaxSize));
            return _immediateLayout.Layout.Size;
        }
        if (target is null || _localContext is null && this.GetVisualParent() is not ILookaheadPlacementHost)
            throw new NotSupportedException("AnimatedBounds requires a registered target and an ILookaheadPlacementHost parent.");
        var layout = target.LayoutData as TargetLayout ??
            throw new InvalidOperationException("AnimatedBounds requires complete target layout data.");
        if (_context != root) Release();
        BindContext(root);
        _reference = LookaheadCoordinates.ResolveReference(this, root, ReferenceScope);
        _targetLayout = layout;
        EnsureClock();
        var clock = _clock?.Faulted == false ? _sharedClock : null;
        var lifecycle = _lifecycle;
        var outerTarget = ToReference(target.Bounds);
        var innerTarget = ToReference(layout.InnerBounds);
        _outer.UpdateTarget(outerTarget, clock, BoundsTransform);
        if (lifecycle == _lifecycle) // A user BoundsTransform can reset or detach this owner.
            _inner.UpdateTarget(innerTarget, clock, BoundsTransform);
        // Capture both boundaries once for this measure/arrange pass. No live Bounds reads.
        _outerSample = _outer.Value ?? outerTarget;
        _innerSample = _inner.Value ?? innerTarget;
        // The outer boundary admits the incoming constraints. The inner boundary replaces them
        // with its animated size, analogous to Compose's inner fixed-constraints node.
        _approachLayout = MeasureLayout(constraints, (child, _) =>
        {
            var size = NonNegativeSize(_innerSample);
            var requested = InnerConstraints(layout, size);
            LookaheadMeasure.Measure(child, requested);
            _lastInnerConstraints = requested;
            // The virtual inner node reports its animated size to the intermediate policy.
            return size;
        });
        PublishGeometry();
        return constraints.Constrain(NonNegativeSize(_outerSample));
    }

    bool ILookaheadPlacementApproach.AppliesPositionApproach => ParentLayoutMode == BoundsParentLayoutMode.AnimatedSize;

    Point ILookaheadPlacementApproach.ApproachPosition(LookaheadContext root, Point target)
        => ReferenceEquals(root, _context) && ParentLayoutMode == BoundsParentLayoutMode.AnimatedSize
            ? ToRoot(_outerSample).TopLeft : target;

    void ILookaheadMotionFrameObserver.InvalidateMotionFrame()
    {
        if (AnimateMotionFrameOfReference) InvalidateMeasure();
    }

    private Rect ToReference(Rect bounds)
    {
        var reference = _reference!;
        var origin = reference == _context?.CoordinateReference ? default : _context!.Snapshot!.Results[reference].Bounds.TopLeft;
        var motion = AnimateMotionFrameOfReference ? LookaheadCoordinates.MotionOffset(this, reference) : default;
        return new Rect(bounds.TopLeft - new Vector(origin.X, origin.Y) + motion + _motionBasis, bounds.Size);
    }

    private Rect ToRoot(Rect bounds)
    {
        // Release can be called synchronously from a user animation policy during measurement.
        var reference = _reference ?? LookaheadCoordinates.ResolveReference(this, _context!, ReferenceScope);
        var origin = reference == _context?.CoordinateReference ? default : LookaheadPlacement.RootPosition(reference);
        var motion = AnimateMotionFrameOfReference ? default : LookaheadCoordinates.MotionOffset(this, reference);
        return new Rect(bounds.TopLeft + new Vector(origin.X, origin.Y) + motion - _motionBasis, bounds.Size);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        try
        {
            if (ParentLayoutMode == BoundsParentLayoutMode.TargetSize && _context is not null)
            {
                _updatingPresentation = true;
                try { _presentation.Measure(_presentationConstraints.MaxSize); }
                finally { _updatingPresentation = false; }
                var display = ToRoot(new Rect(_outerSample.TopLeft, NonNegativeSize(_outerSample)));
                var origin = LookaheadPlacement.RootPosition(this);
                LookaheadPlacement.ArrangeChild(this, _presentation,
                    new Rect(display.TopLeft - new Vector(origin.X, origin.Y), display.Size));
            }
            else LookaheadPlacement.ArrangeChild(this, _presentation, new Rect(finalSize));
            PublishGeometry();
            return finalSize;
        }
        catch
        {
            Release();
            InvalidateMeasure();
            InvalidateArrange();
            throw;
        }
    }

    private Size ArrangeApproach(Size finalSize)
    {
        _context?.VerifyLayoutAllowed();
        if (Child is { } child)
        {
            if (_context is not null && LookaheadRoot.TargetFor(this) is not null)
            {
                var layout = _approachLayout ?? throw new InvalidOperationException("Actual placement requires a measured intermediate layout.");
                var origin = LookaheadPlacement.RootPosition(_presentation);
                var offset = layout.Layout.Place(new(layout.Layout.Size, layout.InnerSize, origin, false));
                if (!ReferenceEquals(layout, _approachLayout) || Child != child) return finalSize;
                var naturalPosition = origin + new Vector(offset.X, offset.Y);
                var animated = ToRoot(new Rect(_innerSample.TopLeft, NonNegativeSize(_innerSample)));
                // The virtual inner boundary compensates the intermediate placement, just as
                // the inner approach modifier offsets its placeable in Compose. Its final root
                // position stays on the bounds trajectory even when the middle layer moves.
                var compensation = animated.TopLeft - naturalPosition;
                LookaheadPlacement.ArrangeChild(_presentation, child,
                    new Rect(offset + compensation, animated.Size));
            }
            else
            {
                var layout = _immediateLayout;
                if (layout is not null)
                {
                    var offset = layout.Layout.Place(new(layout.Layout.Size, layout.InnerSize, default, false));
                    LookaheadPlacement.ArrangeChild(_presentation, child, new Rect(offset, layout.InnerSize));
                }
            }
        }
        return finalSize;
    }

    private TargetLayout MeasureLayout(LookaheadConstraints incoming, Func<Control, LookaheadConstraints, Size> measure)
    {
        var policy = BoundsLayout.Padding(EffectivePadding).Then(IntermediateLayout);
        if (TargetSize is { } size) policy = BoundsLayout.FixedSize(size).Then(policy);
        Size inner = default;
        LookaheadConstraints innerConstraints = default;
        var result = policy.Evaluate(incoming, constraints =>
        {
            innerConstraints = constraints;
            return inner = Child is { } child ? measure(child, constraints) : constraints.Constrain(default(Size));
        });
        return new(result, innerConstraints, inner, default);
    }

    private LookaheadConstraints InnerConstraints(TargetLayout layout, Size sample)
        => _inner.IsRunning ? LookaheadConstraints.Fixed(sample) : layout.InnerConstraints;

    private Thickness EffectivePadding => UseLayoutRounding
        ? LayoutHelper.RoundLayoutThickness(Padding, LayoutHelper.GetLayoutScale(this)) : Padding;

    private static Size NonNegativeSize(Rect bounds) => new(Math.Max(0, bounds.Width), Math.Max(0, bounds.Height));

    private void SampleChanged()
    {
        if (_pendingMove is { } pending) pending.FrameAdvanced = true;
        var outer = _outer.Value;
        var inner = _inner.Value;
        if (ParentLayoutMode == BoundsParentLayoutMode.TargetSize)
        {
            if (outer is { } nextOuter && inner is { } nextInner)
            {
                if (nextOuter.Size != _outerSample.Size || nextInner.Size != _innerSample.Size ||
                    _targetLayout is { } localLayout && InnerConstraints(localLayout, NonNegativeSize(nextInner)) != _lastInnerConstraints)
                    _presentation.InvalidateApproachMeasure();
                _outerSample = nextOuter;
                _innerSample = nextInner;
            }
            InvalidateArrange();
            _context?.InvalidateApproachPlacement();
            PublishGeometry();
            return;
        }
        if (outer is { } outerBounds && inner is { } innerBounds &&
            outerBounds.Size == _outerSample.Size && innerBounds.Size == _innerSample.Size &&
            (Child is null ||
                _targetLayout is { } layout && InnerConstraints(layout, NonNegativeSize(innerBounds)) == _lastInnerConstraints))
        {
            // Position-only frames reuse the last real measure. Publish both positions together;
            // Arrange still compensates for moving ancestors even after this owner's run finishes.
            _outerSample = outerBounds;
            _innerSample = innerBounds;
            InvalidateArrange();
        }
        else InvalidateMeasure();
        _context?.InvalidateApproachPlacement();
        PublishGeometry();
    }

    private void PublishGeometry()
    {
        var value = _outer.Value;
        Rect? current = null;
        if (value is { } sample && _reference is { } reference)
        {
            var direct = AnimateMotionFrameOfReference ? default : LookaheadCoordinates.MotionOffset(this, reference);
            current = new Rect(sample.TopLeft + direct - _motionBasis, sample.Size);
        }
        SetAndRaise(CurrentBoundsProperty, ref _currentBounds, current);
        // Property listeners may invalidate or detach this node while CurrentBounds is raised.
        // Re-read the committed target so no obsolete target is republished after that callback.
        SetAndRaise(TargetBoundsProperty, ref _targetBounds, TargetBounds);
        SetAndRaise(IsRunningProperty, ref _isRunning, _outer.IsRunning || _inner.IsRunning);
    }

    private void EnsureClock()
    {
        if (_clock is not null) return;
        if (VisualAnimationScope.IsEnvironmentEnding(this)) return;
        if (TopLevel.GetTopLevel(this) is null) return;
        var motion = VisualAnimationScope.ForHost(this);
        _motionRegistration = motion.RegisterResource(() => { if (motion.IsEnding) Release(); });
        _clock = new ObservedClock(motion.Clock);
        _sharedClock = new SharedAnimationFrameClock(_clock);
    }

    /// <summary>Cancel both boundaries and initialize at the destination on the next layout.</summary>
    public void ResetAnimation()
    {
        VerifyAccess();
        Release();
        InvalidateMeasure();
    }

    void ILookaheadApproach.ResetApproach()
    {
        Release();
        BindContext(null);
    }

    private void Release(bool preserveValue = false)
    {
        var registration = _motionRegistration;
        _motionRegistration = null;
        registration?.Dispose();
        // A configuration change during a tree edit forfeits move retention. Keeping a value
        // without committing its old-to-new coordinate basis would cause a misplaced resume.
        if (_pendingMove is not null) preserveValue = false;
        _pendingMove = null;
        if (!preserveValue) _motionBasis = default;
        ++_lifecycle;
        _outer.Reset(preserveValue);
        _inner.Reset(preserveValue);
        _targetLayout = null;
        _approachLayout = null;
        _reference = null;
        _lastInnerConstraints = null;
        _sharedClock?.Dispose();
        _sharedClock = null;
        _clock = null;
        SetAndRaise(CurrentBoundsProperty, ref _currentBounds, null);
        SetAndRaise(TargetBoundsProperty, ref _targetBounds, null);
        SetAndRaise(IsRunningProperty, ref _isRunning, false);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadMeasure.Forget(this);
        ReleaseLocalEntry();
        if (_pendingMove is null)
        {
            Release();
            BindContext(null);
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void ReleaseLocalEntry()
    {
        if (_localContext is { } local)
        {
            local.Detach();
            if (LookaheadEnvironment.GetContext(this) == local)
                ClearValue(LookaheadEnvironment.ContextProperty);
            _localContext = null;
            _presentedVersion = -1;
        }
    }

    ILookaheadMoveState? ILookaheadMoveParticipant.CaptureMove(LookaheadContext root)
    {
        if (_pendingMove is not null) throw new InvalidOperationException("This AnimatedBounds is already being moved.");
        if (_context != root || _reference is null || _outer.Value is null || _inner.Value is null) return null;
        return _pendingMove = new MoveState(this, root, _reference);
    }

    private sealed class MoveState : ILookaheadMoveState
    {
        private readonly AnimatedBounds _owner;
        private readonly LookaheadContext _context;
        private readonly Control _reference;
        private readonly List<MotionEdge> _before;
        private Vector _basisDelta;
        public bool FrameAdvanced { get; set; }
        private sealed record MotionEdge(Control Parent, Control Child, Vector Offset);

        public MoveState(AnimatedBounds owner, LookaheadContext root, Control reference)
        {
            _owner = owner;
            _context = root;
            _reference = reference;
            _before = owner.AnimateMotionFrameOfReference ? [] : MotionEdges();
        }

        private bool IsCurrent => ReferenceEquals(_owner._pendingMove, this);

        public bool CanCommit()
        {
            // An explicit Reset, clock/reference change, or failure already released this state.
            if (!IsCurrent) return true;
            if (FrameAdvanced || LookaheadScope.FindContext(_owner) != _context ||
                (_owner.ReferenceScope ?? _context.CoordinateReference) != _reference || !_owner.GetVisualAncestors().Contains(_reference)) return false;
            if (!_owner.AnimateMotionFrameOfReference)
            {
                _basisDelta = default;
                var after = MotionEdges();
                // Only changed edges alter the basis. Scrolling an unchanged ancestor during the
                // edit still follows immediately, rather than being turned into layout animation.
                foreach (var edge in after)
                    if (!_before.Any(old => old.Parent == edge.Parent && old.Child == edge.Child)) _basisDelta += edge.Offset;
                foreach (var edge in _before)
                    if (!after.Any(next => next.Parent == edge.Parent && next.Child == edge.Child)) _basisDelta -= edge.Offset;
                var basis = _owner._motionBasis + _basisDelta;
                if (!double.IsFinite(basis.X) || !double.IsFinite(basis.Y))
                    throw new InvalidOperationException("Moving content produced a non-finite motion-frame basis.");
            }
            return true;
        }

        public void Commit()
        {
            if (!IsCurrent) return;
            _owner._pendingMove = null;
            if (!_owner.IsEffectivelyVisible) _owner.Release();
            else _owner._motionBasis += _basisDelta;
            _owner._targetLayout = null;
            _owner._lastInnerConstraints = null;
            _owner.InvalidateMeasure();
            _owner.InvalidateArrange();
        }

        public void Cancel()
        {
            if (!IsCurrent) return;
            _owner.Release();
            _owner.BindContext(null);
            _owner.InvalidateMeasure();
            _owner.InvalidateArrange();
        }

        private List<MotionEdge> MotionEdges()
        {
            var result = new List<MotionEdge>();
            for (Visual? child = _owner; child != _reference;)
            {
                var parent = child?.GetVisualParent() ?? throw new InvalidOperationException("The move reference is no longer an ancestor.");
                if (parent is Control host && parent is ILookaheadMotionFrameProvider && child is Control content)
                    result.Add(new MotionEdge(host, content, LookaheadCoordinates.EdgeOffset(host, content)));
                child = parent;
            }
            return result;
        }
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Child is { } child ? [child] : [];
    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties =>
        [TargetSizeProperty, IntermediateLayoutProperty];
    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        => MeasureLookahead(builder, LookaheadConstraints.AtMost(available).ApplyTo(this));
    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => MeasureLookahead(builder, constraints);
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints) => MeasureApproach(constraints);

    private Size MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
    {
        var layout = MeasureLayout(constraints, builder.Measure);
        builder.SetLayoutData(this, layout);
        return layout.Layout.Size;
    }

    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
    {
        var layout = (TargetLayout)builder.GetLayoutData(this)!;
        var offset = layout.Layout.Place(new(layout.Layout.Size, layout.InnerSize, bounds.TopLeft, true));
        var inner = new Rect(bounds.TopLeft + new Vector(offset.X, offset.Y), layout.InnerSize);
        builder.SetLayoutData(this, layout with { InnerBounds = inner });
        if (Child is { } child) builder.Place(child, inner);
    }

    void ILookaheadEntryParticipant.EnsureLookaheadEntry() => EnsureEntry();

    private void EnsureEntry()
    {
        if (_localContext is not null)
        {
            // A previously detached subtree may acquire a cooperative ancestor without
            // changing this node's direct visual parent. Its own local value must not hide it.
            var parentContext = this.GetVisualParent() is { } parent
                ? LookaheadEnvironment.GetContext(parent) : null;
            if (parentContext is null || parentContext == _localContext || !parentContext.Contains(this)) return;
            ReleaseLocalEntry();
            Release();
            BindContext(null);
        }
        var inherited = LookaheadScope.FindContext(this);
        if (inherited?.Contains(this) == true) return;
        Release();
        _localContext = new LookaheadContext(this, () => this, localEntry: true);
        BindContext(_localContext);
        LookaheadEnvironment.SetContext(this, _localContext);
        _localContext.Attach();
    }

    private void VerifyParentLayoutMode()
    {
        if (ParentLayoutMode == BoundsParentLayoutMode.AnimatedSize &&
            (_localContext is not null || LookaheadScope.FindContext(this)?.Contains(this) != true ||
             this.GetVisualParent() is not ILookaheadPlacementHost))
        {
            Release();
            throw new NotSupportedException("AnimatedSize requires a participating placement parent in the same lookahead domain.");
        }
    }

    protected override Size MeasureCore(Size availableSize)
    {
        _context?.VerifyLayoutAllowed();
        ApplyStyling();
        ApplyTemplate();
        EnsureEntry();
        VerifyParentLayoutMode();
        if (_localContext is not { } local)
            return LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired) ? desired : base.MeasureCore(availableSize);
        if (!IsVisible) return default;
        var full = LookaheadMeasure.PendingConstraints(this, availableSize);
        var targetDesired = full is { } requested ? local.PrepareLocal(requested) : local.PrepareLocal(availableSize);
        if (full is not null) LookaheadMeasure.AcceptPrepared(this, local.LocalMeasurement);
        _presentationConstraints = (full ?? LookaheadConstraints.AtMost(availableSize))
            .Deflate(LookaheadMeasure.Margin(this)).ApplyTo(this);
        _updatingPresentation = true;
        try
        {
            _presentation.InvalidateApproachMeasure();
            _presentation.Measure(_presentationConstraints.MaxSize);
        }
        finally { _updatingPresentation = false; }
        return targetDesired;
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        if (_localContext is { } local)
        {
            if (!IsVisible || !local.CommitLocal(finalRect)) return;
            _nativeBounds = local.Snapshot!.Results[this].Bounds;
            _updatingPresentation = true;
            try
            {
                local.RunActual(() =>
                {
                    if (_presentedVersion != local.Snapshot.Version)
                    {
                        _presentation.InvalidateApproachMeasure();
                        _presentedVersion = local.Snapshot.Version;
                    }
                    _presentation.Measure(_presentationConstraints.MaxSize);
                    LookaheadPlacement.WithOrigin(this, local, _nativeBounds.TopLeft,
                        () => ArrangeOverride(_nativeBounds.Size));
                });
                Bounds = _nativeBounds;
            }
            finally { _updatingPresentation = false; }
        }
        else if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    private sealed class ObservedClock(IAnimationFrameClock provider) : IAnimationFrameClock, IAnimationLifetimeClock
    {
        public AnimationLifetime? AnimationLifetime => (provider as IAnimationLifetimeClock)?.AnimationLifetime;
        public double DurationScale => (provider as IAnimationLifetimeClock)?.DurationScale ?? 1;
        public bool Faulted { get; private set; }
        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();
        public async ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken)
        {
            try
            {
                provider.VerifyAccess();
                return await provider.NextFrameAsync(cancellationToken);
            }
            catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                Faulted = true;
                throw;
            }
        }
    }
}
