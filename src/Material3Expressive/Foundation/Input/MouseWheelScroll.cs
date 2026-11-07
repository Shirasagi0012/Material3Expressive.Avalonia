// Fixed Core MouseWheelScrollingLogic.kt / DesktopScrollable.desktop.kt, Apache-2.0.
using Avalonia;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Foundation.Input;

// Shared one-axis wheel animation. The owner retains scroll mutation, bounds and fling policy.
internal sealed class MouseWheelScroll
{
    private readonly Queue<(float Delta, ulong Time)> _events = new();
    private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DifferentialVelocityTracker _velocity = new();
    internal bool HasPending => _events.Count != 0;
    internal void Enqueue(float delta, ulong timestamp)
    {
        _events.Enqueue((delta, timestamp));
        _changed.TrySetResult();
    }
    internal void Clear() => _events.Clear();

    // Avalonia has opposite wheel signs and no AWT amount/block/precision metadata.
    // Preserve Core's no-metadata fallback, including dominant-axis selection (ties vertical).
    internal static float Delta(Vector delta, Size bounds, float density, bool vertical)
    {
        float Scale(int length) => OperatingSystem.IsMacOS() ? 10 * density
            : OperatingSystem.IsLinux() ? MathF.Sqrt(length) : length / 20f;
        var x = -(float)delta.X * Scale(MathUtils.DipToPx(bounds.Width, density));
        var y = -(float)delta.Y * Scale(MathUtils.DipToPx(bounds.Height, density));
        return MathF.Abs(y) >= MathF.Abs(x) ? vertical ? y : 0 : vertical ? 0 : x;
    }

    internal async Task<float> ScrollAsync(AnimationChildScope scope, float density, Func<float, float> consume, CancellationToken token)
    {
        if (token.IsCancellationRequested || !_events.TryDequeue(out var first)) return 0;
        _velocity.Add(first.Time, first.Delta);
        var target = first.Delta;
        if (Sum() is { } pending) { target += pending.Delta; _velocity.Add(pending.Time, pending.Delta); }
        if (MathF.Abs(target) < .5f) return 0;
        var animation = new AnimationState<float>(new FloatConverter(), 0);
        var threshold = 6 * density;
        var speed = density;
        var required = true;
        while (required && !token.IsCancellationRequested)
        {
            required = false;
            var leftover = target - animation.Value;
            if (MathF.Abs(leftover) < threshold)
            {
                consume(leftover);
                required = await WaitNext(50);
            }
            else
            {
                var instant = Math.Sign(leftover) * threshold;
                consume(instant);
                var current = animation.Value + instant;
                animation = new(new FloatConverter(), current, AnimationVector.From(animation.VelocityVector),
                    animation.LastFrameTime, animation.FinishedTime, animation.IsRunning);
                var duration = Math.Min(MathUtils.RoundToInt(MathF.Abs(target - current) / speed), 100);
                var previous = current;
                await SuspendAnimation.AnimateToAsync(scope, animation, target,
                    new TweenSpec<float>(TimeSpan.FromMilliseconds(duration), easing: new Avalonia.Animation.Easings.LinearEasing()), sequentialAnimation: true,
                    block: frame =>
                    {
                        var delta = frame.Value - previous;
                        if (MathF.Abs(delta) >= .5f)
                        {
                            var consumed = consume(delta);
                            if (MathF.Abs(delta - consumed) >= .5f) { frame.CancelAnimation(); return; }
                            previous += delta;
                        }
                        if (Sum() is not { } next) return;
                        _velocity.Add(next.Time, next.Delta); target += next.Delta;
                        required = MathF.Abs(target - previous) >= .5f;
                        frame.CancelAnimation();
                    }, cancellationToken: token);
                if (token.IsCancellationRequested) return 0;
                if (!required) required = await WaitNext(50 - duration);
            }
        }
        var velocity = _velocity.Calculate();
        return velocity != 0 ? velocity : Math.Sign(target) * MathF.Min(MathF.Abs(target) / 100, speed) * 1000;

        (float Delta, ulong Time)? Sum()
        {
            _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_events.TryDequeue(out var sum)) return null;
            while (_events.TryDequeue(out var next)) sum = (sum.Delta + next.Delta, Math.Max(sum.Time, next.Time));
            return sum;
        }
        async Task<bool> WaitNext(int milliseconds)
        {
            if (milliseconds <= 0) return false;
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(token);
            if (token.IsCancellationRequested) return false;
            var read = scope.WaitForSignalAsync(_changed.Task, wait.Token);
            var timeout = scope.DelayAsync(TimeSpan.FromMilliseconds(milliseconds), wait.Token);
            var frames = scope.RunFramesAsync(_ => true, wait.Token);
            try
            {
                if (await Task.WhenAny(read, timeout) != read) { await timeout; return false; }
                if (!await read || token.IsCancellationRequested || !_events.TryDequeue(out var next)) return false;
                _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
                target = next.Delta; animation = new(new FloatConverter(), 0);
                _velocity.Add(next.Time, next.Delta);
                return MathF.Abs(target) >= .5f;
            }
            finally
            {
                wait.Cancel();
                await frames;
            }
        }
    }
}
