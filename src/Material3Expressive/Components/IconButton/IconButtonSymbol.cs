using Avalonia;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// IconButton's normal icon uses Modifier.size(iconSize): width and height are constrained
// independently. The general MaterialSymbol chooses a square desired size, and native explicit
// Width/Height can overflow a smaller arrange slot. Keep this size-modifier adaptation local;
// the existing symbol renderer still fits the glyph within these bounds.
internal class IconButtonSymbol : MaterialSymbol
{
    protected override Type StyleKeyOverride => typeof(MaterialSymbol);

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = MaterialSymbol.GetDefaultSize(this);
        return new Size(Math.Min(availableSize.Width, size), Math.Min(availableSize.Height, size));
    }
}
