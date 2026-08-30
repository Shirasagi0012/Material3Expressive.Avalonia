namespace Material3Expressive.Foundation;

/// <summary>
/// Installs the default application-level Material token resolution boundary.
/// </summary>
/// <remarks>
/// The boundary is a real merged resource provider. It does not publish a parallel theme
/// snapshot or inject attached properties into each <c>TopLevel</c>.
/// </remarks>
public sealed class MaterialThemeStyles : Avalonia.Styling.Styles
{
    public MaterialThemeStyles()
    {
        Resources.MergedDictionaries.Add(new MaterialTheme());
    }
}
