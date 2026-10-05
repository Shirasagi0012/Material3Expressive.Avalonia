using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using MaterialSymbols;
using SymbolVariation = MaterialSymbols.Avalonia.SymbolVariation;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// Smoke tests for MaterialSymbol — the MaterialSymbols.Avalonia module's control pinned to the
/// Rounded family: the Rounded font loads from the MaterialSymbols.Avalonia.Rounded assets, glyph
/// outlines land as pixels, and the measure/brush conventions hold. Pixel probes render through
/// the real Skia backend, same as MaterialSurfaceRenderTests.
/// </summary>
public class MaterialSymbolTests
{
    private static Window Show(Control content, int size = 96)
    {
        var window = new MotionTestWindow()
        {
            Width = size,
            Height = size,
            Background = Brushes.White,
            Content = content,
        };
        window.Show();
        return window;
    }

    private static (int R, int G, int B) PixelAt(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var row = new byte[buffer.RowBytes];
        Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, buffer.RowBytes);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (row[x * 4], row[x * 4 + 1], row[x * 4 + 2])
            : (row[x * 4 + 2], row[x * 4 + 1], row[x * 4]);
    }

    private static int DarkPixelCount(WriteableBitmap frame, int width, int height)
    {
        var count = 0;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var p = PixelAt(frame, x, y);
            if (p.R < 128)
                count++;
        }
        return count;
    }

    [AvaloniaFact]
    public void Unconstrained_MeasuresBaselineSize()
    {
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Home };
        symbol.Measure(Size.Infinity);
        Assert.Equal(new Size(MaterialSymbol.DefaultSize, MaterialSymbol.DefaultSize), symbol.DesiredSize);
    }

    [AvaloniaFact]
    public void ExplicitSize_WinsOverBaselineCap()
    {
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Home, Width = 48, Height = 32 };
        symbol.Measure(Size.Infinity);
        Assert.Equal(new Size(48, 32), symbol.DesiredSize);
    }

    [AvaloniaFact]
    public void SlotDefaultSize_ChangesAndReparentsWithoutWritingDimensions()
    {
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Home,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var host = new Border { Child = symbol };
        MaterialIcon.SetDefaultSize(host, 32);
        var window = Show(host);
        Assert.Equal(new Size(32, 32), symbol.Bounds.Size);
        Assert.Equal(32, symbol.ResolvedVariation.OpticalSize);
        Assert.True(double.IsNaN(symbol.Width));
        Assert.True(double.IsNaN(symbol.Height));

        MaterialIcon.SetDefaultSize(host, 18);
        window.UpdateLayout();
        Assert.Equal(new Size(18, 18), symbol.Bounds.Size);
        host.Child = null;
        var other = new Border { Child = symbol };
        MaterialIcon.SetDefaultSize(other, 40);
        window.Content = other;
        window.UpdateLayout();
        Assert.Equal(new Size(40, 40), symbol.Bounds.Size);
        window.Close();
    }

    [AvaloniaFact]
    public void SlotDefaultSize_RespectsConstraintsAndExplicitDimensions()
    {
        var symbol = new MaterialSymbol();
        MaterialIcon.SetDefaultSize(symbol, 40);
        symbol.Measure(new Size(16, 30));
        Assert.Equal(new Size(16, 16), symbol.DesiredSize);
        symbol.Width = 50;
        symbol.Measure(Size.Infinity);
        Assert.Equal(new Size(50, 40), symbol.DesiredSize);
        symbol.Height = 30;
        symbol.Measure(Size.Infinity);
        Assert.Equal(new Size(50, 30), symbol.DesiredSize);
    }

    [AvaloniaFact]
    public void StretchLayout_FillsAllocationAndAutoOpticalSizeUsesActualBounds()
    {
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Home };
        MaterialIcon.SetDefaultSize(symbol, 18);
        symbol.Measure(new Size(32, 40));
        Assert.Equal(new Size(18, 18), symbol.DesiredSize);
        symbol.Arrange(new Rect(0, 0, 32, 40));
        Assert.Equal(new Size(32, 40), symbol.Bounds.Size);
        Assert.Equal(32, symbol.ResolvedVariation.OpticalSize);
    }

    [AvaloniaFact]
    public void CompiledExtension_PreservesDefaultsAndBuildsIndependentTemplateControls()
    {
        var panel = (StackPanel)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/SymbolExtension.axaml"));
        var window = Show(panel, 160);
        var ordinary = Assert.IsType<MaterialSymbol>(((ContentControl)panel.Children[0]).Content);
        Assert.Equal(MaterialSymbolCodepoint.Home, ordinary.Symbol);
        Assert.False(ordinary.IsSet(MaterialSymbol.ForegroundProperty));
        Assert.True(double.IsNaN(ordinary.Grade));
        Assert.True(double.IsNaN(ordinary.OpticalSize));
        ordinary.Measure(Size.Infinity);
        Assert.Equal(new Size(32, 32), ordinary.DesiredSize);

        var explicitSymbol = Assert.IsType<MaterialSymbol>(((ContentControl)panel.Children[1]).Content);
        Assert.Equal(40, explicitSymbol.Width);
        Assert.Equal(40, explicitSymbol.Height);
        Assert.Equal(1, explicitSymbol.Fill);
        Assert.Equal(500, explicitSymbol.Weight);
        Assert.Equal(100, explicitSymbol.Grade);
        Assert.Equal(36, explicitSymbol.OpticalSize);
        Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(explicitSymbol.Foreground).Color);

        var template = ((ContentControl)panel.Children[2]).ContentTemplate!;
        var templateSymbol = panel.Children[2].GetVisualDescendants().OfType<MaterialSymbol>().Single();
        Assert.Equal(32, MaterialIcon.GetDefaultSize(templateSymbol));
        var first = Assert.IsType<ContentControl>(template.Build(null));
        var second = Assert.IsType<ContentControl>(template.Build(null));
        Assert.NotSame(first.Content, second.Content);
        window.Close();
    }

    [AvaloniaFact]
    public void Symbol_PaintsGlyphInk()
    {
        var symbol = new MaterialSymbol
        {
            Symbol = MaterialSymbolCodepoint.Home,
            Foreground = Brushes.Black,
            Width = 48,
            Height = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var window = Show(symbol);

        using var frame = window.CaptureRenderedFrame()!;
        var ink = DarkPixelCount(frame, 96, 96);
        Assert.True(ink > 100, $"expected the home glyph to paint ink, got {ink} dark pixels");
    }

    [AvaloniaFact]
    public void None_PaintsNothing()
    {
        var symbol = new MaterialSymbol
        {
            Foreground = Brushes.Black,
            Width = 48,
            Height = 48,
        };
        var window = Show(symbol);

        using var frame = window.CaptureRenderedFrame()!;
        Assert.Equal(0, DarkPixelCount(frame, 96, 96));
    }

    [AvaloniaFact]
    public void Foreground_InheritsFromTextContext()
    {
        var symbol = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.Home };
        var host = new Border
        {
            [TextElement.ForegroundProperty] = Brushes.Red,
            Child = symbol,
        };
        Show(host);

        Assert.Same(Brushes.Red, symbol.Foreground);
    }

    [AvaloniaFact]
    public void ResolvedVariation_ClampsAndFollowsContext()
    {
        var symbol = new MaterialSymbol
        {
            Symbol = MaterialSymbolCodepoint.Home,
            Weight = 1200, // beyond the 100–700 axis range
            Width = 30,
            Height = 30,
        };
        var window = Show(symbol);
        window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;

        var resolved = symbol.ResolvedVariation;
        Assert.Equal(SymbolVariation.MaxWeight, resolved.Weight);
        Assert.Equal(SymbolVariation.DarkGrade, resolved.Grade); // NaN grade followed the dark variant
        Assert.Equal(30, resolved.OpticalSize); // NaN opsz followed the rendered size
    }
}
