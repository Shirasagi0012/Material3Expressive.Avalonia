using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

// Both AppBar.kt draggable blocks update heightOffset directly, then run the same settle path.
internal sealed class AppBarDirectDrag : IDisposable
{
    private readonly Control _owner;
    private AnimationChildScope? _motion;
    private readonly AppBarVerticalGesture _gesture;
    private CancellationTokenSource? _run;

    internal AppBarDirectDrag(
        Control owner,
        Func<IHidingAppBarState?> state,
        Func<float, FrameScope, CancellationToken, Task<Vector>> settle,
        float deltaSign = 1)
    {
        _owner = owner;
        _gesture = new AppBarVerticalGesture(
            () => state() is { },
            () =>
            {
                _run?.Cancel();
                foreach (var scope in owner.GetVisualAncestors().OfType<AppBarScrollScope>())
                    scope.Interrupt();
            },
            (delta, _) =>
            {
                if (state() is not { } active)
                    return default;

                var old = active.HeightOffset;
                active.HeightOffset += deltaSign * delta * (float)LayoutHelper.GetLayoutScale(owner);
                return new Vector(0, active.HeightOffset - old);
            },
            (velocity, gestureId, cancelled) =>
            {
                if (cancelled || state() is null)
                    return;

                _run?.Cancel();
                _run?.Dispose();
                _run = new CancellationTokenSource();
                var run = _run;
                var scopes = owner.GetVisualAncestors()
                    .OfType<AppBarScrollScope>()
                    .ToArray();
                foreach (var scope in scopes)
                    scope.Register(run);

                if (_motion is null || _motion.Lifetime.IsCancellationRequested)
                    _motion = AnimationScope.Get(owner).CreateChild("AppBar direct drag settle");
                var motion = _motion;
                _ = motion.Launch(async token =>
                {
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, run.Token);
                    try
                    {
                        await settle(velocity * (float)LayoutHelper.GetLayoutScale(owner), motion, linked.Token);
                    }
                    finally
                    {
                        foreach (var scope in scopes)
                            scope.Unregister(run);
                    }
                }, "AppBar direct drag settle");
            },
            directDrag: true);
        owner.GestureRecognizers.Add(_gesture);
    }

    internal void Cancel()
    {
        _gesture.Cancel();
        _run?.Cancel();
    }

    public void Dispose()
    {
        Cancel();
        _owner.GestureRecognizers.Remove(_gesture);
        _run?.Cancel();
        _run?.Dispose();
        _run = null;
        var motion = _motion;
        _motion = null;
        motion?.Dispose();
    }
}
