using Avalonia;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class FloatingToolbarVisibility : AnimatedVisibility
{
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> SizeAnimationProperty = AvaloniaProperty.Register<FloatingToolbarVisibility, Foundation.Motion.SpringSpec>(nameof(SizeAnimation));
    public Foundation.Motion.SpringSpec SizeAnimation { get => GetValue(SizeAnimationProperty); set => SetValue(SizeAnimationProperty, value); }
    private FloatingToolbar? _owner;
    private Foundation.Motion.SpringSpec? _configuredAnimation;
    private double _configuredScale;
    public static readonly StyledProperty<bool> LeadingProperty = AvaloniaProperty.Register<FloatingToolbarVisibility, bool>(nameof(Leading));
    public bool Leading { get => GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }
    public FloatingToolbarVisibility() => TargetCommitPhase = AnimationCommitPhase.AfterFrame;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var owner = this.FindAncestorOfType<FloatingToolbar>()!;
        _owner = owner;
        Visible = ConfigureTransitions() && owner.EffectiveExpanded;
        base.OnAttachedToVisualTree(e);
        owner.PropertyChanged += Changed;
    }
    private bool ConfigureTransitions()
    {
        var spring = SizeAnimation;
        if (spring.Stiffness <= 0) return false;
        var scale = LayoutHelper.GetLayoutScale(this);
        if (_configuredAnimation == spring && _configuredScale == scale) return true;
        _configuredAnimation = spring;
        _configuredScale = scale;
        if (_owner!.Horizontal)
        {
            Enter = FloatingToolbarDefaults.HorizontalEnterTransition(this,
                Leading ? VisibilityHorizontalAlignment.Start : VisibilityHorizontalAlignment.End, spring);
            Exit = FloatingToolbarDefaults.HorizontalExitTransition(this,
                Leading ? VisibilityHorizontalAlignment.End : VisibilityHorizontalAlignment.Start, spring);
        }
        else
        {
            Enter = FloatingToolbarDefaults.VerticalEnterTransition(this,
                Leading ? VerticalAlignment.Bottom : VerticalAlignment.Top, spring);
            Exit = FloatingToolbarDefaults.VerticalExitTransition(this,
                Leading ? VerticalAlignment.Bottom : VerticalAlignment.Top, spring);
        }
        return true;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeAnimationProperty) UpdateTarget();
    }
    private void Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FloatingToolbar.EffectiveExpandedProperty) UpdateTarget();
    }
    private void UpdateTarget()
    {
        if (!this.IsAttachedToVisualTree() || _owner is null) return;
        if (ConfigureTransitions()) Visible = _owner.EffectiveExpanded;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= Changed;
        _owner = null; _configuredAnimation = null;
        base.OnDetachedFromVisualTree(e);
    }
}

// AnimatedVisibility exposes Size (DIP), whereas Compose's size channel converts its
// sampled vector to IntSize (physical pixels). Keep the shared spring and round only
// the projection, including the sampled start used for an interrupted transition.
internal sealed class FloatingToolbarSizeSpec(double damping, double stiffness, AnimatedVisibility owner)
    : SpringSpec<Size>((float)damping, (float)stiffness), IFiniteAnimationSpec<Size>
{
    // Keep the SpringSpec identity so Transition carries its velocity on interruption.
    // Material3's supplied motion spec has no IntSize visibility threshold: it uses
    // the vector spring's default 0.01 physical pixels, rather than expandIn's 1px default.
    // EnterExitTransition.sizeTransitionSpec uses its default 400 spring for PostExit -> Visible.
    public new IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<Size> converter)
    {
        // Factories can run before attachment; density and the transition segment belong to
        // the actual visibility host and must be read when its size animation is configured.
        var density = LayoutHelper.GetLayoutScale(owner);
        var returning = owner.Scope?.Transition.Segment.InitialState == EnterExitState.PostExit
            && owner.Scope.Transition.Segment.TargetState == EnterExitState.Visible;
        var spring = returning
            ? new SpringSpec<Size>(stiffness: 400, visibilityThreshold: new Size(1 / density, 1 / density))
            : new SpringSpec<Size>(DampingRatio, Stiffness, new Size(.01 / density, .01 / density));
        return new PixelProjection(spring.Vectorize(converter), density);
    }
    private sealed class PixelProjection(IVectorizedFiniteAnimationSpec spec, double density) : IVectorizedFiniteAnimationSpec
    {
        public TimeSpan GetDuration(ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity) => spec.GetDuration(initial, target, velocity);
        public void GetValue(TimeSpan time, ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination)
        { spec.GetValue(time, initial, target, velocity, destination); for (var i = 0; i < destination.Length; i++) destination[i] = MathUtils.RoundToInt((float)(destination[i] * density)) / density; }
        public void GetVelocity(TimeSpan time, ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination) => spec.GetVelocity(time, initial, target, velocity, destination);
        public void GetEndVelocity(ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination) => spec.GetEndVelocity(initial, target, velocity, destination);
    }
}
