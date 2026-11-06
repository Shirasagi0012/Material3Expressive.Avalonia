namespace ComposeAnimation.Runtime;

public enum AnimationEndReason
{
    /// <summary>A configured bound stopped the animation.</summary>
    BoundReached,
    /// <summary>The animation or callback-driven operation finished.</summary>
    Finished,
    /// <summary>Replaced, explicitly stopped, disposed, or rejected by mutation priority.</summary>
    Interrupted,
    /// <summary>The owning frame scope ended.</summary>
    ScopeEnded,
    /// <summary>The caller requested cancellation.</summary>
    Canceled
}
