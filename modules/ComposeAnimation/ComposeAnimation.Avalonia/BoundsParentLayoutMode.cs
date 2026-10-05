namespace ComposeAnimation;

/// <summary>Controls the size an AnimatedBounds reports during actual layout.</summary>
public enum BoundsParentLayoutMode
{
    /// <summary>Reserve the destination size while the private presentation layer animates.</summary>
    TargetSize,
    /// <summary>Report the animated size to a participating parent in the same domain.</summary>
    AnimatedSize
}
