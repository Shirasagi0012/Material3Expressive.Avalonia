using System.Runtime.CompilerServices;
using Avalonia.Media.TextFormatting;

namespace ComposeAnimation;

// Avalonia 12.1.2: the paragraph LineSpacing property is internal. Keep the sole
// non-public dependency here; do not access TextBlock's live constraint/layout caches.
internal static class AvaloniaTextLayoutInterop
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_LineSpacing")]
    internal static extern void SetLineSpacing(TextParagraphProperties properties, double value);
}
