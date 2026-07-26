using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 card: elevated, filled or outlined.
/// </summary>
/// <remarks>
/// A container first and a control second, so it derives from <see cref="ContentControl" /> and is
/// inert until <see cref="IsClickable" /> is set. That flag is not decoration: Material says a card
/// only shows a state layer and raises on hover when it is actually actionable, and a card that
/// looks pressable but is not is worse than one that looks flat.
/// </remarks>
public class MaterialCard : ContentControl
{
    public static readonly StyledProperty<CardVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialCard, CardVariant>(nameof(Variant), CardVariant.Filled);

    /// <inheritdoc cref="IsClickable" />
    public static readonly StyledProperty<bool> IsClickableProperty =
        AvaloniaProperty.Register<MaterialCard, bool>(nameof(IsClickable));

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private CardTokens _tokens = CardTokens.Filled;
    private CardColors _colors = CardTokens.Filled.Resolve(null, false);

    public MaterialCard()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => InvalidateVisual();

        ApplyTokens();
    }

    static MaterialCard()
    {
        AffectsRender<MaterialCard>(VariantProperty, IsClickableProperty);
    }

    /// <summary>Elevated, filled or outlined.</summary>
    public CardVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>
    /// Whether the card responds to interaction. Off by default: Material only gives a card a
    /// state layer and a hover lift when it is genuinely actionable.
    /// </summary>
    public bool IsClickable
    {
        get => GetValue(IsClickableProperty);
        set => SetValue(IsClickableProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var enabled = IsEffectivelyEnabled;
        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, Bounds.Size);

        MaterialContainer.Draw(
            context,
            Bounds.Size,
            corners,
            enabled ? _colors.ContainerColor : _colors.DisabledContainerColor,
            enabled ? _colors.OutlineColor : _colors.DisabledOutlineColor,
            _tokens.HasOutline ? _tokens.OutlineWidth : 0,
            CurrentShadows(enabled));

        if (IsClickable)
            _interaction.DrawLayers(context, _colors.StateLayerColor, corners);

        base.Render(context);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty || change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
        else if (change.Property == IsClickableProperty)
            Focusable = IsClickable;
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.GetCardTokens(Variant);
        _colors = _theme.Theme.GetCardColors(Variant, _theme.IsDark);
        InvalidateVisual();
    }

    private BoxShadows CurrentShadows(bool enabled)
    {
        var interaction = IsClickable ? _interaction.Interaction : InteractionState.None;

        var elevation = !enabled ? _tokens.DisabledContainerElevation
            : interaction.HasFlag(InteractionState.Dragged) ? _tokens.DraggedContainerElevation
            : interaction.HasFlag(InteractionState.Pressed) ? _tokens.PressedContainerElevation
            : interaction.HasFlag(InteractionState.Hovered) ? _tokens.HoverContainerElevation
            : interaction.HasFlag(InteractionState.Focused) ? _tokens.FocusContainerElevation
            : _tokens.ContainerElevation;

        if (elevation <= 0)
            return default;

        var shadow = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;
        return Elevation.ToBoxShadows(elevation, shadow);
    }
}
