using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Material3Expressive.Components;

// Fixed Foundation CodepointTransformation.toVisualText replaces one Unicode scalar
// with one mask. TextBox and TextPresenter retain UTF-16 selection/caret indices. Shape
// the real display text first, then project its glyph clusters back to those indices.
// The pinned Avalonia 12.1.5 formatter accepts pre-shaped runs without shaping them again.
internal sealed class SecureTextSource : ITextSource
{
    private sealed record Run(int Start, ReadOnlyMemory<char> Text, GlyphInfo[] Glyphs,
        GlyphTypeface Typeface, double FontSize, sbyte BidiLevel, TextRunProperties Properties);

    private readonly Run[] _runs;

    private SecureTextSource(Run[] runs) => _runs = runs;

    internal static TextLayout Project(TextLayout visualLayout, string visualText,
        IReadOnlyList<int> visualToSourceOffsets, TextParagraphProperties paragraphProperties,
        out string displayedText)
    {
        if (visualToSourceOffsets.Count != visualText.Length + 1 || visualToSourceOffsets[0] != 0)
            throw new ArgumentException("Each display UTF-16 boundary needs its source offset.", nameof(visualToSourceOffsets));

        // This is also the diagnostic display value: it comes from the actual shaped
        // runs before projection, rather than from another prediction of obfuscation.
        displayedText = string.Concat(visualLayout.TextLines.SelectMany(line => line.TextRuns)
            .Select(run => run.Text.ToString()));
        var expanded = new char[visualToSourceOffsets[^1]];
        for (var index = 0; index < visualText.Length; index++)
        {
            var start = visualToSourceOffsets[index];
            var count = visualToSourceOffsets[index + 1] - start;
            if (count < 0)
                throw new ArgumentException("Display offsets must retain source order.", nameof(visualToSourceOffsets));
            Array.Fill(expanded, visualText[index], start, count);
        }

        // Expanded text contains only display characters. A masked surrogate pair
        // occupies two source units but still has exactly one glyph/cluster below.
        // Keeping the secret as the run's Text would leak its bidi/word properties.
        var sourceText = new string(expanded);
        var runs = visualLayout.TextLines.SelectMany(line => line.TextRuns)
            .OfType<ShapedTextRun>()
            .Where(run => !run.Text.IsEmpty)
            .Select(run =>
            {
                if (!MemoryMarshal.TryGetString(run.Text, out var text, out var start, out var length)
                    || !ReferenceEquals(text, visualText))
                    throw new InvalidOperationException("The shaped password run must retain its display text source.");
                var sourceStart = visualToSourceOffsets[start];
                var sourceLength = visualToSourceOffsets[start + length] - sourceStart;
                var buffer = run.ShapedBuffer;
                var clusterStart = buffer.Length == 0 ? 0 : buffer.Min(glyph => glyph.GlyphCluster);
                var glyphs = buffer.Select(glyph => new GlyphInfo(glyph.GlyphIndex,
                    visualToSourceOffsets[start + glyph.GlyphCluster - clusterStart],
                    glyph.GlyphAdvance, glyph.GlyphOffset)).ToArray();
                return new Run(sourceStart, sourceText.AsMemory(sourceStart, sourceLength), glyphs,
                    buffer.GlyphTypeface, buffer.FontRenderingEmSize, buffer.BidiLevel, run.Properties);
            })
            .OrderBy(run => run.Start)
            .ToArray();
        return new TextLayout(new SecureTextSource(runs), paragraphProperties,
            maxWidth: visualLayout.MaxWidth, maxHeight: visualLayout.MaxHeight, maxLines: visualLayout.MaxLines);
    }

    public TextRun? GetTextRun(int textSourceIndex)
    {
        var run = _runs.FirstOrDefault(candidate => candidate.Start == textSourceIndex);
        if (run is null) return null;
        // Each formatting pass owns its buffers. Neither the source nor the temporary
        // display layout lends disposable runs to the final TextLayout.
        var buffer = new ShapedBuffer(run.Text, run.Glyphs.Length, run.Typeface, run.FontSize, run.BidiLevel);
        for (var index = 0; index < run.Glyphs.Length; index++) buffer[index] = run.Glyphs[index];
        return new ShapedTextRun(buffer, run.Properties);
    }
}
