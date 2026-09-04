using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3ExpressiveRefactor.Foundation.Motion;
using Material3ExpressiveRefactor.Foundation.Rendering;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Tokens;

namespace Material3ExpressiveRefactor.Foundation.Interaction;

/// <summary>
/// Everything Material specifies about interaction feedback, hosted on any control.
/// </summary>
/// <remarks>
/// Avalonia's pseudo-classes can say which state a control is in but not how it got there, and
/// Material cares about the transition: the fades are asymmetric, press feedback starts where the
/// pointer went down, and the shape morph has to survive being interrupted. This owns all of that,
/// so a component only has to say what colour and shape it is.
/// <para>
/// Layers draw in the order used by the current Material 3 ripple node — content, press ripples,
/// then the non-press state layer — from the MaterialSurface overlay, which attaches here through
/// <see cref="Attach" />. The colour is the surface's styled StateLayerBrush (the styles own "what
/// colour"; this controller owns "how opaque, and when"), matching upstream where the state layer
/// and ripple ink share the content colour.
/// </para>
/// <para>
/// Upstream strips a solid colour's ambient alpha and replaces it with the state alpha
/// (Ripple.kt: <c>color.copy(alpha = animatedAlpha)</c>), so a solid brush's own opacity is removed
/// before the controller's quantized opacity is applied.
/// </para>
/// </remarks>
internal sealed class MaterialInteractionController
{
    private readonly Control _owner;
    private readonly MaterialMotionController _motion;
    private readonly List<RippleAnimation> _ripples = [];
    private readonly List<InteractionState> _stateLayerInteractions = [];
    private readonly SolidColorBrush _alphaReplacedBrush = new();

    private TweenAnimation? _stateLayerAlpha;
    private MaterialSurface? _presenter;
    private InteractionState _interaction;
    private InteractionState _shownInteraction;

    // Token-resolved state-layer opacities, falling back to the upstream constants in StateLayer.
    private double _hoverOpacity = StateLayer.Opacity(InteractionState.Hovered);
    private double _focusOpacity = StateLayer.Opacity(InteractionState.Focused);
    private double _pressedOpacity = StateLayer.Opacity(InteractionState.Pressed);
    private double _draggedOpacity = StateLayer.Opacity(InteractionState.Dragged);

    internal MaterialInteractionController(Control owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _motion = new MaterialMotionController(owner);
        _motion.BeforeOwnerFrame += RemoveCompletedRipples;
        _motion.AfterOwnerFrame += PublishInteractionFrame;
        _motion.Activate();

        _owner.PointerEntered += (_, _) => Set(InteractionState.Hovered, true);
        _owner.PointerExited += (_, _) => Set(InteractionState.Hovered, false);
        // Button-derived controls handle pointer presses in their class handler. Listen on the
        // tunnel route so Material feedback still sees the physical input before it is consumed.
        _owner.AddHandler(
            InputElement.PointerPressedEvent,
            OnPointerPressed,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _owner.AddHandler(
            InputElement.PointerReleasedEvent,
            OnPointerReleased,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _owner.AddHandler(
            InputElement.KeyDownEvent,
            OnKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _owner.AddHandler(
            InputElement.KeyUpEvent,
            OnKeyUp,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _owner.PointerCaptureLost += (_, _) => EndPress();
        _owner.GotFocus += OnGotFocus;
        _owner.LostFocus += (_, _) =>
        {
            EndPress();
            Set(InteractionState.Focused, false);
        };
        _owner.DetachedFromLogicalTree += OnDetachedFromLogicalTree;
        _owner.ResourcesChanged += (_, _) => OnTokensChanged();
        _owner.ActualThemeVariantChanged += (_, _) => OnTokensChanged();
    }

    /// <summary>This owner's animations and the frame loop that drives them.</summary>
    internal MaterialMotionController Motion => _motion;

    /// <summary>Which interactions the owner is currently under.</summary>
    internal InteractionState Interaction => _interaction;

    /// <summary>The state layer's current opacity, mid-fade included.</summary>
    internal double StateLayerOpacity => _stateLayerAlpha?.Value ?? 0;

    /// <summary>How many ripples are currently alive (test observation point).</summary>
    internal int RippleCount => _ripples.Count;

    /// <summary>Whether the ripple is clipped to the owner's bounds. Bounded is the M3 default.</summary>
    internal bool IsRippleBounded { get; set; } = true;

    /// <summary>
    /// An explicit ripple radius in device-independent pixels, or null to derive it from the
    /// interaction area's size.
    /// </summary>
    internal double? RippleRadius { get; set; }

    /// <summary>
    /// Attaches the template's MaterialSurface as the render front-end, returning the lease that
    /// detaches it. A second attach replaces the first; a disposed lease stops its surface from
    /// drawing controller state (template-replacement protection).
    /// </summary>
    internal IDisposable Attach(MaterialSurface presenter)
    {
        ArgumentNullException.ThrowIfNull(presenter);
        _presenter = presenter;
        // Sync the state the surface missed before attaching (e.g. an already-hovered template
        // swap), so the first frame paints the right thing.
        presenter.OnInteractionChanged(_interaction);
        return new PresenterLease(this, presenter);
    }

    private void Detach(MaterialSurface presenter)
    {
        if (ReferenceEquals(_presenter, presenter))
            _presenter = null;
    }

    /// <summary>
    /// Draws the press ripples and the state layer over the container, in the presenter's own
    /// coordinate space.
    /// </summary>
    /// <param name="context">The presenter's drawing context.</param>
    /// <param name="brush">The state layer colour — the content colour, in Material.</param>
    /// <param name="cornerRadius">The container's corners, so the layers follow any morph.</param>
    /// <param name="size">The interaction area: the presenter's own bounds.</param>
    internal void DrawLayers(DrawingContext context, IBrush? brush, CornerRadius cornerRadius, Size size)
    {
        if (brush is null)
            return;

        var area = new Rect(size);
        var opacity = StateLayerOpacity;

        if (opacity <= 0 && _ripples.Count == 0)
            return;

        var composeColorBrush = WithComposeReplacedAlpha(brush);

        // New material3-ripple draws press ripples before non-press state layers. A bounded node
        // clips both to its target layout; an unbounded node deliberately clips neither.
        if (_ripples.Count > 0)
        {
            if (IsRippleBounded)
            {
                using var rippleClip = context.PushClip(new RoundedRect(area, cornerRadius));
                DrawRipples(context, composeColorBrush);
            }
            else
            {
                DrawRipples(context, composeColorBrush);
            }
        }

        if (opacity > 0)
        {
            if (IsRippleBounded)
            {
                using var stateClip = context.PushClip(new RoundedRect(area, cornerRadius));
                DrawStateLayer(context, composeColorBrush, area, opacity);
            }
            else
            {
                var radius = RippleRadius ?? RippleAnimationEndRadius(area.Size, bounded: false);
                using (context.PushOpacity(MaterialInteractionRendering.ComposeOpacity(opacity)))
                    context.DrawEllipse(composeColorBrush, null, area.Center, radius, radius);
            }
        }
    }

    /// <summary>Sets or clears one interaction flag.</summary>
    internal void Set(InteractionState flag, bool active)
    {
        var updated = active ? _interaction | flag : _interaction & ~flag;
        if (updated == _interaction)
            return;

        _interaction = updated;
        UpdateInteractionStack(flag, active);
        UpdateStateLayer();
        NotifyInteractionChanged();
        Motion.RequestFrame();
        _presenter?.OnInteractionFrame();
    }

    /// <summary>Begins the pressed state, raising a ripple at <paramref name="origin" />.</summary>
    internal void StartPress(Point origin)
    {
        if (_interaction.HasFlag(InteractionState.Pressed))
            return;

        Set(InteractionState.Pressed, true);

        var presenter = _presenter;
        if (presenter is null || presenter.Bounds.Width <= 0 || presenter.Bounds.Height <= 0)
            return;

        // The press point is in owner coordinates; the ripple works in presenter-local ones.
        var local = _owner.TranslatePoint(origin, presenter) ?? origin;
        var area = new Rect(presenter.Bounds.Size);

        // CommonRipple finishes every older animation before adding a new press. This matters for
        // rapid taps: the old wave is allowed to complete while the new one starts independently.
        foreach (var existing in _ripples)
            existing.Finish();

        var ripple = new RippleAnimation(
            local - area.Position,
            area.Size,
            IsRippleBounded,
            RippleRadius);
        _ripples.Add(ripple);
        Motion.Add(ripple);
        Motion.RequestFrame();
    }

    /// <summary>Ends the pressed state and lets any live ripple fade out.</summary>
    internal void EndPress()
    {
        if (!_interaction.HasFlag(InteractionState.Pressed))
            return;

        Set(InteractionState.Pressed, false);

        foreach (var ripple in _ripples)
            ripple.Release();

        Motion.RequestFrame();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_owner).Properties.IsLeftButtonPressed)
            StartPress(e.GetPosition(_owner));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
            EndPress();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Compose clickable emits a centred press for keyboard activation. Limit this bridge to
        // Button-derived owners so a space typed into an editor cannot start a ripple on its
        // surrounding container.
        if (_owner is not Button || e.Key is not (Key.Space or Key.Enter))
            return;

        StartPress(_presenter is { } presenter
            ? presenter.Bounds.Center
            : _owner.Bounds.Center);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (_owner is Button && e.Key is (Key.Space or Key.Enter))
            EndPress();
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        // Material shows the focus layer for keyboard focus only; a click that focuses a control
        // already has a ripple to say so.
        Set(
            InteractionState.Focused,
            e.NavigationMethod is NavigationMethod.Directional or NavigationMethod.Tab);
    }

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) =>
        ClearInputInteractions();

    private void ClearInputInteractions()
    {
        var hadInputInteraction = _interaction.HasFlag(InteractionState.Hovered)
            || _interaction.HasFlag(InteractionState.Focused)
            || _interaction.HasFlag(InteractionState.Pressed);
        var hadRipples = _ripples.Count != 0;

        _interaction &= ~(InteractionState.Hovered | InteractionState.Focused | InteractionState.Pressed);
        _stateLayerInteractions.RemoveAll(
            static state => state is InteractionState.Hovered or InteractionState.Focused);

        foreach (var ripple in _ripples)
            Motion.Remove(ripple);

        _ripples.Clear();

        // A released ripple can outlive Pressed while its fade completes. Clearing that transient
        // animation is still a visual change even though the interaction flags are already clear;
        // otherwise a detach can leave the owner showing one stale frame.
        if (!hadInputInteraction && !hadRipples)
            return;

        if (hadInputInteraction)
        {
            UpdateStateLayer();
            NotifyInteractionChanged();
        }

        Motion.RequestFrame();
        _presenter?.OnInteractionFrame();
    }

    /// <summary>Removes only interaction-owned transient ripples after they complete.</summary>
    private void RemoveCompletedRipples()
    {
        // Completed ripples would otherwise accumulate for the control's lifetime.
        for (var i = _ripples.Count - 1; i >= 0; i--)
        {
            if (!_ripples[i].IsComplete)
                continue;

            Motion.Remove(_ripples[i]);
            _ripples.RemoveAt(i);
        }
    }

    private void PublishInteractionFrame()
    {
        _presenter?.OnInteractionFrame();
    }

    private void NotifyInteractionChanged()
    {
        _presenter?.OnInteractionChanged(_interaction);
    }

    private void OnTokensChanged()
    {
        ResolveOpacities();
        UpdateStateLayer();
        Motion.RequestFrame();
        _presenter?.OnInteractionFrame();
    }

    private void ResolveOpacities()
    {
        _hoverOpacity = Resolve(MaterialTokens.Sys.State.HoverStateLayerOpacity, InteractionState.Hovered);
        _focusOpacity = Resolve(MaterialTokens.Sys.State.FocusStateLayerOpacity, InteractionState.Focused);
        _pressedOpacity = Resolve(MaterialTokens.Sys.State.PressedStateLayerOpacity, InteractionState.Pressed);
        _draggedOpacity = Resolve(MaterialTokens.Sys.State.DraggedStateLayerOpacity, InteractionState.Dragged);

        double Resolve(MaterialTokenKey<double> key, InteractionState state) =>
            TokenResolver.TryResolve(_owner, key, _owner.ActualThemeVariant, out var value) && value is double d
                ? d
                : StateLayer.Opacity(state);
    }

    private double OpacityFor(InteractionState interaction) => interaction switch
    {
        InteractionState.Hovered => _hoverOpacity,
        InteractionState.Focused => _focusOpacity,
        InteractionState.Pressed => _pressedOpacity,
        InteractionState.Dragged => _draggedOpacity,
        _ => 0
    };

    private void UpdateStateLayer()
    {
        // AndroidX stores entry interactions in arrival order and shows lastOrNull(). A fixed
        // focus-over-hover priority is observably wrong when a pointer enters an already focused
        // control.
        var topmost = _stateLayerInteractions.Count == 0
            ? InteractionState.None
            : _stateLayerInteractions[^1];

        var targetOpacity = OpacityFor(topmost);

        if (topmost == _shownInteraction
            && (_stateLayerAlpha is null || _stateLayerAlpha.Target == targetOpacity))
            return;

        // An idle controller should not allocate animation tracks merely because its owner was
        // themed. The first visible interaction creates the state-layer track on demand.
        if (targetOpacity == 0 && _stateLayerAlpha is null)
            return;

        var duration = topmost == InteractionState.None
            ? StateLayer.FadeOutDuration(_shownInteraction)
            : StateLayer.FadeInDuration(topmost);

        _shownInteraction = topmost;
        StateLayerAlpha.AnimateTo(targetOpacity, duration, Easings.Linear);
    }

    private TweenAnimation StateLayerAlpha =>
        _stateLayerAlpha ??= Motion.AddTween(0, StateLayer.FadeInDuration(InteractionState.Hovered));

    private void UpdateInteractionStack(InteractionState flag, bool active)
    {
        if (flag is not (InteractionState.Hovered or InteractionState.Focused or InteractionState.Dragged))
            return;

        _stateLayerInteractions.Remove(flag);
        if (active)
            _stateLayerInteractions.Add(flag);
    }

    private void DrawRipples(DrawingContext context, IBrush brush)
    {
        foreach (var ripple in _ripples)
            ripple.Draw(context, brush, _pressedOpacity);
    }

    private static void DrawStateLayer(DrawingContext context, IBrush brush, Rect area, double opacity)
    {
        // Compose's RippleNode draws `color.copy(alpha = animatedAlpha)` before Skia compositing.
        // Quantize to the same nearest 8-bit alpha before entering Avalonia's opacity stack.
        using (context.PushOpacity(MaterialInteractionRendering.ComposeOpacity(opacity)))
            // The rounded clip already antialiases the edge. Filling an ordinary rectangle avoids
            // antialiasing that edge a second time and producing a faint corner ring.
            context.DrawRectangle(brush, null, area);
    }

    private static double RippleAnimationEndRadius(Size bounds, bool bounded)
    {
        var covering = Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height) / 2;
        return bounded ? covering + 10 : covering;
    }

    private IBrush WithComposeReplacedAlpha(IBrush source)
    {
        // Compose's `Color.copy(alpha = ...)` replaces the Color alpha. A disabled content colour
        // carries 38% opacity, but a state layer must still use that colour's RGB at the full state
        // alpha. Passing the source brush through PushOpacity would incorrectly multiply the two.
        if (source is not ISolidColorBrush solid)
            return source;

        var color = solid.Color;
        _alphaReplacedBrush.Color = Color.FromArgb(byte.MaxValue, color.R, color.G, color.B);
        return _alphaReplacedBrush;
    }

    /// <summary>Stops a surface from drawing controller state once its lease is disposed.</summary>
    private sealed class PresenterLease : IDisposable
    {
        private readonly MaterialInteractionController _controller;
        private readonly MaterialSurface _presenter;
        private bool _disposed;

        internal PresenterLease(MaterialInteractionController controller, MaterialSurface presenter)
        {
            _controller = controller;
            _presenter = presenter;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _controller.Detach(_presenter);
        }
    }
}
