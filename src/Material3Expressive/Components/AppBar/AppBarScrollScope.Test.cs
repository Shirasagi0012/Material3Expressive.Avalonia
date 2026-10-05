using Avalonia;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

// Explicit nested-scroll dispatch, currently used by protocol and visual parity tests.
// This partial remains part of the control library and retains the public custom-host API.
public partial class AppBarScrollScope
{
    internal Vector Pre(Vector available, AppBarScrollSource source)
    {
        if (TopScrollBehavior is DefaultTopAppBarScrollBehavior topDefaults)
            topDefaults.AttachDefaults(this);
        if (BottomScrollBehavior is DefaultBottomAppBarScrollBehavior bottomDefaults)
            bottomDefaults.AttachDefaults(this);

        var top = TopScrollBehavior?.NestedScrollConnection.OnPreScroll(available, source) ?? default;
        var bottom = BottomScrollBehavior?.NestedScrollConnection.OnPreScroll(available - top, source) ?? default;
        return top + bottom;
    }

    internal Vector Post(Vector consumed, Vector available, AppBarScrollSource source)
    {
        var bottom = BottomScrollBehavior?.NestedScrollConnection.OnPostScroll(consumed, available, source) ?? default;
        var top = TopScrollBehavior?.NestedScrollConnection.OnPostScroll(consumed + bottom, available - bottom, source) ?? default;
        return bottom + top;
    }

    internal async ValueTask<Vector> PreFling(Vector available, IAnimationFrameClock clock, CancellationToken token)
    {
        if (TopScrollBehavior is DefaultTopAppBarScrollBehavior topDefaults)
            topDefaults.AttachDefaults(this);
        if (BottomScrollBehavior is DefaultBottomAppBarScrollBehavior bottomDefaults)
            bottomDefaults.AttachDefaults(this);

        var top = TopScrollBehavior is { } topBehavior
            ? await topBehavior.NestedScrollConnection.OnPreFlingAsync(available, clock, token)
            : default;
        var bottom = BottomScrollBehavior is { } bottomBehavior
            ? await bottomBehavior.NestedScrollConnection.OnPreFlingAsync(available - top, clock, token)
            : default;
        return top + bottom;
    }

    internal async ValueTask<Vector> PostFling(Vector consumed, Vector available, IAnimationFrameClock clock, CancellationToken token)
    {
        var bottom = BottomScrollBehavior is { } bottomBehavior
            ? await bottomBehavior.NestedScrollConnection.OnPostFlingAsync(consumed, available, clock, token)
            : default;
        var top = TopScrollBehavior is { } topBehavior
            ? await topBehavior.NestedScrollConnection.OnPostFlingAsync(consumed + bottom, available - bottom, clock, token)
            : default;
        return bottom + top;
    }

    /// <summary>Explicit custom-host path. The content callback returns consumed physical pixels.</summary>
    public Vector DispatchScroll(Vector available, Func<Vector, Vector> consumeContent,
        AppBarScrollSource source = AppBarScrollSource.UserInput)
    {
        var scopes = new[] { this }
            .Concat(this.GetVisualAncestors().OfType<AppBarScrollScope>())
            .ToArray();

        var remaining = available;
        foreach (var scope in scopes.Reverse())
            remaining -= scope.Pre(remaining, source);

        var consumed = consumeContent(remaining);
        remaining -= consumed;

        foreach (var scope in scopes)
        {
            var postConsumed = scope.Post(consumed, remaining, source);
            consumed += postConsumed;
            remaining -= postConsumed;
        }

        return available - remaining;
    }

    /// <summary>Custom-host fling path. The callback returns consumed velocity in physical px/s.
    /// Send each content-inertia delta through DispatchScroll with SideEffect.</summary>
    public async Task<Vector> DispatchFlingAsync(Vector available,
        Func<Vector, CancellationToken, Task<Vector>> consumeContent, IAnimationFrameClock clock,
        CancellationToken cancellationToken = default)
    {
        var scopes = new[] { this }
            .Concat(this.GetVisualAncestors().OfType<AppBarScrollScope>())
            .ToArray();

        using var run = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        foreach (var scope in scopes)
            scope.Register(run);

        try
        {
            var remaining = available;
            foreach (var scope in scopes.Reverse())
                remaining -= await scope.PreFling(remaining, clock, run.Token);

            var consumed = await consumeContent(remaining, run.Token);
            remaining -= consumed;

            foreach (var scope in scopes)
            {
                var postConsumed = await scope.PostFling(consumed, remaining, clock, run.Token);
                consumed += postConsumed;
                remaining -= postConsumed;
            }

            return available - remaining;
        }
        finally
        {
            foreach (var scope in scopes)
                scope.Unregister(run);
        }
    }
}
