using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using SharedTokens = MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>Consumer-ready colour and shadow inputs used by a Material card renderer.</summary>
public sealed record CardVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush OutlineColor,
    IImmutableBrush DisabledOutlineColor,
    IImmutableBrush StateLayerColor,
    Color ShadowColor);

/// <summary>Shape-role and public border input used by a Material card renderer.</summary>
public sealed record CardGeometryProfile(
    ShapeRole ContainerShape,
    Thickness BorderThickness);

/// <summary>Interaction-independent elevation values used by a Material card renderer.</summary>
public sealed record CardElevationProfile(
    double Resting,
    double Hovered,
    double Focused,
    double Pressed,
    double Dragged,
    double Disabled)
{
    /// <summary>Returns the elevation for the current late interaction state.</summary>
    public double For(bool enabled, InteractionState interaction)
    {
        if (!enabled)
            return Disabled;

        if (interaction.HasFlag(InteractionState.Dragged))
            return Dragged;

        if (interaction.HasFlag(InteractionState.Pressed))
            return Pressed;

        if (interaction.HasFlag(InteractionState.Hovered))
            return Hovered;

        if (interaction.HasFlag(InteractionState.Focused))
            return Focused;

        return Resting;
    }
}

/// <summary>
/// Stable Material Value endpoints consumed by <see cref="MaterialCard" />.
/// </summary>
/// <remarks>
/// Variant selection is performed by the ControlTheme. The plans only read their declared Raw
/// dependencies; interaction, bounds, RTL and the final shape/elevation policy stay in the card.
/// </remarks>
public static class CardValueKeys
{
    public static MaterialValueKey<CardVisualColors> ElevatedVisualColors { get; } =
        VisualColors(
            "Value.Card.Elevated.VisualColors",
            ElevatedCardTokensKeys.ContainerColor,
            ElevatedCardTokensKeys.DisabledContainerColor,
            ElevatedCardTokensKeys.DisabledContainerOpacity,
            outlineColor: null,
            disabledOutlineColor: null,
            disabledOutlineOpacity: null);

    public static MaterialValueKey<CardVisualColors> FilledVisualColors { get; } =
        VisualColors(
            "Value.Card.Filled.VisualColors",
            FilledCardTokensKeys.ContainerColor,
            FilledCardTokensKeys.DisabledContainerColor,
            FilledCardTokensKeys.DisabledContainerOpacity,
            outlineColor: null,
            disabledOutlineColor: null,
            disabledOutlineOpacity: null);

    public static MaterialValueKey<CardVisualColors> OutlinedVisualColors { get; } =
        VisualColors(
            "Value.Card.Outlined.VisualColors",
            OutlinedCardTokensKeys.ContainerColor,
            OutlinedCardTokensKeys.ContainerColor,
            opacity: null,
            OutlinedCardTokensKeys.OutlineColor,
            OutlinedCardTokensKeys.DisabledOutlineColor,
            OutlinedCardTokensKeys.DisabledOutlineOpacity);

    public static MaterialValueKey<CardGeometryProfile> ElevatedGeometry { get; } =
        Geometry(
            "Value.Card.Elevated.Geometry",
            ElevatedCardTokensKeys.ContainerShape,
            outlineWidth: null);

    public static MaterialValueKey<CardGeometryProfile> FilledGeometry { get; } =
        Geometry(
            "Value.Card.Filled.Geometry",
            FilledCardTokensKeys.ContainerShape,
            outlineWidth: null);

    public static MaterialValueKey<CardGeometryProfile> OutlinedGeometry { get; } =
        Geometry(
            "Value.Card.Outlined.Geometry",
            OutlinedCardTokensKeys.ContainerShape,
            OutlinedCardTokensKeys.OutlineWidth);

    public static MaterialValueKey<CardElevationProfile> ElevatedElevation { get; } =
        Elevation(
            "Value.Card.Elevated.Elevation",
            ElevatedCardTokensKeys.ContainerElevation,
            ElevatedCardTokensKeys.HoverContainerElevation,
            ElevatedCardTokensKeys.FocusContainerElevation,
            ElevatedCardTokensKeys.PressedContainerElevation,
            ElevatedCardTokensKeys.DraggedContainerElevation,
            ElevatedCardTokensKeys.DisabledContainerElevation);

    public static MaterialValueKey<CardElevationProfile> FilledElevation { get; } =
        Elevation(
            "Value.Card.Filled.Elevation",
            FilledCardTokensKeys.ContainerElevation,
            FilledCardTokensKeys.HoverContainerElevation,
            FilledCardTokensKeys.FocusContainerElevation,
            FilledCardTokensKeys.PressedContainerElevation,
            FilledCardTokensKeys.DraggedContainerElevation,
            FilledCardTokensKeys.DisabledContainerElevation);

    public static MaterialValueKey<CardElevationProfile> OutlinedElevation { get; } =
        Elevation(
            "Value.Card.Outlined.Elevation",
            OutlinedCardTokensKeys.ContainerElevation,
            OutlinedCardTokensKeys.HoverContainerElevation,
            OutlinedCardTokensKeys.FocusContainerElevation,
            OutlinedCardTokensKeys.PressedContainerElevation,
            OutlinedCardTokensKeys.DraggedContainerElevation,
            OutlinedCardTokensKeys.DisabledContainerElevation);

    private static MaterialValueKey<CardVisualColors> VisualColors(
        string path,
        SharedTokens.MaterialToken<Color> containerColor,
        SharedTokens.MaterialToken<Color> disabledContainerColor,
        SharedTokens.MaterialToken<double>? opacity,
        SharedTokens.MaterialToken<Color>? outlineColor,
        SharedTokens.MaterialToken<Color>? disabledOutlineColor,
        SharedTokens.MaterialToken<double>? disabledOutlineOpacity) =>
        new(
            path,
            new RawRecipePlan<CardVisualColors>(
                new CardVisualColorsRecipe(
                    containerColor,
                    disabledContainerColor,
                    opacity,
                    outlineColor,
                    disabledOutlineColor,
                    disabledOutlineOpacity,
                    SystemColorTokens.OnSurface,
                    SystemColorTokens.Shadow)));

    private static MaterialValueKey<CardGeometryProfile> Geometry(
        string path,
        SharedTokens.MaterialToken<ShapeRole> shape,
        SharedTokens.MaterialToken<double>? outlineWidth) =>
        new(
            path,
            new RawRecipePlan<CardGeometryProfile>(
                new CardGeometryProfileRecipe(shape, outlineWidth)));

    private static MaterialValueKey<CardElevationProfile> Elevation(
        string path,
        SharedTokens.MaterialToken<double> resting,
        SharedTokens.MaterialToken<double> hovered,
        SharedTokens.MaterialToken<double> focused,
        SharedTokens.MaterialToken<double> pressed,
        SharedTokens.MaterialToken<double> dragged,
        SharedTokens.MaterialToken<double> disabled) =>
        new(
            path,
            new RawRecipePlan<CardElevationProfile>(
                new CardElevationProfileRecipe(
                    resting,
                    hovered,
                    focused,
                    pressed,
                    dragged,
                    disabled)));
}

internal sealed class CardVisualColorsRecipe : IRawValueRecipe<CardVisualColors>
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Colors.Transparent);

    private readonly IReadOnlyList<SharedTokens.MaterialToken> _dependencies;
    private readonly SharedTokens.MaterialToken<Color> _containerColor;
    private readonly SharedTokens.MaterialToken<Color> _disabledContainerColor;
    private readonly SharedTokens.MaterialToken<double>? _disabledContainerOpacity;
    private readonly SharedTokens.MaterialToken<Color>? _outlineColor;
    private readonly SharedTokens.MaterialToken<Color>? _disabledOutlineColor;
    private readonly SharedTokens.MaterialToken<double>? _disabledOutlineOpacity;
    private readonly SharedTokens.MaterialToken<Color> _stateLayerColor;
    private readonly SharedTokens.MaterialToken<Color> _shadowColor;

    internal CardVisualColorsRecipe(
        SharedTokens.MaterialToken<Color> containerColor,
        SharedTokens.MaterialToken<Color> disabledContainerColor,
        SharedTokens.MaterialToken<double>? disabledContainerOpacity,
        SharedTokens.MaterialToken<Color>? outlineColor,
        SharedTokens.MaterialToken<Color>? disabledOutlineColor,
        SharedTokens.MaterialToken<double>? disabledOutlineOpacity,
        SharedTokens.MaterialToken<Color> stateLayerColor,
        SharedTokens.MaterialToken<Color> shadowColor)
    {
        _containerColor = containerColor ?? throw new ArgumentNullException(nameof(containerColor));
        _disabledContainerColor = disabledContainerColor ??
            throw new ArgumentNullException(nameof(disabledContainerColor));
        _disabledContainerOpacity = disabledContainerOpacity;
        _outlineColor = outlineColor;
        _disabledOutlineColor = disabledOutlineColor;
        _disabledOutlineOpacity = disabledOutlineOpacity;
        if ((_disabledOutlineColor is null) != (_disabledOutlineOpacity is null))
            throw new ArgumentException(
                "Disabled outline colour and opacity must be supplied together.",
                nameof(disabledOutlineColor));
        _stateLayerColor = stateLayerColor ?? throw new ArgumentNullException(nameof(stateLayerColor));
        _shadowColor = shadowColor ?? throw new ArgumentNullException(nameof(shadowColor));

        var dependencies = new List<SharedTokens.MaterialToken>
        {
            _containerColor,
            _disabledContainerColor,
        };
        if (_disabledContainerOpacity is not null)
            dependencies.Add(_disabledContainerOpacity);
        if (_outlineColor is not null)
            dependencies.Add(_outlineColor);
        if (_disabledOutlineColor is not null)
        {
            dependencies.Add(_disabledOutlineColor);
            dependencies.Add(_disabledOutlineOpacity!);
        }
        dependencies.Add(_stateLayerColor);
        dependencies.Add(_shadowColor);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<SharedTokens.MaterialToken> Dependencies => _dependencies;

    public CardVisualColors Evaluate(MaterialValueContext context)
    {
        var container = Brush(context, _containerColor);
        var disabledContainer = Brush(context, _disabledContainerColor, _disabledContainerOpacity);
        var outline = _outlineColor is null ? Transparent : Brush(context, _outlineColor);
        var disabledOutline = _disabledOutlineColor is null
            ? Transparent
            : Brush(context, _disabledOutlineColor, _disabledOutlineOpacity);

        return new(
            container,
            disabledContainer,
            outline,
            disabledOutline,
            Brush(context, _stateLayerColor),
            context.Get(_shadowColor));
    }

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        SharedTokens.MaterialToken<Color> color,
        SharedTokens.MaterialToken<double>? opacity = null)
    {
        var value = context.Get(color);
        if (opacity is not null)
        {
            var alpha = Math.Clamp(context.Get(opacity), 0d, 1d);
            value = new Color(
                (byte)Math.Round(value.A * alpha),
                value.R,
                value.G,
                value.B);
        }

        return new ImmutableSolidColorBrush(value);
    }
}

internal sealed class CardGeometryProfileRecipe : IRawValueRecipe<CardGeometryProfile>
{
    private readonly IReadOnlyList<SharedTokens.MaterialToken> _dependencies;
    private readonly SharedTokens.MaterialToken<ShapeRole> _shape;
    private readonly SharedTokens.MaterialToken<double>? _outlineWidth;

    internal CardGeometryProfileRecipe(
        SharedTokens.MaterialToken<ShapeRole> shape,
        SharedTokens.MaterialToken<double>? outlineWidth)
    {
        _shape = shape ?? throw new ArgumentNullException(nameof(shape));
        _outlineWidth = outlineWidth;
        _dependencies = outlineWidth is null
            ? Array.AsReadOnly<SharedTokens.MaterialToken>([_shape])
            : Array.AsReadOnly<SharedTokens.MaterialToken>([_shape, outlineWidth]);
    }

    public IReadOnlyList<SharedTokens.MaterialToken> Dependencies => _dependencies;

    public CardGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_shape),
        new Thickness(_outlineWidth is null ? 0 : context.Get(_outlineWidth)));
}

internal sealed class CardElevationProfileRecipe : IRawValueRecipe<CardElevationProfile>
{
    private readonly IReadOnlyList<SharedTokens.MaterialToken> _dependencies;
    private readonly SharedTokens.MaterialToken<double> _resting;
    private readonly SharedTokens.MaterialToken<double> _hovered;
    private readonly SharedTokens.MaterialToken<double> _focused;
    private readonly SharedTokens.MaterialToken<double> _pressed;
    private readonly SharedTokens.MaterialToken<double> _dragged;
    private readonly SharedTokens.MaterialToken<double> _disabled;

    internal CardElevationProfileRecipe(
        SharedTokens.MaterialToken<double> resting,
        SharedTokens.MaterialToken<double> hovered,
        SharedTokens.MaterialToken<double> focused,
        SharedTokens.MaterialToken<double> pressed,
        SharedTokens.MaterialToken<double> dragged,
        SharedTokens.MaterialToken<double> disabled)
    {
        _resting = resting ?? throw new ArgumentNullException(nameof(resting));
        _hovered = hovered ?? throw new ArgumentNullException(nameof(hovered));
        _focused = focused ?? throw new ArgumentNullException(nameof(focused));
        _pressed = pressed ?? throw new ArgumentNullException(nameof(pressed));
        _dragged = dragged ?? throw new ArgumentNullException(nameof(dragged));
        _disabled = disabled ?? throw new ArgumentNullException(nameof(disabled));
        _dependencies = Array.AsReadOnly<SharedTokens.MaterialToken>(
            [_resting, _hovered, _focused, _pressed, _dragged, _disabled]);
    }

    public IReadOnlyList<SharedTokens.MaterialToken> Dependencies => _dependencies;

    public CardElevationProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_resting),
        context.Get(_hovered),
        context.Get(_focused),
        context.Get(_pressed),
        context.Get(_dragged),
        context.Get(_disabled));
}
