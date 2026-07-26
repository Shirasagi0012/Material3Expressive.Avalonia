using Avalonia;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Foundation;

public class MaterialThemeScope : AvaloniaObject
{
    public static readonly AttachedProperty<MaterialTheme> ThemeProperty =
        AvaloniaProperty.RegisterAttached<MaterialThemeScope, AvaloniaObject, MaterialTheme>("Theme");

    static MaterialThemeScope()
    {
        ThemeProperty.Changed.AddClassHandler<AvaloniaObject>(static (element, args) =>
        {
            if (args.NewValue is MaterialTheme theme)
            {
                element[!MaterialColor.SchemeProperty] = theme[!MaterialTheme.ColorSchemeProperty];
                element[!TypographyScheme.TypographyHostProperty] = theme[!MaterialTheme.TypographySchemeProperty];
                element[!ButtonTokens.ButtonHostProperty] = theme[!MaterialTheme.ButtonTokensProperty];
            }
            else
            {
                element.ClearValue(MaterialColor.SchemeProperty);
                element.ClearValue(TypographyScheme.TypographyHostProperty);
                element.ClearValue(ButtonTokens.ButtonHostProperty);
            }
        });
    }

    public static void SetTheme(AvaloniaObject element, MaterialTheme value)
    {
        element.SetValue(ThemeProperty, value);
    }

    public static MaterialTheme GetTheme(AvaloniaObject element)
    {
        return element.GetValue(ThemeProperty);
    }
}
