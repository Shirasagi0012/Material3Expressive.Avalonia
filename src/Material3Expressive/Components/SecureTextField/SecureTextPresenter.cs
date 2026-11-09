using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using System.Text;

namespace Material3Expressive.Components;

// Also serves ordinary TextBox templates: those use the unmodified native layout.
internal sealed class SecureTextPresenter : TextPresenter
{
    protected override Type StyleKeyOverride => typeof(TextPresenter);
    internal void RefreshObfuscation() => InvalidateTextLayout();
    internal string DisplayedText { get { _ = TextLayout; return _displayedText; } }
    private string _displayedText = string.Empty;

    protected override TextLayout CreateTextLayout()
    {
        if (TemplatedParent is not SecureTextField owner) return base.CreateTextLayout();
        using var native = base.CreateTextLayout();
        var source = Text ?? string.Empty;
        if (!string.IsNullOrEmpty(PreeditText)) source = source.Insert(CaretIndex, PreeditText);
        var text = source;
        var offsets = new List<int> { 0 };
        if (owner.TextObfuscationMode != TextObfuscationMode.Visible)
        {
            var display = new StringBuilder();
            var sourceIndex = 0;
            var codepointIndex = 0;
            var preeditLength = PreeditText?.Length ?? 0;
            var preeditCodepoints = PreeditText?.EnumerateRunes().Count() ?? 0;
            foreach (var rune in source.EnumerateRunes())
            {
                var inPreedit = preeditLength != 0 && sourceIndex >= CaretIndex && sourceIndex < CaretIndex + preeditLength;
                var committedIndex = codepointIndex - (preeditLength != 0 && sourceIndex >= CaretIndex + preeditLength
                    ? preeditCodepoints : 0);
                // Keep the fixed controller's codepoint-index comparison even after a
                // UTF-16 edit range. Native preedit is a separate display insertion, not
                // an editing transaction: it must not move the old reveal onto a different
                // committed secret character. Composition itself stays masked until commit.
                if (owner.TextObfuscationMode is TextObfuscationMode.RevealLastTyped or TextObfuscationMode.System
                    && !inPreedit && committedIndex == owner.RevealIndex)
                {
                    display.Append(rune.ToString());
                    for (var index = 1; index <= rune.Utf16SequenceLength; index++) offsets.Add(sourceIndex + index);
                }
                else
                {
                    display.Append(owner.PasswordChar);
                    offsets.Add(sourceIndex + rune.Utf16SequenceLength);
                }
                sourceIndex += rune.Utf16SequenceLength;
                codepointIndex++;
            }
            text = display.ToString();
        }
        else offsets.AddRange(Enumerable.Range(1, source.Length));
        int ToDisplay(int index)
        {
            var match = offsets.BinarySearch(Math.Clamp(index, 0, source.Length));
            return match >= 0 ? match : ~match;
        }
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        IReadOnlyList<ValueSpan<TextRunProperties>>? overrides = null;
        if (!string.IsNullOrEmpty(PreeditText))
            overrides = [new(ToDisplay(CaretIndex), ToDisplay(CaretIndex + PreeditText.Length) - ToDisplay(CaretIndex), new GenericTextRunProperties(typeface,
                FontSize, TextDecorations.Underline, Foreground, fontFeatures: FontFeatures))];
        else if (ShowSelectionHighlight && SelectionStart != SelectionEnd && SelectionForegroundBrush is { } selection)
            overrides = [new(ToDisplay(Math.Min(SelectionStart, SelectionEnd)), Math.Abs(ToDisplay(SelectionStart) - ToDisplay(SelectionEnd)),
                new GenericTextRunProperties(typeface, FontSize, foregroundBrush: selection, fontFeatures: FontFeatures))];
        var visual = new TextLayout(text, typeface, FontSize, Foreground, TextAlignment, TextWrapping,
            flowDirection: FlowDirection, maxWidth: native.MaxWidth, maxHeight: native.MaxHeight,
            lineHeight: LineHeight, letterSpacing: LetterSpacing, fontFeatures: FontFeatures, textStyleOverrides: overrides);
        if (source.Length == text.Length)
        {
            // The formatter adds a synthetic space only to obtain empty-line metrics.
            _displayedText = text.Length == 0 ? string.Empty
                : string.Concat(visual.TextLines.SelectMany(line => line.TextRuns).Select(run => run.Text.ToString()));
            return visual;
        }
        using (visual)
            return SecureTextSource.Project(visual, text, offsets,
                new GenericTextParagraphProperties(FlowDirection, TextAlignment, true, false,
                    new GenericTextRunProperties(typeface, FontSize, foregroundBrush: Foreground, fontFeatures: FontFeatures),
                    TextWrapping, LineHeight, 0, LetterSpacing), out _displayedText);
    }
}
