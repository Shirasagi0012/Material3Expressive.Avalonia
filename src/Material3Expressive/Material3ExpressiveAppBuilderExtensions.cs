using Avalonia;
using MaterialSymbols.Avalonia;

namespace Material3Expressive;

/// <summary>Configures runtime services required by Material 3 Expressive.</summary>
public static class Material3ExpressiveAppBuilderExtensions
{
    /// <summary>
    /// Registers the bundled Material Symbols Rounded variable font.
    /// </summary>
    /// <remarks>
    /// Call this from the application's <c>BuildAvaloniaApp</c> pipeline. The explicit call keeps
    /// the font package alive under trimming and does not rely on its module initializer being
    /// discovered at runtime. Applications still add <see cref="Styles.MaterialStyles"/> to their
    /// styles collection in XAML or code.
    /// </remarks>
    public static AppBuilder UseMaterial3Expressive(this AppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseRoundedSymbols();
    }
}
