using Avalonia;
using MaterialColorUtilities.Avalonia;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Themes;

public partial class MaterialTheme : AvaloniaObject
{
    public static readonly AttachedProperty<MaterialTheme> ThemeProperty =
        AvaloniaProperty.RegisterAttached<MaterialTheme, AvaloniaObject, MaterialTheme>("Theme");

    public static void SetTheme(AvaloniaObject element, MaterialTheme value)
    {
        element.SetValue(ThemeProperty, value);
    }

    public static MaterialTheme GetTheme(AvaloniaObject element)
    {
        return element.GetValue(ThemeProperty);
    }

    static MaterialTheme()
    {
        ThemeProperty.Changed.AddClassHandler<AvaloniaObject>(static (element, args) =>
        {
            if (args.NewValue is MaterialTheme theme)
            {
                element[!MaterialColor.SchemeProperty] = theme[!ColorSchemeProperty];
                element[!Typography.TypographyHostProperty] = theme[!TypographySchemeProperty];
            }
            else
            {
                element.ClearValue(MaterialColor.SchemeProperty);
                element.ClearValue(Typography.TypographyHostProperty);
            }
        });
    }

    [GeneratedStyledProperty]
    public partial SchemeProviderBase ColorScheme { get; set; }

    [GeneratedStyledProperty]
    public partial Typography TypographyScheme { get; set; }
}
