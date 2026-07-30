using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// The toggle form of <see cref="MaterialButton" />: it stays checked, and says so.
/// </summary>
/// <remarks>
/// Material gives a toggle button a second set of colours rather than reusing the state layer,
/// because "currently on" and "currently being pressed" are different things and have to look
/// different. It also changes shape when checked — round becomes square, or the other way about —
/// which is a spring morph exactly like the press one, and is the reason this is not a style on
/// the plain button.
/// </remarks>
public class MaterialToggleButton : ToggleButton
{
    public static readonly StyledProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialToggleButton, ButtonVariant>(nameof(Variant));

    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<MaterialToggleButton, ButtonSize>(nameof(Size), ButtonSize.Small);

    /// <inheritdoc cref="MaterialButton.IsSquareProperty" />
    public static readonly StyledProperty<bool> IsSquareProperty =
        AvaloniaProperty.Register<MaterialToggleButton, bool>(nameof(IsSquare));

    /// <inheritdoc cref="MaterialButton.ContainerShapeOverride" />
    public static readonly StyledProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        AvaloniaProperty.Register<MaterialToggleButton, RoundedCornerShape?>(nameof(ContainerShapeOverride));

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private AnimatedShape? _shape;
    private ButtonColorTokens _tokens = ButtonColorTokens.Filled;
    private ButtonColors _colors = ButtonColorTokens.Filled.Resolve(null, false);
    private ButtonSizeTokens _metrics = ButtonSizeTokens.Small;

    public MaterialToggleButton()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => UpdateShape();

        ApplyTokens();
    }

    static MaterialToggleButton()
    {
        AffectsRender<MaterialToggleButton>(VariantProperty, IsSquareProperty);
    }

    public ButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public ButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc cref="MaterialButton.IsSquare" />
    public bool IsSquare
    {
        get => GetValue(IsSquareProperty);
        set => SetValue(IsSquareProperty, value);
    }

    /// <inheritdoc cref="MaterialButton.ContainerShapeOverride" />
    public RoundedCornerShape? ContainerShapeOverride
    {
        get => GetValue(ContainerShapeOverrideProperty);
        set => SetValue(ContainerShapeOverrideProperty, value);
    }

    protected CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    private bool IsSelected => IsChecked == true;

    private AnimatedShape Shape =>
        _shape ??= _interaction.CreateAnimatedShape(RestingShape(), _theme.Spring(MotionRole.DefaultEffects));

    public override void Render(DrawingContext context)
    {
        var enabled = IsEffectivelyEnabled;
        var corners = ContainerCornerRadius;

        MaterialContainer.Draw(
            context,
            Bounds.Size,
            corners,
            enabled ? ContainerBrush() : _colors.DisabledContainerColor,
            enabled ? _colors.OutlineColor : _colors.DisabledOutlineColor,
            _tokens.HasOutline ? _metrics.OutlineWidth : 0);

        base.Render(context);
        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty
            || change.Property == SizeProperty
            || change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
        else if (change.Property == IsCheckedProperty)
            ApplyChecked();
        else if (change.Property == IsSquareProperty)
            UpdateShape();
        else if (change.Property == ContainerShapeOverrideProperty)
            UpdateShape(snap: true);
    }

    private IImmutableBrush ContainerBrush()
    {
        return IsSelected ? _colors.SelectedContainerColor : _colors.UnselectedContainerColor;
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.GetButtonColorTokens(Variant);
        _colors = _theme.Theme.GetButtonColors(Variant, _theme.IsDark);
        _metrics = _theme.Theme.GetButtonSizeTokens(Size);

        var typography = _theme.Theme.Typography;
        var scale = typography.GetTypeScale(_metrics.LabelTextStyle);

        Padding = new Thickness(
            _metrics.LeadingSpace, _metrics.VerticalSpace, _metrics.TrailingSpace, _metrics.VerticalSpace);
        MinHeight = _metrics.ContainerHeight;
        FontSize = scale.FontSize;
        FontFamily = typography.GetFontFamily(_metrics.LabelTextStyle);
        FontWeight = typography.GetFontWeight(_metrics.LabelTextStyle);

        ApplyChecked(snap: true);
    }

    private void ApplyChecked(bool snap = false)
    {
        Foreground = !IsEffectivelyEnabled ? _colors.DisabledLabelTextColor
            : IsSelected ? _colors.SelectedLabelTextColor
            : _colors.UnselectedLabelTextColor;

        UpdateShape(snap);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? _theme.Theme.Shapes.Get(_metrics.PressedContainerShape)
            : RestingShape();

        if (snap && !Shape.IsRunning)
        {
            Shape.SnapTo(target);
            InvalidateVisual();
            return;
        }

        Shape.AnimateTo(target, _theme.Spring(MotionRole.DefaultEffects));
        _interaction.Motion.Start();
    }

    /// <summary>
    /// Checking a toggle swaps its shape: a round button squares off and a square one rounds.
    /// </summary>
    private RoundedCornerShape RestingShape()
    {
        if (ContainerShapeOverride is { } shape)
            return shape;

        var round = IsSquare == IsSelected;
        return _theme.Theme.Shapes.Get(round ? _metrics.ContainerShape : _metrics.SquareContainerShape);
    }
}
