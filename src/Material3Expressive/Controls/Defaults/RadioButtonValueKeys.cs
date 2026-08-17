using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by the self-rendering radio button.
/// </summary>
/// <remarks>
/// The profiles contain only renderer inputs. Checked/enabled selection, glyph bounds, dot
/// animation and interaction motion remain control policy rather than becoming token state.
/// </remarks>
public static class RadioButtonValueKeys
{
    /// <summary>Gets the atomic geometry consumed by the radio renderer.</summary>
    public static MaterialValueKey<RadioButtonGeometryProfile> GeometryProfile { get; } =
        new(
            "Value.RadioButton.GeometryProfile",
            new RawRecipePlan<RadioButtonGeometryProfile>(
                new RadioButtonGeometryProfileRecipe(
                    RadioButtonTokensKeys.IconSize,
                    RadioButtonTokensKeys.StateLayerSize)));

    /// <summary>Gets the four brushes selected by the radio renderer's state policy.</summary>
    public static MaterialValueKey<RadioButtonVisualColors> VisualColors { get; } =
        new(
            "Value.RadioButton.VisualColors",
            new RawRecipePlan<RadioButtonVisualColors>(
                new RadioButtonVisualColorsRecipe(
                    RadioButtonTokensKeys.SelectedIconColor,
                    RadioButtonTokensKeys.UnselectedIconColor,
                    RadioButtonTokensKeys.DisabledSelectedIconColor,
                    RadioButtonTokensKeys.DisabledSelectedIconOpacity,
                    RadioButtonTokensKeys.DisabledUnselectedIconColor,
                    RadioButtonTokensKeys.DisabledUnselectedIconOpacity)));

    /// <summary>Gets the label brush role used by the content presenter.</summary>
    public static MaterialValueKey<IImmutableBrush> LabelBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.RadioButton.LabelBrush",
            SystemColorTokens.OnSurface);
}

/// <summary>Atomic geometry inputs consumed by <see cref="MaterialRadioButton"/>.</summary>
public sealed record RadioButtonGeometryProfile(
    double IconSize,
    double StateLayerSize,
    double RingWidth,
    double DotSize);

/// <summary>Atomic brushes consumed by <see cref="MaterialRadioButton"/>.</summary>
public sealed record RadioButtonVisualColors(
    IImmutableBrush SelectedColor,
    IImmutableBrush UnselectedColor,
    IImmutableBrush DisabledSelectedColor,
    IImmutableBrush DisabledUnselectedColor);

internal sealed class RadioButtonGeometryProfileRecipe : IRawValueRecipe<RadioButtonGeometryProfile>
{
    private const double RingWidth = 2;
    private const double DotSize = 10;

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _iconSize;
    private readonly MaterialToken<double> _stateLayerSize;

    internal RadioButtonGeometryProfileRecipe(
        MaterialToken<double> iconSize,
        MaterialToken<double> stateLayerSize)
    {
        _iconSize = iconSize ?? throw new ArgumentNullException(nameof(iconSize));
        _stateLayerSize = stateLayerSize ??
            throw new ArgumentNullException(nameof(stateLayerSize));
        _dependencies = Array.AsReadOnly<MaterialToken>([iconSize, stateLayerSize]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public RadioButtonGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_iconSize),
        context.Get(_stateLayerSize),
        RingWidth,
        DotSize);
}

internal sealed class RadioButtonVisualColorsRecipe : IRawValueRecipe<RadioButtonVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _selectedColor;
    private readonly MaterialToken<Color> _unselectedColor;
    private readonly MaterialToken<Color> _disabledSelectedColor;
    private readonly MaterialToken<double> _disabledSelectedOpacity;
    private readonly MaterialToken<Color> _disabledUnselectedColor;
    private readonly MaterialToken<double> _disabledUnselectedOpacity;

    internal RadioButtonVisualColorsRecipe(
        MaterialToken<Color> selectedColor,
        MaterialToken<Color> unselectedColor,
        MaterialToken<Color> disabledSelectedColor,
        MaterialToken<double> disabledSelectedOpacity,
        MaterialToken<Color> disabledUnselectedColor,
        MaterialToken<double> disabledUnselectedOpacity)
    {
        _selectedColor = selectedColor ?? throw new ArgumentNullException(nameof(selectedColor));
        _unselectedColor = unselectedColor ??
            throw new ArgumentNullException(nameof(unselectedColor));
        _disabledSelectedColor = disabledSelectedColor ??
            throw new ArgumentNullException(nameof(disabledSelectedColor));
        _disabledSelectedOpacity = disabledSelectedOpacity ??
            throw new ArgumentNullException(nameof(disabledSelectedOpacity));
        _disabledUnselectedColor = disabledUnselectedColor ??
            throw new ArgumentNullException(nameof(disabledUnselectedColor));
        _disabledUnselectedOpacity = disabledUnselectedOpacity ??
            throw new ArgumentNullException(nameof(disabledUnselectedOpacity));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            selectedColor,
            unselectedColor,
            disabledSelectedColor,
            disabledSelectedOpacity,
            disabledUnselectedColor,
            disabledUnselectedOpacity,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public RadioButtonVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context, _selectedColor),
        Brush(context, _unselectedColor),
        Brush(context, _disabledSelectedColor, _disabledSelectedOpacity),
        Brush(context, _disabledUnselectedColor, _disabledUnselectedOpacity));

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        MaterialToken<Color> colorToken,
        MaterialToken<double>? opacityToken = null)
    {
        var color = context.Get(colorToken);
        if (opacityToken is not null)
        {
            var opacity = Math.Clamp(context.Get(opacityToken), 0d, 1d);
            color = new Color(
                (byte)Math.Round(color.A * opacity),
                color.R,
                color.G,
                color.B);
        }

        return new ImmutableSolidColorBrush(color);
    }
}
