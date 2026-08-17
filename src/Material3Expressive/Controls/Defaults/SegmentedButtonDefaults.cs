using System;

namespace Material3Expressive.Controls;

/// <summary>
/// Obsolete name retained as a migration marker for the former requester-local token resolver.
/// </summary>
/// <remarks>
/// Segmented-button production code now consumes <see cref="SegmentedButtonValueKeys"/> through
/// the MaterialValue resource pipeline. This type intentionally has no implementation and can be
/// removed once downstream code has completed its migration.
/// </remarks>
[Obsolete("Use SegmentedButtonValueKeys and typed MaterialValue endpoints instead.")]
internal static class SegmentedButtonDefaults
{
}
