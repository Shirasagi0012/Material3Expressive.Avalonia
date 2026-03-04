using Avalonia;
using MaterialColorUtilities.Avalonia;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

public partial class MaterialTheme : AvaloniaObject
{
    [GeneratedStyledProperty]
    public partial ColorScheme ColorScheme { get; set; }

    [GeneratedStyledProperty]
    public partial TypographyScheme TypographyScheme { get; set; }
}
