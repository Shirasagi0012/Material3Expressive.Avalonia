using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests.TestUtils;

internal static class ThemeTestHelper
{
    public static MaterialTheme ThemeSeededWith(Color seed)
    {
        return new MaterialTheme { ColorScheme = new TonalSpotScheme(seed) };
    }

    /// <summary>
    /// Sets the application-level theme and restores the previous one on dispose. The application
    /// instance is shared by every test in the assembly, so leaving a theme on it would leak.
    /// </summary>
    public static IDisposable AppTheme(MaterialTheme theme)
    {
        var application = Application.Current!;
        var previous = MaterialThemeScope.GetTheme(application);
        MaterialThemeScope.SetTheme(application, theme);

        return new Restore(() => MaterialThemeScope.SetTheme(application, previous));
    }

    public static Color ColorOf(IBrush? brush)
    {
        return Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
    }

    /// <summary>The colour a role resolves to under a theme, computed independently of the tree.</summary>
    public static Color Expected(MaterialTheme theme, SysColorToken role, bool isDark = false)
    {
        return new ResolvedColorScheme(theme.ColorScheme!).GetColor(role, isDark);
    }

    /// <summary>Shows a window and returns it; headless windows need showing to be styled.</summary>
    public static Window ShowWindow(object content)
    {
        var window = new Window { Content = content };
        window.Show();
        return window;
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose()
        {
            restore();
        }
    }
}

/// <summary>
/// A control that reads its tokens in code, standing in for the real components until Phase 4.
/// </summary>
internal sealed class ThemeProbe : MaterialControl
{
    public ResolvedTheme? ObservedTheme { get; private set; }

    public bool ObservedIsDark { get; private set; }

    public int ChangeCount { get; private set; }

    public IImmutableBrush? ContainerBrush { get; private set; }

    protected override void OnThemeChanged(ResolvedTheme theme, bool isDark)
    {
        ObservedTheme = theme;
        ObservedIsDark = isDark;
        ContainerBrush = theme.GetButtonColors(ButtonVariant.Filled, isDark).ContainerColor;
        ChangeCount++;
    }
}
