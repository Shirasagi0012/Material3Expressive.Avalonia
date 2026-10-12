using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Threading;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;

namespace Material3Expressive.Foundation.Interaction;

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
    private VisualAnimationScope? _motion;
    private readonly List<RippleAnimation> _ripples = [];
    private readonly List<InteractionState> _interactions = [];
    private readonly SolidColorBrush _alphaReplacedBrush = new();

    private Animatable<float>? _stateLayerAlpha;
    private ButtonElevationController? _buttonElevation;
    private FloatingActionButtonElevationController? _fabElevation;
    private MaterialSurface? _presenter;
    private InteractionState _interaction;
    private InteractionState _shownInteraction;
    private Point? _keyboardPressCenter;
    private IPointer? _pressedPointer;
    private bool _hoverNeedsMove;
    private bool _indicationFocused = true;

    internal MaterialInteractionController(Control owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        _owner.PointerEntered += (_, _) =>
        {
            _keyboardPressCenter = new Rect(_owner.Bounds.Size).Center;
            if (!_hoverNeedsMove) Set(InteractionState.Hovered, true);
        };
        _owner.PointerExited += (_, e) =>
        {
            _keyboardPressCenter = new Rect(_owner.Bounds.Size).Center;
            // Avalonia 12 suppresses pointer-over when an implicitly captured template part
            // differs from the current hit target. A resizing thumb can cause that while the
            // pointer has stayed inside. Compose hoverable observes the owner's bounds.
            if (_pressedPointer?.Captured is Visual captured
                && (ReferenceEquals(captured, _owner) || _owner.IsVisualAncestorOf(captured))
                && new Rect(_owner.Bounds.Size).Contains(e.GetPosition(_owner))) return;
            Set(InteractionState.Hovered, false);
        };
        _owner.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        // The native class handler requests focus first. Observe the press afterwards (including
        // handled events), matching Desktop clickable's Focus -> Press interaction order.
        _owner.AddHandler(
            InputElement.PointerPressedEvent,
            OnPointerPressed,
            RoutingStrategies.Bubble,
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
        _owner.PointerCaptureLost += (_, _) => { _pressedPointer = null; EndPress(); };
        _owner.GotFocus += OnGotFocus;
        _owner.AttachedToVisualTree += (_, _) => MaterialInputMode.For(_owner);
        _owner.LostFocus += (_, _) =>
        {
            EndPress();
            Set(InteractionState.Focused, false);
        };
        _owner.DetachedFromLogicalTree += OnDetachedFromLogicalTree;
        _owner.PropertyChanged += (_, change) =>
        {
            if (change.Property == InputElement.IsEffectivelyEnabledProperty
                || change.Property == MaterialInteraction.IsInteractiveProperty)
            {
                if (!IsEnabled)
                {
                    _hoverNeedsMove = true; // A re-enabled Compose hoverable waits for pointer input.
                    ClearInputInteractions(finishRipples: true, deferStateLayer: true);
                }
                NotifyInteractionChanged(deferShape: !IsEnabled);
            }
            else if (change.Property == MaterialButton.ElevationProperty || change.Property == FloatingActionButton.ElevationProperty)
                UpdateButtonElevation();
        };
    }

    /// <summary>The attached presenter's shared frame clock and cancellation lifetime.</summary>
    internal VisualAnimationScope? Motion => _motion;

    /// <summary>Which interactions the owner is currently under.</summary>
    internal InteractionState Interaction => _interaction;
    // Chip.kt chooses the last active interaction, retaining its outgoing identity.
    internal InteractionState LastInteraction => _interactions.LastOrDefault();
    internal event Action? Changed;
    internal bool IsEnabled => _owner.IsEffectivelyEnabled && MaterialInteraction.GetIsInteractive(_owner);

    /// <summary>The state layer's current opacity, mid-fade included.</summary>
    internal MaterialSurface? Presenter => _presenter;
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

    // Switch deliberately follows its resizing/moving thumb instead of freezing the local
    // unbounded center from the first draw. Other callers retain the fixed Compose behavior.
    internal bool RippleFollowsPresenterCenter { get; set; }

    // Components with an upstream MappedInteractionSource can supply its mapping while
    // retaining this controller's input, ripple and lifecycle implementation.
    internal Func<Point, Point>? PressPositionMapper { get; set; }

    /// <summary>
    /// Attaches the template's MaterialSurface as the render front-end, returning the lease that
    /// detaches it. A second attach replaces the first; a disposed lease stops its surface from
    /// drawing controller state (template-replacement protection).
    /// </summary>
    internal IDisposable Attach(MaterialSurface presenter)
    {
        ArgumentNullException.ThrowIfNull(presenter);
        if (_presenter is not null && !ReferenceEquals(_presenter, presenter))
            ResetAnimations();
        _presenter = presenter;
        _motion ??= VisualAnimationScope.Create(_owner);
        var inputMode = MaterialInputMode.For(_owner);
        if (_owner.IsFocused && !_interaction.HasFlag(InteractionState.Focused))
            _indicationFocused = inputMode?.IsKeyboard ?? true;
        Set(InteractionState.Focused, _owner.IsFocused);
        UpdateStateLayer();
        // Sync the state the surface missed before attaching (e.g. an already-hovered template
        // swap), so the first frame paints the right thing.
        NotifyInteractionChanged();
        return new PresenterLease(this, presenter);
    }

    private void Detach(MaterialSurface presenter)
    {
        if (ReferenceEquals(_presenter, presenter))
        {
            ResetAnimations();
            _presenter = null;
            ClearInputInteractions();
        }
    }

    private void ResetAnimations()
    {
        _buttonElevation?.Dispose();
        _buttonElevation = null;
        _fabElevation?.Dispose();
        _fabElevation = null;
        _presenter?.ResetMotion();
        var motion = _motion;
        _motion = null;
        motion?.Dispose();
        _stateLayerAlpha = null;
        _shownInteraction = InteractionState.None;
        foreach (var ripple in _ripples.ToArray())
            ripple.Dispose();
        _ripples.Clear();
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
                DrawRipples(context, composeColorBrush, size);
            }
            else
            {
                DrawRipples(context, composeColorBrush, size);
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
                var radius = RippleRadius ?? RippleAnimation.EndRadius(area.Size, bounded: false);
                using (context.PushOpacity(MaterialInteractionRendering.ComposeOpacity(opacity)))
                    context.DrawEllipse(composeColorBrush, null, area.Center, radius, radius);
            }
        }
    }

    /// <summary>Sets or clears one interaction flag.</summary>
    internal void Set(InteractionState flag, bool active)
    {
        if (active && !IsEnabled)
            return;
        var updated = active ? _interaction | flag : _interaction & ~flag;
        if (updated == _interaction)
            return;

        _interaction = updated;
        UpdateInteractionStack(flag, active);
        UpdateStateLayer();
        NotifyInteractionChanged();
    }

    /// <summary>Begins the pressed state, raising a ripple at <paramref name="origin" />.</summary>
    internal void StartPress(Point origin)
    {
        if (!IsEnabled || _interaction.HasFlag(InteractionState.Pressed))
            return;

        Set(InteractionState.Pressed, true);

        var presenter = _presenter;
        if (presenter is null || Motion is not { IsEnding: false } motion
            || presenter.Bounds.Width <= 0 || presenter.Bounds.Height <= 0)
            return;

        // The press point is in owner coordinates; the ripple works in presenter-local ones.
        var local = PressPositionMapper?.Invoke(origin) ?? (_owner.TranslatePoint(origin, presenter) ?? origin);

        // CommonRipple finishes every older animation before adding a new press. This matters for
        // rapid taps: the old wave is allowed to complete while the new one starts independently.
        foreach (var existing in _ripples.ToArray())
            existing.Finish();

        var ripple = new RippleAnimation(
            local,
            presenter.Bounds.Size,
            motion,
            IsRippleBounded,
            RippleRadius);
        _ripples.Add(ripple);
        _ = ripple.Motion.Launch(async ct =>
        {
            try
            {
                await ripple.AnimateAsync(presenter.OnInteractionFrame, ct);
            }
            finally
            {
                _ripples.Remove(ripple);
                // Request cleanup; the scope stays registered until this operation exits.
                ripple.Dispose();
                presenter.OnInteractionFrame();
            }
        }, "Ripple playback");
    }

    /// <summary>Ends the pressed state and lets any live ripple fade out.</summary>
    internal void EndPress()
    {
        if (!_interaction.HasFlag(InteractionState.Pressed))
            return;

        Set(InteractionState.Pressed, false);

        foreach (var ripple in _ripples.ToArray())
            ripple.Finish();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _keyboardPressCenter = new Rect(_owner.Bounds.Size).Center;
        // A descendant button can consume the press. Observe the owner's native class
        // handler above, rather than giving its clickable ancestor a second ripple.
        if (_owner is Button { IsPressed: false }) return;
        if (IsFromHostedPopup(e.Source)) return;
        if (IsEnabled && e.GetCurrentPoint(_owner).Properties.IsLeftButtonPressed)
        {
            _pressedPointer = e.Pointer;
            StartPress(e.GetPosition(_owner));
        }
    }

    // Input from a popup the owner hosts (a MenuItem's submenu) is routed through the owner, but the
    // popup is not inside the owner's visual bounds and its controls handle that input themselves.
    // Treating it as the owner's own press rippled every submenu parent from the clicked point.
    private bool IsFromHostedPopup(object? source) =>
        source is Visual visual && !ReferenceEquals(visual, _owner) && !_owner.IsVisualAncestorOf(visual);

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _keyboardPressCenter = new Rect(_owner.Bounds.Size).Center;
        if (_hoverNeedsMove && IsEnabled && !IsFromHostedPopup(e.Source))
        {
            _hoverNeedsMove = false;
            Set(InteractionState.Hovered, new Rect(_owner.Bounds.Size).Contains(e.GetPosition(_owner)));
        }
        if (!ReferenceEquals(_pressedPointer, e.Pointer)
            || new Rect(_owner.Bounds.Size).Contains(e.GetPosition(_owner)))
            return;

        // Compose clickable cancels the gesture when it leaves the interaction bounds.
        // Releasing Avalonia's owned capture also clears Button.IsPressed and its pending click.
        Set(InteractionState.Hovered, false);
        _pressedPointer = null;
        EndPress();
        if (e.Pointer.Captured is Visual captured
            && (ReferenceEquals(captured, _owner) || _owner.IsVisualAncestorOf(captured)))
            e.Pointer.Capture(null);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _keyboardPressCenter = new Rect(_owner.Bounds.Size).Center;
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            _pressedPointer = null;
            EndPress();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Compose clickable emits a centred press for keyboard activation. Limit this bridge to
        // Button-derived owners so a space typed into an editor cannot start a ripple on its
        // surrounding container.
        if (_owner is not Button || !ReferenceEquals(e.Source, _owner) || e.Key is not (Key.Space or Key.Enter))
            return;

        // Observe feedback only; the owner retains its keyboard activation semantics.
        // AbstractClickableNode starts centerOffset at zero in the clickable layout, which
        // is the Surface container, not its outer minimum-interactive allocation. Mapped
        // sources (navigation items) instead originate in the owner before their mapping.
        // Pointer input replaces this zero with the last observed clickable bounds' centre.
        var origin = _keyboardPressCenter ?? (PressPositionMapper is null && _presenter is { } surface
            ? surface.TranslatePoint(default, _owner) ?? default : default);
        StartPress(origin);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (_owner is Button && ReferenceEquals(e.Source, _owner) && e.Key is (Key.Space or Key.Enter))
            EndPress();
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        // Focusable.kt emits FocusInteraction for actual focus, including pointer and
        // programmatic focus. Core's desktop indication filters the same event by input
        // mode; elevation does not. Preserve the decision until the matching unfocus.
        if (_owner.IsFocused && !_interaction.HasFlag(InteractionState.Focused))
        {
            var mode = MaterialInputMode.For(_owner);
            mode?.ObserveFocusNavigation(e.NavigationMethod);
            _indicationFocused = mode?.IsKeyboard ?? true;
        }
        Set(InteractionState.Focused, _owner.IsFocused);
    }

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) =>
        ClearInputInteractions();

    private void ClearInputInteractions(bool finishRipples = false, bool deferStateLayer = false)
    {
        _pressedPointer = null;
        // A new hoverable node waits for pointer input. The native owner can remain
        // pointer-over while its feedback template is replaced, so no new Enter is
        // guaranteed; the next Move must be able to restore hover as well.
        _hoverNeedsMove = true;
        var hadInputInteraction = _interaction != InteractionState.None;
        var hadRipples = _ripples.Count != 0;

        _interaction = InteractionState.None;
        _interactions.Clear();
        _indicationFocused = true;

        foreach (var ripple in _ripples.ToArray())
        {
            if (finishRipples) ripple.Finish();
            else ripple.Dispose();
        }
        if (!finishRipples) _ripples.Clear();

        if (hadInputInteraction)
        {
            // Clickable's enabled update disposes interactions during recomposition.
            // Its StateLayer exit starts after that event frame, unlike pointer exits,
            // whose interaction collector runs directly before the next animation frame.
            if (deferStateLayer && Motion is { IsEnding: false } motion)
                _ = motion.Launch(async ct =>
                {
                    await motion.Clock.NextFrameAsync(ct);
                    UpdateStateLayer();
                }, "Disabled state layer exit");
            else UpdateStateLayer();
            if (!deferStateLayer) NotifyInteractionChanged();
        }
        else if (hadRipples)
        {
            // A released ripple can outlive Pressed; removing it still changes the visual.
            _presenter?.OnInteractionFrame();
        }
    }

    private void NotifyInteractionChanged(bool deferShape = false)
    {
        Changed?.Invoke();
        UpdateButtonElevation();
        // Disabling cancels Clickable's press during composition. Its shape observer therefore
        // invalidates the following composition, unlike cancellation by a pointer event.
        if (deferShape && Motion is { IsEnding: false } motion)
        {
            _presenter?.DeferShapeInteraction();
            _ = motion.Launch(async ct =>
            {
                await motion.Clock.NextFrameAsync(ct);
                // Drain this frame's existing animation callbacks before registering the
                // invalidated composition, as Core's frame recomposer does after sendFrame.
                Dispatcher.UIThread.Post(() =>
                {
                    if (!ct.IsCancellationRequested) _presenter?.OnInteractionChanged(_interaction);
                }, DispatcherPriority.Background);
            }, "Disabled shape interaction");
        }
        else _presenter?.OnInteractionChanged(_interaction);
    }

    private void UpdateButtonElevation()
    {
        if (_owner is FloatingActionButton fab)
        {
            if (_presenter is null || _motion is null || _motion.IsEnding) return;
            if (fab.Elevation is not { } elevation)
            {
                _fabElevation?.Dispose();
                _fabElevation = null;
                return;
            }
            var fabInteraction = _interactions.LastOrDefault(state => state is
                InteractionState.Pressed or InteractionState.Hovered or InteractionState.Focused);
            _fabElevation ??= new FloatingActionButtonElevationController(_presenter, _motion, elevation, fabInteraction);
            _fabElevation.Update(elevation, fabInteraction);
            return;
        }
        if (_owner is not Button button || MaterialButton.GetElevation(button) is not { } values)
        {
            _buttonElevation?.Dispose();
            _buttonElevation = null;
            return;
        }
        if (_presenter is null || _motion is null || _motion.IsEnding)
            return;

        // Button.kt collects only press, hover, and focus; drag belongs to other components.
        var interaction = _interactions.LastOrDefault(state => state is
            InteractionState.Pressed or InteractionState.Hovered or InteractionState.Focused);
        _buttonElevation ??= new ButtonElevationController(_presenter, _motion,
            ButtonElevationController.Target(values, interaction, IsEnabled));
        _buttonElevation.Update(values, interaction, IsEnabled);
    }

    internal void UpdateStateLayerConfiguration()
    {
        UpdateStateLayer();
        _presenter?.OnInteractionFrame();
    }

    private double OpacityFor(InteractionState interaction) => interaction switch
    {
        InteractionState.Hovered => _presenter?.HoverStateLayerOpacity ?? 0,
        InteractionState.Focused => _presenter?.FocusStateLayerOpacity ?? 0,
        InteractionState.Pressed => _presenter?.PressedStateLayerOpacity ?? 0,
        InteractionState.Dragged => _presenter?.DraggedStateLayerOpacity ?? 0,
        _ => 0
    };

    private void UpdateStateLayer()
    {
        // AndroidX stores entry interactions in arrival order and shows lastOrNull(). A fixed
        // focus-over-hover priority is observably wrong when a pointer enters an already focused
        // control.
        var topmost = _interactions.LastOrDefault(state => state != InteractionState.Pressed
            && (state != InteractionState.Focused || _indicationFocused));

        // Ripple.StateLayer stores and animates alpha as Float, including token values.
        var targetOpacity = (float)OpacityFor(topmost);

        if (topmost == _shownInteraction
            && (_stateLayerAlpha is null || _stateLayerAlpha.TargetValue == targetOpacity))
            return;

        // An idle controller should not allocate animation tracks merely because its owner was
        // themed. The first visible interaction creates the state-layer track on demand.
        if (targetOpacity == 0 && _stateLayerAlpha is null)
            return;

        var duration = topmost == InteractionState.None
            ? StateLayer.FadeOutDuration(_shownInteraction)
            : StateLayer.FadeInDuration(topmost);

        _shownInteraction = topmost;
        if (Motion is not { IsEnding: false } motion)
            return;
        var surface = _presenter;
        if (_stateLayerAlpha is null)
        {
            _stateLayerAlpha = motion.CreateAnimatable(0f);
            motion.SubscribeValue(_stateLayerAlpha, _ => surface?.OnInteractionFrame());
        }
        var alpha = _stateLayerAlpha;
        _ = motion.Launch(ct => alpha.AnimateToAsync(targetOpacity,
            new TweenSpec<float>(duration, easing: ComposeEasings.Linear), cancellationToken: ct), "State layer");
    }

    private void UpdateInteractionStack(InteractionState flag, bool active)
    {
        _interactions.Remove(flag);
        if (active)
            _interactions.Add(flag);
    }

    private void DrawRipples(DrawingContext context, IBrush brush, Size size)
    {
        foreach (var ripple in _ripples)
            ripple.Draw(context, brush, OpacityFor(InteractionState.Pressed), size, RippleFollowsPresenterCenter);
    }

    // Switch's ThumbNode measures its current animated size before launching the pressed snap.
    // Avalonia can settle the subsequent layout before the first render. Preserve the upstream
    // first-draw geometry from that measurement without moving ripple playback into the layout.
    internal void InitializeRippleDrawGeometry(Size size)
    {
        foreach (var ripple in _ripples) ripple.InitializeDrawGeometry(size);
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
