using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class IconButtonContentTests
{
    [AvaloniaTheory]
    [InlineData("Standard", false)] [InlineData("Filled", false)] [InlineData("Tonal", false)]
    [InlineData("Outlined", false)] [InlineData("Vibrant", false)] [InlineData("OutlinedVibrant", false)]
    [InlineData("Standard", true)] [InlineData("Filled", true)] [InlineData("Tonal", true)] [InlineData("Outlined", true)]
    public void CustomContentAndTemplateOnlyTakePriorityAndCanRestoreTheSymbol(string variant, bool toggle)
    {
        using var clock = new MotionTestClockScope();
        IconButton button = toggle ? new IconToggleButton() : new IconButton();
        button.Symbol = MaterialSymbols.MaterialSymbolCodepoint.Add;
        var window = DialogTests.Show(button);
        var clicks = 0; button.Click += (_, _) => clicks++;
        try
        {
            button.Theme = (ControlTheme)window.FindResource($"Material{variant}Icon{(toggle ? "Toggle" : "")}ButtonTheme")!;
            window.UpdateLayout();
            var symbol = button.GetVisualDescendants().OfType<MaterialSymbol>().Single();
            var before = button.Bounds.Size;
            var customSymbol = new MaterialSymbol { Symbol = MaterialSymbols.MaterialSymbolCodepoint.Favorite };
            var marker = new Border { Width = 24, Height = 16, Child = customSymbol };
            button.Content = marker; window.UpdateLayout();
            Assert.False(symbol.IsEffectivelyVisible); Assert.True(marker.IsEffectivelyVisible);
            Assert.True(customSymbol.IsEffectivelyVisible);
            Assert.Equal(new Size(24, 16), marker.Bounds.Size); Assert.Equal(before, button.Bounds.Size);
            button.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Assert.Equal(1, clicks);
            button.Content = null;
            var templateMarker = new Border { Width = 20, Height = 20 };
            button.ContentTemplate = new FuncDataTemplate<object?>((_, _) => templateMarker);
            window.UpdateLayout(); Assert.False(symbol.IsEffectivelyVisible); Assert.False(marker.IsAttachedToVisualTree());
            Assert.True(templateMarker.IsEffectivelyVisible); Assert.Equal(new Size(20, 20), templateMarker.Bounds.Size);
            button.ContentTemplate = null; window.UpdateLayout(); Assert.True(symbol.IsEffectivelyVisible);
            Assert.Equal(before, button.Bounds.Size);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
}
