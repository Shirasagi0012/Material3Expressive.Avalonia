using Avalonia.Logging;

namespace Material3Expressive.Foundation;

/// <summary>
/// Delivers the application-level theme to every <c>TopLevel</c>.
/// </summary>
/// <remarks>
/// <see cref="Avalonia.Application" /> is not part of a visual or logical tree, so a theme
/// declared on it cannot inherit anywhere by itself. Application styles do reach each
/// <c>TopLevel</c> (a top level's styling parent is the application), so one style with a
/// <c>TopLevel</c> selector is enough to seed every window, popup root and embedded root.
/// </remarks>
public sealed class MaterialThemeStyles : Avalonia.Styling.Styles
{
    public MaterialThemeStyles()
    {
        if (Avalonia.Application.Current is { } application)
        {
            MaterialThemeScope.InstallBaselineResources(application);
        }
        else
        {
            // Previewer/designer surfaces can construct styles before an Application exists.
            // Styles offer no later hook, so surface the skipped baseline instead of failing
            // silently — tokens would otherwise resolve to their Transparent fallback.
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                this,
                "MaterialThemeStyles was constructed before Application.Current was assigned; " +
                "baseline token resources were not installed.");
        }

        Add(MaterialThemeScope.CreateTopLevelInjectionStyle());
    }
}
