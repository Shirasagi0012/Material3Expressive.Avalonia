using Avalonia;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public enum TooltipAnchorPosition { Above, Below, Left, Right, Start, End }

/// <summary>Calculates a popup's top-left position in physical window pixels.</summary>
public interface ITooltipPositionProvider
{
    PixelPoint CalculatePosition(PixelRect anchorBounds, PixelSize windowSize, bool rightToLeft, PixelSize tooltipSize);
}

public static class TooltipDefaults
{
    public static readonly MutatorMutex GlobalMutatorMutex = new();
    public static readonly TimeSpan TooltipDuration = TimeSpan.FromMilliseconds(1500);
    public const double Spacing = 4;

    public static ITooltipPositionProvider CreatePositionProvider(TooltipAnchorPosition position = TooltipAnchorPosition.Above,
        double spacing = Spacing, double renderScaling = 1) => new PositionProvider(position, spacing, renderScaling);

    private sealed class PositionProvider(TooltipAnchorPosition position, double spacing, double scaling) : ITooltipPositionProvider
    {
        public PixelPoint CalculatePosition(PixelRect anchorBounds, PixelSize windowSize, bool rightToLeft, PixelSize tooltipSize) =>
            TooltipPosition.Calculate(anchorBounds, windowSize, tooltipSize, (TooltipSide)position,
                (int)MathF.Floor((float)(spacing * scaling) + .5f), rightToLeft);
    }
}
