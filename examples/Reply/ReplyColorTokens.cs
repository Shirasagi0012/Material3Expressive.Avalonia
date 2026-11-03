// Adapted from Android Open Source Project Reply ui/theme/Color.kt (Apache-2.0).
using System.Collections.Frozen;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;

namespace Reply;

public sealed class ReplyColorTokens : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Light =
        new Dictionary<MaterialTokenKey, object>
        {
            [MaterialTokens.Sys.Color.Primary] = Color.Parse("#805610"),
            [MaterialTokens.Sys.Color.OnPrimary] = Color.Parse("#FFFFFF"),
            [MaterialTokens.Sys.Color.PrimaryContainer] = Color.Parse("#FFDDB3"),
            [MaterialTokens.Sys.Color.OnPrimaryContainer] = Color.Parse("#291800"),
            [MaterialTokens.Sys.Color.Secondary] = Color.Parse("#6F5B40"),
            [MaterialTokens.Sys.Color.OnSecondary] = Color.Parse("#FFFFFF"),
            [MaterialTokens.Sys.Color.SecondaryContainer] = Color.Parse("#FBDEBC"),
            [MaterialTokens.Sys.Color.OnSecondaryContainer] = Color.Parse("#271904"),
            [MaterialTokens.Sys.Color.Tertiary] = Color.Parse("#51643F"),
            [MaterialTokens.Sys.Color.OnTertiary] = Color.Parse("#FFFFFF"),
            [MaterialTokens.Sys.Color.TertiaryContainer] = Color.Parse("#D4EABB"),
            [MaterialTokens.Sys.Color.OnTertiaryContainer] = Color.Parse("#102004"),
            [MaterialTokens.Sys.Color.Error] = Color.Parse("#BA1A1A"),
            [MaterialTokens.Sys.Color.OnError] = Color.Parse("#FFFFFF"),
            [MaterialTokens.Sys.Color.ErrorContainer] = Color.Parse("#FFDAD6"),
            [MaterialTokens.Sys.Color.OnErrorContainer] = Color.Parse("#410002"),
            [MaterialTokens.Sys.Color.Background] = Color.Parse("#FFF8F4"),
            [MaterialTokens.Sys.Color.OnBackground] = Color.Parse("#201B13"),
            [MaterialTokens.Sys.Color.Surface] = Color.Parse("#FFF8F4"),
            [MaterialTokens.Sys.Color.OnSurface] = Color.Parse("#201B13"),
            [MaterialTokens.Sys.Color.SurfaceVariant] = Color.Parse("#F0E0CF"),
            [MaterialTokens.Sys.Color.OnSurfaceVariant] = Color.Parse("#4F4539"),
            [MaterialTokens.Sys.Color.Outline] = Color.Parse("#817567"),
            [MaterialTokens.Sys.Color.OutlineVariant] = Color.Parse("#D3C4B4"),
            [MaterialTokens.Sys.Color.Scrim] = Color.Parse("#000000"),
            [MaterialTokens.Sys.Color.InverseSurface] = Color.Parse("#362F27"),
            [MaterialTokens.Sys.Color.InverseOnSurface] = Color.Parse("#FCEFE2"),
            [MaterialTokens.Sys.Color.InversePrimary] = Color.Parse("#F4BD6F"),
            [MaterialTokens.Sys.Color.SurfaceDim] = Color.Parse("#E4D8CC"),
            [MaterialTokens.Sys.Color.SurfaceBright] = Color.Parse("#FFF8F4"),
            [MaterialTokens.Sys.Color.SurfaceContainerLowest] = Color.Parse("#FFFFFF"),
            [MaterialTokens.Sys.Color.SurfaceContainerLow] = Color.Parse("#FFF1E5"),
            [MaterialTokens.Sys.Color.SurfaceContainer] = Color.Parse("#F9ECDF"),
            [MaterialTokens.Sys.Color.SurfaceContainerHigh] = Color.Parse("#F3E6DA"),
            [MaterialTokens.Sys.Color.SurfaceContainerHighest] = Color.Parse("#EDE0D4"),
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<MaterialTokenKey, object> Dark =
        new Dictionary<MaterialTokenKey, object>
        {
            [MaterialTokens.Sys.Color.Primary] = Color.Parse("#F4BD6F"),
            [MaterialTokens.Sys.Color.OnPrimary] = Color.Parse("#452B00"),
            [MaterialTokens.Sys.Color.PrimaryContainer] = Color.Parse("#633F00"),
            [MaterialTokens.Sys.Color.OnPrimaryContainer] = Color.Parse("#FFDDB3"),
            [MaterialTokens.Sys.Color.Secondary] = Color.Parse("#DDC2A1"),
            [MaterialTokens.Sys.Color.OnSecondary] = Color.Parse("#3E2D16"),
            [MaterialTokens.Sys.Color.SecondaryContainer] = Color.Parse("#56442A"),
            [MaterialTokens.Sys.Color.OnSecondaryContainer] = Color.Parse("#FBDEBC"),
            [MaterialTokens.Sys.Color.Tertiary] = Color.Parse("#B8CEA1"),
            [MaterialTokens.Sys.Color.OnTertiary] = Color.Parse("#243515"),
            [MaterialTokens.Sys.Color.TertiaryContainer] = Color.Parse("#3A4C2A"),
            [MaterialTokens.Sys.Color.OnTertiaryContainer] = Color.Parse("#D4EABB"),
            [MaterialTokens.Sys.Color.Error] = Color.Parse("#FFB4AB"),
            [MaterialTokens.Sys.Color.OnError] = Color.Parse("#690005"),
            [MaterialTokens.Sys.Color.ErrorContainer] = Color.Parse("#93000A"),
            [MaterialTokens.Sys.Color.OnErrorContainer] = Color.Parse("#FFDAD6"),
            [MaterialTokens.Sys.Color.Background] = Color.Parse("#18120B"),
            [MaterialTokens.Sys.Color.OnBackground] = Color.Parse("#EDE0D4"),
            [MaterialTokens.Sys.Color.Surface] = Color.Parse("#18120B"),
            [MaterialTokens.Sys.Color.OnSurface] = Color.Parse("#EDE0D4"),
            [MaterialTokens.Sys.Color.SurfaceVariant] = Color.Parse("#4F4539"),
            [MaterialTokens.Sys.Color.OnSurfaceVariant] = Color.Parse("#D3C4B4"),
            [MaterialTokens.Sys.Color.Outline] = Color.Parse("#9C8F80"),
            [MaterialTokens.Sys.Color.OutlineVariant] = Color.Parse("#4F4539"),
            [MaterialTokens.Sys.Color.Scrim] = Color.Parse("#000000"),
            [MaterialTokens.Sys.Color.InverseSurface] = Color.Parse("#EDE0D4"),
            [MaterialTokens.Sys.Color.InverseOnSurface] = Color.Parse("#362F27"),
            [MaterialTokens.Sys.Color.InversePrimary] = Color.Parse("#805610"),
            [MaterialTokens.Sys.Color.SurfaceDim] = Color.Parse("#18120B"),
            [MaterialTokens.Sys.Color.SurfaceBright] = Color.Parse("#3F3830"),
            [MaterialTokens.Sys.Color.SurfaceContainerLowest] = Color.Parse("#120D07"),
            [MaterialTokens.Sys.Color.SurfaceContainerLow] = Color.Parse("#201B13"),
            [MaterialTokens.Sys.Color.SurfaceContainer] = Color.Parse("#251F17"),
            [MaterialTokens.Sys.Color.SurfaceContainerHigh] = Color.Parse("#2F2921"),
            [MaterialTokens.Sys.Color.SurfaceContainerHighest] = Color.Parse("#3B342B"),
        }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        (theme == ThemeVariant.Dark ? Dark : Light).TryGetValue(key, out value!);

    public event EventHandler? Changed { add { } remove { } }
}
