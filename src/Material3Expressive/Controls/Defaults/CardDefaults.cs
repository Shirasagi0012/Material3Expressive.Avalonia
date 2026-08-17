using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// Legacy test/compatibility shape retained while existing callers move to
/// <see cref="CardValueKeys" />.
/// </summary>
[Obsolete(
    "Legacy Card defaults transport. Use CardValueKeys and MaterialValue resources.",
    DiagnosticId = "M3E1005")]
internal sealed record CardElevation(
    double Resting,
    double Hovered,
    double Focused,
    double Pressed,
    double Dragged,
    double Disabled)
{
    [Obsolete("Legacy Card defaults transport. Use CardValueKeys.", DiagnosticId = "M3E1005")]
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

/// <summary>Legacy metrics shape retained for source compatibility during migration.</summary>
[Obsolete(
    "Legacy Card defaults transport. Use CardValueKeys and MaterialValue resources.",
    DiagnosticId = "M3E1005")]
internal sealed record CardMetrics(
    ShapeRole ContainerShape,
    double OutlineWidth,
    double IconSize,
    bool HasOutline,
    CardElevation Elevation);

/// <summary>
/// Obsolete compatibility projection used only by pre-MaterialValue tests and callers.
/// MaterialCard itself no longer reads this type.
/// </summary>
[Obsolete(
    "Legacy Card defaults transport. Use CardValueKeys and MaterialValue resources.",
    DiagnosticId = "M3E1005")]
internal static class CardDefaults
{
    public static CardMetrics Metrics(CardVariant variant) =>
        variant switch
        {
            CardVariant.Elevated => new(
                ShapeRole.CornerMedium,
                OutlineWidth: 0,
                IconSize: 24,
                HasOutline: false,
                new CardElevation(1, 2, 1, 1, 4, 1)),
            CardVariant.Filled => new(
                ShapeRole.CornerMedium,
                OutlineWidth: 0,
                IconSize: 24,
                HasOutline: false,
                new CardElevation(0, 1, 0, 0, 3, 0)),
            CardVariant.Outlined => new(
                ShapeRole.CornerMedium,
                OutlineWidth: 1,
                IconSize: 24,
                HasOutline: true,
                new CardElevation(0, 1, 0, 0, 3, 0)),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };
}
