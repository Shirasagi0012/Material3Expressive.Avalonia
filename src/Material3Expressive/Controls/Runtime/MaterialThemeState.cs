using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// The immutable theme inputs published by one <see cref="MaterialElementContext"/>.
/// </summary>
internal readonly record struct MaterialThemeState(ResolvedTheme Theme, bool IsDark)
{
    internal static MaterialThemeState Default => new(ResolvedTheme.Default, false);
}
