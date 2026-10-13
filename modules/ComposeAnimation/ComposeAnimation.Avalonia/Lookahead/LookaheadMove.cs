namespace ComposeAnimation;

internal interface ILookaheadMoveParticipant
{
    ILookaheadMoveState? CaptureMove(LookaheadContext root);
}

internal interface ILookaheadMoveState
{
    /// <summary>Null when the move can commit; otherwise the reason it cannot (used in the error message).</summary>
    string? CommitBlocker();
    void Commit();
    void Cancel();
}
