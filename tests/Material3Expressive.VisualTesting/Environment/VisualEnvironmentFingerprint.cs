using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Material3Expressive.Foundation;
using SkiaSharp;

namespace Material3Expressive.VisualTesting;

/// <summary>Everything that must stay fixed for a pixel baseline to be meaningful.</summary>
public sealed record VisualEnvironmentFingerprint(
    string OperatingSystem,
    string Architecture,
    string AvaloniaVersion,
    string Material3ExpressiveVersion,
    string SkiaSharpVersion,
    string Renderer,
    string PixelFormat,
    string Culture,
    string FontFamily,
    IReadOnlyList<FontFileFingerprint> FontFiles)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static VisualEnvironmentFingerprint Capture()
    {
        return new VisualEnvironmentFingerprint(
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
            AssemblyVersion(typeof(Application).Assembly),
            AssemblyVersion(typeof(MaterialTheme).Assembly),
            AssemblyVersion(typeof(SKBitmap).Assembly),
            "Skia",
            "RGBA8888-Premul",
            "en-US",
            FontEnvironment.Roboto.Name,
            FontEnvironment.CaptureRobotoFiles());
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions) + Environment.NewLine;

    public static VisualEnvironmentFingerprint FromJson(string json) =>
        JsonSerializer.Deserialize<VisualEnvironmentFingerprint>(json, JsonOptions)
        ?? throw new InvalidDataException("The visual environment fingerprint is empty.");

    private static string AssemblyVersion(Assembly assembly) =>
        assembly.GetName().Version?.ToString() ?? "unknown";
}
