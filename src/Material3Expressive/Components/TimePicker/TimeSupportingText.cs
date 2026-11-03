using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Material3Expressive.Components;

// SupportingText's minLines=2. TextBlock has no native minimum-line property.
internal sealed class TimeSupportingText : TextBlock
{
    protected override Type StyleKeyOverride => typeof(TextBlock);
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        if (double.IsFinite(LineHeight) && LineHeight > 0)
            return new Size(size.Width, Math.Max(size.Height, LineHeight * 2));
        // MinLinesConstrainer measures two sample lines using the active typeface.
        // Preserve the native font metrics when the line height is automatic too.
        using var lines = new TextLayout("H\nH", new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize, Foreground, letterSpacing: LetterSpacing, fontFeatures: FontFeatures);
        return new Size(size.Width, Math.Max(size.Height, lines.Height));
    }
}
