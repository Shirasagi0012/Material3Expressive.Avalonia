namespace Material3Expressive.Foundation.Tokens;

/// <summary>A token expression resolving to DIPs, before entering a TextStyle or Avalonia property.</summary>
public sealed record TextUnit
{
    private TextUnit(double value, MaterialTokenKey<double>? fontSize)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
        FontSize = fontSize;
    }

    public double Value { get; }
    /// <summary>The reference size for em, already resolved to DIPs. Null denotes sp.</summary>
    public MaterialTokenKey<double>? FontSize { get; }

    /// <summary>Scale-independent pixels, resolved using Sys.TypeScale.FontScale; DPI is not applied twice.</summary>
    public static TextUnit Sp(double value) => new(value, null);

    /// <summary>A multiple of the supplied font-size token, resolved in the consumer's scope.</summary>
    public static TextUnit Em(double value, MaterialTokenKey<double> fontSize) =>
        new(value, fontSize ?? throw new ArgumentNullException(nameof(fontSize)));
}
