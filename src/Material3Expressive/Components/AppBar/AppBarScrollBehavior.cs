/* Translated from AndroidX AppBar.kt, Copyright The Android Open Source Project.
 * Apache License 2.0. Fixed source: docs/upstream-baseline.json. */
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Data;
using Avalonia.Metadata;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public enum TopAppBarScrollMode
{
    Pinned,
    EnterAlways,
    ExitUntilCollapsed
}

/// <summary>Configures top bar scrolling. Can be used as an object element or a markup extension.</summary>
public sealed class TopAppBarScrollBehavior : StyledElement
{
    public static readonly StyledProperty<TopAppBarScrollMode> ModeProperty =
        AvaloniaProperty.Register<TopAppBarScrollBehavior, TopAppBarScrollMode>(nameof(Mode));
    public static readonly DirectProperty<TopAppBarScrollBehavior, TopAppBarState> StateProperty =
        AvaloniaProperty.RegisterDirect<TopAppBarScrollBehavior, TopAppBarState>(nameof(State),
            owner => owner.State, (owner, value) => owner.State = value);
    public static readonly StyledProperty<bool> CanScrollProperty =
        AvaloniaProperty.Register<TopAppBarScrollBehavior, bool>(nameof(CanScroll), true);
    public static readonly StyledProperty<bool?> IsScrollingContentAtStartProperty =
        AvaloniaProperty.Register<TopAppBarScrollBehavior, bool?>(nameof(IsScrollingContentAtStart));
    public static readonly StyledProperty<IAnimationSpec<float>?> SnapAnimationSpecProperty =
        AvaloniaProperty.Register<TopAppBarScrollBehavior, IAnimationSpec<float>?>(nameof(SnapAnimationSpec),
            AppBarSettle.TopSnapDefault);
    public static readonly StyledProperty<IDecayAnimationSpec<float>?> FlingAnimationSpecProperty =
        AvaloniaProperty.Register<TopAppBarScrollBehavior, IDecayAnimationSpec<float>?>(nameof(FlingAnimationSpec),
            AppBarSettle.FlingDefault);
    public static readonly DirectProperty<TopAppBarScrollBehavior, bool> IsPinnedProperty =
        AvaloniaProperty.RegisterDirect<TopAppBarScrollBehavior, bool>(nameof(IsPinned), owner => owner.IsPinned);

    private readonly TopAppBarState _defaultState = new();
    private TopAppBarState _state;
    private bool _contentAtStart = true;
    private TopAppBar? Host => Parent as TopAppBar;

    public TopAppBarScrollBehavior() => _state = _defaultState;
    public TopAppBarScrollBehavior(TopAppBarScrollMode mode) : this() => Mode = mode;
    public TopAppBarScrollBehavior(BindingBase mode) : this() => this.Bind(ModeProperty, mode);
    public TopAppBarScrollBehavior ProvideValue(IServiceProvider serviceProvider) => this;

    [ConstructorArgument("mode")]
    public TopAppBarScrollMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    [AllowNull]
    public TopAppBarState State
    {
        get => _state;
        set
        {
            var state = value ?? _defaultState;
            state.IsScrollingContentAtStart = IsScrollingContentAtStart ?? _contentAtStart;
            SetAndRaise(StateProperty, ref _state, state);
        }
    }
    public bool CanScroll { get => GetValue(CanScrollProperty); set => SetValue(CanScrollProperty, value); }
    /// <summary>Null follows the connected body viewport; a value explicitly overrides its position.</summary>
    public bool? IsScrollingContentAtStart
    {
        get => GetValue(IsScrollingContentAtStartProperty);
        set => SetValue(IsScrollingContentAtStartProperty, value);
    }
    /// <summary>Unassigned uses the owner's theme spring. Explicit null disables snapping.</summary>
    public IAnimationSpec<float>? SnapAnimationSpec
    {
        get => GetValue(SnapAnimationSpecProperty);
        set => SetValue(SnapAnimationSpecProperty, value);
    }
    /// <summary>Unassigned uses spline decay at the owner's scale. Explicit null disables flinging.</summary>
    public IDecayAnimationSpec<float>? FlingAnimationSpec
    {
        get => GetValue(FlingAnimationSpecProperty);
        set => SetValue(FlingAnimationSpecProperty, value);
    }
    public bool IsPinned => Mode == TopAppBarScrollMode.Pinned;

    internal IAnimationSpec<float>? GetSnapAnimationSpec() => IsPinned ? null :
        ReferenceEquals(SnapAnimationSpec, AppBarSettle.TopSnapDefault)
            ? AppBarSettle.DefaultSnap(Host, false) : SnapAnimationSpec;
    internal IDecayAnimationSpec<float>? GetFlingAnimationSpec() => IsPinned ? null :
        ReferenceEquals(FlingAnimationSpec, AppBarSettle.FlingDefault)
            ? DecayAnimationSpec.SplineBasedDecay<float>((float)(Host is { } host ? LayoutHelper.GetLayoutScale(host) : 1))
            : FlingAnimationSpec;
    internal Task<Vector> SettleAsync(float velocity, IAnimationFrameClock clock,
        CancellationToken cancellationToken = default) =>
        AppBarSettle.RunAsync(State, velocity, GetFlingAnimationSpec(), GetSnapAnimationSpec(), clock, cancellationToken);

    internal void UpdateContentAtStart(bool value)
    {
        _contentAtStart = value;
        State.IsScrollingContentAtStart = IsScrollingContentAtStart ?? value;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ModeProperty)
            RaisePropertyChanged(IsPinnedProperty, change.GetOldValue<TopAppBarScrollMode>() == TopAppBarScrollMode.Pinned, IsPinned);
        if (change.Property == IsScrollingContentAtStartProperty)
            State.IsScrollingContentAtStart = IsScrollingContentAtStart ?? _contentAtStart;
    }
}

/// <summary>Hides a bottom bar when its body scrolls, using the ExitAlways behavior.</summary>
public sealed class BottomAppBarScrollBehavior : StyledElement
{
    public static readonly DirectProperty<BottomAppBarScrollBehavior, BottomAppBarState> StateProperty =
        AvaloniaProperty.RegisterDirect<BottomAppBarScrollBehavior, BottomAppBarState>(nameof(State),
            owner => owner.State, (owner, value) => owner.State = value);
    public static readonly StyledProperty<bool> CanScrollProperty =
        AvaloniaProperty.Register<BottomAppBarScrollBehavior, bool>(nameof(CanScroll), true);
    public static readonly StyledProperty<IAnimationSpec<float>?> SnapAnimationSpecProperty =
        AvaloniaProperty.Register<BottomAppBarScrollBehavior, IAnimationSpec<float>?>(nameof(SnapAnimationSpec),
            AppBarSettle.BottomSnapDefault);
    public static readonly StyledProperty<IDecayAnimationSpec<float>?> FlingAnimationSpecProperty =
        AvaloniaProperty.Register<BottomAppBarScrollBehavior, IDecayAnimationSpec<float>?>(nameof(FlingAnimationSpec),
            AppBarSettle.FlingDefault);

    private readonly BottomAppBarState _defaultState = new();
    private BottomAppBarState _state;
    private BottomAppBar? Host => Parent as BottomAppBar;

    public BottomAppBarScrollBehavior() => _state = _defaultState;
    public BottomAppBarScrollBehavior ProvideValue(IServiceProvider serviceProvider) => this;
    [AllowNull]
    public BottomAppBarState State
    {
        get => _state;
        set => SetAndRaise(StateProperty, ref _state, value ?? _defaultState);
    }
    public bool CanScroll { get => GetValue(CanScrollProperty); set => SetValue(CanScrollProperty, value); }
    public IAnimationSpec<float>? SnapAnimationSpec
    {
        get => GetValue(SnapAnimationSpecProperty);
        set => SetValue(SnapAnimationSpecProperty, value);
    }
    public IDecayAnimationSpec<float>? FlingAnimationSpec
    {
        get => GetValue(FlingAnimationSpecProperty);
        set => SetValue(FlingAnimationSpecProperty, value);
    }
    public bool IsPinned => false;

    internal IAnimationSpec<float>? GetSnapAnimationSpec() =>
        ReferenceEquals(SnapAnimationSpec, AppBarSettle.BottomSnapDefault)
            ? AppBarSettle.DefaultSnap(Host, true) : SnapAnimationSpec;
    internal IDecayAnimationSpec<float>? GetFlingAnimationSpec() =>
        ReferenceEquals(FlingAnimationSpec, AppBarSettle.FlingDefault)
            ? DecayAnimationSpec.SplineBasedDecay<float>((float)(Host is { } host ? LayoutHelper.GetLayoutScale(host) : 1))
            : FlingAnimationSpec;
    internal Task<Vector> SettleAsync(float velocity, IAnimationFrameClock clock,
        CancellationToken cancellationToken = default) =>
        AppBarSettle.RunAsync(State, velocity, GetFlingAnimationSpec(), GetSnapAnimationSpec(), clock, cancellationToken);
}

internal static class AppBarSettle
{
    // Real specs also serve as the property defaults. Their identities distinguish unset/reset
    // from explicit null, so both changes participate in ordinary Avalonia notifications.
    internal static readonly IAnimationSpec<float> TopSnapDefault = DefaultSnap(null, false);
    internal static readonly IAnimationSpec<float> BottomSnapDefault = DefaultSnap(null, true);
    internal static readonly IDecayAnimationSpec<float> FlingDefault = DecayAnimationSpec.SplineBasedDecay<float>(1);
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
