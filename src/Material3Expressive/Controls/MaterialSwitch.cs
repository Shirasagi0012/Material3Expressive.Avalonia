using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 switch.
/// </summary>
/// <remarks>
/// Derives from <see cref="ToggleButton" /> rather than <see cref="Avalonia.Controls.ToggleSwitch" />: the whole
/// switch is painted here, so <c>ToggleSwitch</c>'s knob template parts and knob animation would
/// be dead machinery, and its <c>OnContent</c>/<c>OffContent</c> surface would be a promise this
/// control does not keep. Drag-to-toggle goes with it, and is not yet reimplemented.
/// <para>
/// Two springs, not one: the handle's <i>position</i> slides along the track, and its <i>size</i>
/// changes independently — 16dp off, 24dp on, 28dp while pressed. Driving them separately is what
/// lets a press grow the handle without disturbing a slide already in flight, and lets a
/// half-completed toggle reverse from where it is.
/// </para>
/// </remarks>
public class MaterialSwitch : ToggleButton, IMaterialThemeConsumer, IMaterialInteractionConsumer
{
    private readonly MaterialElementContext _theme;
    private readonly MaterialInteractionController _interaction;

    private SpringAnimation? _position;
    private SpringAnimation? _handleSize;
    private SwitchDefaultsTokens _tokens = SwitchDefaults.Tokens(ResolvedTheme.Default);
    private SwitchColors _colors = SwitchDefaults.Colors(ResolvedTheme.Default, isDark: false);

    public MaterialSwitch()
    {
        _theme = MaterialElementContext.GetOrCreate(this);
        _interaction = MaterialElementContext.GetOrCreate(this).Interaction;
        _interaction.IsRippleBounded = false;

        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        UpdateHandleSize();

    /// <summary>The track, which is also the state layer's alignment box.</summary>
    protected Rect TrackArea =>
        new(0, (Bounds.Height - _tokens.TrackHeight) / 2, _tokens.TrackWidth, _tokens.TrackHeight);

    private bool IsSelected => IsChecked == true;

    // 0 is fully off, 1 fully on. A fraction rather than a distance so a retuned track width does
    // not need the spring restarting.
    private SpringAnimation Position =>
        _position ??= _interaction.Motion.AddSpring(IsSelected ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));

    private SpringAnimation HandleSize =>
        _handleSize ??= _interaction.Motion.AddSpring(RestingHandleSize(), _theme.Spring(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var track = TrackArea;
        var enabled = IsEffectivelyEnabled;
        var fraction = Position.Value;
        var visuals = SwitchDefaults.Visuals(_colors, enabled, IsSelected);

        var trackFill = visuals.TrackColor;
        var trackOutline = visuals.TrackOutlineColor;

        var radius = track.Height / 2;
        var pen = trackOutline is { } ? new ImmutablePen(trackOutline, _tokens.TrackOutlineWidth) : null;
        var inset = pen is { } ? _tokens.TrackOutlineWidth / 2 : 0;

        context.DrawRectangle(
            trackFill,
            pen,
            new RoundedRect(track.Deflate(inset), new CornerRadius(radius - inset)));

        var handleSize = HandleSize.Value;
        var handleRadius = handleSize / 2;

        // The handle stays inside the track: its centre travels between the two inset extremes.
        var margin = (track.Height - _tokens.SelectedHandleSize) / 2 + _tokens.SelectedHandleSize / 2;
        var left = track.X + margin;
        var right = track.Right - margin;
        var centre = new Point(left + (right - left) * fraction, track.Center.Y);

        var stateLayerSize = _tokens.StateLayerSize;
        var interactionArea = new Rect(
            centre.X - stateLayerSize / 2,
            centre.Y - stateLayerSize / 2,
            stateLayerSize,
            stateLayerSize);
        _theme.InteractionPresentation.UpdateGeometry(
            new MaterialInteractionGeometry(
                interactionArea,
                track,
                new CornerRadius(radius)));

        var handleFill = visuals.HandleColor;

        context.DrawEllipse(handleFill, null, centre, handleRadius, handleRadius);

        base.Render(context);
        _interaction.DrawLayers(
            context,
            visuals.StateLayerColor,
            new CornerRadius(stateLayerSize / 2));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = base.MeasureOverride(availableSize);
        return new Size(
            Math.Max(content.Width, _tokens.TrackWidth),
            Math.Max(content.Height, _tokens.StateLayerSize));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsEffectivelyEnabledProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == IsCheckedProperty)
        {
            Position.AnimateTo(IsSelected ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));
            UpdateHandleSize();
        }
    }

    private void ApplyTokens()
    {
        _tokens = SwitchDefaults.Tokens(_theme.Theme);
        _colors = SwitchDefaults.Colors(_theme.Theme, _theme.IsDark);
        _interaction.RippleRadius = _tokens.StateLayerSize / 2;

        Padding = new Thickness(_tokens.TrackWidth + 12, 0, 0, 0);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void UpdateHandleSize()
    {
        HandleSize.AnimateTo(RestingHandleSize(), _theme.Spring(MotionRole.FastSpatial));
        _interaction.Motion.Start();
        InvalidateVisual();
    }

    private double RestingHandleSize()
    {
        if (_interaction.Interaction.HasFlag(InteractionState.Pressed))
            return _tokens.PressedHandleSize;

        return IsSelected ? _tokens.SelectedHandleSize : _tokens.UnselectedHandleSize;
    }
}
