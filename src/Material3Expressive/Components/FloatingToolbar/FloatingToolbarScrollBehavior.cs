using System.ComponentModel;
using Avalonia.Controls;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Tokens;

namespace Material3Expressive.Components;

/// <summary>Hoistable floating-toolbar scroll state. Distances are DIP; velocities are DIP/s.</summary>
public sealed class FloatingToolbarState(float initialOffsetLimit = -float.MaxValue, float initialOffset = 0, float initialContentOffset = 0) : INotifyPropertyChanged
{
    private float _offsetLimit = initialOffsetLimit, _offset = initialOffset, _contentOffset = initialContentOffset;
    public float OffsetLimit { get => _offsetLimit; set { if (_offsetLimit != value) { _offsetLimit = value; PropertyChanged?.Invoke(this, new(nameof(OffsetLimit))); } } }
    public float Offset { get => _offset; set { var offset = Math.Clamp(value, OffsetLimit, 0); if (_offset != offset) { _offset = offset; PropertyChanged?.Invoke(this, new(nameof(Offset))); } } }
    public float ContentOffset { get => _contentOffset; set { if (_contentOffset != value) { _contentOffset = value; PropertyChanged?.Invoke(this, new(nameof(ContentOffset))); } } }
    public event PropertyChangedEventHandler? PropertyChanged;
    internal float CollapsedFraction => OffsetLimit != 0 ? Offset / OffsetLimit : 0;
}

/// <summary>Fixed Compose exit-always scroll policy. A behavior owns one attached toolbar's settling lifetime.</summary>
public sealed class FloatingToolbarScrollBehavior
{
    private FloatingToolbar? _owner;
    private VisualAnimationScope? _scope;
    private CancellationTokenSource? _settling;
    public FloatingToolbarExitDirection ExitDirection { get; }
    public FloatingToolbarState State { get; }
    public IAnimationSpec<float>? SnapAnimationSpec { get; }
    public IDecayAnimationSpec<float>? FlingAnimationSpec { get; }
    internal FloatingToolbarScrollBehavior(FloatingToolbarExitDirection direction, FloatingToolbarState state,
        IAnimationSpec<float>? snap, IDecayAnimationSpec<float>? fling) { ExitDirection = direction; State = state; SnapAnimationSpec = snap; FlingAnimationSpec = fling; }
    internal void Attach(FloatingToolbar owner, VisualAnimationScope scope)
    {
        if (_owner is not null && _owner != owner) throw new InvalidOperationException("A floating-toolbar scroll behavior cannot drive two attached toolbars.");
        _owner = owner; _scope = scope.CreateChild("Floating toolbar scroll");
    }
    internal void Detach()
    {
        _settling?.Cancel(); _settling?.Dispose(); _settling = null;
        _scope?.Dispose(); _scope = null; _owner = null;
    }
    /// <summary>Actual consumed vertical distance, in Compose's sign convention (up is negative). Does not consume additional scroll.</summary>
    public void OnPostScroll(float consumedY)
    {
        State.ContentOffset += consumedY;
        State.Offset += consumedY;
    }
    /// <summary>Returns remaining vertical velocity after decay and snap. Zero at an already settled endpoint, matching Compose.</summary>
    public async Task<float> OnPostFlingAsync(float availableY, CancellationToken cancellationToken = default)
    {
        if (availableY > 0 && (State.Offset == 0 || State.Offset == State.OffsetLimit)) State.ContentOffset = 0;
        return await SettleAsync(availableY, cancellationToken);
    }
    internal void Interrupt() => _settling?.Cancel();
    internal async Task<float> SettleAsync(float velocity, CancellationToken cancellationToken = default)
    {
        var fraction = State.CollapsedFraction;
        if (fraction < .01f || fraction == 1) return 0;
        if (_scope is not { IsEnding: false } scope || _owner is null) throw new InvalidOperationException("Attach the scroll behavior to a visible floating toolbar before settling.");
        _settling?.Cancel(); _settling?.Dispose();
        using var settling = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, scope.Lifetime);
        _settling = settling;
        var remainingVelocity = velocity;
        var density = (float)(TopLevel.GetTopLevel(_owner)?.RenderScaling ?? 1);
        try
        {
            if (Math.Abs(velocity) * density > 1)
            {
                // The public state uses DIP. Simulate the upstream spline in physical pixels,
                // with the actual density, then convert each consumed delta back to DIP.
                var units = FlingAnimationSpec is null ? density : 1;
                var lastValue = 0f;
                var state = new AnimationState<float>(new FloatConverter(), 0, AnimationVector.From(new double[] { velocity * units }));
                var spec = FlingAnimationSpec ?? DecayAnimationSpec.SplineBasedDecay<float>(density);
                await state.AnimateDecayAsync(spec, scope.Clock, block: animation =>
                {
                    var delta = (animation.Value - lastValue) / units;
                    var initialOffset = State.Offset;
                    State.Offset = initialOffset + delta;
                    var consumed = Math.Abs(initialOffset - State.Offset);
                    lastValue = animation.Value;
                    remainingVelocity = animation.Velocity / units;
                    // Preserve even the upstream signed-delta comparison.
                    if (Math.Abs(delta - consumed) * density > .5f) animation.CancelAnimation();
                }, cancellationToken: settling.Token);
            }
            if (State.Offset < 0 && State.Offset > State.OffsetLimit)
            {
                var units = SnapAnimationSpec is null ? density : 1;
                var state = new AnimationState<float>(new FloatConverter(), State.Offset * units);
                await state.AnimateToAsync(State.CollapsedFraction < .5f ? 0 : State.OffsetLimit * units, scope.Clock,
                    SnapAnimationSpec ?? FabMotion.Spring(_owner, MaterialTokens.Sys.Motion.Spring.EffectsDefault, MotionScheme.Expressive.DefaultEffects),
                    block: animation => State.Offset = animation.Value / units, cancellationToken: settling.Token);
            }
            return remainingVelocity;
        }
        finally { if (ReferenceEquals(_settling, settling)) _settling = null; }
    }
}
