using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>AnimatedVisibility.kt targetEnterExit, including deferred targets and seek previews.</summary>
internal sealed class EnterExitStateMapper<T>(Transition<T> parent, Func<T, bool> visible)
{
    private bool _hasBeenVisible;

    public EnterExitState Map(T state)
    {
        if (parent.IsSeeking)
        {
            return visible(state) ? EnterExitState.Visible
                : visible(parent.CurrentState) ? EnterExitState.PostExit : EnterExitState.PreEnter;
        }

        bool pendingVisible = parent.HasPendingTargetState && visible(parent.PendingTargetState!);
        if (visible(parent.CurrentState) || pendingVisible) _hasBeenVisible = true;
        return visible(state) ? EnterExitState.Visible
            : pendingVisible ? EnterExitState.PreEnter
            : _hasBeenVisible ? EnterExitState.PostExit : EnterExitState.PreEnter;
    }
}
