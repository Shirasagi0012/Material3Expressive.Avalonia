/* Translated from AndroidX AppBar.kt, Copyright The Android Open Source Project.
 * Apache License 2.0. Fixed source: docs/upstream-baseline.json. */
using Avalonia;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

namespace Material3Expressive.TestSupport;

// The explicit Compose protocol drives synthetic test content.
// Real ScrollViewer input uses AppBarScrollParticipant in the control library.
internal enum AppBarScrollSource
{
    UserInput,
    SideEffect
}

/// <summary>A nested scroll connection; return values are consumed physical pixels or px/s.</summary>
internal abstract class AppBarNestedScrollConnection
{
    public virtual Vector OnPreScroll(Vector available, AppBarScrollSource source) => default;

    public virtual Vector OnPostScroll(Vector consumed, Vector available, AppBarScrollSource source) => default;

    public virtual ValueTask<Vector> OnPreFlingAsync(
        Vector available,
        FrameScope clock,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(default(Vector));

    public virtual ValueTask<Vector> OnPostFlingAsync(
        Vector consumed,
        Vector available,
        FrameScope clock,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(default(Vector));
}

internal static class AppBarNestedScrollConnectionExtensions
{
    internal static AppBarNestedScrollConnection GetNestedScrollConnection(this TopAppBarScrollBehavior behavior) =>
        new TopConnection(behavior);

    internal static AppBarNestedScrollConnection GetNestedScrollConnection(this BottomAppBarScrollBehavior behavior) =>
        new BottomConnection(behavior);

    private sealed class TopConnection(TopAppBarScrollBehavior owner) : AppBarNestedScrollConnection
    {
        public override Vector OnPreScroll(Vector available, AppBarScrollSource source)
        {
            if (!owner.CanScroll || owner.IsPinned ||
                owner.Mode == TopAppBarScrollMode.ExitUntilCollapsed && available.Y > 0)
                return default;

            var state = owner.State;
            var previous = state.HeightOffset;
            state.HeightOffset += (float)available.Y;

            // Upstream consumes the complete Y input when the offset changes, including at a boundary.
            return previous != state.HeightOffset ? new Vector(0, available.Y) : default;
        }

        public override Vector OnPostScroll(Vector consumed, Vector available, AppBarScrollSource source)
        {
            if (!owner.CanScroll)
                return default;

            var state = owner.State;
            state.ContentOffset += (float)consumed.Y;
            if (owner.Mode != TopAppBarScrollMode.ExitUntilCollapsed)
                return default;

            var old = state.HeightOffset;
            if (available.Y < 0 || consumed.Y < 0)
                state.HeightOffset += (float)consumed.Y;
            else if (available.Y > 0)
                state.HeightOffset += (float)available.Y;

            return new Vector(0, state.HeightOffset - old);
        }

        public override async ValueTask<Vector> OnPostFlingAsync(
            Vector consumed,
            Vector available,
            FrameScope clock,
            CancellationToken cancellationToken = default)
        {
            if (available.Y > 0)
                owner.State.ContentOffset = 0;

            return owner.IsPinned ? default : await owner.SettleAsync((float)available.Y, clock, cancellationToken);
        }
    }

    private sealed class BottomConnection(BottomAppBarScrollBehavior owner) : AppBarNestedScrollConnection
    {
        public override Vector OnPostScroll(Vector consumed, Vector available, AppBarScrollSource source)
        {
            if (!owner.CanScroll)
                return default;

            owner.State.ContentOffset += (float)consumed.Y;
            owner.State.HeightOffset += (float)consumed.Y;
            return default;
        }

        public override async ValueTask<Vector> OnPostFlingAsync(
            Vector consumed,
            Vector available,
            FrameScope clock,
            CancellationToken cancellationToken = default)
        {
            var state = owner.State;
            if (available.Y > 0 && (state.HeightOffset == 0 || state.HeightOffset == state.HeightOffsetLimit))
                state.ContentOffset = 0;

            return await owner.SettleAsync((float)available.Y, clock, cancellationToken);
        }
    }
}
