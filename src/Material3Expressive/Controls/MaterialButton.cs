using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 Expressive button: five colour variants, five sizes, and a container that morphs
/// while pressed.
/// </summary>
/// <remarks>
/// Derives from Avalonia's <see cref="Button" /> rather than from a Material base class, so
/// content, commands, click modes and keyboard activation keep working exactly as they do
/// everywhere else. What Material adds — token resolution, the state layer, ripples and the shape
/// morph — is hosted, not inherited.
/// <para>
/// The container is drawn in <see cref="Render" /> rather than by a templated <c>Border</c>,
/// because its corner radius changes every frame during a press and resolves against the control's
/// real bounds. Percentage corners need the measured size: <c>CornerFull</c> is genuinely half the
/// height, which is what makes a pill a pill at any size.
/// </para>
/// </remarks>
public class MaterialButton : Button
{
    public static readonly StyledProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialButton, ButtonVariant>(nameof(Variant));

    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<MaterialButton, ButtonSize>(nameof(Size), ButtonSize.Small);

    /// <summary>
    /// Squares the resting container off. Material pairs a round button with a square one in
    /// toggle groups, so the two shapes are a property rather than two variants.
    /// </summary>
    public static readonly StyledProperty<bool> IsSquareProperty =
        AvaloniaProperty.Register<MaterialButton, bool>(nameof(IsSquare));

    /// <inheritdoc cref="ContainerShapeOverride" />
    public static readonly StyledProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        AvaloniaProperty.Register<MaterialButton, RoundedCornerShape?>(nameof(ContainerShapeOverride));

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private AnimatedShape? _shape;
    private ButtonColors _colors = ButtonColorTokens.Filled.Resolve(null, false);
    private ButtonSizeTokens _metrics = ButtonSizeTokens.Small;

    public MaterialButton()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => UpdateShape();

        ApplyTokens();
    }

    static MaterialButton()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialButton>(false);
        AffectsRender<MaterialButton>(VariantProperty, IsSquareProperty);
    }

    /// <summary>The colour variant: filled, tonal, elevated, outlined or text.</summary>
    public ButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>The size, which sets height, padding, icon size and type scale together.</summary>
    public ButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc cref="IsSquareProperty" />
    public bool IsSquare
    {
        get => GetValue(IsSquareProperty);
        set => SetValue(IsSquareProperty, value);
    }

    /// <summary>
    /// Replaces the resting shape outright, corner by corner.
    /// </summary>
    /// <remarks>
    /// The escape hatch for containers that shape their children: a connected button group squares
    /// only the corners where two segments meet, and a split button's two halves each keep one
    /// round end. Neither can be said with <see cref="IsSquare" />, which is all four corners or
    /// none. The press morph still applies on top.
    /// </remarks>
    public RoundedCornerShape? ContainerShapeOverride
    {
        get => GetValue(ContainerShapeOverrideProperty);
        set => SetValue(ContainerShapeOverrideProperty, value);
    }

    /// <summary>The corner radii the container is currently drawn with, morph included.</summary>
    protected CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    private AnimatedShape Shape =>
        _shape ??= _interaction.CreateAnimatedShape(RestingShape(), _theme.Spring(MotionRole.DefaultEffects));

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var corners = ContainerCornerRadius;
        var enabled = IsEffectivelyEnabled;
        var tokens = _theme.Theme.GetButtonColorTokens(Variant);

        MaterialContainer.Draw(
            context,
            Bounds.Size,
            corners,
            enabled ? _colors.ContainerColor : _colors.DisabledContainerColor,
            enabled ? _colors.OutlineColor : _colors.DisabledOutlineColor,
            tokens.HasOutline ? _metrics.OutlineWidth : 0,
            CurrentShadows(enabled));

        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);

        base.Render(context);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty
            || change.Property == SizeProperty
            || change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
        else if (change.Property == IsSquareProperty)
            UpdateShape();
        else if (change.Property == ContainerShapeOverrideProperty)
            // Group/split-button geometry is structural layout, not an interaction transition.
            // Snapping while idle also prevents an identical override written during a later
            // arrange pass from leaving the container at a clock-dependent intermediate shape.
            UpdateShape(snap: true);
    }

    /// <summary>Resolves every token this button reads and pushes them onto its properties.</summary>
    private void ApplyTokens()
    {
        _colors = _theme.Theme.GetButtonColors(Variant, _theme.IsDark);
        _metrics = _theme.Theme.GetButtonSizeTokens(Size);

        var typography = _theme.Theme.Typography;
        var scale = typography.GetTypeScale(_metrics.LabelTextStyle);

        Foreground = IsEffectivelyEnabled ? _colors.LabelTextColor : _colors.DisabledLabelTextColor;
        Padding = new Thickness(
            _metrics.LeadingSpace, _metrics.VerticalSpace, _metrics.TrailingSpace, _metrics.VerticalSpace);
        MinHeight = _metrics.ContainerHeight;
        FontSize = scale.FontSize;
        FontFamily = typography.GetFontFamily(_metrics.LabelTextStyle);
        FontWeight = typography.GetFontWeight(_metrics.LabelTextStyle);
        // Button.kt provides MaterialTheme.typography.labelLarge to the whole content lambda.
        // The attached property carries the corresponding line height and letter spacing to
        // Avalonia TextBlock descendants, rather than leaving them at the inherited BodyMedium
        // defaults.
        TypographyScheme.SetTextStyle(this, _metrics.LabelTextStyle);

        UpdateShape(snap: true);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? ShapeFor(_metrics.PressedContainerShape)
            : RestingShape();

        if (snap && !Shape.IsRunning)
        {
            Shape.SnapTo(target);
            InvalidateVisual();
            return;
        }

        // DefaultEffects, not a spatial spring: upstream picks it deliberately so the container
        // does not bounce on release.
        Shape.AnimateTo(target, _theme.Spring(MotionRole.DefaultEffects));
        _interaction.Motion.Start();
    }

    private RoundedCornerShape RestingShape()
    {
        return ContainerShapeOverride
               ?? ShapeFor(IsSquare ? _metrics.SquareContainerShape : _metrics.ContainerShape);
    }

    private RoundedCornerShape ShapeFor(ShapeRole role)
    {
        return _theme.Theme.Shapes.Get(role);
    }

    private BoxShadows CurrentShadows(bool enabled)
    {
        var tokens = _theme.Theme.GetButtonColorTokens(Variant);
        var interaction = _interaction.Interaction;

        var elevation = !enabled ? tokens.DisabledContainerElevation
            : interaction.HasFlag(InteractionState.Pressed) ? tokens.PressedContainerElevation
            : interaction.HasFlag(InteractionState.Hovered) ? tokens.HoveredContainerElevation
            : interaction.HasFlag(InteractionState.Focused) ? tokens.FocusedContainerElevation
            : tokens.ContainerElevation;

        if (elevation <= 0)
            return default;

        var shadow = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;
        return Elevation.ToBoxShadows(elevation, shadow);
    }
}
