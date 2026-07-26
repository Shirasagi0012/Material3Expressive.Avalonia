namespace Material3Expressive.Foundation.Tokens;

/// <summary>
/// The primitive corner scale. Transcribed from androidx <c>tokens/ShapeTokens.kt</c>,
/// token-DB stamp <c>14_1_0</c>.
/// </summary>
public static class ShapeTokens
{
    public static CornerSize CornerValueNone { get; } = CornerSize.Absolute(0);
    public static CornerSize CornerValueExtraSmall { get; } = CornerSize.Absolute(4);
    public static CornerSize CornerValueSmall { get; } = CornerSize.Absolute(8);
    public static CornerSize CornerValueMedium { get; } = CornerSize.Absolute(12);
    public static CornerSize CornerValueLarge { get; } = CornerSize.Absolute(16);
    public static CornerSize CornerValueLargeIncreased { get; } = CornerSize.Absolute(20);
    public static CornerSize CornerValueExtraLarge { get; } = CornerSize.Absolute(28);
    public static CornerSize CornerValueExtraLargeIncreased { get; } = CornerSize.Absolute(32);
    public static CornerSize CornerValueExtraExtraLarge { get; } = CornerSize.Absolute(48);

    public static RoundedCornerShape CornerNone { get; } = RoundedCornerShape.Rectangle;
    public static RoundedCornerShape CornerExtraSmall { get; } = RoundedCornerShape.Uniform(CornerValueExtraSmall);
    public static RoundedCornerShape CornerSmall { get; } = RoundedCornerShape.Uniform(CornerValueSmall);
    public static RoundedCornerShape CornerMedium { get; } = RoundedCornerShape.Uniform(CornerValueMedium);
    public static RoundedCornerShape CornerLarge { get; } = RoundedCornerShape.Uniform(CornerValueLarge);

    public static RoundedCornerShape CornerLargeIncreased { get; } =
        RoundedCornerShape.Uniform(CornerValueLargeIncreased);

    public static RoundedCornerShape CornerExtraLarge { get; } = RoundedCornerShape.Uniform(CornerValueExtraLarge);

    public static RoundedCornerShape CornerExtraLargeIncreased { get; } =
        RoundedCornerShape.Uniform(CornerValueExtraLargeIncreased);

    public static RoundedCornerShape CornerExtraExtraLarge { get; } =
        RoundedCornerShape.Uniform(CornerValueExtraExtraLarge);

    public static RoundedCornerShape CornerFull { get; } = RoundedCornerShape.Circle;
}
