using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// The immutable theme inputs published by one <see cref="MaterialElementContext"/>.
/// </summary>
[Obsolete(
    "Legacy whole-theme snapshot state. Consume typed MaterialValueKey<T> resources.",
    DiagnosticId = "M3E1004")]
internal readonly record struct MaterialThemeState(ResolvedTheme Theme, bool IsDark)
{
    internal static MaterialThemeState Default => new(ResolvedTheme.Default, false);
}
