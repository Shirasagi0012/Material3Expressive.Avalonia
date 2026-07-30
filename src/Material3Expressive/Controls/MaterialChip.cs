using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 chip: assist, filter, input or suggestion.
/// </summary>
/// <remarks>
/// Derives from <see cref="ToggleButton" /> because a filter or input chip genuinely holds a
/// selected state; assist and suggestion chips simply never set it, and their token sets say so
/// with <c>IsSelectable</c>. One control rather than four types: the four kinds differ in what they
/// mean and therefore in their colours, not in their behaviour or content model.
/// <para>
/// Selecting a chip changes its shape as well as its colours — Expressive rounds it fully — which
/// is the same spring morph the buttons use.
/// </para>
/// </remarks>
public class MaterialChip : ToggleButton
{
    public static readonly StyledProperty<ChipVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialChip, ChipVariant>(nameof(Variant));

    /// <inheritdoc cref="LeadingIcon" />
    public static readonly StyledProperty<object?> LeadingIconProperty =
        AvaloniaProperty.Register<MaterialChip, object?>(nameof(LeadingIcon));

    /// <inheritdoc cref="TrailingIcon" />
    public static readonly StyledProperty<object?> TrailingIconProperty =
        AvaloniaProperty.Register<MaterialChip, object?>(nameof(TrailingIcon));

    // Read-only outputs of token resolution; the template binds to them.
    public static readonly DirectProperty<MaterialChip, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialChip, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialChip, double> IconLabelSpaceProperty =
        AvaloniaProperty.RegisterDirect<MaterialChip, double>(nameof(IconLabelSpace), o => o.IconLabelSpace);

    public static readonly DirectProperty<MaterialChip, IImmutableBrush> IconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialChip, IImmutableBrush>(nameof(IconBrush), o => o.IconBrush);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private AnimatedShape? _shape;
    private ChipTokens _tokens = ChipTokens.Assist;
    private ChipColors _colors = ChipTokens.Assist.Resolve(null, false);

    public MaterialChip()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => UpdateShape();

        ApplyTokens();
    }

    static MaterialChip()
    {
        AffectsRender<MaterialChip>(VariantProperty);
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

    /// <summary>The size leading and trailing icons should be drawn at.</summary>
    public double IconSize => _tokens.IconSize;

    /// <summary>The gap between an icon and the label.</summary>
    public double IconLabelSpace => _tokens.IconLabelSpace;

    /// <summary>The colour an icon in either slot should take right now.</summary>
    public IImmutableBrush IconBrush =>
        !IsEffectivelyEnabled ? _colors.DisabledIconColor
        : IsSelected ? _colors.SelectedIconColor
        : _colors.LeadingIconColor;

    protected CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    private bool IsSelected => _tokens.IsSelectable && IsChecked == true;

    private AnimatedShape Shape =>
        _shape ??= _interaction.CreateAnimatedShape(RestingShape(), _theme.Spring(MotionRole.DefaultEffects));

    public override void Render(DrawingContext context)
    {
        var enabled = IsEffectivelyEnabled;
        var corners = ContainerCornerRadius;

        // A selected chip is filled and loses its outline; an unselected one is the other way
        // about. That is why the outline width is a token and not a constant.
        var outlineWidth = IsSelected ? 0 : _tokens.OutlineWidth;

        MaterialContainer.Draw(
            context,
            Bounds.Size,
            corners,
            enabled
                ? IsSelected ? _colors.SelectedContainerColor : _colors.ContainerColor
                : _colors.DisabledContainerColor,
            enabled ? _colors.OutlineColor : _colors.DisabledOutlineColor,
            outlineWidth);

        base.Render(context);
        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty || change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
        else if (change.Property == IsCheckedProperty)
            ApplyChecked();
    }

    private void ApplyTokens()
    {
        var previousIconSize = IconSize;
        var previousSpace = IconLabelSpace;

        _tokens = _theme.Theme.GetChipTokens(Variant);
        _colors = _theme.Theme.GetChipColors(Variant, _theme.IsDark);

        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);
        RaisePropertyChanged(IconLabelSpaceProperty, previousSpace, IconLabelSpace);

        var typography = _theme.Theme.Typography;

        Padding = new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace, 0);
        MinHeight = _tokens.ContainerHeight;
        FontSize = typography.GetTypeScale(_tokens.LabelTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.LabelTextStyle);
        FontWeight = typography.GetFontWeight(_tokens.LabelTextStyle);

        ApplyChecked(snap: true);
    }

    private void ApplyChecked(bool snap = false)
    {
        var previousIconBrush = IconBrush;

        Foreground = !IsEffectivelyEnabled ? _colors.DisabledLabelTextColor
            : IsSelected ? _colors.SelectedLabelTextColor
            : _colors.LabelTextColor;

        RaisePropertyChanged(IconBrushProperty, previousIconBrush, IconBrush);

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

        Shape.AnimateTo(target, _theme.Spring(MotionRole.DefaultEffects));
        _interaction.Motion.Start();
    }

    private RoundedCornerShape RestingShape()
    {
        return _theme.Theme.Shapes.Get(IsSelected ? _tokens.SelectedContainerShape : _tokens.ContainerShape);
    }
}
