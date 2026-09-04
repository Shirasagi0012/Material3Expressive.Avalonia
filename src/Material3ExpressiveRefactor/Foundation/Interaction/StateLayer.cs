using System;

namespace Material3ExpressiveRefactor.Foundation.Interaction;

/// <summary>
/// The rules behind the translucent overlay a control draws while it is being interacted with.
/// </summary>
/// <remarks>
/// Ported from androidx <c>material-ripple/Ripple.kt</c>'s internal <c>StateLayer</c>. Two things
/// there are easy to get wrong and are the reason this is data rather than a pile of style
/// triggers:
/// <list type="number">
/// <item>
/// Interactions stack, but only the most recent one is shown. The ordered stack lives in
/// <see cref="MaterialInteractionController" />; this type supplies the fallback opacities and the
/// transition timing. The opacities live in the <c>md.sys.state.*</c> tokens and are resolved per
/// owner — these constants mirror them for hosts without the baseline source.
/// </item>
/// <item>
/// The fades are not symmetric. Focus arrives over 45ms and leaves over 15; a drag arrives over 45
/// and leaves over 150. Upstream chose those deliberately — a drag ending abruptly reads as a
/// glitch, while a focus ring lingering reads as lag.
/// </item>
/// </list>
/// <para>
/// Press is absent on purpose: it is drawn as a ripple, which starts where the pointer went down
/// and so cannot be a uniform overlay.
/// </para>
/// </remarks>
public static class StateLayer
{
    private static readonly TimeSpan DefaultFade = TimeSpan.FromMilliseconds(15);
    private static readonly TimeSpan SlowIn = TimeSpan.FromMilliseconds(45);
    private static readonly TimeSpan DragOut = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// The interaction the layer should show, or <see cref="InteractionState.None" />.
    /// </summary>
    /// <remarks>
    /// This compatibility helper resolves an unordered flag set. The interaction controller itself
    /// retains arrival order, matching AndroidX's <c>interactions.lastOrNull()</c>.
    /// </remarks>
    public static InteractionState Topmost(InteractionState state)
    {
        if (state.HasFlag(InteractionState.Dragged))
            return InteractionState.Dragged;

        if (state.HasFlag(InteractionState.Pressed))
            return InteractionState.Pressed;

        if (state.HasFlag(InteractionState.Focused))
            return InteractionState.Focused;

        if (state.HasFlag(InteractionState.Hovered))
            return InteractionState.Hovered;

        return InteractionState.None;
    }

    /// <summary>The overlay opacity for one interaction — the upstream state token values.</summary>
    public static double Opacity(InteractionState interaction)
    {
        return interaction switch
        {
            InteractionState.Hovered => 0.08,
            InteractionState.Focused => 0.10,
            InteractionState.Pressed => 0.10,
            InteractionState.Dragged => 0.16,
            _ => 0
        };
    }

    /// <summary>How long the layer takes to appear for an arriving interaction.</summary>
    public static TimeSpan FadeInDuration(InteractionState interaction)
    {
        return interaction switch
        {
            InteractionState.Focused => SlowIn,
            InteractionState.Dragged => SlowIn,
            _ => DefaultFade
        };
    }

    /// <summary>How long the layer takes to clear once <paramref name="interaction" /> ends.</summary>
    public static TimeSpan FadeOutDuration(InteractionState interaction)
    {
        return interaction == InteractionState.Dragged ? DragOut : DefaultFade;
    }
}
