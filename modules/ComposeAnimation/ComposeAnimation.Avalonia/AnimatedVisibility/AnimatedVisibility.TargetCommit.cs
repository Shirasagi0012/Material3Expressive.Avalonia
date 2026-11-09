using Avalonia;

namespace ComposeAnimation;

public partial class AnimatedVisibility : IAnimatedStateCommit
{
    public static readonly StyledProperty<AnimationCommitPhase> TargetCommitPhaseProperty =
        AvaloniaProperty.Register<AnimatedVisibility, AnimationCommitPhase>(nameof(TargetCommitPhase));

    /// <summary>Controls commits of the local Visible target. External Source transitions retain their own timing.</summary>
    public AnimationCommitPhase TargetCommitPhase
    {
        get => GetValue(TargetCommitPhaseProperty);
        set => SetValue(TargetCommitPhaseProperty, value);
    }

    private AnimatedStateCoordinator? _targetCoordinator;
    private bool _targetCommitQueued, _targetCommitCaptured;
    private int _targetCommitEpoch;

    // Returns true while the requested local target belongs to a future frame commit.
    private bool UpdateVisibleTarget()
    {
        if (Source is not null || _visibilityConnection is not { } connection)
        { CancelTargetCommit(); return false; }
        if (TargetCommitPhase != AnimationCommitPhase.AfterFrame || !_attached || !IsVisible)
        {
            CancelTargetCommit();
            connection.Transition.UpdateTarget(Visible);
            return false;
        }
        if (connection.Transition.TargetState == Visible && !_targetCommitCaptured)
        { CancelTargetCommit(); return false; }

        _targetCoordinator ??= AnimatedStateCoordinator.Get(AnimationScope.Get(this), AnimationCommitPhase.AfterFrame);
        _targetCommitQueued = true;
        _targetCoordinator.Schedule(this);
        SetAndRaise(IsIdleProperty, ref _isIdle, false);
        return true;
    }

    private void CancelTargetCommit()
    {
        _targetCoordinator?.Remove(this);
        _targetCoordinator = null;
        _targetCommitQueued = _targetCommitCaptured = false;
        _targetCommitEpoch++;
    }

    Action IAnimatedStateCommit.CaptureCommit()
    {
        var target = Visible;
        var connection = _visibilityConnection;
        var epoch = _targetCommitEpoch;
        _targetCommitQueued = false;
        _targetCommitCaptured = true;
        return () =>
        {
            if (epoch != _targetCommitEpoch || !_attached || Source is not null
                || connection is null || !ReferenceEquals(connection, _visibilityConnection)) return;
            _targetCommitCaptured = false;
            try
            {
                connection.Transition.UpdateTarget(target);
                SynchronizePresentation();
            }
            catch (Exception error) { ((IAnimatedStateCommit)this).RejectCommitFrame(error, true); }
        };
    }

    void IAnimatedStateCommit.RejectCommitFrame(Exception error, bool report)
    {
        _failed = true;
        DisconnectVisibilitySource();
        if (report) AnimationScope.ReportError(this, error);
    }
}
