using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// Everything Material specifies about interaction feedback, hosted on any control.
/// </summary>
/// <remarks>
/// Avalonia's pseudo-classes can say which state a control is in but not how it got there, and
/// Material cares about the transition: the fades are asymmetric, press feedback starts where the
/// pointer went down, and the shape morph has to survive being interrupted. This owns all of that,
/// so a component only has to say what colour and shape it is.
/// <para>
/// Layers draw in the order Material stacks them — container, then state layer, then ripples, then
/// content. The host paints the middle two; the owner paints its own container and content around
/// the call to <see cref="DrawLayers" />.
/// </para>
/// </remarks>
public sealed class MaterialInteractionHost
{
    private readonly Control _owner;
    private readonly List<RippleAnimation> _ripples = new();

    private TweenAnimation? _stateLayerAlpha;
    private InteractionState _interaction;
    private InteractionState _shownInteraction;

    public MaterialInteractionHost(Control owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Motion = new MotionGroup(owner);
        Motion.Ticked += (_, _) => OnTick();

        _owner.PointerEntered += (_, _) => Set(InteractionState.Hovered, true);
        _owner.PointerExited += (_, _) => Set(InteractionState.Hovered, false);
        _owner.PointerPressed += OnPointerPressed;
        _owner.PointerReleased += (_, _) => EndPress();
        _owner.PointerCaptureLost += (_, _) => EndPress();
        _owner.GotFocus += OnGotFocus;
        _owner.LostFocus += (_, _) => Set(InteractionState.Focused, false);

        // Off the tree there is no render clock, so anything mid-flight would freeze.
        _owner.DetachedFromLogicalTree += OnDetached;
    }

    /// <summary>Raised once per frame while anything is animating.</summary>
    public event EventHandler? Ticked;

    /// <summary>Raised when the set of active interactions changes.</summary>
    public event EventHandler? InteractionChanged;

    /// <summary>This control's animations and the frame loop that drives them.</summary>
    public MotionGroup Motion { get; }

    /// <summary>Which interactions the owner is currently under.</summary>
    public InteractionState Interaction => _interaction;

    /// <summary>The state layer's current opacity, mid-fade included.</summary>
    public double StateLayerOpacity => _stateLayerAlpha?.Value ?? 0;

    /// <summary>Whether a press raises a ripple.</summary>
    public bool IsRippleEnabled { get; set; } = true;

    /// <summary>Whether the ripple is clipped to the owner's bounds.</summary>
    public bool IsRippleBounded { get; set; } = true;

    /// <summary>
    /// The part of the control the state layer and ripples cover. Null means all of it.
    /// </summary>
    /// <remarks>
    /// Selection controls need this: a checkbox's feedback belongs to the 40dp circle around its
    /// box, not to the label sitting next to it.
    /// </remarks>
    public Rect? InteractionArea { get; set; }

    private Rect Area => InteractionArea ?? new Rect(_owner.Bounds.Size);

    private TweenAnimation StateLayerAlpha =>
        _stateLayerAlpha ??= Motion.AddTween(0, StateLayer.FadeInDuration(InteractionState.Hovered));

    /// <summary>Creates a shape that morphs with a spring, on this host's frame loop.</summary>
    public AnimatedShape CreateAnimatedShape(RoundedCornerShape initial, SpringSpec spec)
    {
        var shape = new AnimatedShape(initial, spec);
        Motion.Add(shape.Progress);
        return shape;
    }

    /// <summary>
    /// Draws the state layer and any live ripples over the container.
    /// </summary>
    /// <param name="context">The owner's drawing context.</param>
    /// <param name="brush">The state layer colour — the content colour, in Material.</param>
    /// <param name="cornerRadius">The container's corners, so the layer follows any morph.</param>
    public void DrawLayers(DrawingContext context, IBrush? brush, CornerRadius cornerRadius)
    {
        if (brush is null)
            return;

        var area = Area;
        var container = new RoundedRect(area, cornerRadius);
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

        if (_ripples.Count == 0)
            return;

        // Ripples are computed in area-local coordinates, so that a checkbox's ripple grows from
        // the middle of its box rather than the middle of the label beside it.
        using var origin = context.PushTransform(Matrix.CreateTranslation(area.X, area.Y));

        foreach (var ripple in _ripples)
            ripple.Draw(context, brush, StateLayer.Opacity(InteractionState.Pressed));
    }

    /// <summary>Sets or clears one interaction flag.</summary>
    public void Set(InteractionState flag, bool active)
    {
        var updated = active ? _interaction | flag : _interaction & ~flag;
        if (updated == _interaction)
            return;

        _interaction = updated;
        UpdateStateLayer();
        InteractionChanged?.Invoke(this, EventArgs.Empty);
        Motion.Start();
        _owner.InvalidateVisual();
    }

    /// <summary>Begins the pressed state, raising a ripple at <paramref name="origin" />.</summary>
    public void StartPress(Point origin)
    {
        if (_interaction.HasFlag(InteractionState.Pressed))
            return;

        Set(InteractionState.Pressed, true);

        var area = Area;
        if (!IsRippleEnabled || area.Width <= 0 || area.Height <= 0)
            return;

        // The press point is in control coordinates; the ripple works in area-local ones.
        var ripple = new RippleAnimation(origin - area.Position, area.Size, IsRippleBounded);
        _ripples.Add(ripple);
        Motion.Add(ripple);
        Motion.Start();
    }

    /// <summary>Ends the pressed state and lets any live ripple fade out.</summary>
    public void EndPress()
    {
        if (!_interaction.HasFlag(InteractionState.Pressed))
            return;

        Set(InteractionState.Pressed, false);

        foreach (var ripple in _ripples)
            ripple.Release();

        Motion.Start();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_owner).Properties.IsLeftButtonPressed)
            StartPress(e.GetPosition(_owner));
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        // Material shows the focus layer for keyboard focus only; a click that focuses a control
        // already has a ripple to say so.
        Set(
            InteractionState.Focused,
            e.NavigationMethod is NavigationMethod.Directional or NavigationMethod.Tab);
    }

    private void OnDetached(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        Motion.SettleAll();
    }

    private void OnTick()
    {
        // Completed ripples would otherwise accumulate for the control's lifetime.
        for (var i = _ripples.Count - 1; i >= 0; i--)
        {
            if (!_ripples[i].IsComplete)
                continue;

            Motion.Remove(_ripples[i]);
            _ripples.RemoveAt(i);
        }

        Ticked?.Invoke(this, EventArgs.Empty);
        _owner.InvalidateVisual();
    }

    private void UpdateStateLayer()
    {
        // Press is drawn as a ripple, not as a uniform overlay, so it is excluded here — otherwise
        // a press would darken the whole container as well.
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
