using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.VisualTesting.Cases;

internal static class CaseLayout
{
    public static StackPanel Horizontal(params Control[] children)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.AddRange(children);
        return panel;
    }

    public static TextBlock Label(
        string text,
        VisualCaseContext context,
        double size = 12,
        FontWeight? weight = null)
    {
        return new TextBlock
        {
            Text = text,
            FontFamily = context.Roboto,
            FontSize = size,
            FontWeight = weight ?? FontWeight.Regular,
            Foreground = Brush(context, SysColorToken.OnSurfaceVariant)
        };
    }

    public static IBrush Brush(VisualCaseContext context, SysColorToken role)
    {
        var dynamicScheme = new TonalSpotScheme(context.Theme.SeedColor)
        {
            SpecVersion = context.Theme.SpecVersion,
            Platform = context.Theme.Platform
        };
        var scheme = new ResolvedColorScheme(dynamicScheme);
        return new SolidColorBrush(scheme.GetColor(role, context.Theme.IsDark));
    }
}
