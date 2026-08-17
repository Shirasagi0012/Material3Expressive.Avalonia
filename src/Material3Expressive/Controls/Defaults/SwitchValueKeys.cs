using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints used by the self-rendering switch.
/// </summary>
/// <remarks>
/// These profiles contain only immutable renderer inputs. Checked/enabled selection, icon
/// presence, state-layer interaction, bounds, and both handle animations remain control policy.
/// The state-specific brush slots are kept in one profile so a Raw mutation updates one coherent
/// renderer input rather than making the control resolve individual tokens.
/// </remarks>
public static class SwitchValueKeys
{
    /// <summary>Gets the geometry profile consumed by the switch renderer.</summary>
    public static MaterialValueKey<SwitchGeometryProfile> GeometryProfile { get; } =
        new(
            "Value.Switch.GeometryProfile",
            new RawRecipePlan<SwitchGeometryProfile>(
                new SwitchGeometryProfileRecipe(
                    SwitchTokensKeys.TrackWidth,
                    SwitchTokensKeys.TrackHeight,
                    SwitchTokensKeys.TrackOutlineWidth,
                    SwitchTokensKeys.UnselectedHandleWidth,
                    SwitchTokensKeys.SelectedHandleWidth,
                    SwitchTokensKeys.PressedHandleWidth,
                    SwitchTokensKeys.StateLayerSize)));

    /// <summary>Gets the state-independent visual brush profile used by the switch renderer.</summary>
    public static MaterialValueKey<SwitchVisualColors> VisualColors { get; } =
        new(
            "Value.Switch.VisualColors",
            new RawRecipePlan<SwitchVisualColors>(
                new SwitchVisualColorsRecipe(
                    SwitchTokensKeys.SelectedTrackColor,
                    SwitchTokensKeys.SelectedHandleColor,
                    SwitchTokensKeys.UnselectedTrackColor,
                    SwitchTokensKeys.UnselectedTrackOutlineColor,
                    SwitchTokensKeys.UnselectedHandleColor,
                    SwitchTokensKeys.DisabledSelectedTrackColor,
                    SwitchTokensKeys.DisabledTrackOpacity,
                    SwitchTokensKeys.DisabledSelectedHandleColor,
                    SwitchTokensKeys.DisabledSelectedHandleOpacity,
                    SwitchTokensKeys.DisabledUnselectedTrackColor,
                    SwitchTokensKeys.DisabledUnselectedTrackOutlineColor,
                    SwitchTokensKeys.DisabledUnselectedHandleColor,
                    SwitchTokensKeys.DisabledUnselectedHandleOpacity)));

    /// <summary>Gets the label brush role used by the switch content presenter.</summary>
    public static MaterialValueKey<IImmutableBrush> LabelBrush { get; } =
        MaterialValue.SolidBrush(
            "Value.Switch.LabelBrush",
            SystemColorTokens.OnSurface);
}

/// <summary>Immutable geometry inputs consumed by <see cref="MaterialSwitch" />.</summary>
public sealed record SwitchGeometryProfile(
    double TrackWidth,
    double TrackHeight,
    double TrackOutlineWidth,
    double UnselectedHandleSize,
    double SelectedHandleSize,
    double PressedHandleSize,
    double StateLayerSize);

/// <summary>Immutable brush inputs consumed by <see cref="MaterialSwitch" />.</summary>
public sealed record SwitchVisualColors(
    IImmutableBrush SelectedTrackColor,
    IImmutableBrush SelectedHandleColor,
    IImmutableBrush UnselectedTrackColor,
    IImmutableBrush UnselectedTrackOutlineColor,
    IImmutableBrush UnselectedHandleColor,
    IImmutableBrush DisabledSelectedTrackColor,
    IImmutableBrush DisabledSelectedHandleColor,
    IImmutableBrush DisabledUnselectedTrackColor,
    IImmutableBrush DisabledUnselectedTrackOutlineColor,
    IImmutableBrush DisabledUnselectedHandleColor,
    IImmutableBrush SelectedStateLayerColor,
    IImmutableBrush UnselectedStateLayerColor);

internal sealed class SwitchGeometryProfileRecipe : IRawValueRecipe<SwitchGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _trackWidth;
    private readonly MaterialToken<double> _trackHeight;
    private readonly MaterialToken<double> _trackOutlineWidth;
    private readonly MaterialToken<double> _unselectedHandleSize;
    private readonly MaterialToken<double> _selectedHandleSize;
    private readonly MaterialToken<double> _pressedHandleSize;
    private readonly MaterialToken<double> _stateLayerSize;

    internal SwitchGeometryProfileRecipe(
        MaterialToken<double> trackWidth,
        MaterialToken<double> trackHeight,
        MaterialToken<double> trackOutlineWidth,
        MaterialToken<double> unselectedHandleSize,
        MaterialToken<double> selectedHandleSize,
        MaterialToken<double> pressedHandleSize,
        MaterialToken<double> stateLayerSize)
    {
        _trackWidth = trackWidth ?? throw new ArgumentNullException(nameof(trackWidth));
        _trackHeight = trackHeight ?? throw new ArgumentNullException(nameof(trackHeight));
        _trackOutlineWidth = trackOutlineWidth ??
            throw new ArgumentNullException(nameof(trackOutlineWidth));
        _unselectedHandleSize = unselectedHandleSize ??
            throw new ArgumentNullException(nameof(unselectedHandleSize));
        _selectedHandleSize = selectedHandleSize ??
            throw new ArgumentNullException(nameof(selectedHandleSize));
        _pressedHandleSize = pressedHandleSize ??
            throw new ArgumentNullException(nameof(pressedHandleSize));
        _stateLayerSize = stateLayerSize ?? throw new ArgumentNullException(nameof(stateLayerSize));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            trackWidth,
            trackHeight,
            trackOutlineWidth,
            unselectedHandleSize,
            selectedHandleSize,
            pressedHandleSize,
            stateLayerSize,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SwitchGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_trackWidth),
        context.Get(_trackHeight),
        context.Get(_trackOutlineWidth),
        context.Get(_unselectedHandleSize),
        context.Get(_selectedHandleSize),
        context.Get(_pressedHandleSize),
        context.Get(_stateLayerSize));
}

internal sealed class SwitchVisualColorsRecipe : IRawValueRecipe<SwitchVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _selectedTrackColor;
    private readonly MaterialToken<Color> _selectedHandleColor;
    private readonly MaterialToken<Color> _unselectedTrackColor;
    private readonly MaterialToken<Color> _unselectedTrackOutlineColor;
    private readonly MaterialToken<Color> _unselectedHandleColor;
    private readonly MaterialToken<Color> _disabledSelectedTrackColor;
    private readonly MaterialToken<double> _disabledTrackOpacity;
    private readonly MaterialToken<Color> _disabledSelectedHandleColor;
    private readonly MaterialToken<double> _disabledSelectedHandleOpacity;
    private readonly MaterialToken<Color> _disabledUnselectedTrackColor;
    private readonly MaterialToken<Color> _disabledUnselectedTrackOutlineColor;
    private readonly MaterialToken<Color> _disabledUnselectedHandleColor;
    private readonly MaterialToken<double> _disabledUnselectedHandleOpacity;

    internal SwitchVisualColorsRecipe(
        MaterialToken<Color> selectedTrackColor,
        MaterialToken<Color> selectedHandleColor,
        MaterialToken<Color> unselectedTrackColor,
        MaterialToken<Color> unselectedTrackOutlineColor,
        MaterialToken<Color> unselectedHandleColor,
        MaterialToken<Color> disabledSelectedTrackColor,
        MaterialToken<double> disabledTrackOpacity,
        MaterialToken<Color> disabledSelectedHandleColor,
        MaterialToken<double> disabledSelectedHandleOpacity,
        MaterialToken<Color> disabledUnselectedTrackColor,
        MaterialToken<Color> disabledUnselectedTrackOutlineColor,
        MaterialToken<Color> disabledUnselectedHandleColor,
        MaterialToken<double> disabledUnselectedHandleOpacity)
    {
        _selectedTrackColor = selectedTrackColor ??
            throw new ArgumentNullException(nameof(selectedTrackColor));
        _selectedHandleColor = selectedHandleColor ??
            throw new ArgumentNullException(nameof(selectedHandleColor));
        _unselectedTrackColor = unselectedTrackColor ??
            throw new ArgumentNullException(nameof(unselectedTrackColor));
        _unselectedTrackOutlineColor = unselectedTrackOutlineColor ??
            throw new ArgumentNullException(nameof(unselectedTrackOutlineColor));
        _unselectedHandleColor = unselectedHandleColor ??
            throw new ArgumentNullException(nameof(unselectedHandleColor));
        _disabledSelectedTrackColor = disabledSelectedTrackColor ??
            throw new ArgumentNullException(nameof(disabledSelectedTrackColor));
        _disabledTrackOpacity = disabledTrackOpacity ??
            throw new ArgumentNullException(nameof(disabledTrackOpacity));
        _disabledSelectedHandleColor = disabledSelectedHandleColor ??
            throw new ArgumentNullException(nameof(disabledSelectedHandleColor));
        _disabledSelectedHandleOpacity = disabledSelectedHandleOpacity ??
            throw new ArgumentNullException(nameof(disabledSelectedHandleOpacity));
        _disabledUnselectedTrackColor = disabledUnselectedTrackColor ??
            throw new ArgumentNullException(nameof(disabledUnselectedTrackColor));
        _disabledUnselectedTrackOutlineColor = disabledUnselectedTrackOutlineColor ??
            throw new ArgumentNullException(nameof(disabledUnselectedTrackOutlineColor));
        _disabledUnselectedHandleColor = disabledUnselectedHandleColor ??
            throw new ArgumentNullException(nameof(disabledUnselectedHandleColor));
        _disabledUnselectedHandleOpacity = disabledUnselectedHandleOpacity ??
            throw new ArgumentNullException(nameof(disabledUnselectedHandleOpacity));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            selectedTrackColor,
            selectedHandleColor,
            unselectedTrackColor,
            unselectedTrackOutlineColor,
            unselectedHandleColor,
            disabledSelectedTrackColor,
            disabledTrackOpacity,
            disabledSelectedHandleColor,
            disabledSelectedHandleOpacity,
            disabledUnselectedTrackColor,
            disabledUnselectedTrackOutlineColor,
            disabledUnselectedHandleColor,
            disabledUnselectedHandleOpacity,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SwitchVisualColors Evaluate(MaterialValueContext context)
    {
        var selectedTrack = Brush(context, _selectedTrackColor);
        var unselectedTrack = Brush(context, _unselectedTrackColor);

        return new SwitchVisualColors(
            selectedTrack,
            Brush(context, _selectedHandleColor),
            unselectedTrack,
            Brush(context, _unselectedTrackOutlineColor),
            Brush(context, _unselectedHandleColor),
            Brush(context, _disabledSelectedTrackColor, _disabledTrackOpacity),
            Brush(context, _disabledSelectedHandleColor, _disabledSelectedHandleOpacity),
            Brush(context, _disabledUnselectedTrackColor, _disabledTrackOpacity),
            Brush(context, _disabledUnselectedTrackOutlineColor, _disabledTrackOpacity),
            Brush(context, _disabledUnselectedHandleColor, _disabledUnselectedHandleOpacity),
            selectedTrack,
            unselectedTrack);
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
