/* Translated from AndroidX NestedScrollNode.kt, Copyright The Android Open Source Project.
 * Apache License 2.0. Fixed source: docs/upstream-baseline.json. */
using Avalonia;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

namespace Material3Expressive.TestSupport;

// Explicit dispatch for protocol and visual parity tests; values are physical px or px/s.
internal static class AppBarScrollScopeExtensions
{
    private static Vector Pre(AppBarScrollScope scope, Vector available, AppBarScrollSource source)
    {
        var top = scope.TopScrollBehavior?.GetNestedScrollConnection().OnPreScroll(available, source) ?? default;
        var bottom = scope.BottomScrollBehavior?.GetNestedScrollConnection().OnPreScroll(available - top, source) ?? default;
        return top + bottom;
    }

    private static Vector Post(AppBarScrollScope scope, Vector consumed, Vector available, AppBarScrollSource source)
    {
        var bottom = scope.BottomScrollBehavior?.GetNestedScrollConnection().OnPostScroll(consumed, available, source) ?? default;
        var top = scope.TopScrollBehavior?.GetNestedScrollConnection().OnPostScroll(consumed + bottom, available - bottom, source) ?? default;
        return bottom + top;
    }

    private static async ValueTask<Vector> PreFling(AppBarScrollScope scope, Vector available, FrameScope clock, CancellationToken token)
    {
        var top = scope.TopScrollBehavior is { } topBehavior
            ? await topBehavior.GetNestedScrollConnection().OnPreFlingAsync(available, clock, token)
            : default;
        var bottom = scope.BottomScrollBehavior is { } bottomBehavior
            ? await bottomBehavior.GetNestedScrollConnection().OnPreFlingAsync(available - top, clock, token)
            : default;
        return top + bottom;
    }

    private static async ValueTask<Vector> PostFling(AppBarScrollScope scope, Vector consumed, Vector available, FrameScope clock, CancellationToken token)
    {
        var bottom = scope.BottomScrollBehavior is { } bottomBehavior
            ? await bottomBehavior.GetNestedScrollConnection().OnPostFlingAsync(consumed, available, clock, token)
            : default;
        var top = scope.TopScrollBehavior is { } topBehavior
            ? await topBehavior.GetNestedScrollConnection().OnPostFlingAsync(consumed + bottom, available - bottom, clock, token)
            : default;
        return bottom + top;
    }

    /// <summary>The synthetic content callback returns consumed physical pixels.</summary>
    internal static Vector DispatchScroll(this AppBarScrollScope scope, Vector available, Func<Vector, Vector> consumeContent,
        AppBarScrollSource source = AppBarScrollSource.UserInput)
    {
        var scopes = new[] { scope }
            .Concat(scope.GetVisualAncestors().OfType<AppBarScrollScope>())
            .ToArray();

        var remaining = available;
        foreach (var currentScope in scopes.Reverse())
            remaining -= Pre(currentScope, remaining, source);

        var consumed = consumeContent(remaining);
        remaining -= consumed;

        foreach (var currentScope in scopes)
        {
            var postConsumed = Post(currentScope, consumed, remaining, source);
            consumed += postConsumed;
            remaining -= postConsumed;
        }

        return available - remaining;
    }

    /// <summary>The synthetic content callback returns consumed velocity in physical px/s.
    /// Send each content-inertia delta through DispatchScroll with SideEffect.</summary>
    internal static async Task<Vector> DispatchFlingAsync(this AppBarScrollScope scope, Vector available,
        Func<Vector, CancellationToken, Task<Vector>> consumeContent, FrameScope clock,
        CancellationToken cancellationToken = default)
    {
        var scopes = new[] { scope }
            .Concat(scope.GetVisualAncestors().OfType<AppBarScrollScope>())
            .ToArray();

        using var run = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        foreach (var currentScope in scopes)
            currentScope.Register(run);

        try
        {
            var remaining = available;
            foreach (var currentScope in scopes.Reverse())
                remaining -= await PreFling(currentScope, remaining, clock, run.Token);

            var consumed = await consumeContent(remaining, run.Token);
            remaining -= consumed;

            foreach (var currentScope in scopes)
            {
                var postConsumed = await PostFling(currentScope, consumed, remaining, clock, run.Token);
                consumed += postConsumed;
                remaining -= postConsumed;
            }

            return available - remaining;
        }
        finally
        {
            foreach (var currentScope in scopes)
                currentScope.Unregister(run);
        }
    }
}
