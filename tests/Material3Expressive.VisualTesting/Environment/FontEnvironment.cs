using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Avalonia.Media;

namespace Material3Expressive.VisualTesting;

/// <summary>The Roboto family and the concrete font files backing it on this machine.</summary>
public static class FontEnvironment
{
    public static FontFamily Roboto { get; } = new("Roboto");

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
