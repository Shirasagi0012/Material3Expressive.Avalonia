using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// Base class for controls that respond to being hovered, focused, pressed or dragged.
/// </summary>
/// <remarks>
/// Everything Material specifies about interaction feedback lives here, so a component only has to
/// say what colour and shape it is. Avalonia's own pseudo-classes (<c>:pointerover</c>,
/// <c>:pressed</c>) can express which state a control is in but not how it got there, and Material
/// cares about the transition: the fades are asymmetric, the press feedback starts where the
/// pointer went down, and the shape morph has to survive being interrupted.
/// <para>
/// The layers stack in the order Material draws them — container, then state layer, then ripples,
/// then content. Subclasses paint the container and call
/// <see cref="DrawInteractionLayers" /> at the right point in their own <see cref="Render" />.
/// </para>
/// </remarks>
public abstract class MaterialInteractiveControl : MaterialControl
{
    private readonly List<RippleAnimation> _ripples = new();

    private TweenAnimation? _stateLayerAlpha;
    private InteractionState _interaction;
    private InteractionState _shownInteraction;

    /// <summary>Which interactions the control is currently under.</summary>
    public InteractionState Interaction => _interaction;

    /// <summary>
    /// The state layer's current opacity, mid-fade included.
    /// </summary>
    protected double StateLayerOpacity => _stateLayerAlpha?.Value ?? 0;

    /// <summary>Whether a press should raise a ripple. Override to opt out.</summary>
    protected virtual bool IsRippleEnabled => true;

    /// <summary>Whether the ripple is clipped to the control's bounds.</summary>
    protected virtual bool IsRippleBounded => true;

    private TweenAnimation StateLayerAlpha =>
        _stateLayerAlpha ??= Motion.AddTween(0, StateLayer.FadeInDuration(InteractionState.Hovered));

    /// <summary>
    /// Creates a shape that morphs with a spring, registered on this control's frame loop.
    /// </summary>
    protected AnimatedShape CreateAnimatedShape(RoundedCornerShape initial, MotionRole role = MotionRole.DefaultEffects)
    {
        var shape = new AnimatedShape(initial, Spring(role));
        Motion.Add(shape.Progress);
        return shape;
    }

    /// <summary>
    /// Draws the state layer and any live ripples over the container.
    /// </summary>
    /// <param name="context">The control's drawing context.</param>
    /// <param name="brush">The state layer colour — the content colour, in Material.</param>
    /// <param name="cornerRadius">The container's corners, so the layer follows the morph.</param>
    protected void DrawInteractionLayers(DrawingContext context, IBrush? brush, CornerRadius cornerRadius)
    {
        if (brush is null)
            return;

        var container = new RoundedRect(new Rect(Bounds.Size), cornerRadius);
        var opacity = StateLayerOpacity;

        if (opacity <= 0 && _ripples.Count == 0)
            return;

        // Both layers are clipped to the container, so a ripple cannot escape a rounded corner.
        using var clip = context.PushClip(container);

        if (opacity > 0)
        {
            using (context.PushOpacity(opacity))
                context.DrawRectangle(brush, null, container);
        }

        foreach (var ripple in _ripples)
            ripple.Draw(context, brush, StateLayer.Opacity(InteractionState.Pressed));
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        SetInteraction(InteractionState.Hovered, true);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetInteraction(InteractionState.Hovered, false);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);

        // Material shows the focus layer for keyboard focus only; a click that focuses a control
        // already has a ripple to say so.
        SetInteraction(
            InteractionState.Focused,
            e.NavigationMethod is NavigationMethod.Directional or NavigationMethod.Tab);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        SetInteraction(InteractionState.Focused, false);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        StartPress(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndPress();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndPress();
    }

    /// <summary>Begins the pressed state, raising a ripple at <paramref name="origin" />.</summary>
    protected void StartPress(Point origin)
    {
        if (_interaction.HasFlag(InteractionState.Pressed))
            return;

        SetInteraction(InteractionState.Pressed, true);

        if (!IsRippleEnabled || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        var ripple = new RippleAnimation(origin, Bounds.Size, IsRippleBounded);
        _ripples.Add(ripple);
        Motion.Add(ripple);
        Motion.Start();
    }

    /// <summary>Ends the pressed state and lets any live ripple fade out.</summary>
    protected void EndPress()
    {
        if (!_interaction.HasFlag(InteractionState.Pressed))
            return;

        SetInteraction(InteractionState.Pressed, false);

        foreach (var ripple in _ripples)
            ripple.Release();

        Motion.Start();
    }

    /// <summary>
    /// Called when the set of active interactions changes. Retarget shape and colour springs here.
    /// </summary>
    protected virtual void OnInteractionChanged(InteractionState interaction)
    {
    }

    protected override void OnMotionTick()
    {
        // Completed ripples would otherwise accumulate for the control's lifetime.
        for (var i = _ripples.Count - 1; i >= 0; i--)
        {
            if (!_ripples[i].IsComplete)
                continue;

            Motion.Remove(_ripples[i]);
            _ripples.RemoveAt(i);
        }

        base.OnMotionTick();
    }

    private void SetInteraction(InteractionState flag, bool active)
    {
        var updated = active ? _interaction | flag : _interaction & ~flag;
        if (updated == _interaction)
            return;

        _interaction = updated;
        UpdateStateLayer();
        OnInteractionChanged(updated);
        Motion.Start();
        InvalidateVisual();
    }

    private void UpdateStateLayer()
    {
        // Press is drawn as a ripple, not as a uniform overlay, so it is excluded from the layer's
        // own state — otherwise a press would darken the whole container as well.
        var topmost = StateLayer.Topmost(_interaction & ~InteractionState.Pressed);

        if (topmost == _shownInteraction)
            return;

        var duration = topmost == InteractionState.None
            ? StateLayer.FadeOutDuration(_shownInteraction)
            : StateLayer.FadeInDuration(topmost);

        _shownInteraction = topmost;
        StateLayerAlpha.AnimateTo(StateLayer.Opacity(topmost), duration, Easings.Linear);
    }
}
