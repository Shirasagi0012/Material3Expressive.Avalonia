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
