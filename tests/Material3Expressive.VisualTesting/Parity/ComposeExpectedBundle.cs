using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>Reads and validates one freshly generated CMP expected bundle. It has no import state.</summary>
public sealed class ComposeExpectedBundle
{
    private readonly string _expectedDirectory;

    private ComposeExpectedBundle(string expectedDirectory, ParityManifest manifest)
    {
        _expectedDirectory = expectedDirectory;
        Manifest = manifest;
    }

    public ParityManifest Manifest { get; }

    public static ComposeExpectedBundle Open(string expectedDirectory)
    {
        var manifestPath = Path.Combine(expectedDirectory, "manifest.json");
        var manifest = ParityJson.Read<ParityManifest>(manifestPath);
        if (manifest.SchemaVersion != ParityManifest.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported parity manifest schema {manifest.SchemaVersion}.");
        if (manifest.CaptureSchemaVersion != ParityCaptureResult.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported parity capture schema {manifest.CaptureSchemaVersion}.");
        if (manifest.ViewportScale != 2f || manifest.Density != 2f || manifest.FontScale != 1f
            || !string.Equals(manifest.Locale, "en-US", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(manifest.LayoutDirection, "ltr", StringComparison.OrdinalIgnoreCase)
            || manifest.ComposeMultiplatformPluginVersion != "1.12.0-beta02"
            || manifest.ComposeRuntimeVersion != "1.12.0-beta02"
            || manifest.ComposeMaterial3Version != "1.12.0-alpha03"
            || manifest.JetpackMaterial3Lineage != "Jetpack Compose Material3 1.5.0-alpha22")
        {
            throw new InvalidDataException("The Compose expected bundle does not use the pinned Material 3 or canonical 2x/en-US/LTR environment.");
        }

        foreach (var item in manifest.Cases)
        {
            ValidateRelativePath(item.Id);
            ValidateFile(expectedDirectory, item.ImagePath, item.ImageSha256);
            ValidateFile(expectedDirectory, item.LayoutPath, item.LayoutSha256);
        }
        return new ComposeExpectedBundle(expectedDirectory, manifest);
    }

    public ParityManifestCase GetCase(string id) => Manifest.Cases.Single(item => item.Id == id);

    public RenderedImage ReadImage(ParityManifestCase item) =>
        RenderedImage.DecodePng(File.ReadAllBytes(Resolve(item.ImagePath)));

    public ParityCaptureResult ReadLayout(ParityManifestCase item)
    {
        var layout = ParityJson.Read<ParityCaptureResult>(Resolve(item.LayoutPath));
        if (layout.SchemaVersion != ParityCaptureResult.CurrentSchemaVersion || layout.CaseId != item.Id)
            throw new InvalidDataException($"Invalid layout contract for parity case '{item.Id}'.");
        if (layout.Motion is not null)
            ParityMotionTimeline.Validate(layout.Motion, item.Id);
        return layout;
    }

    private string Resolve(string relative)
    {
        ValidateRelativePath(relative);
        var root = Path.GetFullPath(_expectedDirectory) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(_expectedDirectory, relative));
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Path '{relative}' escapes the expected bundle.");
        return full;
    }

    private static void ValidateFile(string root, string relative, string expectedHash)
    {
        ValidateRelativePath(relative);
        if (expectedHash.Length != 64 || expectedHash.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException($"Invalid SHA-256 for '{relative}'.");
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!File.Exists(path) || !string.Equals(ParityJson.Sha256(path), expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Parity artifact '{relative}' is missing or its SHA-256 does not match the manifest.");
    }

    private static void ValidateRelativePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value)
            || value.Split(['/', '\\']).Any(part => part is "" or "." or ".."))
            throw new InvalidDataException($"'{value}' is not a safe bundle-relative path.");
    }
}
