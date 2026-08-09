namespace Material3Expressive.Foundation;

/// <summary>The container-colour variants available to a Material floating action button.</summary>
/// <remarks>
/// This component API is intentionally hand-authored. <c>FabDefaults</c> maps the selection to
/// raw upstream token tables and to the system roles used by the spec-only tertiary and surface
/// variants.
/// </remarks>
public enum FabVariant
{
    /// <summary>The primary-container FAB.</summary>
    Primary,

    /// <summary>The secondary-container FAB.</summary>
    Secondary,

    /// <summary>The tertiary-container FAB.</summary>
    Tertiary,

    /// <summary>A neutral FAB for a surface where a coloured one would compete.</summary>
    Surface
}

/// <summary>The three standard Material floating action button sizes.</summary>
/// <remarks>
/// Raw token selection and the expressive extended-FAB layout policy belong to
/// <c>FabDefaults</c>, not to a generated defaults record.
/// </remarks>
public enum FabSize
{
    /// <summary>The 56dp baseline FAB.</summary>
    Small,

    /// <summary>The 80dp FAB.</summary>
    Medium,

    /// <summary>The 96dp FAB.</summary>
    Large
}
