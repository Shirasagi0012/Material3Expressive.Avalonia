using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 switch.
/// </summary>
/// <remarks>
/// Derives from <see cref="ToggleButton" /> rather than <see cref="Avalonia.Controls.ToggleSwitch" />:
/// the whole switch is painted here, so <c>ToggleSwitch</c>'s knob template parts and knob
/// animation would be dead machinery, and its <c>OnContent</c>/<c>OffContent</c> surface would be
/// a promise this control does not keep. Drag-to-toggle goes with it, and is not yet reimplemented.
/// <para>
/// Two springs, not one: the handle's <i>position</i> slides along the track, and its <i>size</i>
/// changes independently — 16dp off, 24dp on, 28dp while pressed. Driving them separately is what
/// lets a press grow the handle without disturbing a slide already in flight, and lets a
/// half-completed toggle reverse from where it is.
/// </para>
/// </remarks>
public class MaterialSwitch : ToggleButton, IMaterialInteractionConsumer
{
    private static readonly SwitchGeometryProfile DefaultGeometryProfile = new(
        TrackWidth: 52,
        TrackHeight: 32,
        TrackOutlineWidth: 2,
        UnselectedHandleSize: 16,
        SelectedHandleSize: 24,
        PressedHandleSize: 28,
        StateLayerSize: 40);

    private static readonly SwitchVisualColors DefaultVisualColors = new(
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush());

    /// <summary>
    /// The renderer geometry profile supplied by the Material control theme.
    /// </summary>
    /// <remarks>This is an implementation property, not a public authoring surface.</remarks>
    internal static readonly StyledProperty<SwitchGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialSwitch, SwitchGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    /// <summary>
    /// The renderer brush profile supplied by the Material control theme.
    /// </summary>
    /// <remarks>This is an implementation property, not a public authoring surface.</remarks>
    internal static readonly StyledProperty<SwitchVisualColors> VisualColorsProperty =
        AvaloniaProperty.Register<MaterialSwitch, SwitchVisualColors>(
            nameof(VisualColors),
            DefaultVisualColors);

    private readonly MaterialThemeController _theme;
    private readonly MaterialInteractionController _interaction;
    private MaterialInteractionPresentationLease? _presentation;

    private MaterialInteractionPresentationLease InteractionPresentation =>
        _presentation is { IsDisposed: false } existing
            ? existing
            : _presentation = _interaction.AttachPresentation(this);

    private SpringAnimation? _position;
    private SpringAnimation? _handleSize;
    private SwitchGeometryProfile _tokens = DefaultGeometryProfile;
    private SwitchVisualColors _colors = DefaultVisualColors;

    static MaterialSwitch()
    {
        AffectsRender<MaterialSwitch>(
            IsCheckedProperty,
            IsEffectivelyEnabledProperty,
            GeometryProfileProperty,
            VisualColorsProperty);
        AffectsMeasure<MaterialSwitch>(GeometryProfileProperty);
    }

    public MaterialSwitch()
    {
        // The controller is retained only for motion specifications. Geometry and visual values
        // arrive through typed MaterialValueKey resources in the ControlTheme.
        _theme = MaterialElementContext.Ensure(this).Theme;
        _interaction = MaterialElementContext.Ensure(this).Interaction;
        _interaction.IsRippleBounded = false;

        ApplyTokens();
    }

    /// <summary>Gets the renderer geometry profile.</summary>
    internal SwitchGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    /// <summary>Gets the renderer brush profile.</summary>
    internal SwitchVisualColors VisualColors
    {
        get => GetValue(VisualColorsProperty);
        set => SetValue(VisualColorsProperty, value);
    }

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        UpdateHandleSize();

    /// <summary>The track, which is also the state layer's alignment box.</summary>
    protected Rect TrackArea =>
        new(0, (Bounds.Height - _tokens.TrackHeight) / 2, _tokens.TrackWidth, _tokens.TrackHeight);

    private bool IsSelected => IsChecked == true;

    // 0 is fully off, 1 fully on. A fraction rather than a distance so a retuned track width does
    // not need the spring restarting.
    private SpringAnimation Position =>
        _position ??= _interaction.Motion.AddSpring(
            IsSelected ? 1 : 0,
            _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));

    private SpringAnimation HandleSize =>
        _handleSize ??= _interaction.Motion.AddSpring(
            RestingHandleSize(),
            _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var track = TrackArea;
        var enabled = IsEffectivelyEnabled;
        var selected = IsSelected;
        var visuals = VisualTargets(enabled, selected);

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
        var centre = new Point(left + (right - left) * Position.Value, track.Center.Y);

        var stateLayerSize = _tokens.StateLayerSize;
        var interactionArea = new Rect(
            centre.X - stateLayerSize / 2,
            centre.Y - stateLayerSize / 2,
            stateLayerSize,
            stateLayerSize);
        InteractionPresentation.UpdateGeometry(
            new MaterialInteractionGeometry(
                interactionArea,
                track,
                new CornerRadius(radius)));

        context.DrawEllipse(visuals.HandleColor, null, centre, handleRadius, handleRadius);

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

        if (change.Property == GeometryProfileProperty || change.Property == VisualColorsProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == IsCheckedProperty)
        {
            Position.AnimateTo(
                IsSelected ? 1 : 0,
                _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));
            UpdateHandleSize();
        }
    }

    private void ApplyTokens()
    {
        _tokens = GeometryProfile;
        _colors = VisualColors;
        _interaction.RippleRadius = _tokens.StateLayerSize / 2;

        if (_handleSize is not null)
        {
            _handleSize.AnimateTo(
                RestingHandleSize(),
                _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));
            _interaction.Motion.RequestFrame();
        }

        InvalidateMeasure();
        InvalidateVisual();
    }

    private void UpdateHandleSize()
    {
        HandleSize.AnimateTo(
            RestingHandleSize(),
            _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));
        _interaction.Motion.RequestFrame();
        InvalidateVisual();
    }

    private SwitchRenderColors VisualTargets(bool enabled, bool selected)
    {
        var track = !enabled
            ? selected ? _colors.DisabledSelectedTrackColor : _colors.DisabledUnselectedTrackColor
            : selected ? _colors.SelectedTrackColor : _colors.UnselectedTrackColor;
        var outline = selected
            ? null
            : !enabled ? _colors.DisabledUnselectedTrackOutlineColor : _colors.UnselectedTrackOutlineColor;
        var handle = !enabled
            ? selected ? _colors.DisabledSelectedHandleColor : _colors.DisabledUnselectedHandleColor
            : selected ? _colors.SelectedHandleColor : _colors.UnselectedHandleColor;
        var stateLayer = selected
            ? _colors.SelectedStateLayerColor
            : _colors.UnselectedStateLayerColor;

        return new SwitchRenderColors(track, outline, handle, stateLayer);
    }

    private double RestingHandleSize()
    {
        if (_interaction.Interaction.HasFlag(InteractionState.Pressed))
            return _tokens.PressedHandleSize;

        return IsSelected ? _tokens.SelectedHandleSize : _tokens.UnselectedHandleSize;
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);

    private sealed record SwitchRenderColors(
        IImmutableBrush TrackColor,
        IImmutableBrush? TrackOutlineColor,
        IImmutableBrush HandleColor,
        IImmutableBrush StateLayerColor);
}
