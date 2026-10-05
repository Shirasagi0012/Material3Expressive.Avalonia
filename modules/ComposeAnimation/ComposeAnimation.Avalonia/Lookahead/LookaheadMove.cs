namespace ComposeAnimation;

internal interface ILookaheadMoveParticipant
{
    ILookaheadMoveState? CaptureMove(LookaheadContext root);
}

internal interface ILookaheadMoveState
{
    bool CanCommit();
    void Commit();
    void Cancel();
}
