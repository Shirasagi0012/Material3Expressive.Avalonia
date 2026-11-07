using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

// Button.kt / ToggleButton.kt / SplitButton.kt: shape and elevation collectors live outside
// the indication node. The template layout owns their composition lifetime.
internal sealed class ButtonInteraction : IDisposable
{
    public static readonly AttachedProperty<bool> IsPressedProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsPressed", typeof(ButtonInteraction));
    public static bool GetIsPressed(Button button) => button.GetValue(IsPressedProperty);
    public static void SetIsPressed(Button button, bool value) => button.SetValue(IsPressedProperty, value);

    internal static readonly AttachedProperty<InteractionState?> ElevationInteractionProperty =
        AvaloniaProperty.RegisterAttached<Button, InteractionState?>("ElevationInteraction", typeof(ButtonInteraction));

    private readonly Button _owner;
    private readonly MaterialInteractionController _controller;
    private readonly MaterialInteractionSource _localSource = new();
    private readonly List<MaterialInteractionEvent.Press> _presses = [];
    private readonly List<MaterialInteractionEvent> _elevationInteractions = [];
    private MaterialInteractionSource? _source;
    private bool _collectPresses;

    internal ButtonInteraction(Button owner)
    {
        _owner = owner;
        _controller = MaterialInteraction.GetOrCreate(owner);
        owner.SetValue(ElevationInteractionProperty, InteractionState.None);
        UpdateShapeCollection();
        owner.PropertyChanged += OwnerChanged;
        ConnectSource();
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialButton.InteractionSourceProperty) ConnectSource();
        if (e.Property == MaterialButton.PressedCornerRadiusProperty || e.Property == MaterialButton.ShapeProperty
            || e.Property == MaterialButton.PressedShapeProperty) UpdateShapeCollection();
    }

    private void UpdateShapeCollection()
    {
        var collect = _owner is ToggleButton || MaterialButton.GetPressedCornerRadius(_owner) is not null
            || MaterialButton.GetShape(_owner) is not null && MaterialButton.GetPressedShape(_owner) is not null;
        if (_collectPresses == collect) return;
        _collectPresses = collect;
        _presses.Clear();
        SetIsPressed(_owner, false);
    }

    private void ConnectSource()
    {
        var source = MaterialButton.GetInteractionSource(_owner) ?? _localSource;
        if (ReferenceEquals(_source, source)) return;
        var replacing = _source is not null;
        var cancelsNativePress = replacing && _owner.IsPressed;
        _controller.ConnectSource(null);
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = source;
        // collectIsPressedAsState remembers the bool, but its list is inside LaunchedEffect.
        // Elevation's list is remembered outside that effect and survives source replacement.
        _presses.Clear();
        source.Interaction += ReceiveInteraction;
        _controller.ConnectSource(source);
        if (replacing) _controller.ResetSourceIndication(preserveSurfaceMotion: true);
        if (cancelsNativePress) _controller.NotifyInteractionChanged(deferShape: true);
    }

    private void ReceiveInteraction(MaterialInteractionEvent interaction)
    {
        if (_collectPresses)
        {
            switch (interaction)
            {
                case MaterialInteractionEvent.Press press: _presses.Add(press); break;
                case MaterialInteractionEvent.Release release: _presses.Remove(release.Start); break;
                case MaterialInteractionEvent.CancelPress cancel: _presses.Remove(cancel.Start); break;
            }
            SetIsPressed(_owner, _presses.Count != 0);
        }

        switch (interaction)
        {
            case MaterialInteractionEvent.Press or MaterialInteractionEvent.HoverEnter or MaterialInteractionEvent.Focus:
                _elevationInteractions.Add(interaction); break;
            case MaterialInteractionEvent.Release release: _elevationInteractions.Remove(release.Start); break;
            case MaterialInteractionEvent.CancelPress cancel: _elevationInteractions.Remove(cancel.Start); break;
            case MaterialInteractionEvent.HoverExit exit: _elevationInteractions.Remove(exit.Start); break;
            case MaterialInteractionEvent.Unfocus unfocus: _elevationInteractions.Remove(unfocus.Start); break;
        }
        _owner.SetValue(ElevationInteractionProperty, _elevationInteractions.LastOrDefault() switch
        {
            MaterialInteractionEvent.Press => InteractionState.Pressed,
            MaterialInteractionEvent.HoverEnter => InteractionState.Hovered,
            MaterialInteractionEvent.Focus => InteractionState.Focused,
            _ => InteractionState.None,
        });
    }

    public void Dispose()
    {
        _owner.PropertyChanged -= OwnerChanged;
        _controller.ConnectSource(null);
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = null;
        _presses.Clear();
        _elevationInteractions.Clear();
        SetIsPressed(_owner, false);
        _owner.ClearValue(ElevationInteractionProperty);
    }
}
