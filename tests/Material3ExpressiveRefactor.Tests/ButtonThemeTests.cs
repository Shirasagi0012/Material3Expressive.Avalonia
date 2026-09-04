using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3ExpressiveRefactor.Foundation.Interaction;
using Material3ExpressiveRefactor.Foundation.Rendering;
using Xunit;

namespace Material3ExpressiveRefactor.Tests;

/// <summary>
/// The compiled-AXAML path of the Button slice: Styles/Button.axaml loads, the implicit
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

        // §7.4: the state layer is painted by MaterialSurface itself; at rest the tint is the
        // (aliased) content color with zero opacity.
        Assert.Equal(0d, container.StateLayerOpacity);
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
    public void ImplicitTheme_AnimatesElevationChanges()
    {
        // Upstream Elevation.kt tweens elevation between states (120ms incoming, FastOutSlowIn);
        // that transition rides the filled theme's part style, inherited by every BasedOn variant.
        // The state layer is NOT a styled transition anymore: the theme opts the button into
        // MaterialInteraction, and MaterialSurface's MaterialInteractionController paints the
        // fade (15/45/150ms per state) itself.
        ButtonThemeFixture.ShowWindow(out var button);
        var container = button.GetVisualDescendants().OfType<MaterialSurface>().First(b => b.Name == "PART_Container");

        Assert.True(MaterialInteraction.GetIsInteractive(button));

        var transitions = Assert.IsAssignableFrom<Transitions>(container.Transitions);
        var elevation = transitions.OfType<DoubleTransition>()
            .SingleOrDefault(t => t.Property == MaterialSurface.ElevationProperty);
        Assert.NotNull(elevation);
        Assert.Equal(TimeSpan.FromMilliseconds(120), elevation.Duration);

        Assert.DoesNotContain(transitions.OfType<DoubleTransition>(),
            t => t.Property == MaterialSurface.StateLayerOpacityProperty);
    }
}
