using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public static class CaseDefaults
{
    public static Control Center(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        return new Grid { Children = { control } };
    }

    public static void SetThemeColors(AvaloniaHost host, bool dark)
    {
        host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        // Same fixed Compose ColorScheme inputs on both ends. This isolates component parity from MCU.
        host.Window.Resources[Sys.Color.Primary] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
        host.Window.Resources[Sys.Color.OnPrimary] = Color.Parse(dark ? "#381E72" : "#FFFFFF");
        host.Window.Resources[Sys.Color.SecondaryContainer] = Color.Parse(dark ? "#4A4458" : "#E8DEF8");
        host.Window.Resources[Sys.Color.OnSecondaryContainer] = Color.Parse(dark ? "#E8DEF8" : "#1D192B");
        host.Window.Resources[Sys.Color.OnSurface] = Color.Parse(dark ? "#E6E0E9" : "#1D1B20");
        host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse(dark ? "#CAC4D0" : "#49454F");
        host.Window.Resources[Sys.Color.OutlineVariant] = Color.Parse(dark ? "#49454F" : "#CAC4D0");
        host.Window.Resources[Sys.Color.SurfaceContainerLow] = Color.Parse(dark ? "#1D1B20" : "#F7F2FA");
    }
}
