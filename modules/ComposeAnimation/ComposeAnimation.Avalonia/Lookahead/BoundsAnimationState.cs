using System.ComponentModel;
using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>A complete rectangle target is committed at once, after target measure and placement.</summary>
internal sealed class BoundsAnimationState(Action changed, Action<Exception> failed)
{
    private Animatable<Rect>? _animation;
    private Rect? _requestedTarget;
    private Rect? _failedTarget;
    private long _generation;
    private bool _pending;

    public Rect? Value { get; private set; }
    public bool IsRunning => _pending || _animation?.IsRunning == true;

    public void UpdateTarget(Rect target, FrameScope? runScope, BoundsTransform transform)
    {
        if (!IsFinite(target)) throw new InvalidOperationException("AnimatedBounds requires finite target bounds.");
        Value ??= target; // First layout initializes at its destination, without invoking the policy.
        if (runScope is null)
        {
            Reset();
            Value = target;
            return;
        }
        if (_failedTarget == target || _requestedTarget == target) return;
        _animation ??= CreateAnimation(Value.Value);
        _requestedTarget = target; // Includes requests queued behind the Animatable mutation mutex.
        if (_animation.Value == target && !IsRunning) return;
        _pending = true;
        _ = Animate(runScope, _animation, target, transform, ++_generation);
    }

    private Animatable<Rect> CreateAnimation(Rect initial)
    {
        var animation = new Animatable<Rect>(new RectConverter(), initial,
            VisibilityThresholds.Rect, "Animated bounds");
        animation.PropertyChanged += SampleChanged;
        return animation;
    }

    private async Task Animate(FrameScope scope, Animatable<Rect> animation, Rect target, BoundsTransform transform, long generation)
    {
        try
        {
            var spec = transform.Create(animation.Value, target)
                ?? throw new InvalidOperationException("BoundsTransform returned a null animation spec.");
            // A user policy may synchronously reset/detach its owner.
            if (!IsCurrent()) return;
            await animation.AnimateToAsync(scope, target, spec);
        }
        catch (Exception error)
        {
            if (!IsCurrent()) return;
            Reset();
            Value = target;
            _failedTarget = target;
            changed();
            failed(error);
            return;
        }
        if (!IsCurrent()) return;
        _pending = false;
        Value = animation.Value;
        changed(); // Ensure the final geometry is laid out, including zero-duration animations.

        bool IsCurrent() => generation == _generation && ReferenceEquals(_animation, animation);
    }

    private void SampleChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(Animatable<Rect>.Value) or nameof(Animatable<Rect>.IsRunning))) return;
        if (_animation is not { } animation) return;
        if (!IsFinite(animation.Value))
            throw new InvalidOperationException("AnimatedBounds animation produced non-finite bounds.");
        Value = animation.Value;
        changed();
    }

    public void RetryFailedTarget() => _failedTarget = null;

    public void Reset(bool preserveValue = false)
    {
        ++_generation;
        _pending = false;
        _requestedTarget = null;
        _failedTarget = null;
        if (_animation is { } animation)
        {
            _animation = null;
            animation.PropertyChanged -= SampleChanged;
            animation.Stop();
        }
        if (!preserveValue) Value = null;
    }

    private static bool IsFinite(Rect rect) => double.IsFinite(rect.Left) && double.IsFinite(rect.Top) &&
        double.IsFinite(rect.Right) && double.IsFinite(rect.Bottom) &&
        double.IsFinite(rect.Width) && double.IsFinite(rect.Height);
}
