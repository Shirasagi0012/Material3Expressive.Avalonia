// Portions translated from AndroidX Slider.kt / rangeSliderPressDragModifier / ThumbContent.
// Copyright 2022 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using Avalonia;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

public sealed partial class RangeSlider
{
    public static readonly StyledProperty<MaterialInteractionSource?> StartThumbInteractionSourceProperty =
        AvaloniaProperty.Register<RangeSlider, MaterialInteractionSource?>(nameof(StartThumbInteractionSource));
    public static readonly StyledProperty<MaterialInteractionSource?> EndThumbInteractionSourceProperty =
        AvaloniaProperty.Register<RangeSlider, MaterialInteractionSource?>(nameof(EndThumbInteractionSource));

    /// <summary>The start thumb's paired press, drag, focus and hover events.</summary>
    public MaterialInteractionSource? StartThumbInteractionSource { get => GetValue(StartThumbInteractionSourceProperty); set => SetValue(StartThumbInteractionSourceProperty, value); }
    /// <summary>The end thumb's paired press, drag, focus and hover events.</summary>
    public MaterialInteractionSource? EndThumbInteractionSource { get => GetValue(EndThumbInteractionSourceProperty); set => SetValue(EndThumbInteractionSourceProperty, value); }

    private readonly MaterialInteractionSource _implicitStartSource = new(), _implicitEndSource = new();
    private MaterialInteractionSource StartSource => StartThumbInteractionSource ?? _implicitStartSource;
    private MaterialInteractionSource EndSource => EndThumbInteractionSource ?? _implicitEndSource;
    private MaterialInteractionSource? _observedStart, _observedEnd, _gestureSource;
    private Action<MaterialInteractionEvent>? _startListener, _endListener;
    private readonly List<MaterialInteractionEvent> _startEvents = [], _endEvents = [];
    private MaterialInteractionEvent.Press? _pressInteraction;
    private MaterialInteractionEvent.DragStart? _dragInteraction;
    private MaterialInteractionEvent.Focus? _startFocus, _endFocus;

    private void ConnectInteractions()
    {
        if (_observedStart is null)
        {
            var start = _observedStart = StartSource;
            start.Interaction += _startListener = interaction => { if (_observedStart == start) OnStartInteraction(interaction); };
        }
        if (_observedEnd is null)
        {
            var end = _observedEnd = EndSource;
            end.Interaction += _endListener = interaction => { if (_observedEnd == end) OnEndInteraction(interaction); };
        }
    }

    private void DisconnectInteractions()
    {
        EndFocusInteractions();
        if (_observedStart is not null) _observedStart.Interaction -= _startListener;
        if (_observedEnd is not null) _observedEnd.Interaction -= _endListener;
        _observedStart = _observedEnd = null;
        _startEvents.Clear(); _endEvents.Clear();
        SetAndRaise(StartInteractionProperty, ref _startInteraction, InteractionState.None);
        SetAndRaise(EndInteractionProperty, ref _endInteraction, InteractionState.None);
        RefreshVisuals();
    }

    private void OnStartInteraction(MaterialInteractionEvent interaction)
    {
        SetAndRaise(StartInteractionProperty, ref _startInteraction, SliderInteraction.Update(_startEvents, interaction));
        RefreshVisuals();
    }

    private void OnEndInteraction(MaterialInteractionEvent interaction)
    {
        SetAndRaise(EndInteractionProperty, ref _endInteraction, SliderInteraction.Update(_endEvents, interaction));
        RefreshVisuals();
    }

    private void SetFocusInteraction(bool start, bool focused)
    {
        ref var active = ref (start ? ref _startFocus : ref _endFocus);
        var source = start ? _observedStart : _observedEnd;
        if (focused && IsEffectivelyEnabled && active is null && source is not null)
        {
            var focus = active = new();
            source.Emit(focus);
        }
        else if (!focused && active is { } focus)
        {
            active = null;
            source?.Emit(new MaterialInteractionEvent.Unfocus(focus));
        }
    }

    private void EndFocusInteractions() { SetFocusInteraction(true, false); SetFocusInteraction(false, false); }

    private void BeginDragInteraction()
    {
        // rangeSliderPressDragModifier cancels the press before emitting Drag.Start. When the
        // approved collapsed-range rule switches ends, cancel on the original source first.
        var source = _gestureSource;
        var press = _pressInteraction; _pressInteraction = null;
        if (press is not null) source?.Emit(new MaterialInteractionEvent.CancelPress(press));
        if (_gestureSource != source) return;
        _gestureSource = _draggingStart ? StartSource : EndSource;
        var drag = _dragInteraction = new();
        _gestureSource.Emit(drag);
    }

    private void EndGestureInteraction(bool cancelled)
    {
        var source = _gestureSource;
        var press = _pressInteraction; var drag = _dragInteraction;
        _gestureSource = null; _pressInteraction = null; _dragInteraction = null;
        if (press is not null) source?.Emit(cancelled ? new MaterialInteractionEvent.CancelPress(press) : new MaterialInteractionEvent.Release(press));
        if (drag is not null) source?.Emit(cancelled ? new MaterialInteractionEvent.DragCancel(drag) : new MaterialInteractionEvent.DragStop(drag));
    }
}

internal static class SliderInteraction
{
    // ThumbContent keeps start identities, including overlapping events of the same type.
    public static InteractionState Update(List<MaterialInteractionEvent> active, MaterialInteractionEvent interaction)
    {
        var ended = interaction switch
        {
            MaterialInteractionEvent.Release e => e.Start,
            MaterialInteractionEvent.CancelPress e => e.Start,
            MaterialInteractionEvent.DragStop e => e.Start,
            MaterialInteractionEvent.DragCancel e => e.Start,
            MaterialInteractionEvent.Unfocus e => e.Start,
            MaterialInteractionEvent.HoverExit e => e.Start,
            _ => null as MaterialInteractionEvent
        };
        if (ended is null) active.Add(interaction); else active.Remove(ended);
        return active.Aggregate(InteractionState.None, (state, next) => state | (next switch
        {
            MaterialInteractionEvent.Press => InteractionState.Pressed,
            MaterialInteractionEvent.DragStart => InteractionState.Dragged,
            MaterialInteractionEvent.Focus => InteractionState.Focused,
            _ => InteractionState.None
        }));
    }
}
