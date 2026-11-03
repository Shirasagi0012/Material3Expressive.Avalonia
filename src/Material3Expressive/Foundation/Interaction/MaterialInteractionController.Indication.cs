using Avalonia.Controls;
using Material3Expressive.Components;

namespace Material3Expressive.Foundation.Interaction;

internal sealed partial class MaterialInteractionController
{
    // Independent of the Surface attachment: track focus and thumb ripple can observe the
    // same owner. Subscribers observe future events only and own their Desktop input-mode
    // filter, whose counters belong to each collection in Core Indication.skiko.kt.
    internal event Action<MaterialInteractionEvent>? IndicationInteraction;
    internal event Action? IndicationReset;
    private readonly Dictionary<InteractionState, MaterialInteractionEvent> _indicationNativeStarts = [];
    private int _indicationGeneration;
    private readonly HashSet<InteractionState> _previousNativeIndicationStates = [];

    // Checkbox.kt, RadioButton.kt and ListItem.kt keep one ripple node across focus-theme changes.
    // Surface/Switch instead add a focus-only indication and replace the clickable ripple.
    internal bool HasSeparateFocusIndication => _owner is not (CheckBox or RadioButton or ListItem);

    private void ResetRippleForFocusFeedback()
    {
        if (!HasSeparateFocusIndication)
        {
            _presenter?.OnInteractionFrame();
            return;
        }
        // Surface/Switch change the clickable ripple factory's enableFocusIndication.
        // The replacement collector observes future starts; shape/elevation keep their state.
        _stateLayerAlpha?.Stop();
        _stateLayerSubscription?.Dispose();
        _stateLayerSubscription = null;
        _stateLayerAlpha = null;
        _shownInteraction = InteractionState.None;
        foreach (var ripple in _ripples.ToArray()) ripple.Dispose();
        _ripples.Clear();
        _sourceRipples.Clear();
        _previousIndicationStarts.UnionWith(_sourceInteractions);
        _previousNativeIndicationStates.UnionWith(_interactions);
        _presenter?.OnInteractionFrame();
    }

    private void PublishNativeIndication(InteractionState state, bool active)
    {
        if (state == InteractionState.Pressed) return;
        if (active)
        {
            MaterialInteractionEvent start = state switch
            {
                InteractionState.Focused => new MaterialInteractionEvent.Focus(),
                InteractionState.Hovered => new MaterialInteractionEvent.HoverEnter(),
                InteractionState.Dragged => new MaterialInteractionEvent.DragStart(),
                _ => throw new ArgumentOutOfRangeException(nameof(state)),
            };
            _indicationNativeStarts[state] = start;
            PublishIndication(start);
        }
        else if (_indicationNativeStarts.Remove(state, out var start)) PublishIndication(End(start));
    }

    private void PublishIndication(MaterialInteractionEvent interaction) => IndicationInteraction?.Invoke(interaction);

    private void ResetIndicationObservers(bool sourceChanged = false)
    {
        if (sourceChanged)
        {
            _indicationGeneration++;
            _indicationNativeStarts.Clear();
        }
        IndicationReset?.Invoke();
    }
}
