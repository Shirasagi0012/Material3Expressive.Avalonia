using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>How much room an icon button leaves around its icon.</summary>
public enum IconButtonWidth
{
    Narrow,
    Default,
    Wide
}

/// <summary>
/// A Material 3 Expressive icon button: four variants, five sizes, three widths.
/// </summary>
/// <remarks>
/// The icon is the button's <see cref="ContentControl.Content" />, not a typed property, so the
/// library never has to know what an icon is. That is deliberate: the Material Symbols package
/// stays out of this assembly's dependency graph, and an app can put a path, a glyph or an image in
/// the slot without waiting for the library to support it.
/// <para>
/// The width axis is separate from the size axis because Expressive treats them separately — a
/// medium button can be narrow in a dense toolbar and wide on its own, without changing the icon.
/// </para>
/// </remarks>
public class MaterialIconButton : Button
{
    public static readonly StyledProperty<IconButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialIconButton, IconButtonVariant>(nameof(Variant));

    public static readonly StyledProperty<IconButtonSize> SizeProperty =
        AvaloniaProperty.Register<MaterialIconButton, IconButtonSize>(nameof(Size), IconButtonSize.Small);

    public static readonly StyledProperty<IconButtonWidth> ButtonWidthProperty =
        AvaloniaProperty.Register<MaterialIconButton, IconButtonWidth>(nameof(ButtonWidth), IconButtonWidth.Default);

    /// <inheritdoc cref="MaterialButton.IsSquareProperty" />
    public static readonly StyledProperty<bool> IsSquareProperty =
        AvaloniaProperty.Register<MaterialIconButton, bool>(nameof(IsSquare));

    // A read-only output of token resolution. A direct property rather than a plain CLR one
    // because the template binds to it, and a TemplateBinding needs an AvaloniaProperty.
    public static readonly DirectProperty<MaterialIconButton, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialIconButton, double>(nameof(IconSize), o => o.IconSize);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private AnimatedShape? _shape;
    private IconButtonColorTokens _tokens = IconButtonColorTokens.Standard;
    private IconButtonColors _colors = IconButtonColorTokens.Standard.Resolve(null, false);
    private IconButtonSizeTokens _metrics = IconButtonSizeTokens.Small;

    public MaterialIconButton()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => UpdateShape();

        ApplyTokens();
    }

    static MaterialIconButton()
    {
        AffectsRender<MaterialIconButton>(VariantProperty, IsSquareProperty);
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

    /// <summary>Narrow, default or wide horizontal padding at the current size.</summary>
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
            enabled ? _colors.ContainerColor : _colors.DisabledContainerColor,
            enabled ? _colors.OutlineColor : _colors.DisabledOutlineColor,
            _tokens.HasOutline ? _metrics.OutlineWidth : 0);

        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);

        base.Render(context);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty
            || change.Property == SizeProperty
            || change.Property == ButtonWidthProperty
            || change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
        else if (change.Property == IsSquareProperty)
            UpdateShape();
    }

    private void ApplyTokens()
    {
        var previousIconSize = IconSize;

        _tokens = _theme.Theme.GetIconButtonColorTokens(Variant);
        _colors = _theme.Theme.GetIconButtonColors(Variant, _theme.IsDark);
        _metrics = _theme.Theme.GetIconButtonSizeTokens(Size);

        // Tells the template that the value it binds to has been recomputed.
        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);

        var space = ButtonWidth switch
        {
            IconButtonWidth.Narrow => _metrics.NarrowSpace,
            IconButtonWidth.Wide => _metrics.WideSpace,
            _ => _metrics.DefaultSpace
        };

        Foreground = IsEffectivelyEnabled ? _colors.IconColor : _colors.DisabledIconColor;
        Padding = new Thickness(space, 0, space, 0);
        MinHeight = _metrics.ContainerHeight;
        MinWidth = _metrics.IconSize + space * 2;

        UpdateShape(snap: true);
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

    private RoundedCornerShape RestingShape()
    {
        return _theme.Theme.Shapes.Get(IsSquare ? _metrics.SquareContainerShape : _metrics.ContainerShape);
    }
}
