using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Avalonia.Media;

namespace Material3Expressive.VisualTesting;

/// <summary>The Roboto family and the concrete font files backing it on this machine.</summary>
public static class FontEnvironment
{
    public const string CanonicalRobotoSha256 = "B25986D18730960C7B27384AB2BC500856AE7FE9E71C9850019195FF9019F0B2";
    private static readonly Lazy<FontFileFingerprint> CanonicalRoboto = new(FindCanonicalRoboto);

    public static FontFamily Roboto { get; } = new("Roboto");

    /// <summary>
    /// Verifies the concrete file pinned by the Compose producer is locally available. The result
    /// is deliberately reported separately from Avalonia's resolved face: public Avalonia APIs can
    /// identify the resolved family/weight but not the backing system-font file path.
    /// </summary>
    public static FontFileFingerprint RequireCanonicalRoboto()
        => CanonicalRoboto.Value;

    private static FontFileFingerprint FindCanonicalRoboto()
    {
        var canonical = CaptureRobotoFiles()
            .FirstOrDefault(file => string.Equals(
                file.Sha256,
                CanonicalRobotoSha256,
                StringComparison.OrdinalIgnoreCase));
        return canonical ?? throw new InvalidOperationException(
            "Avalonia parity capture requires the same pinned Roboto font file as the Compose producer.");
    }

    public static IReadOnlyList<FontFileFingerprint> CaptureRobotoFiles()
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
                return new FontFileFingerprint(
                    Path.GetFileName(path),
                    stream.Length,
                    Convert.ToHexString(SHA256.HashData(stream)));
            })
            .ToArray();
    }
}

public sealed record FontFileFingerprint(string FileName, long Length, string Sha256);
