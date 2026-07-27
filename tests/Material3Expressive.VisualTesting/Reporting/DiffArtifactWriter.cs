using System.Net;
using System.Text.Json;
using Material3Expressive.VisualTesting.Comparison;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting.Reporting;

/// <summary>Writes actionable local evidence without ever touching approved baselines.</summary>
public sealed class DiffArtifactWriter(RepositoryPaths paths, PixelComparer comparer)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string Write(
        VisualCase visualCase,
        RenderedImage actual,
        RenderedImage? expected,
        PixelComparisonResult? result,
        string summary)
    {
        var directory = Path.Combine(
            paths.ArtifactRoot,
            visualCase.Id.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(directory);

        File.WriteAllBytes(Path.Combine(directory, "actual.png"), actual.EncodePng());
        if (expected is not null)
        {
            File.WriteAllBytes(Path.Combine(directory, "expected.png"), expected.EncodePng());
            if (result is not null)
            {
                File.WriteAllBytes(
                    Path.Combine(directory, "diff.png"),
                    comparer.CreateDiff(expected, actual, result).EncodePng());
                File.WriteAllBytes(
                    Path.Combine(directory, "overlay.png"),
                    comparer.CreateOverlay(expected, actual).EncodePng());
            }
        }

        File.WriteAllText(
            Path.Combine(directory, "metrics.json"),
            JsonSerializer.Serialize(new
            {
                visualCase.Id,
                Profile = visualCase.Comparison.Name,
                Summary = summary,
                Metrics = result is null
                    ? null
                    : new
                    {
                        result.DimensionsMatch,
                        result.ChangedPixels,
                        result.ChangedPixelRatio,
                        result.MaximumChannelDelta,
                        result.RootMeanSquareError,
                        result.LargestChangedComponent,
                        result.Passed
                    }
            }, JsonOptions));

        File.WriteAllText(
            Path.Combine(directory, "index.html"),
            BuildHtml(visualCase, expected is not null, result is not null, summary));
        return directory;
    }

    private static string BuildHtml(
        VisualCase visualCase,
        bool hasExpected,
        bool hasDiff,
        string summary)
    {
        var images = new List<string>();
        if (hasExpected)
            images.Add(Figure("Expected", "expected.png"));
        images.Add(Figure("Actual", "actual.png"));
        if (hasDiff)
        {
            images.Add(Figure("Diff", "diff.png"));
            images.Add(Figure("Overlay", "overlay.png"));
        }

        return $$"""
                 <!doctype html>
                 <html lang="en">
                 <head>
                   <meta charset="utf-8">
                   <title>{{WebUtility.HtmlEncode(visualCase.Id)}} visual diff</title>
                   <style>
                     body { font: 14px Roboto, sans-serif; margin: 24px; background: #f7f2fa; color: #1d1b20; }
                     main { max-width: 1400px; margin: auto; }
                     .images { display: grid; grid-template-columns: repeat(auto-fit, minmax(360px, 1fr)); gap: 20px; }
                     figure { margin: 0; padding: 12px; background: white; border: 1px solid #cac4d0; border-radius: 12px; }
                     img { display: block; max-width: 100%; image-rendering: auto; border: 1px solid #eee; }
                     figcaption { margin-bottom: 8px; font-weight: 600; }
                     code { overflow-wrap: anywhere; }
                   </style>
                 </head>
                 <body><main>
                   <h1><code>{{WebUtility.HtmlEncode(visualCase.Id)}}</code></h1>
                   <p>{{WebUtility.HtmlEncode(summary)}}</p>
                   <div class="images">{{string.Join(Environment.NewLine, images)}}</div>
                 </main></body>
                 </html>
                 """;
    }

    private static string Figure(string caption, string file) =>
        $"<figure><figcaption>{caption}</figcaption><img src=\"{file}\" alt=\"{caption}\"></figure>";
}
