using Avalonia;
using MaterialSymbols.Avalonia;

namespace Material3Expressive.Foundation.Rendering;

/// <summary>
/// The Material 3 Expressive symbol control: the MaterialSymbols.Avalonia module's
/// <see cref="MaterialSymbols.Avalonia.MaterialSymbol"/>, defaulting to the Rounded family —
/// Material 3 Expressive's first-party icon style.
/// </summary>
/// <remarks>
/// The control, the variable-font render stack and the axis-resolution behaviour all live in the
/// MaterialSymbols.Avalonia module; this subclass uses the slot's <see cref="MaterialSymbol.DefaultSizeProperty"/>
/// for measurement and defaults to <see cref="IconStyle.Rounded"/>. The Rounded family still has to be registered — call
/// <c>UseRoundedSymbols()</c> on the <c>AppBuilder</c> during application setup.
/// </remarks>
public class MaterialSymbol : MaterialSymbols.Avalonia.MaterialSymbol
{
    /// <summary>Inheritable size hint supplied by an icon slot to symbols without explicit dimensions.</summary>
    public static readonly AttachedProperty<double> DefaultSizeProperty =
        AvaloniaProperty.RegisterAttached<MaterialSymbol, StyledElement, double>(
            "DefaultSize", 24d, inherits: true,
            validate: value => double.IsFinite(value) && value >= 0);

    static MaterialSymbol()
    {
        StyleProperty.OverrideMetadata<MaterialSymbol>(
            new StyledPropertyMetadata<IconStyle>(IconStyle.Rounded));
        AffectsMeasure<MaterialSymbol>(DefaultSizeProperty);
    }

    public static double GetDefaultSize(StyledElement element) => element.GetValue(DefaultSizeProperty);
    public static void SetDefaultSize(StyledElement element, double value) => element.SetValue(DefaultSizeProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var side = Math.Min(Math.Min(availableSize.Width, availableSize.Height), GetDefaultSize(this));
        return new Size(side, side);
    }
}
