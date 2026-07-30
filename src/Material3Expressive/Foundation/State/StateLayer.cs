using System;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation;

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
/// <c>MaterialInteractionHost</c>; this type supplies the token and transition lookup.
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
    /// This compatibility helper resolves an unordered flag set. The interaction host itself
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

    /// <summary>The overlay opacity for one interaction.</summary>
    public static double Opacity(InteractionState interaction)
    {
        return interaction switch
        {
            InteractionState.Hovered => StateTokens.HoverStateLayerOpacity,
            InteractionState.Focused => StateTokens.FocusStateLayerOpacity,
            InteractionState.Pressed => StateTokens.PressedStateLayerOpacity,
            InteractionState.Dragged => StateTokens.DraggedStateLayerOpacity,
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
