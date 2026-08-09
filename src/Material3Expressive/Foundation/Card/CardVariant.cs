namespace Material3Expressive.Foundation;

/// <summary>The three visual variants available to a Material card.</summary>
/// <remarks>
/// This is component API rather than generated defaults-table metadata. The raw card token tables
/// retain the upstream facts; <c>CardDefaults</c> owns the mapping from this axis to those tables
/// and to the active system theme.
/// </remarks>
public enum CardVariant
{
    /// <summary>A low surface lifted from its background by elevation.</summary>
    Elevated,

    /// <summary>A flat surface using the highest surface-container role.</summary>
    Filled,

    /// <summary>A plain surface behind an outline.</summary>
    Outlined
}
