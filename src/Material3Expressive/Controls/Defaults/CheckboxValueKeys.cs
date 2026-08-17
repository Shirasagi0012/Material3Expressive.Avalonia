using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by the self-rendering checkbox.
/// </summary>
/// <remarks>
/// The checkbox renderer consumes two atomic profiles instead of asking the resource provider for
/// individual state values.  The profiles deliberately contain only renderer inputs; state,
/// bounds, glyph progress and motion remain control concerns.
/// </remarks>
public static class CheckboxValueKeys
{
    /// <summary>Gets the geometry profile consumed by the checkbox renderer.</summary>
    public static MaterialValueKey<CheckboxGeometryProfile> GeometryProfile { get; } =
        new(
            "Value.Checkbox.GeometryProfile",
            new RawRecipePlan<CheckboxGeometryProfile>(
                new CheckboxGeometryProfileRecipe(
                    CheckboxTokensKeys.ContainerShape,
                    CheckboxTokensKeys.StateLayerSize,
                    CheckboxTokensKeys.UnselectedOutlineWidth)));

    /// <summary>Gets the state-independent brush profile consumed by the checkbox renderer.</summary>
    public static MaterialValueKey<CheckboxVisualColors> VisualColors { get; } =
        new(
            "Value.Checkbox.VisualColors",
            new RawRecipePlan<CheckboxVisualColors>(
                new CheckboxVisualColorsRecipe(
                    CheckboxTokensKeys.SelectedContainerColor,
                    CheckboxTokensKeys.SelectedIconColor,
                    CheckboxTokensKeys.UnselectedOutlineColor,
                    CheckboxTokensKeys.UnselectedFocusOutlineColor,
                    CheckboxTokensKeys.SelectedDisabledContainerColor,
                    CheckboxTokensKeys.SelectedDisabledContainerOpacity,
                    CheckboxTokensKeys.SelectedDisabledIconColor,
                    CheckboxTokensKeys.UnselectedDisabledOutlineColor,
                    CheckboxTokensKeys.SelectedErrorContainerColor,
                    CheckboxTokensKeys.SelectedErrorIconColor,
                    CheckboxTokensKeys.UnselectedErrorOutlineColor)));

    /// <summary>Gets the label brush role used by the checkbox content presenter.</summary>
    public static MaterialValueKey<IImmutableBrush> LabelBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Checkbox.LabelBrush",
            SystemColorTokens.OnSurface);
}

/// <summary>Atomic geometry inputs consumed by <see cref="MaterialCheckbox" />.</summary>
public sealed record CheckboxGeometryProfile(
    double ContainerSize,
    double ContainerCornerRadius,
    double StateLayerSize,
    double OutlineWidth);

/// <summary>Atomic brush inputs consumed by <see cref="MaterialCheckbox" />.</summary>
/// <remarks>
/// The eleven slots are exactly the brushes the current renderer consumes.  The two state-layer
/// slots intentionally preserve the existing checkbox mapping: selected state layer reuses the
/// selected container, and unselected state layer uses the focus outline role.
/// </remarks>
public sealed record CheckboxVisualColors(
    IImmutableBrush SelectedContainerColor,
    IImmutableBrush SelectedIconColor,
    IImmutableBrush SelectedStateLayerColor,
    IImmutableBrush UnselectedOutlineColor,
    IImmutableBrush UnselectedStateLayerColor,
    IImmutableBrush DisabledSelectedContainerColor,
    IImmutableBrush DisabledSelectedIconColor,
    IImmutableBrush DisabledOutlineColor,
    IImmutableBrush ErrorContainerColor,
    IImmutableBrush ErrorIconColor,
    IImmutableBrush ErrorOutlineColor);

internal sealed class CheckboxGeometryProfileRecipe : IRawValueRecipe<CheckboxGeometryProfile>
{
    private const double ReferenceContainerSize = 20;

    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _containerShape;
    private readonly MaterialToken<double> _stateLayerSize;
    private readonly MaterialToken<double> _outlineWidth;

    internal CheckboxGeometryProfileRecipe(
        MaterialToken<double> containerShape,
        MaterialToken<double> stateLayerSize,
        MaterialToken<double> outlineWidth)
    {
        _containerShape = containerShape ?? throw new ArgumentNullException(nameof(containerShape));
        _stateLayerSize = stateLayerSize ?? throw new ArgumentNullException(nameof(stateLayerSize));
        _outlineWidth = outlineWidth ?? throw new ArgumentNullException(nameof(outlineWidth));
        _dependencies = Array.AsReadOnly<MaterialToken>(
            [containerShape, stateLayerSize, outlineWidth]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public CheckboxGeometryProfile Evaluate(MaterialValueContext context) => new(
        ReferenceContainerSize,
        context.Get(_containerShape),
        context.Get(_stateLayerSize),
        context.Get(_outlineWidth));
}

internal sealed class CheckboxVisualColorsRecipe : IRawValueRecipe<CheckboxVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _selectedContainerColor;
    private readonly MaterialToken<Color> _selectedIconColor;
    private readonly MaterialToken<Color> _unselectedOutlineColor;
    private readonly MaterialToken<Color> _unselectedFocusOutlineColor;
    private readonly MaterialToken<Color> _selectedDisabledContainerColor;
    private readonly MaterialToken<double> _selectedDisabledContainerOpacity;
    private readonly MaterialToken<Color> _selectedDisabledIconColor;
    private readonly MaterialToken<Color> _unselectedDisabledOutlineColor;
    private readonly MaterialToken<Color> _selectedErrorContainerColor;
    private readonly MaterialToken<Color> _selectedErrorIconColor;
    private readonly MaterialToken<Color> _unselectedErrorOutlineColor;

    internal CheckboxVisualColorsRecipe(
        MaterialToken<Color> selectedContainerColor,
        MaterialToken<Color> selectedIconColor,
        MaterialToken<Color> unselectedOutlineColor,
        MaterialToken<Color> unselectedFocusOutlineColor,
        MaterialToken<Color> selectedDisabledContainerColor,
        MaterialToken<double> selectedDisabledContainerOpacity,
        MaterialToken<Color> selectedDisabledIconColor,
        MaterialToken<Color> unselectedDisabledOutlineColor,
        MaterialToken<Color> selectedErrorContainerColor,
        MaterialToken<Color> selectedErrorIconColor,
        MaterialToken<Color> unselectedErrorOutlineColor)
    {
        _selectedContainerColor = selectedContainerColor ??
            throw new ArgumentNullException(nameof(selectedContainerColor));
        _selectedIconColor = selectedIconColor ??
            throw new ArgumentNullException(nameof(selectedIconColor));
        _unselectedOutlineColor = unselectedOutlineColor ??
            throw new ArgumentNullException(nameof(unselectedOutlineColor));
        _unselectedFocusOutlineColor = unselectedFocusOutlineColor ??
            throw new ArgumentNullException(nameof(unselectedFocusOutlineColor));
        _selectedDisabledContainerColor = selectedDisabledContainerColor ??
            throw new ArgumentNullException(nameof(selectedDisabledContainerColor));
        _selectedDisabledContainerOpacity = selectedDisabledContainerOpacity ??
            throw new ArgumentNullException(nameof(selectedDisabledContainerOpacity));
        _selectedDisabledIconColor = selectedDisabledIconColor ??
            throw new ArgumentNullException(nameof(selectedDisabledIconColor));
        _unselectedDisabledOutlineColor = unselectedDisabledOutlineColor ??
            throw new ArgumentNullException(nameof(unselectedDisabledOutlineColor));
        _selectedErrorContainerColor = selectedErrorContainerColor ??
            throw new ArgumentNullException(nameof(selectedErrorContainerColor));
        _selectedErrorIconColor = selectedErrorIconColor ??
            throw new ArgumentNullException(nameof(selectedErrorIconColor));
        _unselectedErrorOutlineColor = unselectedErrorOutlineColor ??
            throw new ArgumentNullException(nameof(unselectedErrorOutlineColor));

        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            selectedContainerColor,
            selectedIconColor,
            unselectedOutlineColor,
            unselectedFocusOutlineColor,
            selectedDisabledContainerColor,
            selectedDisabledContainerOpacity,
            selectedDisabledIconColor,
            unselectedDisabledOutlineColor,
            selectedErrorContainerColor,
            selectedErrorIconColor,
            unselectedErrorOutlineColor,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public CheckboxVisualColors Evaluate(MaterialValueContext context)
    {
        var selectedContainer = Brush(context, _selectedContainerColor);
        var selectedIcon = Brush(context, _selectedIconColor);
        var unselectedOutline = Brush(context, _unselectedOutlineColor);
        var unselectedFocusOutline = Brush(context, _unselectedFocusOutlineColor);
        var disabledContainer = Brush(
            context,
            _selectedDisabledContainerColor,
            _selectedDisabledContainerOpacity);
        var disabledIcon = Brush(context, _selectedDisabledIconColor);
        var disabledOutline = Brush(
            context,
            _unselectedDisabledOutlineColor,
            _selectedDisabledContainerOpacity);
        var errorContainer = Brush(context, _selectedErrorContainerColor);
        var errorIcon = Brush(context, _selectedErrorIconColor);
        var errorOutline = Brush(context, _unselectedErrorOutlineColor);

        return new CheckboxVisualColors(
            selectedContainer,
            selectedIcon,
            selectedContainer,
            unselectedOutline,
            unselectedFocusOutline,
            disabledContainer,
            disabledIcon,
            disabledOutline,
            errorContainer,
            errorIcon,
            errorOutline);
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
