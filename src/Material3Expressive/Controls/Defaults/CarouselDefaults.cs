using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

internal sealed record CarouselMetrics(
    double ItemSpacing,
    double LargeItemWidth,
    double SmallItemWidth,
    double ItemHeight,
    ShapeRole ItemShape);

/// <summary>Component-owned policy for the specification-defined carousel metrics.</summary>
internal static class CarouselDefaults
{
    public static CarouselMetrics Metrics(CarouselVariant variant) => variant switch
    {
        CarouselVariant.MultiBrowse => new(8, 200, 56, 200, ShapeRole.CornerExtraLarge),
        CarouselVariant.Hero => new(8, 320, 40, 200, ShapeRole.CornerExtraLarge),
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };
}
