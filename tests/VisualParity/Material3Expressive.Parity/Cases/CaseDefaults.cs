using Avalonia.Controls;
using Avalonia.Layout;

namespace Material3Expressive.Parity;

public static class CaseDefaults
{
    public static Control Center(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        return new Grid { Children = { control } };
    }

}
