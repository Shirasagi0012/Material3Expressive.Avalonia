using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 radio button.
/// </summary>
/// <remarks>
/// Derives from Avalonia's <see cref="RadioButton" />, so grouping and mutual exclusion are the
/// stock behaviour. The dot scales in with a spring rather than appearing: Material animates the
/// selection, and at this size a snap reads as a glitch.
/// </remarks>
public class MaterialRadioButton : RadioButton, IMaterialInteractionConsumer
{
    // The theme controller is retained only for motion specs. Geometry and colour values arrive
    // through the typed DynamicResource profiles below.
    private readonly MaterialThemeController _theme;
    private readonly MaterialInteractionController _interaction;
    private MaterialInteractionPresentationLease? _presentation;

    private MaterialInteractionPresentationLease InteractionPresentation =>
        _presentation is { IsDisposed: false } existing
            ? existing
            : _presentation = _interaction.AttachPresentation(this);

    private SpringAnimation? _dot;

    private static readonly RadioButtonGeometryProfile DefaultGeometryProfile = new(
        IconSize: 20,
        StateLayerSize: 40,
        RingWidth: 2,
        DotSize: 10);

    private static readonly RadioButtonVisualColors DefaultVisualColors = new(
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush());

    /// <summary>
    /// The renderer geometry profile supplied by the Material control theme.
    /// </summary>
    /// <remarks>This is an implementation property, not a public authoring surface.</remarks>
    internal static readonly StyledProperty<RadioButtonGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialRadioButton, RadioButtonGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    /// <summary>
    /// The state-dependent brushes supplied by the Material control theme.
    /// </summary>
    /// <remarks>This is an implementation property, not a public authoring surface.</remarks>
    internal static readonly StyledProperty<RadioButtonVisualColors> VisualColorsProperty =
        AvaloniaProperty.Register<MaterialRadioButton, RadioButtonVisualColors>(
            nameof(VisualColors),
            DefaultVisualColors);

    static MaterialRadioButton()
    {
        AffectsRender<MaterialRadioButton>(
            IsCheckedProperty,
            IsEffectivelyEnabledProperty,
            GeometryProfileProperty,
            VisualColorsProperty);
        AffectsMeasure<MaterialRadioButton>(GeometryProfileProperty);
    }

    public MaterialRadioButton()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _interaction = MaterialElementContext.Ensure(this).Interaction;
        _interaction.IsRippleBounded = false;

        ApplyProfile();
    }

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        InvalidateVisual();

    /// <summary>Gets the renderer geometry profile.</summary>
    internal RadioButtonGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    /// <summary>Gets the renderer's state-dependent brushes.</summary>
    internal RadioButtonVisualColors VisualColors
    {
        get => GetValue(VisualColorsProperty);
        set => SetValue(VisualColorsProperty, value);
    }

    /// <summary>The state-layer circle, which is also the ring's alignment box.</summary>
    protected Rect GlyphArea
    {
        get
        {
            var size = GeometryProfile.StateLayerSize;
            return new Rect(0, (Bounds.Height - size) / 2, size, size);
        }
    }

    private SpringAnimation Dot =>
        _dot ??= _interaction.Motion.AddSpring(
            IsChecked == true ? 1 : 0,
            _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var area = GlyphArea;
        InteractionPresentation.UpdateGeometry(
            new MaterialInteractionGeometry(area, null, null));

        var enabled = IsEffectivelyEnabled;
        var selected = IsChecked == true;

        var color = enabled
            ? selected ? VisualColors.SelectedColor : VisualColors.UnselectedColor
            : selected ? VisualColors.DisabledSelectedColor : VisualColors.DisabledUnselectedColor;

        var centre = area.Center;
        var ringRadius = (GeometryProfile.IconSize - GeometryProfile.RingWidth) / 2;
        context.DrawEllipse(
            null,
            new ImmutablePen(color, GeometryProfile.RingWidth),
            centre,
            ringRadius,
            ringRadius);

        var scale = Dot.Value;
        if (scale > 0)
        {
            var dotRadius = GeometryProfile.DotSize / 2 * scale;
            context.DrawEllipse(color, null, centre, dotRadius, dotRadius);
        }

        base.Render(context);
        _interaction.DrawLayers(context, color, new CornerRadius(GeometryProfile.StateLayerSize / 2));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = base.MeasureOverride(availableSize);
        return new Size(content.Width, Math.Max(content.Height, GeometryProfile.StateLayerSize));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GeometryProfileProperty || change.Property == VisualColorsProperty)
        {
            ApplyProfile();
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            InvalidateVisual();
        }
        else if (change.Property == IsCheckedProperty)
        {
            Dot.AnimateTo(IsChecked == true ? 1 : 0, _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));
            _interaction.Motion.RequestFrame();
            InvalidateVisual();
        }
    }

    private void ApplyProfile()
    {
        _interaction.RippleRadius = GeometryProfile.StateLayerSize / 2;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}
