using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>Consumer-ready Material value endpoints for text fields and search bars.</summary>
public static class TextInputValueKeys
{
    public static MaterialValueKey<TextFieldGeometryProfile> FilledTextFieldGeometry { get; } =
        new(
            "Value.TextField.Filled.Geometry",
            new RawRecipePlan<TextFieldGeometryProfile>(
                new TextFieldGeometryRecipe(
                    FilledTextFieldTokensKeys.ContainerShape,
                    containerHeight: null,
                    FilledTextFieldTokensKeys.ActiveIndicatorHeight,
                    FilledTextFieldTokensKeys.FocusActiveIndicatorHeight,
                    FilledTextFieldTokensKeys.LeadingIconSize,
                    isFilled: true)));

    public static MaterialValueKey<TextFieldGeometryProfile> OutlinedTextFieldGeometry { get; } =
        new(
            "Value.TextField.Outlined.Geometry",
            new RawRecipePlan<TextFieldGeometryProfile>(
                new TextFieldGeometryRecipe(
                    OutlinedTextFieldTokensKeys.ContainerShape,
                    OutlinedTextFieldTokensKeys.ContainerHeight,
                    OutlinedTextFieldTokensKeys.OutlineWidth,
                    OutlinedTextFieldTokensKeys.FocusOutlineWidth,
                    OutlinedTextFieldTokensKeys.LeadingIconSize,
                    isFilled: false)));

    public static MaterialValueKey<TextFieldTypographyProfile> FilledTextFieldTypography { get; } =
        new(
            "Value.TextField.Filled.Typography",
            new RawRecipePlan<TextFieldTypographyProfile>(
                new TextFieldTypographyRecipe(
                    FilledTextFieldTokensKeys.InputFont,
                    FilledTextFieldTokensKeys.SupportingFont)));

    public static MaterialValueKey<TextFieldTypographyProfile> OutlinedTextFieldTypography { get; } =
        new(
            "Value.TextField.Outlined.Typography",
            new RawRecipePlan<TextFieldTypographyProfile>(
                new TextFieldTypographyRecipe(
                    OutlinedTextFieldTokensKeys.InputFont,
                    OutlinedTextFieldTokensKeys.SupportingFont)));

    public static MaterialValueKey<TextFieldVisualColors> FilledTextFieldVisualColors { get; } =
        new(
            "Value.TextField.Filled.VisualColors",
            new RawRecipePlan<TextFieldVisualColors>(
                TextFieldVisualColorsRecipe.Filled()));

    public static MaterialValueKey<TextFieldVisualColors> OutlinedTextFieldVisualColors { get; } =
        new(
            "Value.TextField.Outlined.VisualColors",
            new RawRecipePlan<TextFieldVisualColors>(
                TextFieldVisualColorsRecipe.Outlined()));

    public static MaterialValueKey<SearchBarGeometryProfile> SearchBarGeometry { get; } =
        new(
            "Value.SearchBar.Geometry",
            new RawRecipePlan<SearchBarGeometryProfile>(new SearchBarGeometryRecipe()));

    public static MaterialValueKey<SearchBarTypographyProfile> SearchBarTypography { get; } =
        new(
            "Value.SearchBar.Typography",
            new RawRecipePlan<SearchBarTypographyProfile>(new SearchBarTypographyRecipe()));

    public static MaterialValueKey<SearchBarVisualColors> SearchBarVisualColors { get; } =
        new(
            "Value.SearchBar.VisualColors",
            new RawRecipePlan<SearchBarVisualColors>(new SearchBarVisualColorsRecipe()));
}

public sealed record TextFieldGeometryProfile(
    ShapeRole ContainerShape,
    double ContainerHeight,
    double OutlineWidth,
    double FocusOutlineWidth,
    double IconSize,
    Thickness ContentPadding,
    bool IsFilled);

public sealed record TextFieldTypographyProfile(
    TypographyRole InputTextStyle,
    TypographyRole FloatingLabelTextStyle,
    TypographyRole SupportingTextStyle);

public sealed record TextFieldVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush DisabledContainerColor,
    IImmutableBrush InputColor,
    IImmutableBrush PlaceholderColor,
    IImmutableBrush LabelColor,
    IImmutableBrush FocusLabelColor,
    IImmutableBrush SupportingColor,
    IImmutableBrush LeadingIconColor,
    IImmutableBrush TrailingIconColor,
    IImmutableBrush OutlineColor,
    IImmutableBrush FocusOutlineColor,
    IImmutableBrush ErrorColor,
    IImmutableBrush ErrorInputColor,
    IImmutableBrush DisabledInputColor,
    IImmutableBrush DisabledLabelColor,
    IImmutableBrush DisabledSupportingColor,
    IImmutableBrush DisabledIconColor,
    IImmutableBrush DisabledOutlineColor);

public sealed record SearchBarGeometryProfile(
    ShapeRole ContainerShape,
    ShapeRole ResultsContainerShape,
    double ContainerElevation,
    double ContainerHeight,
    Thickness ContentPadding,
    double IconSize,
    double IconLabelSpace,
    double DividerHeight);

public sealed record SearchBarTypographyProfile(TypographyRole InputTextStyle);

public sealed record SearchBarVisualColors(
    IImmutableBrush ContainerColor,
    IImmutableBrush InputTextColor,
    IImmutableBrush SupportingTextColor,
    IImmutableBrush LeadingIconColor,
    IImmutableBrush TrailingIconColor,
    IImmutableBrush ResultsContainerColor,
    IImmutableBrush DividerColor,
    Color ShadowColor);

internal sealed class TextFieldGeometryRecipe : IRawValueRecipe<TextFieldGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<ShapeRole> _shape;
    private readonly MaterialToken<double>? _height;
    private readonly MaterialToken<double> _outlineWidth;
    private readonly MaterialToken<double> _focusOutlineWidth;
    private readonly MaterialToken<double> _iconSize;
    private readonly bool _isFilled;

    internal TextFieldGeometryRecipe(
        MaterialToken<ShapeRole> shape,
        MaterialToken<double>? containerHeight,
        MaterialToken<double> outlineWidth,
        MaterialToken<double> focusOutlineWidth,
        MaterialToken<double> iconSize,
        bool isFilled)
    {
        _shape = shape;
        _height = containerHeight;
        _outlineWidth = outlineWidth;
        _focusOutlineWidth = focusOutlineWidth;
        _iconSize = iconSize;
        _isFilled = isFilled;
        var dependencies = new List<MaterialToken>
        {
            shape,
            outlineWidth,
            focusOutlineWidth,
            iconSize,
        };
        if (containerHeight is not null)
            dependencies.Add(containerHeight);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TextFieldGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_shape),
        _height is null ? 56 : context.Get(_height),
        context.Get(_outlineWidth),
        context.Get(_focusOutlineWidth),
        context.Get(_iconSize),
        ContentPadding: new Thickness(16, 0),
        _isFilled);
}

internal sealed class TextFieldTypographyRecipe : IRawValueRecipe<TextFieldTypographyProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<TypographyRole> _input;
    private readonly MaterialToken<TypographyRole> _supporting;

    internal TextFieldTypographyRecipe(
        MaterialToken<TypographyRole> input,
        MaterialToken<TypographyRole> supporting)
    {
        _input = input;
        _supporting = supporting;
        _dependencies = Array.AsReadOnly<MaterialToken>([input, supporting]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TextFieldTypographyProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_input),
        TypographyRole.BodySmall,
        context.Get(_supporting));
}

internal sealed class TextFieldVisualColorsRecipe : IRawValueRecipe<TextFieldVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color>? _container;
    private readonly MaterialToken<Color>? _disabledContainer;
    private readonly MaterialToken<double>? _disabledContainerOpacity;
    private readonly MaterialToken<Color> _input;
    private readonly MaterialToken<Color> _placeholder;
    private readonly MaterialToken<Color> _label;
    private readonly MaterialToken<Color> _focusLabel;
    private readonly MaterialToken<Color> _supporting;
    private readonly MaterialToken<Color> _leadingIcon;
    private readonly MaterialToken<Color> _trailingIcon;
    private readonly MaterialToken<Color> _outline;
    private readonly MaterialToken<Color> _focusOutline;
    private readonly MaterialToken<Color> _error;
    private readonly MaterialToken<Color> _errorInput;
    private readonly MaterialToken<Color> _disabledInput;
    private readonly MaterialToken<Color> _disabledLabel;
    private readonly MaterialToken<Color> _disabledSupporting;
    private readonly MaterialToken<Color> _disabledIcon;
    private readonly MaterialToken<Color> _disabledOutline;
    private readonly MaterialToken<double> _disabledContentOpacity;
    private readonly MaterialToken<double> _disabledOutlineOpacity;

    private TextFieldVisualColorsRecipe(
        MaterialToken<Color>? container,
        MaterialToken<Color>? disabledContainer,
        MaterialToken<double>? disabledContainerOpacity,
        MaterialToken<Color> input,
        MaterialToken<Color> placeholder,
        MaterialToken<Color> label,
        MaterialToken<Color> focusLabel,
        MaterialToken<Color> supporting,
        MaterialToken<Color> leadingIcon,
        MaterialToken<Color> trailingIcon,
        MaterialToken<Color> outline,
        MaterialToken<Color> focusOutline,
        MaterialToken<Color> error,
        MaterialToken<Color> errorInput,
        MaterialToken<Color> disabledInput,
        MaterialToken<Color> disabledLabel,
        MaterialToken<Color> disabledSupporting,
        MaterialToken<Color> disabledIcon,
        MaterialToken<Color> disabledOutline,
        MaterialToken<double> disabledContentOpacity,
        MaterialToken<double> disabledOutlineOpacity)
    {
        _container = container;
        _disabledContainer = disabledContainer;
        _disabledContainerOpacity = disabledContainerOpacity;
        _input = input;
        _placeholder = placeholder;
        _label = label;
        _focusLabel = focusLabel;
        _supporting = supporting;
        _leadingIcon = leadingIcon;
        _trailingIcon = trailingIcon;
        _outline = outline;
        _focusOutline = focusOutline;
        _error = error;
        _errorInput = errorInput;
        _disabledInput = disabledInput;
        _disabledLabel = disabledLabel;
        _disabledSupporting = disabledSupporting;
        _disabledIcon = disabledIcon;
        _disabledOutline = disabledOutline;
        _disabledContentOpacity = disabledContentOpacity;
        _disabledOutlineOpacity = disabledOutlineOpacity;

        var dependencies = new List<MaterialToken>
        {
            input,
            placeholder,
            label,
            focusLabel,
            supporting,
            leadingIcon,
            trailingIcon,
            outline,
            focusOutline,
            error,
            errorInput,
            disabledInput,
            disabledLabel,
            disabledSupporting,
            disabledIcon,
            disabledOutline,
            disabledContentOpacity,
            disabledOutlineOpacity,
        };
        if (container is not null)
            dependencies.Add(container);
        if (disabledContainer is not null)
            dependencies.Add(disabledContainer);
        if (disabledContainerOpacity is not null)
            dependencies.Add(disabledContainerOpacity);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    internal static TextFieldVisualColorsRecipe Filled() => new(
        FilledTextFieldTokensKeys.ContainerColor,
        disabledContainer: null,
        disabledContainerOpacity: null,
        FilledTextFieldTokensKeys.InputColor,
        FilledTextFieldTokensKeys.InputPlaceholderColor,
        FilledTextFieldTokensKeys.LabelColor,
        FilledTextFieldTokensKeys.FocusLabelColor,
        FilledTextFieldTokensKeys.SupportingColor,
        FilledTextFieldTokensKeys.LeadingIconColor,
        FilledTextFieldTokensKeys.TrailingIconColor,
        FilledTextFieldTokensKeys.ActiveIndicatorColor,
        FilledTextFieldTokensKeys.FocusActiveIndicatorColor,
        FilledTextFieldTokensKeys.ErrorLabelColor,
        FilledTextFieldTokensKeys.ErrorInputColor,
        FilledTextFieldTokensKeys.DisabledInputColor,
        FilledTextFieldTokensKeys.DisabledLabelColor,
        FilledTextFieldTokensKeys.DisabledSupportingColor,
        FilledTextFieldTokensKeys.DisabledLeadingIconColor,
        FilledTextFieldTokensKeys.DisabledActiveIndicatorColor,
        FilledTextFieldTokensKeys.DisabledInputOpacity,
        FilledTextFieldTokensKeys.DisabledActiveIndicatorOpacity);

    internal static TextFieldVisualColorsRecipe Outlined() => new(
        container: null,
        disabledContainer: null,
        disabledContainerOpacity: null,
        OutlinedTextFieldTokensKeys.InputColor,
        OutlinedTextFieldTokensKeys.InputPlaceholderColor,
        OutlinedTextFieldTokensKeys.LabelColor,
        OutlinedTextFieldTokensKeys.FocusLabelColor,
        OutlinedTextFieldTokensKeys.SupportingColor,
        OutlinedTextFieldTokensKeys.LeadingIconColor,
        OutlinedTextFieldTokensKeys.TrailingIconColor,
        OutlinedTextFieldTokensKeys.OutlineColor,
        OutlinedTextFieldTokensKeys.FocusOutlineColor,
        OutlinedTextFieldTokensKeys.ErrorLabelColor,
        OutlinedTextFieldTokensKeys.ErrorInputColor,
        OutlinedTextFieldTokensKeys.DisabledInputColor,
        OutlinedTextFieldTokensKeys.DisabledLabelColor,
        OutlinedTextFieldTokensKeys.DisabledSupportingColor,
        OutlinedTextFieldTokensKeys.DisabledLeadingIconColor,
        OutlinedTextFieldTokensKeys.DisabledOutlineColor,
        OutlinedTextFieldTokensKeys.DisabledInputOpacity,
        OutlinedTextFieldTokensKeys.DisabledOutlineOpacity);

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public TextFieldVisualColors Evaluate(MaterialValueContext context)
    {
        var container = _container is null
            ? new ImmutableSolidColorBrush(Colors.Transparent)
            : Brush(context, _container);
        return new(
            container,
            _disabledContainer is null
                ? container
                : Brush(context, _disabledContainer, _disabledContainerOpacity),
            Brush(context, _input),
            Brush(context, _placeholder),
            Brush(context, _label),
            Brush(context, _focusLabel),
            Brush(context, _supporting),
            Brush(context, _leadingIcon),
            Brush(context, _trailingIcon),
            Brush(context, _outline),
            Brush(context, _focusOutline),
            Brush(context, _error),
            Brush(context, _errorInput),
            Brush(context, _disabledInput, _disabledContentOpacity),
            Brush(context, _disabledLabel, _disabledContentOpacity),
            Brush(context, _disabledSupporting, _disabledContentOpacity),
            Brush(context, _disabledIcon, _disabledContentOpacity),
            Brush(context, _disabledOutline, _disabledOutlineOpacity));
    }

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        MaterialToken<Color> token,
        MaterialToken<double>? opacity = null)
    {
        var color = context.Get(token);
        if (opacity is not null)
        {
            color = new Color(
                (byte)Math.Round(color.A * Math.Clamp(context.Get(opacity), 0d, 1d)),
                color.R,
                color.G,
                color.B);
        }
        return new ImmutableSolidColorBrush(color);
    }
}

internal sealed class SearchBarGeometryRecipe : IRawValueRecipe<SearchBarGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies = Array.AsReadOnly<MaterialToken>(
    [
        SearchBarTokensKeys.ContainerShape,
        SearchViewTokensKeys.DockedContainerShape,
        SearchBarTokensKeys.ContainerElevation,
        SearchBarTokensKeys.ContainerHeight,
    ]);

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SearchBarGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(SearchBarTokensKeys.ContainerShape),
        context.Get(SearchViewTokensKeys.DockedContainerShape),
        context.Get(SearchBarTokensKeys.ContainerElevation),
        context.Get(SearchBarTokensKeys.ContainerHeight),
        ContentPadding: new Thickness(16, 0),
        IconSize: 24,
        IconLabelSpace: 16,
        DividerHeight: 1);
}

internal sealed class SearchBarTypographyRecipe : IRawValueRecipe<SearchBarTypographyProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies = Array.AsReadOnly<MaterialToken>(
        [SearchBarTokensKeys.InputTextFont]);

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SearchBarTypographyProfile Evaluate(MaterialValueContext context) =>
        new(context.Get(SearchBarTokensKeys.InputTextFont));
}

internal sealed class SearchBarVisualColorsRecipe : IRawValueRecipe<SearchBarVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies = Array.AsReadOnly<MaterialToken>(
    [
        SearchBarTokensKeys.ContainerColor,
        SearchBarTokensKeys.InputTextColor,
        SearchBarTokensKeys.SupportingTextColor,
        SearchBarTokensKeys.LeadingIconColor,
        SearchBarTokensKeys.TrailingIconColor,
        SearchViewTokensKeys.ContainerColor,
        SearchViewTokensKeys.DividerColor,
        SystemColorTokens.Shadow,
    ]);

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SearchBarVisualColors Evaluate(MaterialValueContext context) => new(
        Brush(context.Get(SearchBarTokensKeys.ContainerColor)),
        Brush(context.Get(SearchBarTokensKeys.InputTextColor)),
        Brush(context.Get(SearchBarTokensKeys.SupportingTextColor)),
        Brush(context.Get(SearchBarTokensKeys.LeadingIconColor)),
        Brush(context.Get(SearchBarTokensKeys.TrailingIconColor)),
        Brush(context.Get(SearchViewTokensKeys.ContainerColor)),
        Brush(context.Get(SearchViewTokensKeys.DividerColor)),
        context.Get(SystemColorTokens.Shadow));

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}
