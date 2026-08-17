using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by <see cref="MaterialChip" />.
/// </summary>
/// <remarks>
/// The chip's variant is selected by the ControlTheme.  Checked/enabled state remains control
/// policy: the colour profile contains the state alternatives, while the renderer chooses among
/// them.  The plans do not read the requester, bounds, flow direction or motion state.
/// </remarks>
public static class ChipValueKeys
{
    /// <summary>Gets the layout and late-shape inputs shared by the four chip variants.</summary>
    public static MaterialValueKey<ChipGeometryProfile> GeometryProfile { get; } =
        new(
            "Value.Chip.GeometryProfile",
            new RawRecipePlan<ChipGeometryProfile>(
                new ChipGeometryProfileRecipe(
                    AssistChipTokensKeys.ContainerHeight,
                    AssistChipTokensKeys.ContainerShape,
                    ChipsTokensKeys.SelectedShape,
                    AssistChipTokensKeys.FlatOutlineWidth,
                    AssistChipTokensKeys.IconSize)));

    /// <summary>Gets the assist-chip visual alternatives.</summary>
    public static MaterialValueKey<ChipVisualColors> AssistVisualColors { get; } =
        VisualColors(
            "Value.Chip.Assist.VisualColors",
            AssistChipTokensKeys.LabelTextColor,
            AssistChipTokensKeys.IconColor,
            AssistChipTokensKeys.IconColor,
            AssistChipTokensKeys.FlatOutlineColor,
            ChipsTokensKeys.SelectedContainerColor,
            ChipsTokensKeys.SelectedLabelTextColor,
            ChipsTokensKeys.SelectedLeadingIconColor,
            AssistChipTokensKeys.DisabledLabelTextColor,
            AssistChipTokensKeys.DisabledLabelTextOpacity,
            AssistChipTokensKeys.DisabledIconColor,
            AssistChipTokensKeys.DisabledIconOpacity,
            AssistChipTokensKeys.FlatDisabledOutlineColor,
            AssistChipTokensKeys.FlatDisabledOutlineOpacity);

    /// <summary>Gets the filter-chip visual alternatives.</summary>
    public static MaterialValueKey<ChipVisualColors> FilterVisualColors { get; } =
        VisualColors(
            "Value.Chip.Filter.VisualColors",
            FilterChipTokensKeys.UnselectedLabelTextColor,
            FilterChipTokensKeys.UnselectedLeadingIconColor,
            FilterChipTokensKeys.UnselectedTrailingIconColor,
            AssistChipTokensKeys.FlatOutlineColor,
            FilterChipTokensKeys.FlatSelectedContainerColor,
            FilterChipTokensKeys.SelectedLabelTextColor,
            FilterChipTokensKeys.SelectedLeadingIconColor,
            AssistChipTokensKeys.DisabledLabelTextColor,
            AssistChipTokensKeys.DisabledLabelTextOpacity,
            AssistChipTokensKeys.DisabledIconColor,
            AssistChipTokensKeys.DisabledIconOpacity,
            AssistChipTokensKeys.FlatDisabledOutlineColor,
            AssistChipTokensKeys.FlatDisabledOutlineOpacity);

    /// <summary>Gets the input-chip visual alternatives.</summary>
    public static MaterialValueKey<ChipVisualColors> InputVisualColors { get; } =
        VisualColors(
            "Value.Chip.Input.VisualColors",
            InputChipTokensKeys.UnselectedLabelTextColor,
            InputChipTokensKeys.UnselectedLeadingIconColor,
            InputChipTokensKeys.UnselectedTrailingIconColor,
            AssistChipTokensKeys.FlatOutlineColor,
            InputChipTokensKeys.SelectedContainerColor,
            InputChipTokensKeys.SelectedLabelTextColor,
            InputChipTokensKeys.SelectedLeadingIconColor,
            AssistChipTokensKeys.DisabledLabelTextColor,
            AssistChipTokensKeys.DisabledLabelTextOpacity,
            AssistChipTokensKeys.DisabledIconColor,
            AssistChipTokensKeys.DisabledIconOpacity,
            AssistChipTokensKeys.FlatDisabledOutlineColor,
            AssistChipTokensKeys.FlatDisabledOutlineOpacity);

    /// <summary>Gets the suggestion-chip visual alternatives.</summary>
    public static MaterialValueKey<ChipVisualColors> SuggestionVisualColors { get; } =
        VisualColors(
            "Value.Chip.Suggestion.VisualColors",
            SuggestionChipTokensKeys.LabelTextColor,
            SuggestionChipTokensKeys.LeadingIconColor,
            SuggestionChipTokensKeys.LeadingIconColor,
            AssistChipTokensKeys.FlatOutlineColor,
            ChipsTokensKeys.SelectedContainerColor,
            ChipsTokensKeys.SelectedLabelTextColor,
            ChipsTokensKeys.SelectedLeadingIconColor,
            AssistChipTokensKeys.DisabledLabelTextColor,
            AssistChipTokensKeys.DisabledLabelTextOpacity,
            AssistChipTokensKeys.DisabledIconColor,
            AssistChipTokensKeys.DisabledIconOpacity,
            AssistChipTokensKeys.FlatDisabledOutlineColor,
            AssistChipTokensKeys.FlatDisabledOutlineOpacity);

    /// <summary>Gets the assist-chip label typography role.</summary>
    public static MaterialValueKey<TypographyRole> AssistLabelTextStyle { get; } =
        MaterialValue.FromRaw(
            "Value.Chip.Assist.LabelTextStyle",
            AssistChipTokensKeys.LabelTextFont);

    /// <summary>Gets the filter-chip label typography role.</summary>
    public static MaterialValueKey<TypographyRole> FilterLabelTextStyle { get; } =
        MaterialValue.FromRaw(
            "Value.Chip.Filter.LabelTextStyle",
            FilterChipTokensKeys.LabelTextFont);

    /// <summary>Gets the input-chip label typography role.</summary>
    public static MaterialValueKey<TypographyRole> InputLabelTextStyle { get; } =
        MaterialValue.FromRaw(
            "Value.Chip.Input.LabelTextStyle",
            InputChipTokensKeys.LabelTextFont);

    /// <summary>Gets the suggestion-chip label typography role.</summary>
    public static MaterialValueKey<TypographyRole> SuggestionLabelTextStyle { get; } =
        MaterialValue.FromRaw(
            "Value.Chip.Suggestion.LabelTextStyle",
            SuggestionChipTokensKeys.LabelTextFont);

    private static MaterialValueKey<ChipVisualColors> VisualColors(
        string path,
        MaterialToken<Color> labelTextColor,
        MaterialToken<Color> leadingIconColor,
        MaterialToken<Color> trailingIconColor,
        MaterialToken<Color> outlineColor,
        MaterialToken<Color> selectedContainerColor,
        MaterialToken<Color> selectedLabelTextColor,
        MaterialToken<Color> selectedIconColor,
        MaterialToken<Color> disabledLabelTextColor,
        MaterialToken<double> disabledLabelTextOpacity,
        MaterialToken<Color> disabledIconColor,
        MaterialToken<double> disabledIconOpacity,
        MaterialToken<Color> disabledOutlineColor,
        MaterialToken<double> disabledOutlineOpacity) =>
        new(
            path,
            new RawRecipePlan<ChipVisualColors>(
                new ChipVisualColorsRecipe(
                    labelTextColor,
                    leadingIconColor,
                    trailingIconColor,
                    outlineColor,
                    selectedContainerColor,
                    selectedLabelTextColor,
                    selectedIconColor,
                    disabledLabelTextColor,
                    disabledLabelTextOpacity,
                    disabledIconColor,
                    disabledIconOpacity,
                    disabledOutlineColor,
                    disabledOutlineOpacity)));
}

/// <summary>
/// Immutable layout inputs consumed by the chip template and renderer.
/// </summary>
public sealed record ChipGeometryProfile(
    double ContainerHeight,
    ShapeRole ContainerShape,
    ShapeRole SelectedContainerShape,
    double OutlineWidth,
    double IconSize,
    Thickness ContentPadding,
    double IconLabelSpace);

/// <summary>
/// Immutable state alternatives consumed by the chip renderer and content slots.
/// </summary>
public sealed record ChipVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush LabelTextColor,
    IImmutableBrush LeadingIconColor,
    IImmutableBrush TrailingIconColor,
    IImmutableBrush OutlineColor,
    IImmutableBrush StateLayerColor,
    IImmutableBrush SelectedContainerColor,
    IImmutableBrush SelectedLabelTextColor,
    IImmutableBrush SelectedIconColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush DisabledLabelTextColor,
    IImmutableBrush DisabledIconColor,
    IImmutableBrush DisabledOutlineColor);

internal sealed class ChipGeometryProfileRecipe : IRawValueRecipe<ChipGeometryProfile>
{
    private const double HorizontalSpace = 16;
    private const double IconLabelSpace = 8;

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerHeight;
    private readonly MaterialToken<ShapeRole> _containerShape;
    private readonly MaterialToken<ShapeRole> _selectedContainerShape;
    private readonly MaterialToken<double> _outlineWidth;
    private readonly MaterialToken<double> _iconSize;

    internal ChipGeometryProfileRecipe(
        MaterialToken<double> containerHeight,
        MaterialToken<ShapeRole> containerShape,
        MaterialToken<ShapeRole> selectedContainerShape,
        MaterialToken<double> outlineWidth,
        MaterialToken<double> iconSize)
    {
        _containerHeight = containerHeight ?? throw new ArgumentNullException(nameof(containerHeight));
        _containerShape = containerShape ?? throw new ArgumentNullException(nameof(containerShape));
        _selectedContainerShape = selectedContainerShape ??
            throw new ArgumentNullException(nameof(selectedContainerShape));
        _outlineWidth = outlineWidth ?? throw new ArgumentNullException(nameof(outlineWidth));
        _iconSize = iconSize ?? throw new ArgumentNullException(nameof(iconSize));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [containerHeight, containerShape, selectedContainerShape, outlineWidth, iconSize]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ChipGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_containerHeight),
        context.Get(_containerShape),
        context.Get(_selectedContainerShape),
        context.Get(_outlineWidth),
        context.Get(_iconSize),
        new Thickness(HorizontalSpace, 0, HorizontalSpace, 0),
        IconLabelSpace);
}

internal sealed class ChipVisualColorsRecipe : IRawValueRecipe<ChipVisualColors>
{
    private static readonly IImmutableBrush Transparent =
        new ImmutableSolidColorBrush(Colors.Transparent);

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _labelTextColor;
    private readonly MaterialToken<Color> _leadingIconColor;
    private readonly MaterialToken<Color> _trailingIconColor;
    private readonly MaterialToken<Color> _outlineColor;
    private readonly MaterialToken<Color> _selectedContainerColor;
    private readonly MaterialToken<Color> _selectedLabelTextColor;
    private readonly MaterialToken<Color> _selectedIconColor;
    private readonly MaterialToken<Color> _disabledLabelTextColor;
    private readonly MaterialToken<double> _disabledLabelTextOpacity;
    private readonly MaterialToken<Color> _disabledIconColor;
    private readonly MaterialToken<double> _disabledIconOpacity;
    private readonly MaterialToken<Color> _disabledOutlineColor;
    private readonly MaterialToken<double> _disabledOutlineOpacity;

    internal ChipVisualColorsRecipe(
        MaterialToken<Color> labelTextColor,
        MaterialToken<Color> leadingIconColor,
        MaterialToken<Color> trailingIconColor,
        MaterialToken<Color> outlineColor,
        MaterialToken<Color> selectedContainerColor,
        MaterialToken<Color> selectedLabelTextColor,
        MaterialToken<Color> selectedIconColor,
        MaterialToken<Color> disabledLabelTextColor,
        MaterialToken<double> disabledLabelTextOpacity,
        MaterialToken<Color> disabledIconColor,
        MaterialToken<double> disabledIconOpacity,
        MaterialToken<Color> disabledOutlineColor,
        MaterialToken<double> disabledOutlineOpacity)
    {
        _labelTextColor = labelTextColor ?? throw new ArgumentNullException(nameof(labelTextColor));
        _leadingIconColor = leadingIconColor ?? throw new ArgumentNullException(nameof(leadingIconColor));
        _trailingIconColor = trailingIconColor ?? throw new ArgumentNullException(nameof(trailingIconColor));
        _outlineColor = outlineColor ?? throw new ArgumentNullException(nameof(outlineColor));
        _selectedContainerColor = selectedContainerColor ??
            throw new ArgumentNullException(nameof(selectedContainerColor));
        _selectedLabelTextColor = selectedLabelTextColor ??
            throw new ArgumentNullException(nameof(selectedLabelTextColor));
        _selectedIconColor = selectedIconColor ?? throw new ArgumentNullException(nameof(selectedIconColor));
        _disabledLabelTextColor = disabledLabelTextColor ??
            throw new ArgumentNullException(nameof(disabledLabelTextColor));
        _disabledLabelTextOpacity = disabledLabelTextOpacity ??
            throw new ArgumentNullException(nameof(disabledLabelTextOpacity));
        _disabledIconColor = disabledIconColor ?? throw new ArgumentNullException(nameof(disabledIconColor));
        _disabledIconOpacity = disabledIconOpacity ??
            throw new ArgumentNullException(nameof(disabledIconOpacity));
        _disabledOutlineColor = disabledOutlineColor ??
            throw new ArgumentNullException(nameof(disabledOutlineColor));
        _disabledOutlineOpacity = disabledOutlineOpacity ??
            throw new ArgumentNullException(nameof(disabledOutlineOpacity));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            labelTextColor,
            leadingIconColor,
            trailingIconColor,
            outlineColor,
            selectedContainerColor,
            selectedLabelTextColor,
            selectedIconColor,
            disabledLabelTextColor,
            disabledLabelTextOpacity,
            disabledIconColor,
            disabledIconOpacity,
            disabledOutlineColor,
            disabledOutlineOpacity,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public ChipVisualColors Evaluate(MaterialValueContext context)
    {
        var label = Brush(context, _labelTextColor);
        return new(
            Transparent,
            label,
            Brush(context, _leadingIconColor),
            Brush(context, _trailingIconColor),
            Brush(context, _outlineColor),
            label,
            Brush(context, _selectedContainerColor),
            Brush(context, _selectedLabelTextColor),
            Brush(context, _selectedIconColor),
            Transparent,
            Brush(context, _disabledLabelTextColor, _disabledLabelTextOpacity),
            Brush(context, _disabledIconColor, _disabledIconOpacity),
            Brush(context, _disabledOutlineColor, _disabledOutlineOpacity));
    }

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
