using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// The compiled-AXAML path of the Button slice: Components/Button/Button.axaml loads, the implicit
/// {x:Type Button} theme applies the filled defaults (metrics now resolved from the shared
/// small-size table), and the {m3:Token} setter bindings re-resolve on a ThemeVariant switch.
/// Interaction states (pointer/keyboard) are verified manually for now.
/// </summary>
public class ButtonThemeTests
{
    [AvaloniaFact]
    public void ImplicitTheme_AppliesFilledDefaults()
    {
        ButtonThemeFixture.ShowWindow(out var button);

        // Container/content colors resolve through the comp slots into sys.color and are
        // converted to brushes per-consumer (§5.7 item 4).
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            Assert.IsType<ImmutableSolidColorBrush>(button.Background).Color);
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnPrimary),
            Assert.IsType<ImmutableSolidColorBrush>(button.Foreground).Color);
        Assert.Equal(40d, button.MinHeight);
        Assert.Equal(new Thickness(16, 0, 16, 0), button.Padding);
        Assert.Equal(14d, button.FontSize);
        Assert.Equal(FontWeight.Medium, button.FontWeight);

        var container = button.GetVisualDescendants().OfType<MaterialSurface>().First(b => b.Name == "PART_Container");
        Assert.Equal(new CornerRadius(1000), container.CornerRadius);

        // §7.4: the state layer is painted by MaterialSurface itself. At rest the controller's
        // animated opacity is zero; the surface only carries the aliased content color.
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.OnPrimary),
            Assert.IsType<ImmutableSolidColorBrush>(container.StateLayerBrush).Color);

        var presenter = button.GetVisualDescendants().OfType<ContentPresenter>().First();
        Assert.Equal(0.1d, presenter.LetterSpacing);
        Assert.Equal(20d, presenter.LineHeight);
    }

    [AvaloniaFact]
    public void ImplicitTheme_ThemeVariantSwitch_ReResolves()
    {
        var window = ButtonThemeFixture.ShowWindow(out var button);
        Assert.Equal(ButtonThemeFixture.SchemeColor(false, static s => s.Primary),
            Assert.IsType<ImmutableSolidColorBrush>(button.Background).Color);

        window.RequestedThemeVariant = ThemeVariant.Dark;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(ButtonThemeFixture.SchemeColor(true, static s => s.Primary),
            Assert.IsType<ImmutableSolidColorBrush>(button.Background).Color);
    }

    [AvaloniaFact]
    public void ImplicitTheme_UsesComposeInteractionMotion()
    {
        ButtonThemeFixture.ShowWindow(out var button);
        var container = button.GetVisualDescendants().OfType<MaterialSurface>().First(b => b.Name == "PART_Container");

        Assert.True(MaterialInteraction.GetIsInteractive(button));

        Assert.Null(container.Transitions);
    }
}
