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
/// MaterialSymbols.Avalonia module; this subclass only changes the <see cref="Style"/> default to
/// <see cref="IconStyle.Rounded"/>. The Rounded family still has to be registered — call
/// <c>UseRoundedSymbols()</c> on the <c>AppBuilder</c> during application setup.
/// </remarks>
public class MaterialSymbol : MaterialSymbols.Avalonia.MaterialSymbol
{
    static MaterialSymbol() =>
        StyleProperty.OverrideMetadata<MaterialSymbol>(
            new StyledPropertyMetadata<IconStyle>(IconStyle.Rounded));
}
