using Avalonia;
using Avalonia.Animation;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using MaterialSymbols;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// The compiled-AXAML path of the IconButton slice: Styles/IconButton.axaml loads, the implicit
/// {x:Type m3:IconButton} theme applies the standard defaults, the named variant themes reproduce
/// their baseline colors through BasedOn, and a size class re-points the metric slots live.
/// Interaction states (pointer/keyboard) are verified manually for now.
/// </summary>
public class IconButtonThemeTests
{
    private static MaterialSurface Container(IconButton button) =>
        button.GetVisualDescendants().OfType<MaterialSurface>().First(b => b.Name == "PART_Container");

    private static MaterialSymbol Symbol(IconButton button) =>
        button.GetVisualDescendants().OfType<MaterialSymbol>().First();

    private static Color BrushColor(IBrush? brush) => Assert.IsType<ImmutableSolidColorBrush>(brush).Color;

    [AvaloniaFact]
    public void ImplicitTheme_AppliesStandardDefaults()
    {
        IconButtonThemeFixture.ShowWindow(out var button);

        // Standard: transparent container, on-surface-variant content (the token-table color —
        // the plain upstream composable's LocalContentColor default is a recorded deviation).
        Assert.Equal(Colors.Transparent, BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSurfaceVariant),
            BrushColor(button.Foreground));

        // Small is the default size; the default width option is square (MinWidth = MinHeight).
        Assert.Equal(40d, button.MinHeight);
        Assert.Equal(40d, button.MinWidth);

        var container = Container(button);
        Assert.Equal(new CornerRadius(1000), container.CornerRadius); // CornerFull
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSurfaceVariant),
            BrushColor(container.StateLayerBrush));

        // The icon part renders the owner's symbol at the size table's icon size, inheriting the
        // content color through TextElement.Foreground.
        var symbol = Symbol(button);
        Assert.Equal(MaterialSymbolCodepoint.Home, symbol.Symbol);
        Assert.Equal(24d, symbol.Width);
        Assert.Equal(24d, symbol.Height);
        Assert.Equal(button.Foreground, symbol.Foreground);
    }

    [AvaloniaFact]
    public void FilledTheme_AppliesPrimaryContainer()
    {
        IconButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialFilledIconButtonTheme");

        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnPrimary),
            BrushColor(button.Foreground));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnPrimary),
            BrushColor(Container(button).StateLayerBrush));
    }

    [AvaloniaFact]
    public void TonalTheme_AppliesSecondaryContainer()
    {
        IconButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialTonalIconButtonTheme");

        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.SecondaryContainer),
            BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSecondaryContainer),
            BrushColor(button.Foreground));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSecondaryContainer),
            BrushColor(Container(button).StateLayerBrush));
    }

    [AvaloniaFact]
    public void OutlinedTheme_AppliesTransparentContainerAndOutline()
    {
        IconButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialOutlinedIconButtonTheme");

        Assert.Equal(Colors.Transparent, BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnSurfaceVariant),
            BrushColor(button.Foreground));
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OutlineVariant),
            BrushColor(button.BorderBrush));
        Assert.Equal(1d, button.BorderThickness.Left);
    }

    [AvaloniaFact]
    public void OutlinedTheme_SizeClass_ThickensOutline()
    {
        IconButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialOutlinedIconButtonTheme", sizeClass: "xlarge");
        Assert.Equal(3d, button.BorderThickness.Left);
        Assert.Equal(136d, button.MinHeight);
    }

    [AvaloniaFact]
    public void DisabledState_AppliesDisabledColors()
    {
        IconButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialFilledIconButtonTheme");
        button.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();

        // The icon button tables disable the container at 0.10 over on-surface and the content
        // at 0.38 — not the sys 0.12 the Button family uses.
        var onSurface = ButtonThemeFixture.SchemeColor(false, static s => s.OnSurface);
        var disabledContainer = BrushColor(button.Background);
        Assert.Equal((byte)Math.Round(0.10 * onSurface.A), disabledContainer.A);
        Assert.Equal(onSurface.R, disabledContainer.R);

        var disabledContent = BrushColor(button.Foreground);
        Assert.Equal((byte)Math.Round(0.38 * onSurface.A), disabledContent.A);
        Assert.Equal(onSurface.R, disabledContent.R);
    }

    [AvaloniaFact]
    public void SizeClass_RePointsMetrics_AndReverts()
    {
        IconButtonThemeFixture.ShowWindow(out var button);
        Assert.Equal(40d, button.MinHeight);
        Assert.Equal(24d, Symbol(button).Width);

        button.Classes.Add("large");

        Assert.Equal(96d, button.MinHeight);
        Assert.Equal(96d, button.MinWidth);
        Assert.Equal(new CornerRadius(1000), Container(button).CornerRadius);
        Assert.Equal(32d, Symbol(button).Width);
        Assert.Equal(32d, Symbol(button).Height);

        button.Classes.Remove("large");
        Assert.Equal(40d, button.MinHeight);
        Assert.Equal(24d, Symbol(button).Width);
    }

    [AvaloniaFact]
    public void ImplicitTheme_ThemeVariantSwitch_ReResolves()
    {
        var window = IconButtonThemeFixture.ShowWindow(out var button, themeKey: "MaterialFilledIconButtonTheme");
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            BrushColor(button.Background));

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(ButtonThemeFixture.SchemeColor(true, static s => s.Primary),
            BrushColor(button.Background));
        Assert.Equal(ButtonThemeFixture.SchemeColor(true, static s => s.OnPrimary),
            BrushColor(Container(button).StateLayerBrush));
    }

    [AvaloniaFact]
    public void ImplicitTheme_OptsIntoMaterialInteraction()
    {
        // Icon buttons are flat (no elevation tokens upstream), so the container carries no
        // styled transitions at all. Feedback comes from MaterialInteraction: the standard theme
        // opts the button in, and MaterialSurface's controller paints ripples and timed
        // state-layer fades directly.
        IconButtonThemeFixture.ShowWindow(out var button);
        var container = Container(button);

        Assert.True(MaterialInteraction.GetIsInteractive(button));
        Assert.Null(container.Transitions);
    }
}
