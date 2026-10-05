/* Translated from AndroidX AppBar.kt, Copyright The Android Open Source Project.
 * Apache License 2.0. Fixed source: docs/upstream-baseline.json. */
using Avalonia;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public abstract class TopAppBarScrollBehavior
{
    public abstract TopAppBarState State { get; }
    public abstract bool IsPinned { get; }
    public abstract IAnimationSpec<float>? SnapAnimationSpec { get; }
    public abstract IDecayAnimationSpec<float>? FlingAnimationSpec { get; }

    public Task<Vector> SettleAsync(float velocity, IAnimationFrameClock clock,
        CancellationToken cancellationToken = default) =>
        AppBarSettle.RunAsync(State, velocity, FlingAnimationSpec, SnapAnimationSpec, clock, cancellationToken);
}

public abstract class BottomAppBarScrollBehavior
{
    public abstract BottomAppBarState State { get; }
    public abstract bool IsPinned { get; }
    public abstract IAnimationSpec<float>? SnapAnimationSpec { get; }
    public abstract IDecayAnimationSpec<float>? FlingAnimationSpec { get; }

    public Task<Vector> SettleAsync(float velocity, IAnimationFrameClock clock,
        CancellationToken cancellationToken = default) =>
        AppBarSettle.RunAsync(State, velocity, FlingAnimationSpec, SnapAnimationSpec, clock, cancellationToken);
}

internal enum TopScrollKind
{
    Pinned,
    EnterAlways,
    ExitUntilCollapsed
}

internal sealed class DefaultTopAppBarScrollBehavior : TopAppBarScrollBehavior
{
    public DefaultTopAppBarScrollBehavior(
        TopScrollKind kind,
        TopAppBarState state,
        Func<bool>? canScroll,
        Func<bool>? isScrollingContentAtStart,
        IAnimationSpec<float>? snap,
        IDecayAnimationSpec<float>? fling,
        bool defaultSpecs = false,
        float? density = null)
    {
        Kind = kind;
        State = state;
        CanScroll = canScroll ?? (() => true);
        if (isScrollingContentAtStart is { })
            state.IsScrollingContentAtStart = isScrollingContentAtStart;

        SnapAnimationSpec = snap;
        FlingAnimationSpec = fling;
        _defaultSpecs = defaultSpecs;
        _density = density;
    }

    internal TopScrollKind Kind { get; }
    private Func<bool> CanScroll { get; }

    internal bool CanScrollForComposition() => CanScroll();
    public override TopAppBarState State { get; }
    public override bool IsPinned => Kind == TopScrollKind.Pinned;

    private readonly bool _defaultSpecs;
    private readonly float? _density;
    private WeakReference<Layoutable>? _host;

    internal void AttachDefaults(Layoutable host)
    {
        if (_defaultSpecs && !(host is AppBarScrollScope && Host is TopAppBar bar && bar.IsAttachedToVisualTree()))
            _host = new(host);
    }

    private Layoutable? Host => _host is { } && _host.TryGetTarget(out var host) ? host : null;

    public override IAnimationSpec<float>? SnapAnimationSpec =>
        IsPinned ? null : _defaultSpecs ? AppBarSettle.DefaultSnap(Host, false) : field;

    public override IDecayAnimationSpec<float>? FlingAnimationSpec =>
        IsPinned
            ? null
            : _defaultSpecs
                ? DecayAnimationSpec.SplineBasedDecay<float>(
                    _density ?? (float)(Host is { } host ? LayoutHelper.GetLayoutScale(host) : 1))
                : field;
}

internal sealed class DefaultBottomAppBarScrollBehavior : BottomAppBarScrollBehavior
{
    public DefaultBottomAppBarScrollBehavior(
        BottomAppBarState state,
        Func<bool>? canScroll,
        IAnimationSpec<float>? snap,
        IDecayAnimationSpec<float>? fling,
        bool defaultSpecs = false,
        float? density = null)
    {
        State = state;
        CanScroll = canScroll ?? (() => true);
        SnapAnimationSpec = snap;
        FlingAnimationSpec = fling;
        _defaultSpecs = defaultSpecs;
        _density = density;
    }

    private Func<bool> CanScroll { get; }

    internal bool CanScrollForComposition() => CanScroll();
    public override BottomAppBarState State { get; }
    public override bool IsPinned => false;

    private readonly bool _defaultSpecs;
    private readonly float? _density;
    private WeakReference<Layoutable>? _host;

    internal void AttachDefaults(Layoutable host)
    {
        if (_defaultSpecs && !(host is AppBarScrollScope && Host is BottomAppBar bar && bar.IsAttachedToVisualTree()))
            _host = new(host);
    }

    private Layoutable? Host => _host is { } && _host.TryGetTarget(out var host) ? host : null;

    public override IAnimationSpec<float>? SnapAnimationSpec =>
        _defaultSpecs ? AppBarSettle.DefaultSnap(Host, true) : field;

    public override IDecayAnimationSpec<float>? FlingAnimationSpec =>
        _defaultSpecs
            ? DecayAnimationSpec.SplineBasedDecay<float>(
                _density ?? (float)(Host is { } host ? LayoutHelper.GetLayoutScale(host) : 1))
            : field;
}

internal static class AppBarSettle
{
    internal static IAnimationSpec<float> DefaultSnap(Visual? host, bool bottom)
    {
        var spring = host is { } &&
                     TokenResolver.TryResolve(
                         host,
                         bottom ? Sys.Motion.Spring.SpatialFast : Sys.Motion.Spring.EffectsDefault,
                         host.ActualThemeVariant,
                         out var value) &&
                     value is SpringSpec spec
            ? spec
            : bottom
                ? MotionScheme.Expressive.FastSpatial
                : MotionScheme.Expressive.DefaultEffects;

        // Compose SpringSpec's public parameters are Float. Retain their precision
        // before entering ComposeAnimation's double-valued spring solver.
        return new SpringSpec<float>((float)spring.DampingRatio, (float)spring.Stiffness);
    }

    // Top and bottom settle functions in AppBar.kt are identical.
    internal static async Task<Vector> RunAsync(
        IHidingAppBarState state,
        float velocity,
        IDecayAnimationSpec<float>? fling,
        IAnimationSpec<float>? snap,
        IAnimationFrameClock clock,
        CancellationToken cancellationToken)
    {
        if (state.CollapsedFraction is < .01f or 1)
            return default;

        var remainingVelocity = velocity;
        if (fling is { } && MathF.Abs(velocity) > 1)
        {
            var lastValue = 0f;
            var animation = new AnimationState<float>(new FloatConverter(), 0, AnimationVector.From([velocity]));
            await animation.AnimateDecayAsync(fling, clock, block: frame =>
            {
                var delta = frame.Value - lastValue;
                var initial = state.HeightOffset;
                state.HeightOffset = initial + delta;
                var consumed = MathF.Abs(initial - state.HeightOffset);
                lastValue = frame.Value;
                remainingVelocity = frame.Velocity;

                // Keep the signed delta / absolute consumed comparison from the fixed baseline.
                if (MathF.Abs(delta - consumed) > .5f)
                    frame.CancelAnimation();
            }, cancellationToken: cancellationToken);
        }

        if (snap is { } && state.HeightOffset < 0 && state.HeightOffset > state.HeightOffsetLimit)
        {
            var animation = new AnimationState<float>(new FloatConverter(), state.HeightOffset);
            await animation.AnimateToAsync(
                state.CollapsedFraction < .5f ? 0 : state.HeightOffsetLimit,
                clock,
                snap,
                block: frame => state.HeightOffset = frame.Value,
                cancellationToken: cancellationToken);
        }

        // This is the connection's consumed velocity, despite the upstream local variable name.
        return new Vector(0, remainingVelocity);
    }
}
