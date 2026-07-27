using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>
/// Schema shared by the fresh Compose expected bundle and the Avalonia actual bundle.
/// Coordinates are physical pixels in the root coordinate space.
/// </summary>
public sealed record ParityManifest(
    int SchemaVersion,
    float ViewportScale,
    float Density,
    float FontScale,
    string Locale,
    string LayoutDirection,
    string FontFileSha256,
    string CaseCatalogSha256,
    IReadOnlyList<ParityManifestCase> Cases)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record ParityManifestCase(
    string Id,
    string ImagePath,
    string LayoutPath,
    string ImageSha256,
    string LayoutSha256);

public sealed record ParityCaptureResult(
    int SchemaVersion,
    string CaseId,
    ParityCoordinateSpace CoordinateSpace,
    ParityViewport ViewportPx,
    IReadOnlyList<ParityNode> Nodes)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record ParityCoordinateSpace(
    string Unit,
    string Origin,
    string XDirection,
    string YDirection,
    float Scale);

public sealed record ParityViewport(int Width, int Height);

public sealed record ParityNode(
    string Id,
    string Kind,
    string? ParentId,
    ParityRect? BoundsInRootPx,
    bool Visible,
    ParityTextLayout? TextLayout = null);

public sealed record ParityRect(float Left, float Top, float Right, float Bottom)
{
    public float Width => Right - Left;
    public float Height => Bottom - Top;
}

public sealed record ParityTextLayout(
    string Text,
    string Encoding,
    float FirstBaselineInRootPx,
    float LastBaselineInRootPx,
    int LineCount,
    bool OverflowEllipsis,
    IReadOnlyList<ParityTextLine> Lines,
    IReadOnlyList<ParityCharacterBox> CharacterBoxes);

public sealed record ParityTextLine(
    int Index,
    int StartUtf16,
    int EndUtf16,
    int VisibleEndUtf16,
    float LeftPx,
    float RightPx,
    float TopPx,
    float BottomPx,
    float BaselinePx,
    bool Ellipsized);

public sealed record ParityCharacterBox(
    int Utf16Offset,
    string CodeUnit,
    int LineIndex,
    ParityRect BoundsInRootPx);

public static class ParityJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static T Read<T>(string path)
    {
        var result = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        return result ?? throw new InvalidDataException($"Parity JSON file '{path}' is empty.");
    }

    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, Options));
    }

    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
