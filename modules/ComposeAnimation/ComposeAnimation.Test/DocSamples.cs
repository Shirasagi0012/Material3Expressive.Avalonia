using Avalonia.Controls;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

// Compile-only samples quoted by docs/usage. Each class mirrors one document; keep them in sync.

/// <summary>docs/usage/migration-from-visual-animation-scope.md, section 2 ("新 · New").</summary>
internal sealed class MigrationSamples : Border
{
    private static readonly TweenSpec<float> Spec = new(TimeSpan.FromMilliseconds(200));
    private readonly Animatable<float> _alpha = new(new FloatConverter(), 0f, VisibilityThresholds.Float);

    private void Animate(float target) => _ = _alpha.AnimateToAsync(AnimationScope.Get(this), target, Spec);

    // Former `{ IsEnding: false }` guard on a stored scope.
    private static bool IsLive(AnimationScope scope) => !scope.Lifetime.IsCancellationRequested;
}

/// <summary>docs/usage/animation-scope.md samples.</summary>
internal static class AnimationScopeSamples
{
    public static void Hover(Button button, Action<double> setLayerOpacity)
    {
        var motion = AnimationScope.Get(button);
        var alpha = Animatable.Create(0d, label: "State layer");
        motion.SubscribeValue(alpha, setLayerOpacity);
        _ = motion.Launch(ct => alpha.AnimateToAsync(motion, .12,
            new TweenSpec<double>(TimeSpan.FromMilliseconds(75)), cancellationToken: ct), "Hover");
        AnimationScope.AddScopeChangedHandler(button, (_, e) =>
        {
            if (e.NewScope is { } scope) _ = alpha.AnimateToAsync(scope, .12);
        });
    }

    public static async Task Ripple(AnimationScope motion, Action<double> setRadius)
    {
        using var ripple = motion.CreateChild("Ripple");
        var radius = Animatable.Create(0d);
        ripple.SubscribeValue(radius, setRadius);
        await ripple.Launch(ct => radius.AnimateToAsync(ripple, 100, cancellationToken: ct), "Grow");
    }

    public static async Task Speed(Border demo)
    {
        AnimationScope.SetDurationScale(demo, 4);
        await Animated.SnapToTargetAsync(demo, Avalonia.Visual.OpacityProperty);
    }
}

/// <summary>docs/usage/animated-state.md: public factories and template connection lifetime.</summary>
internal sealed class AnimatedStateSamples : Avalonia.Controls.Primitives.TemplatedControl
{
    private readonly AnimatedState<double> _opacity;
    private IDisposable? _connection;

    public AnimatedStateSamples() => _opacity = AnimatedState.Create(this, 1d, label: "Content opacity");

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _connection?.Dispose();
        _connection = e.NameScope.Find<Border>("PART_Content") is { } target
            ? _opacity.Bind(target, Avalonia.Visual.OpacityProperty) : null;
    }

    public void Update(bool active) => _opacity.UpdateTarget(active ? 1 : .4,
        new TweenSpec<double>(TimeSpan.FromMilliseconds(150)));
}
