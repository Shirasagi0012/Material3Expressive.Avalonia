using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>The four Material chip meanings.</summary>
public enum ChipVariant { Assist, Filter, Input, Suggestion }

/// <summary>
/// A Material 3 chip: assist, filter, input or suggestion.
/// </summary>
/// <remarks>
/// Derives from <see cref="ToggleButton" /> because a filter or input chip genuinely holds a
/// selected state; assist and suggestion chips simply never set it. Variant selection is made by
/// the ControlTheme, while the control owns selected/enabled policy and the late shape animation.
/// </remarks>
public class MaterialChip : ToggleButton, IMaterialInteractionConsumer
{
    private static readonly ChipGeometryProfile DefaultGeometryProfile = new(
        ContainerHeight: 32,
        ContainerShape: ShapeRole.CornerSmall,
        SelectedContainerShape: ShapeRole.CornerFull,
        OutlineWidth: 1,
        IconSize: 18,
        ContentPadding: new Thickness(16, 0, 16, 0),
        IconLabelSpace: 8);

    private static readonly ChipVisualColors DefaultVisualColors = new(
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
        TransparentBrush(),
        TransparentBrush());

    public static readonly StyledProperty<ChipVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialChip, ChipVariant>(nameof(Variant));

    /// <inheritdoc cref="LeadingIcon" />
    public static readonly StyledProperty<object?> LeadingIconProperty =
        AvaloniaProperty.Register<MaterialChip, object?>(nameof(LeadingIcon));

    /// <inheritdoc cref="TrailingIcon" />
    public static readonly StyledProperty<object?> TrailingIconProperty =
        AvaloniaProperty.Register<MaterialChip, object?>(nameof(TrailingIcon));

    // These are the hidden semantic endpoints supplied by the ControlTheme. Ordinary public
    // properties such as Foreground, Padding and MinHeight remain normal setters/bindings so an
    // application style or a local value retains Avalonia's usual precedence.
    internal static readonly StyledProperty<ChipGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialChip, ChipGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    internal static readonly StyledProperty<ChipVisualColors> VisualColorsProperty =
        AvaloniaProperty.Register<MaterialChip, ChipVisualColors>(
            nameof(VisualColors),
            DefaultVisualColors);

    // Read-only projections used by the template. They are derived from the current profile and
    // state; no raw token or legacy snapshot is read by the control.
    public static readonly DirectProperty<MaterialChip, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialChip, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialChip, double> IconLabelSpaceProperty =
        AvaloniaProperty.RegisterDirect<MaterialChip, double>(nameof(IconLabelSpace), o => o.IconLabelSpace);

    public static readonly DirectProperty<MaterialChip, IImmutableBrush> IconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialChip, IImmutableBrush>(nameof(IconBrush), o => o.IconBrush);

    internal static readonly DirectProperty<MaterialChip, IImmutableBrush> LabelBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialChip, IImmutableBrush>(nameof(LabelBrush), o => o.LabelBrush);

    private readonly MaterialThemeController _theme;
    private readonly MaterialInteractionController _interaction;

    private AnimatedShape? _shape;

    public MaterialChip()
    {
        // The legacy controller remains only for the chip's motion spring and shape scheme. All
        // consumed colour/metric/typography values arrive through MaterialValue endpoints.
        _theme = MaterialElementContext.Ensure(this).Theme;
        _interaction = MaterialElementContext.Ensure(this).Interaction;

        ApplyProfiles(snap: true);
    }

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        UpdateShape();

    static MaterialChip()
    {
        AffectsRender<MaterialChip>(
            VariantProperty,
            IsCheckedProperty,
            IsEffectivelyEnabledProperty,
            GeometryProfileProperty,
            VisualColorsProperty);
        AffectsMeasure<MaterialChip>(GeometryProfileProperty);
    }

    /// <summary>Assist, filter, input or suggestion.</summary>
    public ChipVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Content shown before the label, at the icon size.</summary>
    public object? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    /// <summary>Content shown after the label — a dismiss affordance, on an input chip.</summary>
    public object? TrailingIcon
    {
        get => GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    /// <summary>Gets the renderer's immutable layout profile.</summary>
    internal ChipGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    /// <summary>Gets the renderer's state-independent colour alternatives.</summary>
    internal ChipVisualColors VisualColors
    {
        get => GetValue(VisualColorsProperty);
        set => SetValue(VisualColorsProperty, value);
    }

    /// <summary>The size leading and trailing icons should be drawn at.</summary>
    public double IconSize => GeometryProfile.IconSize;

    /// <summary>The gap between an icon and the label.</summary>
    public double IconLabelSpace => GeometryProfile.IconLabelSpace;

    /// <summary>The colour an icon in either slot should take right now.</summary>
    public IImmutableBrush IconBrush =>
        !IsEffectivelyEnabled ? VisualColors.DisabledIconColor
        : IsSelected ? VisualColors.SelectedIconColor
        : VisualColors.LeadingIconColor;

    /// <summary>The label colour selected by the control's enabled/checked policy.</summary>
    internal IImmutableBrush LabelBrush =>
        !IsEffectivelyEnabled ? VisualColors.DisabledLabelTextColor
        : IsSelected ? VisualColors.SelectedLabelTextColor
        : VisualColors.LabelTextColor;

    protected CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    private bool IsSelected => IsSelectable(Variant) && IsChecked == true;

    private AnimatedShape Shape => _shape ??= CreateShape();

    private AnimatedShape CreateShape()
    {
        var shape = new AnimatedShape(
            RestingShape(),
            _theme.Current.Theme.Motion.Get(MotionRole.DefaultEffects));
        _interaction.Motion.Add(shape.Progress);
        return shape;
    }

    public override void Render(DrawingContext context)
    {
        var enabled = IsEffectivelyEnabled;
        var corners = ContainerCornerRadius;

        // A selected chip is filled and loses its outline; an unselected one is the other way
        // about. That is why the outline width is a profile input rather than a constant.
        var outlineWidth = IsSelected ? 0 : GeometryProfile.OutlineWidth;

        MaterialContainer.Draw(
            context,
            Bounds.Size,
            corners,
            enabled
                ? IsSelected ? VisualColors.SelectedContainerColor : VisualColors.ContainerColor
                : VisualColors.DisabledContainerColor,
            enabled ? VisualColors.OutlineColor : VisualColors.DisabledOutlineColor,
            outlineWidth);

        base.Render(context);
        _interaction.DrawLayers(context, VisualColors.StateLayerColor, corners);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GeometryProfileProperty || change.Property == VisualColorsProperty)
            ApplyProfiles(snap: !this.IsAttachedToVisualTree());
        else if (change.Property == VariantProperty ||
                 change.Property == IsEffectivelyEnabledProperty ||
                 change.Property == IsCheckedProperty)
            ApplyChecked(snap: !this.IsAttachedToVisualTree());
    }

    private void ApplyProfiles(bool snap)
    {
        var previousIconSize = IconSize;
        var previousSpace = IconLabelSpace;
        var previousIconBrush = IconBrush;
        var previousLabelBrush = LabelBrush;

        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);
        RaisePropertyChanged(IconLabelSpaceProperty, previousSpace, IconLabelSpace);
        RaisePropertyChanged(IconBrushProperty, previousIconBrush, IconBrush);
        RaisePropertyChanged(LabelBrushProperty, previousLabelBrush, LabelBrush);

        UpdateShape(snap);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void ApplyChecked(bool snap)
    {
        var previousIconBrush = IconBrush;
        var previousLabelBrush = LabelBrush;

        RaisePropertyChanged(IconBrushProperty, previousIconBrush, IconBrush);
        RaisePropertyChanged(LabelBrushProperty, previousLabelBrush, LabelBrush);

        UpdateShape(snap);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        var target = RestingShape();

        if (snap && !Shape.IsRunning)
        {
            Shape.SnapTo(target);
            InvalidateVisual();
            return;
        }

        Shape.AnimateTo(target, _theme.Current.Theme.Motion.Get(MotionRole.DefaultEffects));
        _interaction.Motion.RequestFrame();
    }

    private RoundedCornerShape RestingShape() =>
        _theme.Current.Theme.Shapes.Get(
            IsSelected
                ? GeometryProfile.SelectedContainerShape
                : GeometryProfile.ContainerShape);

    private static bool IsSelectable(ChipVariant variant) =>
        variant is ChipVariant.Filter or ChipVariant.Input;

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}
