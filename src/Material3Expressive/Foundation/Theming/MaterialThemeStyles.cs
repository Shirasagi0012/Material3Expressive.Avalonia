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
        // MaterialStyles must remain usable without requiring every application to repeat a
        // root MaterialTheme declaration. Keeping the baseline in this Styles resource scope
        // gives it a real ResourceProvider placement while leaving application resources and
        // subtree placements closer to the requester (and therefore higher priority).
        Resources.MergedDictionaries.Add(new MaterialTheme());

        if (Avalonia.Application.Current is null)
        {
            // Previewer/designer surfaces can construct styles before an Application exists.
            // Styles offer no later hook for the legacy TopLevel snapshot injection. The new
            // resource path remains available once this Styles instance receives an owner.
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                this,
                "MaterialThemeStyles was constructed before Application.Current was assigned; " +
                "legacy TopLevel theme snapshot injection was not installed.");
        }

        Add(MaterialThemeScope.CreateTopLevelInjectionStyle());
    }
}
