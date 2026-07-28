using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Skia;
using SkiaSharp;

namespace Material3Expressive.VisualTesting;

/// <summary>The Roboto family and the concrete font files backing it on this machine.</summary>
public static class FontEnvironment
{
    public const string CanonicalRobotoSha256 = "B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2";
    public const string PinnedRobotoCollectionUri = "fonts:Material3Expressive.VisualTesting.Roboto";
    public const string PinnedRobotoFamilyUri = PinnedRobotoCollectionUri + "#Roboto";

    private static readonly Lazy<LocatedFontFile> CanonicalRoboto = new(FindCanonicalRoboto);

    /// <summary>
    /// A collection-qualified family backed by explicit wght-axis instances of the exact variable
    /// file used by Compose. The resolved face is byte-verified before every parity capture.
    /// </summary>
    public static FontFamily Roboto { get; } = new(PinnedRobotoFamilyUri);

    /// <summary>
    /// Verifies the concrete file pinned by the Compose producer is locally available. Resolved
    /// Avalonia face bytes are separately verified through <see cref="IPlatformTypeface.TryGetStream"/>.
    /// </summary>
    public static FontFileFingerprint RequireCanonicalRoboto()
        => CanonicalRoboto.Value.Fingerprint;

    /// <summary>
    /// Registers Normal, Medium, and Bold wght-axis instances of the canonical variable file.
    /// Avalonia 12's normal embedded-font path otherwise loads only the font's default (400)
    /// instance, which is not equivalent to Compose's explicit 400/500/700 registrations.
    /// </summary>
    public static void ConfigurePinnedRoboto(FontManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        manager.AddFontCollection(new PinnedRobotoFontCollection(CanonicalRoboto.Value.Path));
    }

    /// <summary>
    /// Returns the canonical hash only when this is a resolved Roboto face whose actual font
    /// bytes match the Compose producer. A matching family name by itself is deliberately not
    /// enough: Skia exposes the selected face stream through <see cref="IPlatformTypeface"/>.
    /// </summary>
    public static string? GetCanonicalRobotoHash(GlyphTypeface glyphTypeface)
    {
        ArgumentNullException.ThrowIfNull(glyphTypeface);
        if (!IsRoboto(glyphTypeface))
            return null;
        var actualHash = GetResolvedFaceSha256(glyphTypeface);
        return string.Equals(actualHash, CanonicalRobotoSha256, StringComparison.OrdinalIgnoreCase)
            ? CanonicalRobotoSha256
            : null;
    }

    /// <summary>
    /// Ensures the requested family resolves to an actual Skia face with the same bytes as the
    /// Compose producer. This retains the platform font manager's correct variable-weight
    /// selection while rejecting an installed Roboto version that only shares the family name.
    /// </summary>
    public static void RequirePinnedRobotoFaces()
    {
        _ = RequireCanonicalRoboto();
        foreach (var (_, weight) in PinnedWeights)
        {
            var typeface = new Typeface(Roboto, FontStyle.Normal, weight, FontStretch.Normal);
            var glyphTypeface = typeface.GlyphTypeface;
            var actualHash = GetResolvedFaceSha256(glyphTypeface);
            var platformWeight = glyphTypeface.PlatformTypeface.Weight;
            if (!IsRoboto(glyphTypeface)
                || platformWeight != weight
                || !string.Equals(actualHash, CanonicalRobotoSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Parity Roboto request for weight {weight} resolved to " +
                    $"'{ResolvedFamilyName(glyphTypeface)}' at platform weight {platformWeight} with SHA-256 " +
                    $"'{actualHash ?? "unavailable"}', " +
                    $"not the canonical '{CanonicalRobotoSha256}'.");
            }
        }
    }

    /// <summary>
    /// Captures font provenance and stable glyph-metric probes for a parity artifact. Advances
    /// are font-design units, independent of a component's padding, position, or rasterizer.
    /// </summary>
    public static string DescribePinnedRobotoEnvironment()
    {
        var font = RequireCanonicalRoboto();
        var report = new StringBuilder()
            .AppendLine("font-family: " + Roboto.Name)
            .AppendLine("font-source: " + (Roboto.Key?.Source?.AbsoluteUri ?? "<none>"))
            .AppendLine("font-file: " + font.FileName)
            .AppendLine("font-file-length: " + font.Length)
            .AppendLine("font-file-sha256: " + font.Sha256);

        foreach (var (name, weight) in PinnedWeights)
        {
            var typeface = new Typeface(Roboto, FontStyle.Normal, weight, FontStretch.Normal);
            var glyphTypeface = typeface.GlyphTypeface;
            report.Append("face ").Append(name)
                .Append(": requestedWeight=").Append(weight)
                .Append(", resolvedFamily=").Append(ResolvedFamilyName(glyphTypeface))
                .Append(", glyphTableWeight=").Append(glyphTypeface.Weight)
                .Append(", platformWeight=").Append(glyphTypeface.PlatformTypeface.Weight)
                .Append(", resolvedStyle=").Append(glyphTypeface.Style)
                .Append(", resolvedStretch=").Append(glyphTypeface.Stretch)
                .Append(", designEm=").Append(glyphTypeface.Metrics.DesignEmHeight)
                .Append(", ascent=").Append(glyphTypeface.Metrics.Ascent)
                .Append(", descent=").Append(glyphTypeface.Metrics.Descent)
                .Append(", lineGap=").Append(glyphTypeface.Metrics.LineGap)
                .Append(", platformTypeface=").Append(glyphTypeface.PlatformTypeface.GetType().FullName)
                .Append(", resolvedFileSha256=").Append(GetResolvedFaceSha256(glyphTypeface) ?? "unavailable")
                .AppendLine();

            report.Append("  advances: ");
            foreach (var codePoint in GlyphAdvanceProbe)
            {
                if (!glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codePoint, out var glyph)
                    || !glyphTypeface.TryGetHorizontalGlyphAdvance(glyph, out var advance))
                {
                    report.Append($"U+{codePoint:X4}=missing; ");
                    continue;
                }
                report.Append($"U+{codePoint:X4}=glyph{glyph}/advance{advance}; ");
            }
            report.AppendLine();
        }

        return report.ToString();
    }

    private static LocatedFontFile FindCanonicalRoboto()
    {
        var canonical = CaptureRobotoFilesWithPaths()
            .FirstOrDefault(file => string.Equals(
                file.Fingerprint.Sha256,
                CanonicalRobotoSha256,
                StringComparison.OrdinalIgnoreCase));
        return canonical ?? throw new InvalidOperationException(
            "Avalonia parity capture requires the same pinned Roboto font file as the Compose producer.");
    }

    public static IReadOnlyList<FontFileFingerprint> CaptureRobotoFiles()
        => CaptureRobotoFilesWithPaths().Select(file => file.Fingerprint).ToArray();

    private static IReadOnlyList<LocatedFontFile> CaptureRobotoFilesWithPaths()
    {
        var directories = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Fonts"),
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts)
        };

        var files = directories
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "Roboto*.ttf"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && files.Length == 0)
        {
            throw new InvalidOperationException(
                "Roboto is the canonical visual-test font, but no Roboto .ttf file was found " +
                "in either the per-user or Windows font directory.");
        }

        return files
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return new LocatedFontFile(
                    path,
                    new FontFileFingerprint(
                        Path.GetFileName(path),
                        stream.Length,
                        Convert.ToHexString(SHA256.HashData(stream))));
            })
            .ToArray();
    }

    /// <summary>
    /// A test-only collection that instantiates the canonical variable font at the same three
    /// weights passed to Compose's <c>Font(file, weight)</c> registrations. Avalonia.Skia's
    /// wrapper is internal, so construction is deliberately isolated here and fails fast if the
    /// pinned Avalonia 12.1 implementation changes its constructor contract.
    /// </summary>
    private sealed class PinnedRobotoFontCollection : FontCollectionBase
    {
        private static readonly Uri CollectionUri = new(PinnedRobotoCollectionUri, UriKind.Absolute);
        private static readonly SKFourByteTag WeightAxis = new('w', 'g', 'h', 't');

        public PinnedRobotoFontCollection(string path)
        {
            using var source = SKTypeface.FromFile(path)
                ?? throw new InvalidOperationException($"Could not load the canonical Roboto file '{path}'.");
            if (!source.VariationDesignParameters.Any(axis => axis.Tag == WeightAxis))
                throw new InvalidOperationException($"The canonical Roboto file '{path}' has no wght variation axis.");

            foreach (var (_, weight) in PinnedWeights)
            {
                var instance = source.Clone(CreateVariationPosition(source, (int)weight));
                var platformTypeface = CreateSkiaPlatformTypeface(instance);
                var glyphTypeface = new GlyphTypeface(platformTypeface, FontSimulations.None);
                var key = new FontCollectionKey(FontStyle.Normal, weight, FontStretch.Normal);
                if (!TryAddGlyphTypeface(glyphTypeface, key))
                    throw new InvalidOperationException($"Could not register the canonical Roboto {weight} instance.");
            }
        }

        public override Uri Key => CollectionUri;

        private static SKFontVariationPositionCoordinate[] CreateVariationPosition(SKTypeface source, int weight)
        {
            var position = source.VariationDesignPosition.ToArray();
            var foundWeight = false;
            for (var index = 0; index < position.Length; index++)
            {
                if (position[index].Axis != WeightAxis)
                    continue;
                position[index] = new SKFontVariationPositionCoordinate
                {
                    Axis = WeightAxis,
                    Value = weight
                };
                foundWeight = true;
            }
            if (!foundWeight)
                throw new InvalidOperationException("The canonical Roboto wght axis has no active design position.");
            return position;
        }

        private static IPlatformTypeface CreateSkiaPlatformTypeface(SKTypeface typeface)
        {
            try
            {
                // SkiaTypeface is internal, but SkiaOptions is a stable public type in the
                // same Avalonia.Skia assembly. Do not use PlatformRenderInterface here: it is
                // internal as well and makes this test-only integration fail to compile.
                var wrapperType = typeof(Avalonia.SkiaOptions).Assembly.GetType(
                    "Avalonia.Skia.SkiaTypeface",
                    throwOnError: true)!;
                var constructor = wrapperType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    types: [typeof(SKTypeface), typeof(FontSimulations)],
                    modifiers: null)
                    ?? throw new InvalidOperationException(
                        "Avalonia.Skia.SkiaTypeface(SKTypeface, FontSimulations) is unavailable.");
                return (IPlatformTypeface)(constructor.Invoke([typeface, FontSimulations.None])
                    ?? throw new InvalidOperationException("Avalonia.Skia returned a null platform typeface."));
            }
            catch
            {
                typeface.Dispose();
                throw;
            }
        }
    }

    private sealed record LocatedFontFile(string Path, FontFileFingerprint Fingerprint);

    private static bool IsRoboto(GlyphTypeface glyphTypeface) =>
        string.Equals(glyphTypeface.TypographicFamilyName, "Roboto", StringComparison.OrdinalIgnoreCase)
        || string.Equals(glyphTypeface.FamilyName, "Roboto", StringComparison.OrdinalIgnoreCase);

    private static string ResolvedFamilyName(GlyphTypeface glyphTypeface) =>
        string.IsNullOrWhiteSpace(glyphTypeface.TypographicFamilyName)
            ? glyphTypeface.FamilyName
            : glyphTypeface.TypographicFamilyName;

    private static string? GetResolvedFaceSha256(GlyphTypeface glyphTypeface)
    {
        if (!glyphTypeface.PlatformTypeface.TryGetStream(out var stream) || stream is null)
            return null;
        using (stream)
            return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static readonly (string Name, FontWeight Weight)[] PinnedWeights =
    [
        ("normal", FontWeight.Normal),
        ("medium", FontWeight.Medium),
        ("bold", FontWeight.Bold)
    ];

    private static readonly int[] GlyphAdvanceProbe = ['A', 'a', 'M', 'W', '0', '1', ' '];
}

public sealed record FontFileFingerprint(string FileName, long Length, string Sha256);
