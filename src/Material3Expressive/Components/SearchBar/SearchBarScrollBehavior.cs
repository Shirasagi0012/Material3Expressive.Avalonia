// SearchBar.kt / SearchBarScrollState / EnterAlwaysSearchBarScrollBehavior; Apache-2.0.
using Avalonia;
using Avalonia.Layout;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Application-owned search app bar offsets, in DIP. Changing the limit does not reclamp the offset.</summary>
public sealed class SearchBarScrollState : AvaloniaObject
{
    public static readonly DirectProperty<SearchBarScrollState, float> ScrollOffsetProperty = AvaloniaProperty.RegisterDirect<SearchBarScrollState, float>(nameof(ScrollOffset), s => s.ScrollOffset, (s, v) => s.ScrollOffset = v);
    public static readonly DirectProperty<SearchBarScrollState, float> ScrollOffsetLimitProperty = AvaloniaProperty.RegisterDirect<SearchBarScrollState, float>(nameof(ScrollOffsetLimit), s => s.ScrollOffsetLimit, (s, v) => s.ScrollOffsetLimit = v);
    public static readonly DirectProperty<SearchBarScrollState, float> ContentOffsetProperty = AvaloniaProperty.RegisterDirect<SearchBarScrollState, float>(nameof(ContentOffset), s => s.ContentOffset, (s, v) => s.ContentOffset = v);
    private float _offset, _limit, _content;
    public SearchBarScrollState() : this(-float.MaxValue, 0, 0) { }
    public SearchBarScrollState(float initialScrollOffsetLimit, float initialScrollOffset, float initialContentOffset)
    { _limit = initialScrollOffsetLimit; _offset = initialScrollOffset; _content = initialContentOffset; }
    public float ScrollOffset { get => _offset; set => SetAndRaise(ScrollOffsetProperty, ref _offset, Math.Clamp(value, ScrollOffsetLimit, 0)); }
    public float ScrollOffsetLimit { get => _limit; set => SetAndRaise(ScrollOffsetLimitProperty, ref _limit, value); }
    public float ContentOffset { get => _content; set => SetAndRaise(ContentOffsetProperty, ref _content, value); }
    internal float OverlappedFraction => ScrollOffsetLimit != 0 ? 1 - Math.Clamp(ScrollOffsetLimit - ContentOffset, ScrollOffsetLimit, 0) / ScrollOffsetLimit : 0;
}

/// <summary>Enter-always search app bar policy. The host supplies nested pre/post scroll and remaining fling velocity.</summary>
public sealed class SearchBarScrollBehavior : AvaloniaObject
{
    public static readonly StyledProperty<bool> CanScrollProperty = AvaloniaProperty.Register<SearchBarScrollBehavior, bool>(nameof(CanScroll), true);
    public static readonly StyledProperty<bool> ReverseLayoutProperty = AvaloniaProperty.Register<SearchBarScrollBehavior, bool>(nameof(ReverseLayout));
    public bool CanScroll { get => GetValue(CanScrollProperty); set => SetValue(CanScrollProperty, value); }
    public bool ReverseLayout { get => GetValue(ReverseLayoutProperty); set => SetValue(ReverseLayoutProperty, value); }
    public SearchBarScrollState State { get; }
    public IAnimationSpec<float>? SnapAnimationSpec { get; }
    public IDecayAnimationSpec<float>? FlingAnimationSpec { get; }
    private AppBarWithSearchLayout? _owner;
    private bool? _directDragEnabled;
    // Upstream builds its draggable modifier once; only nested callbacks read canScroll live.
    internal bool DirectDragEnabled => _directDragEnabled ?? false;
    private AnimationChildScope? _scope;
    private CancellationTokenSource? _settling;
    public SearchBarScrollBehavior() : this(new SearchBarScrollState()) { }
    /// <param name="snapAnimationSpec">Null uses the mounted app bar's default effects spring.</param>
    /// <param name="flingAnimationSpec">Null uses spline decay at the mounted app bar's density.</param>
    public SearchBarScrollBehavior(SearchBarScrollState state, IAnimationSpec<float>? snapAnimationSpec = null, IDecayAnimationSpec<float>? flingAnimationSpec = null)
    { State = state ?? throw new ArgumentNullException(nameof(state)); SnapAnimationSpec = snapAnimationSpec; FlingAnimationSpec = flingAnimationSpec; }
    internal void Attach(AppBarWithSearchLayout owner)
    {
        if (_owner is not null) throw new InvalidOperationException("A search scroll behavior can have only one mounted app bar.");
        _directDragEnabled ??= CanScroll;
        _owner = owner; _scope = AnimationScope.Get(owner).CreateChild("Search app bar scrolling");
    }
    internal void Detach()
    {
        Interrupt(); _scope?.Dispose(); _scope = null; _owner = null;
    }
    internal void Interrupt() => _settling?.Cancel();
    /// <summary>Returns consumed vertical distance. Positive is downward; distances are DIP.</summary>
    public float OnPreScroll(float availableY)
    {
        VerifyAccess();
        if (!CanScroll) return 0;
        var previous = State.ScrollOffset;
        State.ScrollOffset += availableY;
        // Upstream consumes the entire available delta when the clamped offset changes.
        return !ReverseLayout && previous != State.ScrollOffset ? availableY : 0;
    }
    /// <summary>Receives the child's consumed and remaining distances; returns additional consumption.</summary>
    public float OnPostScroll(float consumedY, float availableY)
    {
        VerifyAccess();
        if (!CanScroll) return 0;
        if (ReverseLayout && availableY > 0)
        {
            State.ScrollOffset += availableY; State.ContentOffset += availableY;
            return availableY;
        }
        if (!ReverseLayout) { State.ScrollOffset += consumedY; State.ContentOffset += consumedY; }
        return 0;
    }
    /// <summary>Returns remaining vertical velocity in DIP/s; zero at an already settled endpoint.</summary>
    public Task<float> OnPostFlingAsync(float availableY, CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        return CanScroll ? SettleAsync(availableY, cancellationToken) : Task.FromResult(0f);
    }
    internal async Task<float> SettleAsync(float velocity, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return velocity;
        var fraction = State.ScrollOffsetLimit != 0 ? State.ScrollOffset / State.ScrollOffsetLimit : 0;
        if (fraction < .01f || fraction == 1) return 0;
        var owner = _owner ?? throw new InvalidOperationException("Mount an AppBarWithSearch before settling its scroll behavior.");
        var scope = _scope!;
        Interrupt();
        using var settling = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.Lifetime);
        _settling = settling;
        var density = (float)LayoutHelper.GetLayoutScale(owner);
        var remainingVelocity = velocity;
        try
        {
            if (Math.Abs(velocity) * density > 1)
            {
                var units = FlingAnimationSpec is null ? density : 1;
                var last = 0f;
                var animation = new AnimationState<float>(new FloatConverter(), 0, AnimationVector.From([velocity * units]));
                await SuspendAnimation.AnimateDecayAsync(scope, animation, FlingAnimationSpec ?? DecayAnimationSpec.SplineBasedDecay<float>(density), block: frame =>
                {
                    var delta = (frame.Value - last) / units;
                    var initial = State.ScrollOffset;
                    State.ScrollOffset = initial + delta;
                    var consumed = Math.Abs(initial - State.ScrollOffset);
                    last = frame.Value; remainingVelocity = frame.Velocity / units;
                    if (Math.Abs(delta - consumed) * density > .5f) frame.CancelAnimation();
                }, cancellationToken: settling.Token);
            }
            if (settling.IsCancellationRequested) return remainingVelocity;
            if (State.ScrollOffsetLimit < State.ScrollOffset && State.ScrollOffset < 0)
            {
                var units = SnapAnimationSpec is null ? density : 1;
                var spring = owner.ScrollSnapAnimation;
                var spec = SnapAnimationSpec ?? (spring is { } configured
                    ? new SpringSpec<float>(configured.DampingRatio, configured.Stiffness)
                    : throw new InvalidOperationException("The search scroll motion theme is not ready."));
                var animation = new AnimationState<float>(new FloatConverter(), State.ScrollOffset * units);
                // Preserve the fraction captured BEFORE decay, unlike FloatingToolbar's policy.
                await SuspendAnimation.AnimateToAsync(scope, animation, fraction < .5f ? 0 : State.ScrollOffsetLimit * units, spec,
                    block: frame => State.ScrollOffset = frame.Value / units, cancellationToken: settling.Token);
            }
            return remainingVelocity;
        }
        finally { if (ReferenceEquals(_settling, settling)) _settling = null; }
    }
}
