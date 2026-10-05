using ComposeAnimation.Runtime;

namespace ComposeAnimation;

internal enum HasBeenVisibleMode
{
    /// <summary>AnimatedVisibility: set when the mapped argument is visible. No pending branch.</summary>
    Argument,
    /// <summary>Compose targetEnterExit: set from parent.CurrentState. v1 has no pending branch.</summary>
    ParentCurrent
}

/// <summary>
/// Maps a parent state onto <see cref="EnterExitState"/>. Argument and ParentCurrent are not
/// the same function; AnimatedVisibility must keep Argument to preserve reverse-enter.
/// </summary>
internal sealed class EnterExitStateMapper<T>(
    Transition<T> parent, Func<T, bool> visible, HasBeenVisibleMode mode)
{
    private bool _hasBeenVisible;

    public EnterExitState Map(T state)
    {
        if (parent.IsSeeking)
            return visible(state) ? EnterExitState.Visible
                : visible(parent.CurrentState) ? EnterExitState.PostExit
                : EnterExitState.PreEnter;

        if (mode == HasBeenVisibleMode.Argument)
        {
            if (visible(state))
            {
                _hasBeenVisible = true;
                return EnterExitState.Visible;
            }
            return _hasBeenVisible ? EnterExitState.PostExit : EnterExitState.PreEnter;
        }

        if (visible(parent.CurrentState)) _hasBeenVisible = true;
        if (visible(state)) return EnterExitState.Visible;
        return _hasBeenVisible ? EnterExitState.PostExit : EnterExitState.PreEnter;
    }
}
