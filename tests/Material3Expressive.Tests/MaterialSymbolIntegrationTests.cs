using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using MaterialSymbols;
using MaterialSymbols.Avalonia;
using Xunit;

namespace Material3Expressive.Tests;

public class MaterialSymbolIntegrationTests
{
    [AvaloniaFact]
    public void AnIconButton_ArrangesItsSymbolAtTheResolvedTokenSize()
    {
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Edit };
        var button = new Button
        {
            Theme = Theme("MaterialStandardIconButtonTheme"),
            Content = symbol,
        };
        M3IconButton.SetSize(button, IconButtonSize.Large);

        var window = ThemeTestHelper.ShowWindow(button);

        var iconSize = M3IconButton.GetIconSize(button);
        Assert.Equal(iconSize, symbol.Bounds.Width, 3);
        Assert.Equal(iconSize, symbol.Bounds.Height, 3);
        Assert.Equal(iconSize, symbol.ResolvedVariation.OpticalSize, 3);
        Assert.Equal(IconStyle.Rounded, symbol.Style);
        window.Close();
    }

    [AvaloniaFact]
    public void ACheckedToggle_FillsItsVariableSymbol()
    {
        var symbol = new MaterialSymbol
        {
            Symbol = MaterialSymbolCodepoint.Favorite,
            // Disable the visual transition so this test checks the selected style value rather
            // than depending on the process-wide animation clock.
            Transitions = new Transitions(),
        };
        var button = new ToggleButton
        {
            Theme = Theme("MaterialStandardToggleIconButtonTheme"),
            Content = symbol,
            IsChecked = true,
        };

        var window = ThemeTestHelper.ShowWindow(button);

        Assert.Equal(1, symbol.Fill);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("navigation")]
    [InlineData("tab")]
    [InlineData("chip")]
    public void SelectionAwareIconSlots_FillTheirVariableSymbol(string component)
    {
        var symbol = new MaterialSymbol
        {
            Symbol = MaterialSymbolCodepoint.Star,
            Transitions = new Transitions(),
        };

        Avalonia.Controls.Control control = component switch
        {
            "navigation" => new MaterialNavigationItem
            {
                Icon = symbol,
                Content = "Home",
                IsChecked = true,
            },
            "tab" => new MaterialTab
            {
                Icon = symbol,
                Content = "Overview",
                IsChecked = true,
            },
            "chip" => new MaterialChip
            {
                LeadingIcon = symbol,
                Content = "Selected",
                IsChecked = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(component)),
        };

        var window = ThemeTestHelper.ShowWindow(control);

        Assert.Equal(1, symbol.Fill);
        window.Close();
    }

    private static ControlTheme Theme(string key)
    {
        Assert.True(Application.Current!.TryFindResource(key, out var resource));
        return Assert.IsType<ControlTheme>(resource);
    }

}
