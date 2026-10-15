using System.ComponentModel;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation.Runtime;
using DoubleConverter = ComposeAnimation.Runtime.DoubleConverter;

namespace ComposeAnimation;

/// <summary>One effect owner's persistent enter/exit state; independent of its current visual layout.</summary>
internal sealed class EnterExitMotion : IDisposable
{
    private readonly IDisposable _scopeRegistration;
    private readonly Transition<EnterExitState>.DeferredAnimation<double> _opacityAnimation;
    private readonly Transition<EnterExitState>.DeferredAnimation<double> _scaleAnimation;
    private readonly Transition<EnterExitState>.DeferredAnimation<Point> _transformOriginAnimation;
    private readonly Transition<EnterExitState>.DeferredAnimation<Vector> _slideOffsetAnimation;
    private readonly Transition<EnterExitState>.DeferredAnimation<Size> _sizeAnimation;
    private readonly Transition<EnterExitState>.DeferredAnimation<Vector> _alignmentOffsetAnimation;
    private readonly HashSet<INotifyPropertyChanged> _observedAnimations = [];
    private Transition<EnterExitState>.TransitionAnimationState<double>? _opacity;
    private Transition<EnterExitState>.TransitionAnimationState<double>? _scale;
    private Transition<EnterExitState>.TransitionAnimationState<Point>? _transformOrigin;
    private Transition<EnterExitState>.TransitionAnimationState<Vector>? _slideOffset;
    private Transition<EnterExitState>.TransitionAnimationState<Size>? _size;
    private Transition<EnterExitState>.TransitionAnimationState<Vector>? _alignmentOffset;
    private EnterTransition? _enterDeclaration;
    private ExitTransition? _exitDeclaration;
    private EnterExitEffects _declaredEnter = new();
    private EnterExitEffects _declaredExit = new();
    private EnterExitEffects _activeEnter = new();
    private EnterExitEffects _activeExit = new();
    private IFiniteAnimationSpec<double>? _alphaSpecOverride;
    private TransitionPreparationLease? _configurationPreparation;
    private Size _referenceContentSize;
    private VisibilityAlignment? _currentAlignment;
    private (EnterExitState Current, EnterExitState Target, bool Seeking, bool Pending, EnterExitState PendingState) _phase;
    private bool _effectsDirty = true;
    private bool _reservingPreparation;
    private bool _disposed;

    internal AnimatedVisibilityScope Scope { get; }
    private Transition<EnterExitState> Transition => Scope.Transition;
    internal double Opacity => Math.Clamp(_opacity?.Value ?? 1, 0, 1);
    internal double Scale => _scale?.Value ?? 1;
    internal Point TransformOrigin => _transformOrigin?.Value ?? new Point(0.5, 0.5);
    internal Vector SlideOffset => _slideOffset?.Value ?? default;
    internal bool ChangesSize => _activeEnter.ChangeSize is { } || _activeExit.ChangeSize is { };

    internal bool ClipsToBounds => ChangesSize &&
        _activeEnter.ChangeSize?.Clip != false && _activeExit.ChangeSize?.Clip != false;

    internal event Action? MeasureInvalidated;
    internal event Action? ArrangeInvalidated;
    internal event Action? VisualInvalidated;

    internal EnterExitMotion(AnimatedVisibilityScope scope, string? label)
    {
        Scope = scope;
        label ??= "Visibility";
        _opacityAnimation = Transition.CreateDeferredAnimation(new DoubleConverter(), $"{label} alpha");
        _scaleAnimation = Transition.CreateDeferredAnimation(new DoubleConverter(), $"{label} scale");
        _transformOriginAnimation = Transition.CreateDeferredAnimation(new PointConverter(), $"{label} origin");
        _slideOffsetAnimation = Transition.CreateDeferredAnimation(new VectorConverter(), $"{label} offset");
        _sizeAnimation = Transition.CreateDeferredAnimation(new SizeConverter(), $"{label} size");
        _alignmentOffsetAnimation = Transition.CreateDeferredAnimation(new VectorConverter(), $"{label} alignment offset");
        _scopeRegistration = scope.Own(this);
        _phase = ReadPhase();
        Transition.TargetChanging += PrepareConfiguration;
        Transition.PhaseChanged += PhaseChanged;
        Transition.AnimationsReconfiguring += UpdateActiveEffects;
    }

    internal void UpdateTransitions(EnterTransition enter, ExitTransition exit,
        IFiniteAnimationSpec<double>? alphaSpecOverride = null)
    {
        if (ReferenceEquals(enter, _enterDeclaration) && ReferenceEquals(exit, _exitDeclaration) &&
            ReferenceEquals(alphaSpecOverride, _alphaSpecOverride)) return;
        _enterDeclaration = enter;
        _exitDeclaration = exit;
        _declaredEnter = EnterExitEffects.Read(enter);
        _declaredExit = EnterExitEffects.Read(exit);
        _alphaSpecOverride = alphaSpecOverride;
        _effectsDirty = true;
        MeasureInvalidated?.Invoke();
    }

    private (EnterExitState, EnterExitState, bool, bool, EnterExitState) ReadPhase()
        => (Transition.CurrentState,
            Transition.TargetState,
            Transition.IsSeeking,
            Transition.HasPendingTargetState,
            Transition.PendingTargetState);

    private void PhaseChanged()
    {
        if (_disposed || _phase == ReadPhase()) return;

        _phase = ReadPhase();
        _effectsDirty = true;
        MeasureInvalidated?.Invoke();
    }

    private void PrepareConfiguration()
    {
        if (_disposed || _reservingPreparation) return;

        _reservingPreparation = true;
        try
        {
            if (!Transition.IsSeeking && _configurationPreparation?.IsActive != true)
                _configurationPreparation = Scope.BeginPreparation();
            MeasureInvalidated?.Invoke();
        }
        finally
        {
            _reservingPreparation = false;
        }
    }

    internal void ConfigureAnimations(Size measuredContentSize, Size? targetContentSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _referenceContentSize = targetContentSize ?? measuredContentSize;
        UpdateActiveEffects();

        if (Transition.CurrentState == Transition.TargetState) _currentAlignment = null;
        else _currentAlignment ??= Alignment ?? VisibilityAlignment.TopStart;

        _opacity = Observe(_opacityAnimation.Animate(segment => _alphaSpecOverride ??
                (IsEntering(segment) ? _activeEnter.Fade?.Spec
                    : IsExiting(segment) ? _activeExit.Fade?.Spec
                    : null) ?? EnterExitEffects.Spring<double>(),
            state => state switch {
                EnterExitState.PreEnter => _activeEnter.Fade?.Alpha ?? 1,
                EnterExitState.PostExit => _activeExit.Fade?.Alpha ?? 1,
                _ => 1
            }));

        _scale = Observe(_scaleAnimation.Animate(segment =>
                (IsEntering(segment) ? _activeEnter.Scale?.Spec
                    : IsExiting(segment) ? _activeExit.Scale?.Spec
                    : null) ?? new SpringSpec<double>(stiffness: Spring.StiffnessMediumLow, visibilityThreshold: 0.002),
            state => state switch {
                EnterExitState.PreEnter => _activeEnter.Scale?.Scale ?? 1,
                EnterExitState.PostExit => _activeExit.Scale?.Scale ?? 1,
                _ => 1
            }));

        _transformOrigin = Observe(_transformOriginAnimation.Animate(_ =>
                _activeEnter.Scale is null && _activeExit.Scale is null
                    ? new SnapSpec<Point>()
                    : new SpringSpec<Point>(visibilityThreshold: new Point(0.01, 0.01)),
            state => OriginFraction(state switch {
                EnterExitState.PreEnter => _activeEnter.Scale?.Origin ?? _activeExit.Scale?.Origin,
                EnterExitState.PostExit => _activeExit.Scale?.Origin,
                _ => Transition.CurrentState == EnterExitState.PreEnter
                    ? _activeEnter.Scale?.Origin ?? _activeExit.Scale?.Origin
                    : _activeExit.Scale?.Origin ?? _activeEnter.Scale?.Origin
            } ?? RelativePoint.Center)));

        _slideOffset = Observe(_slideOffsetAnimation.Animate(
            segment => (IsEntering(segment) ? _activeEnter.Slide?.Spec
                : IsExiting(segment) ? _activeExit.Slide?.Spec
                : null) ?? EnterExitEffects.Spring<Vector>(),
            state => Checked(state switch {
                EnterExitState.PreEnter => _activeEnter.Slide?.Offset(_referenceContentSize) ?? default,
                EnterExitState.PostExit => _activeExit.Slide?.Offset(_referenceContentSize) ?? default,
                _ => default
            })));

        _size = Observe(_sizeAnimation.Animate(segment =>
            !ChangesSize || Transition is { CurrentState: EnterExitState.Visible, TargetState: EnterExitState.Visible }
                ? new SnapSpec<Size>()
                : (IsEntering(segment) ? _activeEnter.ChangeSize?.Spec
                    : IsExiting(segment) ? _activeExit.ChangeSize?.Spec
                    : null) ?? EnterExitEffects.Spring<Size>(), SizeByState));

        _alignmentOffset =
            Observe(_alignmentOffsetAnimation.Animate(_ => EnterExitEffects.Spring<Vector>(), AlignmentOffsetByState));

        TransitionPreparationLease? preparation = _configurationPreparation;
        _configurationPreparation = null;
        preparation?.Dispose();
    }

    private void UpdateActiveEffects()
    {
        if (_disposed) return;
        if (_phase != ReadPhase())
        {
            _phase = ReadPhase();
            _effectsDirty = true;
        }

        if (_effectsDirty)
        {
            _effectsDirty = false;
            // EnterExitTransition.kt trackActiveEnter/trackActiveExit. Keep unfinished effects
            // across reversal; old exit channels return to their neutral values before replacement.
            if (Transition is { CurrentState: EnterExitState.Visible, TargetState: EnterExitState.Visible })
            {
                _activeEnter = Transition.IsSeeking ? _declaredEnter : new EnterExitEffects();
                _activeExit = Transition.IsSeeking ? _declaredExit : new EnterExitEffects();
            }
            else
            {
                if (Transition.TargetState != EnterExitState.PostExit)
                    _activeEnter = _activeEnter.Overlay(_declaredEnter);

                if (Transition.TargetState != EnterExitState.Visible)
                {
                    EnterExitEffects neutralExit = _activeExit with {
                        Fade = _activeExit.Fade is { } fade ? fade with { Alpha = 1 } : null,
                        Scale = _activeExit.Scale is { } scale ? scale with { Scale = 1 } : null,
                        Slide = _activeExit.Slide is { } slide ? slide with { Offset = static _ => default } : null,
                        ChangeSize = _activeExit.ChangeSize is { } size ? size with { Size = static size => size } : null
                    };
                    _activeExit = neutralExit.Overlay(_declaredExit);
                }
            }
        }
    }

    private static bool IsEntering(TransitionSegment<EnterExitState> segment)
        => segment is { InitialState: EnterExitState.PreEnter, TargetState: EnterExitState.Visible };

    private static bool IsExiting(TransitionSegment<EnterExitState> segment)
        => segment is { InitialState: EnterExitState.Visible, TargetState: EnterExitState.PostExit };

    private VisibilityAlignment? Alignment => IsEntering(Transition.Segment)
        ? _activeEnter.ChangeSize?.Alignment ?? _activeExit.ChangeSize?.Alignment
        : _activeExit.ChangeSize?.Alignment ?? _activeEnter.ChangeSize?.Alignment;

    private Size SizeByState(EnterExitState state)
    {
        Size size = state switch {
            EnterExitState.PreEnter => _activeEnter.ChangeSize?.Size(_referenceContentSize) ?? _referenceContentSize,
            EnterExitState.PostExit => _activeExit.ChangeSize?.Size(_referenceContentSize) ?? _referenceContentSize,
            _ => _referenceContentSize
        };
        if (!Double.IsFinite(size.Width) || !Double.IsFinite(size.Height) || size.Width < 0 || size.Height < 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Visibility sizes must be finite and non-negative.");

        return size;
    }

    private Vector AlignmentOffsetByState(EnterExitState state)
    {
        if (state != EnterExitState.PostExit || _currentAlignment is not { } current ||
            Alignment is not { } target || current == target || _activeExit.ChangeSize is not { } changeSize)
            return default;
        Size endSize = changeSize.Size(_referenceContentSize);
        return Align(_referenceContentSize, endSize, target, FlowDirection.LeftToRight) -
            Align(_referenceContentSize, endSize, current, FlowDirection.LeftToRight);
    }

    private Point OriginFraction(RelativePoint origin)
        => origin.Unit == RelativeUnit.Relative ? origin.Point : new Point(
            _referenceContentSize.Width == 0 ? 0 : origin.Point.X / _referenceContentSize.Width,
            _referenceContentSize.Height == 0 ? 0 : origin.Point.Y / _referenceContentSize.Height);

    private static Vector Checked(Vector offset)
    {
        EnterExitEffects.Finite(offset.X);
        EnterExitEffects.Finite(offset.Y);
        return offset;
    }

    internal Size GetAnimatedSize(Size measuredContentSize)
        => ChangesSize && _size is { } ? new Size(Math.Max(0, _size.Value.Width), Math.Max(0, _size.Value.Height))
            : measuredContentSize;

    internal Point GetContentOffset(Size layoutSize)
        => (ChangesSize && _currentAlignment is { } alignment
                ? Align(_referenceContentSize, layoutSize, alignment, FlowDirection.LeftToRight) : default)
          + (_alignmentOffset?.Value ?? default);

    internal Point AlignmentOffset(Size available, Size full, FlowDirection flowDirection)
        => ChangesSize ? Align(full, available, _currentAlignment ?? Alignment ?? VisibilityAlignment.TopStart, flowDirection)
            : default;

    // Also used by AnimatedContent's container geometry; flow direction there remains explicit.
    internal static Point Align(Size child, Size available, VisibilityAlignment alignment, FlowDirection flowDirection)
    {
        double x = alignment switch {
            VisibilityAlignment.TopStart or VisibilityAlignment.Start or VisibilityAlignment.BottomStart => 0d,
            VisibilityAlignment.TopEnd or VisibilityAlignment.End or VisibilityAlignment.BottomEnd => 1d,
            _ => 0.5
        };
        double y = alignment switch {
            VisibilityAlignment.TopStart or VisibilityAlignment.Top or VisibilityAlignment.TopEnd => 0d,
            VisibilityAlignment.BottomStart or VisibilityAlignment.Bottom or VisibilityAlignment.BottomEnd => 1d,
            _ => 0.5
        };

        if (flowDirection == FlowDirection.RightToLeft) x = 1 - x;

        return new Point((available.Width - child.Width) * x, (available.Height - child.Height) * y);
    }

    private Transition<EnterExitState>.TransitionAnimationState<T> Observe<T>(
        Transition<EnterExitState>.TransitionAnimationState<T> animation)
    {
        if (_observedAnimations.Add(animation))
            animation.PropertyChanged += AnimationChanged;

        return animation;
    }

    private void AnimationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_disposed || e.PropertyName != "Value") return;

        if (ReferenceEquals(sender, _size))
            MeasureInvalidated?.Invoke();
        else if (ReferenceEquals(sender, _alignmentOffset))
            ArrangeInvalidated?.Invoke();

        else VisualInvalidated?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Transition.TargetChanging -= PrepareConfiguration;
        Transition.PhaseChanged -= PhaseChanged;
        Transition.AnimationsReconfiguring -= UpdateActiveEffects;
        foreach (INotifyPropertyChanged animation in _observedAnimations) animation.PropertyChanged -= AnimationChanged;
        _observedAnimations.Clear();

        _opacityAnimation.Dispose();
        _scaleAnimation.Dispose();
        _transformOriginAnimation.Dispose();
        _slideOffsetAnimation.Dispose();
        _sizeAnimation.Dispose();
        _alignmentOffsetAnimation.Dispose();
        _configurationPreparation?.Dispose();
        _scopeRegistration.Dispose();
    }
}
