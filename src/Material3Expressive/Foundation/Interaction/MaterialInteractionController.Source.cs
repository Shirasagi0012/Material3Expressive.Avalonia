using System.Collections.Generic;

namespace Material3Expressive.Foundation.Interaction;

internal sealed partial class MaterialInteractionController
{
    private MaterialInteractionSource? _source;
    private bool _receivingSource;
    private bool _deferSourceStateLayer;
    private MaterialInteractionEvent.Press? _nativePress;
    private readonly Dictionary<Avalonia.Input.Key, MaterialInteractionEvent.Press> _nativeKeyPresses = [];
    private readonly Dictionary<InteractionState, MaterialInteractionEvent> _nativeInteractions = [];
    private readonly List<MaterialInteractionEvent> _sourceInteractions = [];
    private readonly Dictionary<MaterialInteractionEvent.Press, RippleAnimation> _sourceRipples = [];
    private readonly HashSet<MaterialInteractionEvent> _previousIndicationStarts = [];

    internal void ResetSourceIndication()
    {
        // A new Compose indication node observes future events only. The component's
        // independent interaction collector retains its active shape/color state.
        ResetAnimations();
        _sourceRipples.Clear();
        _previousIndicationStarts.Clear();
        _previousIndicationStarts.UnionWith(_sourceInteractions);
        ResetIndicationObservers();
        _motion = ComposeAnimation.AnimationScope.Get(_owner).CreateChild("Interaction motion");
        _presenter?.OnInteractionFrame();
    }

    private static InteractionState StateOf(MaterialInteractionEvent interaction) => interaction switch
    {
        MaterialInteractionEvent.Press => InteractionState.Pressed,
        MaterialInteractionEvent.Focus => InteractionState.Focused,
        MaterialInteractionEvent.HoverEnter => InteractionState.Hovered,
        MaterialInteractionEvent.DragStart => InteractionState.Dragged,
        _ => InteractionState.None,
    };

    internal void ConnectSource(MaterialInteractionSource? source)
    {
        if (ReferenceEquals(_source, source)) return;
        CancelSourceInput();
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = source;
        _sourceInteractions.Clear();
        _sourceRipples.Clear();
        _previousIndicationStarts.Clear();
        ResetIndicationObservers(sourceChanged: true);
        _receivingSource = true;
        try
        {
            EndPress();
            Set(InteractionState.Focused, false);
            Set(InteractionState.Hovered, false);
            Set(InteractionState.Dragged, false);
        }
        finally { _receivingSource = false; }
        if (_source is not null)
        {
            _source.Interaction += ReceiveInteraction;
            if (_owner.IsFocused && IsEnabled) EmitNative(InteractionState.Focused, true);
        }
    }

    private void EmitNative(InteractionState state, bool active)
    {
        if (_source is null || active && !IsEnabled) return;
        if (active)
        {
            if (_nativeInteractions.ContainsKey(state)) return;
            MaterialInteractionEvent start = state switch
            {
                InteractionState.Focused => new MaterialInteractionEvent.Focus(),
                InteractionState.Hovered => new MaterialInteractionEvent.HoverEnter(),
                InteractionState.Dragged => new MaterialInteractionEvent.DragStart(),
                _ => throw new InvalidOperationException("Presses carry their position through StartPress."),
            };
            _nativeInteractions.Add(state, start);
            _source.Emit(start);
        }
        else if (_nativeInteractions.Remove(state, out var start)) _source.Emit(End(start));
    }

    private static MaterialInteractionEvent End(MaterialInteractionEvent start) => start switch
    {
        MaterialInteractionEvent.Focus focus => new MaterialInteractionEvent.Unfocus(focus),
        MaterialInteractionEvent.HoverEnter hover => new MaterialInteractionEvent.HoverExit(hover),
        MaterialInteractionEvent.DragStart drag => new MaterialInteractionEvent.DragCancel(drag),
        MaterialInteractionEvent.Press press => new MaterialInteractionEvent.CancelPress(press),
        _ => throw new ArgumentException("Expected an interaction start.", nameof(start)),
    };

    private void CancelSourceInput()
    {
        if (_source is null) return;
        CancelSourceKeyPresses();
        var starts = _nativeInteractions.Values.ToArray();
        _nativeInteractions.Clear();
        if (_nativePress is { } press)
        {
            _nativePress = null;
            _source.Emit(new MaterialInteractionEvent.CancelPress(press));
        }
        foreach (var start in starts) _source.Emit(End(start));
    }

    private void CancelSourceKeyPresses()
    {
        var presses = _nativeKeyPresses.Values.ToArray();
        _nativeKeyPresses.Clear();
        foreach (var press in presses) _source?.Emit(new MaterialInteractionEvent.CancelPress(press));
    }

    private void ReceiveInteraction(MaterialInteractionEvent interaction)
    {
        var start = interaction switch
        {
            MaterialInteractionEvent.Release e => e.Start,
            MaterialInteractionEvent.CancelPress e => e.Start,
            MaterialInteractionEvent.Unfocus e => e.Start,
            MaterialInteractionEvent.HoverExit e => e.Start,
            MaterialInteractionEvent.DragStop e => e.Start,
            MaterialInteractionEvent.DragCancel e => e.Start,
            _ => null as MaterialInteractionEvent,
        };
        if (start is null) _sourceInteractions.Add(interaction);
        else { _sourceInteractions.Remove(start); _previousIndicationStarts.Remove(start); }
        var wasReceiving = _receivingSource;
        _receivingSource = true;
        try
        {
            if (interaction is MaterialInteractionEvent.Press press)
            {
                var count = _ripples.Count;
                StartPress(press.Position);
                if (_ripples.Count > count) _sourceRipples[press] = _ripples[^1];
            }
            else if (start is MaterialInteractionEvent.Press released && _sourceRipples.Remove(released, out var ripple))
                ripple.Finish();
            // Preserve identity and arrival order, including two overlapping starts of the same kind.
            // StateLayer uses the last surviving interaction; shape uses the aggregate flags.
            _interactions.Clear();
            _interactions.AddRange(_sourceInteractions.Select(StateOf));
            _interaction = _interactions.Aggregate(InteractionState.None, (flags, next) => flags | next);
            var indicationGeneration = _indicationGeneration;
            if (_deferSourceStateLayer && Motion is { } motion)
                _ = motion.Launch(async ct =>
                {
                    await motion.NextFrameAsync(ct);
                    if (indicationGeneration == _indicationGeneration) PublishIndication(interaction);
                }, "Disabled source focus indication exit");
            else PublishIndication(interaction);
            if (!_deferSourceStateLayer) UpdateStateLayer();
            NotifyInteractionChanged();
        }
        finally { _receivingSource = wasReceiving; }
    }
}
