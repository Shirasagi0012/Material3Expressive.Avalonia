using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Material3Expressive.Foundation.Rendering;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class IconButtonWidthTests
{
    [AvaloniaTheory]
    [InlineData("xsmall", 12, 8)]
    [InlineData("small", 12, 8)]
    [InlineData("medium", 16, 12)]
    [InlineData("large", 28, 16)]
    [InlineData("xlarge", 28, 16)]
    public void ShapePresets_AreExplicitAndKeepUserValuesAheadOfLiveTokens(string size, double square, double pressed)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconToggleButton();
        if (size != "small") button.Classes.Add(size);
        var window = Show(button);
        try
        {
            Assert.Null(button.CheckedCornerRadius);
            Assert.Null(MaterialButton.GetPressedCornerRadius(button));
            button.Classes.Add("morph"); Flush(window);
            Assert.Equal(new CornerRadius(pressed), MaterialButton.GetPressedCornerRadius(button));
            Assert.Equal(new CornerRadius(square), button.CheckedCornerRadius);
            button.Classes.Add("square"); Flush(window);
            Assert.Equal(new CornerRadius(square), button.CornerRadius);
            Assert.Equal(new CornerRadius(1000), button.CheckedCornerRadius);
            window.Resources[Sys.Shape.Full] = new CornerRadius(77); Flush(window);
            Assert.Equal(new CornerRadius(77), button.CheckedCornerRadius);

            var style = new Style { Setters = {
                new Setter(IconButton.CornerRadiusProperty, new CornerRadius(3)),
                new Setter(MaterialButton.PressedCornerRadiusProperty, new CornerRadius(4)),
                new Setter(IconToggleButton.CheckedCornerRadiusProperty, new CornerRadius(5)) } };
            button.Styles.Add(style); button.ApplyStyling(); Flush(window);
            Assert.Equal(new CornerRadius(3), button.CornerRadius);
            Assert.Equal(new CornerRadius(4), MaterialButton.GetPressedCornerRadius(button));
            Assert.Equal(new CornerRadius(5), button.CheckedCornerRadius);
            button.CheckedCornerRadius = null; Flush(window); Assert.Null(button.CheckedCornerRadius);
            button.ClearValue(IconToggleButton.CheckedCornerRadiusProperty); Flush(window);
            Assert.Equal(new CornerRadius(5), button.CheckedCornerRadius);
            button.Styles.Remove(style); Flush(window);
            Assert.Equal(new CornerRadius(77), button.CheckedCornerRadius);
            button.Classes.Remove("morph"); Flush(window);
            Assert.Null(button.CheckedCornerRadius);
            Assert.Null(MaterialButton.GetPressedCornerRadius(button));
            Assert.Equal(new CornerRadius(square), button.CornerRadius);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("xsmall", 28, 32, 40, 32)]
    [InlineData("small", 32, 40, 52, 40)]
    [InlineData("medium", 48, 56, 72, 56)]
    [InlineData("large", 64, 96, 128, 96)]
    [InlineData("xlarge", 104, 136, 184, 136)]
    public void WidthChoices_UpdateLiveWithoutChangingHeightOrIcon(string size, double narrow, double uniform, double wide, double height)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconButton();
        if (size != "small") button.Classes.Add(size);
        var window = Show(button);
        try
        {
            var layout = button.GetVisualDescendants().OfType<ButtonLayout>().Single();
            foreach (var (option, width) in new[] { (IconButtonWidthOption.Narrow, narrow), (IconButtonWidthOption.Wide, wide), (IconButtonWidthOption.Uniform, uniform) })
            {
                button.WidthOption = option; Flush(window);
                Assert.Equal(width, layout.ContainerMinWidth);
                Assert.Equal(height, layout.ContainerMinHeight);
                Assert.Equal(new Size(width, height), layout.ContainerSize);
                Assert.Equal(size == "xsmall" ? width : Math.Max(48, width), button.Bounds.Width);
                Assert.Equal(size == "xsmall" ? height : Math.Max(48, height), button.Bounds.Height);
                Assert.Equal(0, button.MinWidth);
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void WidthChoices_ResolveLiveSpaceTokens_AndPreserveExplicitDimensions()
    {
        using var clock = new MotionTestClockScope();
        var button = new IconButton { WidthOption = IconButtonWidthOption.Wide };
        button.Classes.Add("xsmall");
        var window = Show(button);
        try
        {
            Assert.Equal(40, button.Width);
            button.Resources[Comp.IconButtonXSmall.WideLeadingSpace] = 13d;
            button.Resources[Comp.IconButtonXSmall.WideTrailingSpace] = 17d;
            Flush(window); Assert.Equal(50, button.Width);
            var style = new Style { Setters = { new Setter(IconButton.WidthProperty, 70d) } };
            button.Styles.Add(style); button.ApplyStyling(); Flush(window);
            button.WidthOption = IconButtonWidthOption.Narrow; Flush(window);
            Assert.Equal(70, button.Bounds.Width);
            button.Width = 91; Flush(window); Assert.Equal(91, button.Bounds.Width);
            button.ClearValue(IconButton.WidthProperty); Flush(window); Assert.Equal(70, button.Bounds.Width);
            button.Styles.Remove(style); Flush(window); Assert.Equal(28, button.Bounds.Width);
            button.Classes.Clear(); Flush(window);
            Assert.True(double.IsNaN(button.Width));
            Assert.Equal(48, button.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(20, 40, 20, 24)]
    [InlineData(40, 20, 24, 20)]
    [InlineData(18, 16, 18, 16)]
    public void ExplicitConstraints_ConstrainEachIconAxis(double width, double height, double iconWidth, double iconHeight)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconButton { Width = width, Height = height, Symbol = MaterialSymbols.MaterialSymbolCodepoint.Home };
        var window = Show(button);
        try
        {
            var icon = button.GetVisualDescendants().OfType<MaterialSymbol>().Single();
            Assert.Equal(new Size(iconWidth, iconHeight), icon.Bounds.Size);
            Assert.Equal(new Size(width, height), button.GetVisualDescendants().OfType<MaterialSurface>().Single().Bounds.Size);
        }
        finally { window.Close(); }
    }

    private static Window Show(IconButton button)
    {
        var window = new MotionTestWindow { Width = 600, Height = 300 };
        window.Styles.Add(new MaterialTheme());
        window.Content = button;
        window.Show(); Flush(window); return window;
    }

    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}
