using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Metadata;
using MaterialSymbols;
using MaterialSymbols.Avalonia;

namespace Material3Expressive.Foundation.Rendering;

/// <summary>Creates a new symbol control, for example <c>{m3:Symbol Home, Size=32}</c>.</summary>
public sealed class SymbolExtension : MarkupExtension
{
    public SymbolExtension() { }
    public SymbolExtension(MaterialSymbolCodepoint symbol) => Symbol = symbol;

    [ConstructorArgument("symbol")]
    public MaterialSymbolCodepoint Symbol { get; set; }
    public double? Size { get; set; }
    public double? Weight { get; set; }
    public double? Fill { get; set; }
    public double? Grade { get; set; }
    public double? OpticalSize { get; set; }
    public IconStyle? Style { get; set; }
    public IBrush? Foreground { get; set; }

    public override MaterialSymbol ProvideValue(IServiceProvider serviceProvider)
    {
        var symbol = new MaterialSymbol { Symbol = Symbol };
        if (Size is { } size)
            symbol.Width = symbol.Height = size;
        if (Weight is { } weight)
            symbol.Weight = weight;
        if (Fill is { } fill)
            symbol.Fill = fill;
        if (Grade is { } grade)
            symbol.Grade = grade;
        if (OpticalSize is { } opticalSize)
            symbol.OpticalSize = opticalSize;
        if (Style is { } style)
            symbol.Style = style;
        if (Foreground is { } foreground)
            symbol.Foreground = foreground;
        return symbol;
    }
}
