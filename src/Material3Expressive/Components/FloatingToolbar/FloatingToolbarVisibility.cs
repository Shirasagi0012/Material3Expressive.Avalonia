using Avalonia;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

internal sealed class FloatingToolbarVisibility : AnimatedVisibility
{
    private FloatingToolbar? _owner;
    private VisualAnimationScope? _targetScope;
    private bool _pending;
    public static readonly StyledProperty<bool> LeadingProperty = AvaloniaProperty.Register<FloatingToolbarVisibility, bool>(nameof(Leading));
    public bool Leading { get => GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var owner = this.FindAncestorOfType<FloatingToolbar>()!;
        _owner = owner;
        var spring = TokenResolver.TryResolve(this, Sys.Motion.Spring.SpatialFast, ActualThemeVariant, out var value) && value is Foundation.Motion.SpringSpec resolved ? resolved : MotionScheme.Expressive.FastSpatial;
        var scale = LayoutHelper.GetLayoutScale(this);
        var spec = new FloatingToolbarSizeSpec((float)spring.DampingRatio, (float)spring.Stiffness, scale,
            () => Scope?.Transition.Segment.InitialState == EnterExitState.PostExit && Scope.Transition.Segment.TargetState == EnterExitState.Visible);
        if (owner.Horizontal)
        {
            Enter = new ExpandHorizontally { ExpandFrom = Leading ? VisibilityHorizontalAlignment.Start : VisibilityHorizontalAlignment.End, Spec = spec };
            Exit = new ShrinkHorizontally { ShrinkTowards = Leading ? VisibilityHorizontalAlignment.End : VisibilityHorizontalAlignment.Start, Spec = spec };
        }
        else
        {
            Enter = new ExpandVertically { ExpandFrom = Leading ? VerticalAlignment.Bottom : VerticalAlignment.Top, Spec = spec };
            Exit = new ShrinkVertically { ShrinkTowards = Leading ? VerticalAlignment.Bottom : VerticalAlignment.Top, Spec = spec };
        }
        Visible = owner.EffectiveExpanded;
        base.OnAttachedToVisualTree(e);
        _targetScope = VisualAnimationScope.Create(this);
        owner.PropertyChanged += Changed;
    }
    private void Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != FloatingToolbar.EffectiveExpandedProperty || _pending || _targetScope is not { IsEnding: false } scope) return;
        _pending = true;
        _ = scope.Launch(async ct => { await scope.Clock.NextFrameAsync(ct); _pending = false; Visible = _owner!.EffectiveExpanded; }, "Floating toolbar visibility target");
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= Changed;
        _targetScope?.Dispose(); _targetScope = null; _owner = null; _pending = false;
        base.OnDetachedFromVisualTree(e);
    }
}

// AnimatedVisibility exposes Size (DIP), whereas Compose's size channel converts its
// sampled vector to IntSize (physical pixels). Keep the shared spring and round only
// the projection, including the sampled start used for an interrupted transition.
internal sealed class FloatingToolbarSizeSpec(double damping, double stiffness, double density, Func<bool> defaultSizeSpec)
    : SpringSpec<Size>(damping, stiffness, new Size(.01 / density, .01 / density)), IFiniteAnimationSpec<Size>
{
    // Keep the SpringSpec identity so Transition carries its velocity on interruption.
    // Material3's supplied motion spec has no IntSize visibility threshold: it uses
    // the vector spring's default 0.01 physical pixels, rather than expandIn's 1px default.
    // EnterExitTransition.sizeTransitionSpec uses its default 400 spring for PostExit -> Visible.
    public new IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<Size> converter) => new PixelProjection(
        defaultSizeSpec() ? new SpringSpec<Size>(stiffness: 400, visibilityThreshold: new Size(1 / density, 1 / density)).Vectorize(converter) : base.Vectorize(converter), density);
    private sealed class PixelProjection(IVectorizedFiniteAnimationSpec spec, double density) : IVectorizedFiniteAnimationSpec
    {
        public TimeSpan GetDuration(ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity) => spec.GetDuration(initial, target, velocity);
        public void GetValue(TimeSpan time, ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination)
        { spec.GetValue(time, initial, target, velocity, destination); for (var i = 0; i < destination.Length; i++) destination[i] = MathUtils.RoundToInt((float)(destination[i] * density)) / density; }
        public void GetVelocity(TimeSpan time, ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination) => spec.GetVelocity(time, initial, target, velocity, destination);
        public void GetEndVelocity(ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination) => spec.GetEndVelocity(initial, target, velocity, destination);
    }
}
