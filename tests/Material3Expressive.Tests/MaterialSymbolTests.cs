using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
