namespace Material3Expressive.Foundation;

/// <summary>The five colour variants available to an ordinary Material button.</summary>
/// <remarks>
/// This is component API, not a generated token-set enum. The raw token tables describe the
/// upstream facts; <c>ButtonDefaults</c> owns the mapping from this axis to those facts.
/// </remarks>
public enum ButtonVariant
{
    /// <summary>High emphasis: a primary-coloured container.</summary>
    Filled,

    /// <summary>Medium emphasis: a secondary-container fill.</summary>
    Tonal,

    /// <summary>A low surface fill separated from the background by elevation.</summary>
    Elevated,

    /// <summary>A transparent container behind an outline.</summary>
    Outlined,

    /// <summary>Lowest emphasis: label and state layer only.</summary>
    Text
}

/// <summary>The five expressive size presets available to Material buttons.</summary>
/// <remarks>
/// This is component API, not a generated defaults-table enum. <c>ButtonDefaults</c> owns the
/// size-to-raw-token mapping as well as the Compose corrections and Avalonia adaptations that
/// turn one size selection into the metrics consumed by a control.
/// </remarks>
public enum ButtonSize
{
    /// <summary>A compact 32dp button.</summary>
    ExtraSmall,

    /// <summary>The default 40dp button.</summary>
    Small,

    /// <summary>A 56dp button.</summary>
    Medium,

    /// <summary>A 96dp button.</summary>
    Large,

    /// <summary>A 136dp button.</summary>
    ExtraLarge
}
