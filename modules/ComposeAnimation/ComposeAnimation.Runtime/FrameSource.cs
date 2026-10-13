namespace ComposeAnimation.Runtime;

/// <summary>
/// A window's frame source; the window's internal FrameClock is its only consumer. Pull model:
/// the consumer awaits one frame at a time via <see cref="NextFrameAsync"/>, so cancellation of
/// the supplied token unwinds the wait with an <see cref="OperationCanceledException"/>. All
/// members must be called on the source's owning thread.
/// </summary>
public interface IFrameSource
{
    /// <summary>Throws if called from a thread other than the clock's owning thread.</summary>
    void VerifyAccess();

    /// <summary>
    /// Suspends until the next frame and returns its timestamp in the clock's timebase (this is
    /// not elapsed play time). Typical implementations allow at most one pending call; when used as a window's frame
    /// source, the window's internal frame clock is its only consumer and multiplexes it.
    /// </summary>
    ValueTask<TimeSpan> NextFrameAsync(CancellationToken cancellationToken);
}
