using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Consumer-ready Material value endpoints shared by the Slider and RangeSlider controls.
/// </summary>
/// <remarks>
/// The endpoint profiles contain only immutable renderer inputs. Endpoint selection, value state,
/// bounds, orientation, interaction, and motion remain policies of the consuming control.
/// </remarks>
public static class SliderValueKeys
{
    /// <summary>Gets the shared geometry profile consumed by both slider renderers.</summary>
    public static MaterialValueKey<SliderGeometryProfile> GeometryProfile { get; } =
        new(
            "Value.Slider.GeometryProfile",
            new RawRecipePlan<SliderGeometryProfile>(
                new SliderGeometryProfileRecipe(
                    SliderTokensKeys.ActiveTrackHeight,
                    SliderTokensKeys.HandleWidth,
                    SliderTokensKeys.PressedHandleWidth,
                    SliderTokensKeys.HandleHeight,
                    SliderTokensKeys.ActiveHandlePadding,
                    SliderTokensKeys.StopIndicatorSize)));

    /// <summary>Gets the shared enabled/disabled brush profile consumed by both slider renderers.</summary>
    public static MaterialValueKey<SliderVisualColors> VisualColors { get; } =
        new(
            "Value.Slider.VisualColors",
            new RawRecipePlan<SliderVisualColors>(
                new SliderVisualColorsRecipe(
                    SliderTokensKeys.ActiveTrackColor,
                    SliderTokensKeys.InactiveTrackColor,
                    SliderTokensKeys.HandleColor,
                    SliderTokensKeys.DisabledActiveTrackColor,
                    SliderTokensKeys.DisabledActiveTrackOpacity,
                    SliderTokensKeys.DisabledInactiveTrackColor,
                    SliderTokensKeys.DisabledInactiveTrackOpacity,
                    SliderTokensKeys.DisabledHandleColor,
                    SliderTokensKeys.DisabledHandleOpacity)));
}

/// <summary>Raw-derived geometry consumed by the Slider and RangeSlider renderers.</summary>
public sealed record SliderGeometryProfile(
    double TrackHeight,
    double HandleWidth,
    double PressedHandleWidth,
    double HandleHeight,
    double HandlePadding,
    double StopIndicatorSize);

/// <summary>
/// State-independent and disabled brushes consumed by the Slider and RangeSlider renderers.
/// </summary>
public sealed record SliderVisualColors(
    IImmutableBrush ActiveTrackColor,
    IImmutableBrush InactiveTrackColor,
    IImmutableBrush HandleColor,
    IImmutableBrush DisabledActiveTrackColor,
    IImmutableBrush DisabledInactiveTrackColor,
    IImmutableBrush DisabledHandleColor,
    IImmutableBrush StateLayerColor);

internal sealed class SliderGeometryProfileRecipe : IRawValueRecipe<SliderGeometryProfile>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<double> _trackHeight;
    private readonly MaterialToken<double> _handleWidth;
    private readonly MaterialToken<double> _pressedHandleWidth;
    private readonly MaterialToken<double> _handleHeight;
    private readonly MaterialToken<double> _handlePadding;
    private readonly MaterialToken<double> _stopIndicatorSize;

    internal SliderGeometryProfileRecipe(
        MaterialToken<double> trackHeight,
        MaterialToken<double> handleWidth,
        MaterialToken<double> pressedHandleWidth,
        MaterialToken<double> handleHeight,
        MaterialToken<double> handlePadding,
        MaterialToken<double> stopIndicatorSize)
    {
        _trackHeight = trackHeight ?? throw new ArgumentNullException(nameof(trackHeight));
        _handleWidth = handleWidth ?? throw new ArgumentNullException(nameof(handleWidth));
        _pressedHandleWidth = pressedHandleWidth ??
            throw new ArgumentNullException(nameof(pressedHandleWidth));
        _handleHeight = handleHeight ?? throw new ArgumentNullException(nameof(handleHeight));
        _handlePadding = handlePadding ?? throw new ArgumentNullException(nameof(handlePadding));
        _stopIndicatorSize = stopIndicatorSize ??
            throw new ArgumentNullException(nameof(stopIndicatorSize));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            trackHeight,
            handleWidth,
            pressedHandleWidth,
            handleHeight,
            handlePadding,
            stopIndicatorSize,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SliderGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_trackHeight),
        context.Get(_handleWidth),
        context.Get(_pressedHandleWidth),
        context.Get(_handleHeight),
        context.Get(_handlePadding),
        context.Get(_stopIndicatorSize));
}

internal sealed class SliderVisualColorsRecipe : IRawValueRecipe<SliderVisualColors>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<Color> _activeTrackColor;
    private readonly MaterialToken<Color> _inactiveTrackColor;
    private readonly MaterialToken<Color> _handleColor;
    private readonly MaterialToken<Color> _disabledActiveTrackColor;
    private readonly MaterialToken<double> _disabledActiveTrackOpacity;
    private readonly MaterialToken<Color> _disabledInactiveTrackColor;
    private readonly MaterialToken<double> _disabledInactiveTrackOpacity;
    private readonly MaterialToken<Color> _disabledHandleColor;
    private readonly MaterialToken<double> _disabledHandleOpacity;

    internal SliderVisualColorsRecipe(
        MaterialToken<Color> activeTrackColor,
        MaterialToken<Color> inactiveTrackColor,
        MaterialToken<Color> handleColor,
        MaterialToken<Color> disabledActiveTrackColor,
        MaterialToken<double> disabledActiveTrackOpacity,
        MaterialToken<Color> disabledInactiveTrackColor,
        MaterialToken<double> disabledInactiveTrackOpacity,
        MaterialToken<Color> disabledHandleColor,
        MaterialToken<double> disabledHandleOpacity)
    {
        _activeTrackColor = activeTrackColor ??
            throw new ArgumentNullException(nameof(activeTrackColor));
        _inactiveTrackColor = inactiveTrackColor ??
            throw new ArgumentNullException(nameof(inactiveTrackColor));
        _handleColor = handleColor ?? throw new ArgumentNullException(nameof(handleColor));
        _disabledActiveTrackColor = disabledActiveTrackColor ??
            throw new ArgumentNullException(nameof(disabledActiveTrackColor));
        _disabledActiveTrackOpacity = disabledActiveTrackOpacity ??
            throw new ArgumentNullException(nameof(disabledActiveTrackOpacity));
        _disabledInactiveTrackColor = disabledInactiveTrackColor ??
            throw new ArgumentNullException(nameof(disabledInactiveTrackColor));
        _disabledInactiveTrackOpacity = disabledInactiveTrackOpacity ??
            throw new ArgumentNullException(nameof(disabledInactiveTrackOpacity));
        _disabledHandleColor = disabledHandleColor ??
            throw new ArgumentNullException(nameof(disabledHandleColor));
        _disabledHandleOpacity = disabledHandleOpacity ??
            throw new ArgumentNullException(nameof(disabledHandleOpacity));
        _dependencies = Array.AsReadOnly<MaterialToken>(
        [
            activeTrackColor,
            inactiveTrackColor,
            handleColor,
            disabledActiveTrackColor,
            disabledActiveTrackOpacity,
            disabledInactiveTrackColor,
            disabledInactiveTrackOpacity,
            disabledHandleColor,
            disabledHandleOpacity,
        ]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public SliderVisualColors Evaluate(MaterialValueContext context)
    {
        var handle = Brush(context.Get(_handleColor));

        return new SliderVisualColors(
            Brush(context.Get(_activeTrackColor)),
            Brush(context.Get(_inactiveTrackColor)),
            handle,
            Brush(context, _disabledActiveTrackColor, _disabledActiveTrackOpacity),
            Brush(context, _disabledInactiveTrackColor, _disabledInactiveTrackOpacity),
            Brush(context, _disabledHandleColor, _disabledHandleOpacity),
            // The slider interaction layer follows the handle role in the upstream defaults.
            handle);
    }

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);

    private static IImmutableBrush Brush(
        MaterialValueContext context,
        MaterialToken<Color> colorToken,
        MaterialToken<double> opacityToken)
    {
        var color = context.Get(colorToken);
        var opacity = Math.Clamp(context.Get(opacityToken), 0d, 1d);
        color = new Color(
            (byte)Math.Round(color.A * opacity),
            color.R,
            color.G,
            color.B);
        return Brush(color);
    }
}
