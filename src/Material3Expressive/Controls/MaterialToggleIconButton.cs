using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// The toggle form of <see cref="MaterialIconButton" />.
/// </summary>
/// <inheritdoc cref="MaterialToggleButton" path="/remarks" />
public class MaterialToggleIconButton : ToggleButton
{
    public static readonly StyledProperty<IconButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialToggleIconButton, IconButtonVariant>(nameof(Variant));

    public static readonly StyledProperty<IconButtonSize> SizeProperty =
        AvaloniaProperty.Register<MaterialToggleIconButton, IconButtonSize>(nameof(Size), IconButtonSize.Small);

    public static readonly StyledProperty<IconButtonWidth> ButtonWidthProperty =
        AvaloniaProperty.Register<MaterialToggleIconButton, IconButtonWidth>(
            nameof(ButtonWidth), IconButtonWidth.Default);

    /// <inheritdoc cref="MaterialButton.IsSquareProperty" />
    public static readonly StyledProperty<bool> IsSquareProperty =
        AvaloniaProperty.Register<MaterialToggleIconButton, bool>(nameof(IsSquare));

    // A read-only output of token resolution; the template binds to it.
    public static readonly DirectProperty<MaterialToggleIconButton, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialToggleIconButton, double>(nameof(IconSize), o => o.IconSize);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private AnimatedShape? _shape;
    private IconButtonColorTokens _tokens = IconButtonColorTokens.Standard;
    private IconButtonColors _colors = IconButtonColorTokens.Standard.Resolve(null, false);
    private IconButtonSizeTokens _metrics = IconButtonSizeTokens.Small;

    public MaterialToggleIconButton()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => UpdateShape();

        ApplyTokens();
    }

    static MaterialToggleIconButton()
    {
        AffectsRender<MaterialToggleIconButton>(VariantProperty, IsSquareProperty);
    }

    public IconButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public IconButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc cref="MaterialIconButton.ButtonWidth" />
    public IconButtonWidth ButtonWidth
    {
        get => GetValue(ButtonWidthProperty);
        set => SetValue(ButtonWidthProperty, value);
    }

    /// <inheritdoc cref="MaterialButton.IsSquare" />
    public bool IsSquare
    {
        get => GetValue(IsSquareProperty);
        set => SetValue(IsSquareProperty, value);
    }

    /// <summary>The size the icon in the content slot should be drawn at.</summary>
    public double IconSize => _metrics.IconSize;

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
            enabled
                ? IsSelected ? _colors.SelectedContainerColor : _colors.UnselectedContainerColor
                : _colors.DisabledContainerColor,
            enabled
                ? IsSelected ? _colors.OutlineColor : _colors.UnselectedOutlineColor
                : _colors.DisabledOutlineColor,
            _tokens.HasOutline ? _metrics.OutlineWidth : 0);

        base.Render(context);
        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty
            || change.Property == SizeProperty
            || change.Property == ButtonWidthProperty
            || change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
        else if (change.Property == IsCheckedProperty)
            ApplyChecked();
        else if (change.Property == IsSquareProperty)
            UpdateShape();
    }

    private void ApplyTokens()
    {
        var previousIconSize = IconSize;

        _tokens = _theme.Theme.GetIconButtonColorTokens(Variant);
        _colors = _theme.Theme.GetIconButtonColors(Variant, _theme.IsDark);
        _metrics = _theme.Theme.GetIconButtonSizeTokens(Size);
        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);

        var space = ButtonWidth switch
        {
            IconButtonWidth.Narrow => _metrics.NarrowSpace,
            IconButtonWidth.Wide => _metrics.WideSpace,
            _ => _metrics.DefaultSpace
        };

        Padding = new Thickness(space, 0, space, 0);
        MinHeight = _metrics.ContainerHeight;
        MinWidth = _metrics.IconSize + space * 2;

        ApplyChecked(snap: true);
    }

    private void ApplyChecked(bool snap = false)
    {
        Foreground = !IsEffectivelyEnabled ? _colors.DisabledIconColor
            : IsSelected ? _colors.SelectedIconColor
            : _colors.UnselectedIconColor;

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
    /// Checking a toggle swaps its shape. The icon button has its own selected shape tokens rather
    /// than reusing the unselected pair, which is why this is not simply the inverse.
    /// </summary>
    private RoundedCornerShape RestingShape()
    {
        var role = (IsSelected, IsSquare) switch
        {
            (true, false) => _metrics.SelectedContainerShape,
            (true, true) => _metrics.SelectedSquareContainerShape,
            (false, false) => _metrics.ContainerShape,
            (false, true) => _metrics.SquareContainerShape
        };

        return _theme.Theme.Shapes.Get(role);
    }
}
