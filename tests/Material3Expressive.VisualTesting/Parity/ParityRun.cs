using Material3Expressive.VisualTesting.Cases;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Storage;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>Captures one Avalonia scene against the already-generated Compose bundle.</summary>
public sealed class ParityRun
{
    private readonly AvaloniaParityCapture _capture = new();
    private readonly ParityComparer _comparer = new();

    public ParityRunResult CompareCase(ComposeExpectedBundle expectedBundle, string caseId, string artifactRoot)
    {
        ArgumentNullException.ThrowIfNull(expectedBundle);
        var composeCase = expectedBundle.GetCase(caseId);
        var expectedLayout = expectedBundle.ReadLayout(composeCase);
        var avaloniaCase = VisualCaseCatalog.All.SingleOrDefault(item => item.Id == caseId)
            ?? throw new InvalidOperationException($"Avalonia parity scene '{caseId}' is not registered.");
        if (VisualCaseCatalog.All.Count(item => item.Id == caseId) != 1)
            throw new InvalidOperationException($"Avalonia parity scene '{caseId}' is registered more than once.");
        if (avaloniaCase.Viewport.Width * expectedLayout.CoordinateSpace.Scale != expectedLayout.ViewportPx.Width
            || avaloniaCase.Viewport.Height * expectedLayout.CoordinateSpace.Scale != expectedLayout.ViewportPx.Height)
        {
            throw new InvalidOperationException($"Avalonia scene '{caseId}' logical viewport does not match the Compose contract.");
        }

        var actual = _capture.Capture(avaloniaCase, expectedLayout);
        var profile = ParityComparisonProfiles.ForCase(caseId);
        var comparison = _comparer.Compare(
            expectedLayout,
            actual.Layout,
            expectedBundle.ReadImage(composeCase),
            actual.Image,
            profile);
        var caseDir = Path.Combine(artifactRoot, "actual", "cases", caseId.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(caseDir);
        File.Copy(
            Path.Combine(artifactRoot, "expected", composeCase.ImagePath.Replace('/', Path.DirectorySeparatorChar)),
            Path.Combine(caseDir, "expected.png"),
            overwrite: true);
        File.WriteAllBytes(Path.Combine(caseDir, "image.png"), actual.Image.EncodePng());
        File.WriteAllBytes(Path.Combine(caseDir, "diff.png"), CreateDiff(expectedBundle.ReadImage(composeCase), actual.Image).EncodePng());
        ParityJson.Write(Path.Combine(caseDir, "layout.json"), actual.Layout);
        ParityJson.Write(Path.Combine(caseDir, "comparison.json"), comparison);
        File.WriteAllText(Path.Combine(caseDir, "comparison.txt"), comparison.Summary);
        return new ParityRunResult(caseId, comparison, caseDir);
    }

    public static void ValidateCaseCatalog(ComposeExpectedBundle expectedBundle)
    {
        var avalonia = VisualCaseCatalog.All.GroupBy(item => item.Id, StringComparer.Ordinal).ToArray();
        var duplicate = avalonia.FirstOrDefault(group => group.Count() != 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Avalonia parity catalog contains duplicate ID '{duplicate.Key}'.");
        var avaloniaIds = avalonia.Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var composeIds = expectedBundle.Manifest.Cases.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var missing = composeIds.Except(avaloniaIds, StringComparer.Ordinal).Order().ToArray();
        var undeclared = avaloniaIds.Except(composeIds, StringComparer.Ordinal).Order().ToArray();
        if (missing.Length != 0 || undeclared.Length != 0)
        {
            throw new InvalidOperationException(
                $"Parity case catalogs differ. Missing Avalonia: {string.Join(", ", missing)}. " +
                $"Undeclared Avalonia: {string.Join(", ", undeclared)}.");
        }
    }

    public static void WriteActualManifest(
        string artifactRoot,
        ComposeExpectedBundle expected,
        IReadOnlyList<ParityRunResult> results)
    {
        var cases = results.Select(result =>
        {
            var relative = Path.Combine("actual", "cases", result.CaseId.Replace('/', Path.DirectorySeparatorChar));
            var image = Path.Combine(artifactRoot, relative, "image.png");
            var layout = Path.Combine(artifactRoot, relative, "layout.json");
            return new ParityActualManifestCase(
                result.CaseId,
                relative.Replace(Path.DirectorySeparatorChar, '/') + "/image.png",
                relative.Replace(Path.DirectorySeparatorChar, '/') + "/layout.json",
                relative.Replace(Path.DirectorySeparatorChar, '/') + "/comparison.json",
                ParityJson.Sha256(image),
                ParityJson.Sha256(layout),
                result.Comparison.IsMatch);
        }).ToArray();
        var manifest = new ParityActualManifest(
            ParityManifest.CurrentSchemaVersion,
            DateTimeOffset.UtcNow,
            Environment.Version.ToString(),
            typeof(Avalonia.Application).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(Avalonia.Application).Assembly.GetName().Version?.ToString()
                ?? "unknown",
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            expected.Manifest.ViewportScale,
            expected.Manifest.FontFileSha256,
            ParityJson.Sha256(Path.Combine(
                RepositoryRoot.Find(), "tests", "VisualParity", "comparison-profiles.json")),
            cases);
        ParityJson.Write(Path.Combine(artifactRoot, "actual", "manifest.json"), manifest);
    }

    private static RenderedImage CreateDiff(RenderedImage expected, RenderedImage actual)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            return actual;
        var pixels = new byte[expected.Pixels.Length];
        for (var pixel = 0; pixel < expected.PixelCount; pixel++)
        {
            var offset = pixel * 4;
            var delta = 0;
            for (var channel = 0; channel < 3; channel++)
                delta = Math.Max(delta, Math.Abs(expected.Pixels[offset + channel] - actual.Pixels[offset + channel]));
            pixels[offset] = (byte)Math.Min(255, delta * 3);
            pixels[offset + 1] = (byte)Math.Max(0, 255 - delta * 2);
            pixels[offset + 2] = 0;
            pixels[offset + 3] = 255;
        }
        return new RenderedImage(expected.Width, expected.Height, pixels);
    }
}

public sealed record ParityRunResult(string CaseId, ParityComparison Comparison, string ArtifactDirectory);
