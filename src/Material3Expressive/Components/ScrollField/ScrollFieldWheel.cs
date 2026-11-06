// Fixed Core MouseWheelScrollingLogic.kt / DesktopScrollable.desktop.kt, Apache-2.0.
using System.Threading.Channels;
using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Input;

namespace Material3Expressive.Components;

internal sealed class ScrollFieldWheel(ScrollField owner)
{
    private readonly Channel<(float Delta, ulong Time)> _events = Channel.CreateUnbounded<(float, ulong)>();
    private readonly DifferentialVelocityTracker _velocity = new();
    internal bool IsReceiving { get; private set; }

    internal bool Receive(PointerWheelEventArgs e)
    {
        if (owner.State is not { PageSize: > 0 } state || !owner.IsEffectivelyEnabled) return false;
        var density = state.Density;
        // Avalonia wheel units have the opposite sign to Compose scrollDelta. Its public
        // event has no AWT scrollAmount / block-scroll / precise-device metadata; use the
        // fixed Core fallback for events without that metadata (amount=1, smooth=true).
        var width = MathUtils.DipToPx(owner.Bounds.Width, density);
        var height = MathUtils.DipToPx(owner.Bounds.Height, density);
        float Scale(int length) => OperatingSystem.IsMacOS() ? 10 * density
            : OperatingSystem.IsLinux() ? MathF.Sqrt(length) : length / 20f;
        var x = -(float)e.Delta.X * Scale(width);
        var y = -(float)e.Delta.Y * Scale(height);
        if (MathF.Abs(y) < MathF.Abs(x) || y == 0) return IsReceiving;
        if (y < 0 && state.CurrentPage == 0 && state.OffsetFraction <= 0 ||
            y > 0 && state.CurrentPage == ScrollFieldState.PageCount - 1 && state.OffsetFraction >= 0) return IsReceiving;
        _events.Writer.TryWrite((y, e.Timestamp));
        if (!IsReceiving)
        {
            IsReceiving = true;
            _ = AnimationScope.Get(owner).Launch(async lifetime =>
            {
                try
                {
                    while (_events.Reader.TryPeek(out _) && owner.IsAttachedToVisualTree() && owner.State is { } current)
                    {
                        try
                        {
                            var velocity = 0f;
                            await current.WheelScrollAsync(async (scope, token) => velocity = await Scroll(scope, current, token), lifetime);
                            // The callback launches its fling; a queued event may interrupt it.
                            owner.SettleWheel(velocity);
                        }
                        // Core's receiver survives cancellation of the userScroll child. Events
                        // accepted immediately after a state request must not be discarded.
                        catch (OperationCanceledException) when (!lifetime.IsCancellationRequested) { }
                    }
                }
                catch (OperationCanceledException) { }
                finally { IsReceiving = false; while (_events.Reader.TryRead(out _)) { } }
            }, "Scroll field wheel");
        }
        return true;
    }

    private async Task<float> Scroll(AnimationChildScope scope, ScrollFieldState state, CancellationToken token)
    {
        var first = await _events.Reader.ReadAsync(token);
        _velocity.Add(first.Time, first.Delta);
        var target = first.Delta;
        if (Sum() is { } pending) { target += pending.Delta; _velocity.Add(pending.Time, pending.Delta); }
        if (MathF.Abs(target) < .5f) return 0;
        var animation = new AnimationState<float>(new FloatConverter(), 0);
        var threshold = 6 * state.Density;
        var speed = state.Density;
        var required = true;
        while (required)
        {
            required = false;
            var leftover = target - animation.Value;
            if (MathF.Abs(leftover) < threshold)
            {
                state.Consume(leftover);
                required = await WaitNext(50);
            }
            else
            {
                var instant = Math.Sign(leftover) * threshold;
                state.Consume(instant);
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
                            var consumed = state.Consume(delta);
                            if (MathF.Abs(delta - consumed) >= .5f) { frame.CancelAnimation(); return; }
                            previous += delta;
                        }
                        if (Sum() is not { } next) return;
                        _velocity.Add(next.Time, next.Delta); target += next.Delta;
                        required = MathF.Abs(target - previous) >= .5f;
                        frame.CancelAnimation();
                    }, cancellationToken: token);
                if (!required) required = await WaitNext(50 - duration);
            }
        }
        var velocity = _velocity.Calculate();
        return velocity != 0 ? velocity : Math.Sign(target) * MathF.Min(MathF.Abs(target) / 100, speed) * 1000;

        (float Delta, ulong Time)? Sum()
        {
            if (!_events.Reader.TryRead(out var sum)) return null;
            while (_events.Reader.TryRead(out var next)) sum = (sum.Delta + next.Delta, Math.Max(sum.Time, next.Time));
            return sum;
        }
        async Task<bool> WaitNext(int milliseconds)
        {
            if (milliseconds <= 0) return false;
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(token);
            var read = _events.Reader.ReadAsync(wait.Token).AsTask();
            var timeout = scope.DelayAsync(TimeSpan.FromMilliseconds(milliseconds), wait.Token);
            var frames = BusyFrames(wait.Token);
            try
            {
                if (await Task.WhenAny(read, timeout) != read) { await timeout; return false; }
                var next = await read;
                target = next.Delta; animation = new(new FloatConverter(), 0);
                _velocity.Add(next.Time, next.Delta);
                return MathF.Abs(target) >= .5f;
            }
            finally
            {
                wait.Cancel();
                try { await frames; } catch (OperationCanceledException) { }
            }
        }
        async Task BusyFrames(CancellationToken cancellationToken)
        {
            while (true) await scope.NextFrameAsync(cancellationToken);
        }
    }
}
