namespace ComposeAnimation.Runtime;

/// <summary>Shared constants used by animation specs (Compose <c>AnimationConstants</c>).</summary>
public static class AnimationConstants
{
    /// <summary>The default duration used in duration-based animation specs. Compose: 300 ms.</summary>
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Sentinel used when an animation time is not yet set. Compose uses <c>Long.MIN_VALUE</c>
    /// for nanos; here the equivalent is <see cref="TimeSpan.MinValue"/>.
    /// </summary>
    public static readonly TimeSpan UnspecifiedTime = TimeSpan.MinValue;

    /// <summary>Returns whether <paramref name="time"/> is <see cref="UnspecifiedTime"/>.</summary>
    public static bool IsUnspecified(TimeSpan time) => time == UnspecifiedTime;
}