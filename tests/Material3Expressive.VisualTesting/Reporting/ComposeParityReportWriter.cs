using System.Net;
using System.Text.Json;
using Material3Expressive.VisualTesting.Compose;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting.Reporting;

/// <summary>Writes parity evidence only. It never applies a cross-renderer pass/fail threshold.</summary>
public sealed class ComposeParityReportWriter(
    RepositoryPaths paths,
    ComposeParityComparer comparer)
{
    private static readonly JsonSerializerOptions JsonOptions = new(
        ComposeReferenceManifest.JsonOptions)
    {
        WriteIndented = true
    };

    public ComposeParityResult Write(
        ComposeReferenceCase referenceCase,
        RenderedImage? avalonia,
        RenderedImage? reference,
        ComposeParityMetrics? metrics,
        ComposeParityStatus status,
        string summary)
    {
        var directory = Path.Combine(
            paths.ComposeArtifactRoot,
            referenceCase.Id.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(directory);

        if (avalonia is not null)
            File.WriteAllBytes(Path.Combine(directory, "avalonia.png"), avalonia.EncodePng());
        if (reference is not null)
            File.WriteAllBytes(Path.Combine(directory, "compose.png"), reference.EncodePng());
        if (reference is not null && avalonia is not null && metrics is not null)
        {
            File.WriteAllBytes(
                Path.Combine(directory, "delta-e.png"),
                comparer.CreateDeltaEHeatmap(reference, avalonia, metrics).EncodePng());
            File.WriteAllBytes(
                Path.Combine(directory, "overlay.png"),
                comparer.CreateAlignedOverlay(reference, avalonia, metrics).EncodePng());
            File.WriteAllBytes(
                Path.Combine(directory, "edges.png"),
                comparer.CreateEdgeOverlay(reference, avalonia, metrics).EncodePng());
        }

        File.WriteAllText(
            Path.Combine(directory, "metrics.json"),
            JsonSerializer.Serialize(new
            {
                referenceCase.Id,
                Status = status,
                Summary = summary,
                Source = new
                {
                    referenceCase.SourceTest,
                    referenceCase.SourceGolden
                },
                Metrics = metrics
            }, JsonOptions) + Environment.NewLine);
        File.WriteAllText(
            Path.Combine(directory, "index.html"),
            BuildCaseHtml(referenceCase, status, summary, avalonia is not null, metrics));

        return new ComposeParityResult(referenceCase.Id, status, summary, directory, metrics);
    }

    public string WriteIndex(
        IReadOnlyList<ComposeParityResult> results,
        ComposeReferenceManifest manifest)
    {
        Directory.CreateDirectory(paths.ComposeArtifactRoot);
        var cards = results.Select(result =>
        {
            var relative = Path.GetRelativePath(
                    paths.ComposeArtifactRoot,
                    result.ArtifactDirectory)
                .Replace('\\', '/');
            var stateClass = result.Status switch
            {
                ComposeParityStatus.Compared => "compared",
                ComposeParityStatus.ReferenceMissing => "missing",
                _ => "invalid"
            };
            var thumbnail = File.Exists(Path.Combine(result.ArtifactDirectory, "avalonia.png"))
                ? $"<img src=\"{WebUtility.HtmlEncode(relative)}/avalonia.png\" alt=\"Avalonia\">"
                : "<div class=\"empty\">No Avalonia render</div>";
            return $$"""
                     <article>
                       <a href="{{WebUtility.HtmlEncode(relative)}}/index.html">
                         <header><code>{{WebUtility.HtmlEncode(result.CaseId)}}</code>
                           <span class="{{stateClass}}">{{WebUtility.HtmlEncode(result.Status.ToString())}}</span>
                         </header>
                         {{thumbnail}}
                         <p>{{WebUtility.HtmlEncode(result.Summary)}}</p>
                       </a>
                     </article>
                     """;
        });

        var compared = results.Count(item => item.Status == ComposeParityStatus.Compared);
        var missing = results.Count(item => item.Status == ComposeParityStatus.ReferenceMissing);
        var invalid = results.Count - compared - missing;
        var output = Path.Combine(paths.ComposeArtifactRoot, "index.html");
        File.WriteAllText(
            output,
            $$"""
              <!doctype html>
              <html lang="en">
              <head>
                <meta charset="utf-8">
                <title>Material3Expressive Compose parity</title>
                <style>
                  body { margin: 24px; font: 14px Roboto, sans-serif; background: #f7f2fa; color: #1d1b20; }
                  main { max-width: 1500px; margin: auto; }
                  .notice { padding: 16px; background: #eaddff; border-radius: 12px; }
                  .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(360px, 1fr)); gap: 18px; margin-top: 24px; }
                  article { background: white; border: 1px solid #cac4d0; border-radius: 14px; overflow: hidden; }
                  article a { display: block; color: inherit; text-decoration: none; }
                  article header { display: flex; justify-content: space-between; gap: 12px; padding: 12px; }
                  article img { display: block; width: 100%; height: auto; border-block: 1px solid #e7e0ec; }
                  article p { margin: 12px; color: #49454f; }
                  span { border-radius: 999px; padding: 3px 8px; font-size: 12px; }
                  .compared { background: #c4eed0; color: #07391c; }
                  .missing { background: #ffdf9a; color: #442b00; }
                  .invalid { background: #ffdad6; color: #410002; }
                  .empty { padding: 48px; text-align: center; }
                  code { overflow-wrap: anywhere; }
                </style>
              </head>
              <body><main>
                <h1>Avalonia ↔ Compose parity</h1>
                <p class="notice">
                  Diagnostic phase: {{compared}} compared, {{missing}} awaiting Compose capture,
                  {{invalid}} invalid. These metrics are review evidence and are not an automatic
                  parity gate yet.
                </p>
                <p>
                  {{WebUtility.HtmlEncode(manifest.ComposeMaterial3Version)}} · API
                  {{manifest.Environment.AndroidApi}} · {{manifest.Environment.DensityDpi}} dpi ·
                  {{WebUtility.HtmlEncode(manifest.Environment.FontFamily)}} preset weights ·
                  <code>{{WebUtility.HtmlEncode(manifest.SourceArtifact)}}</code> · AAR SHA-256
                  <code>{{WebUtility.HtmlEncode(manifest.SourceArtifactSha256)}}</code>
                </p>
                <div class="grid">{{string.Join(Environment.NewLine, cards)}}</div>
              </main></body>
              </html>
              """);
        return output;
    }

    private static string BuildCaseHtml(
        ComposeReferenceCase referenceCase,
        ComposeParityStatus status,
        string summary,
        bool hasAvalonia,
        ComposeParityMetrics? metrics)
    {
        var figures = new List<string>();
        if (metrics is not null)
            figures.Add(Figure("Compose", "compose.png"));
        if (hasAvalonia)
            figures.Add(Figure("Avalonia", "avalonia.png"));
        if (metrics is not null)
        {
            figures.Add(Figure("Aligned overlay", "overlay.png"));
            figures.Add(Figure("ΔE76 heatmap", "delta-e.png"));
            figures.Add(Figure("Edges (Compose magenta, Avalonia cyan, exact white)", "edges.png"));
        }

        var metricTable = metrics is null
            ? "<p>No cross-renderer metrics are available until a trusted Compose image is imported.</p>"
            : BuildMetricTable(metrics);
        var regionTable = metrics is null || metrics.Regions.Count == 0
            ? ""
            : BuildRegionTable(metrics.Regions);

        return $$"""
                 <!doctype html>
                 <html lang="en">
                 <head>
                   <meta charset="utf-8">
                   <title>{{WebUtility.HtmlEncode(referenceCase.Id)}} parity</title>
                   <style>
                     body { margin: 24px; font: 14px Roboto, sans-serif; background: #f7f2fa; color: #1d1b20; }
                     main { max-width: 1500px; margin: auto; }
                     .status { padding: 14px; border-radius: 10px; background: white; border: 1px solid #cac4d0; }
                     .images { display: grid; grid-template-columns: repeat(auto-fit, minmax(360px, 1fr)); gap: 18px; margin-block: 24px; }
                     figure { margin: 0; padding: 12px; background: white; border: 1px solid #cac4d0; border-radius: 12px; }
                     figcaption { margin-bottom: 8px; font-weight: 600; }
                     img { display: block; width: 100%; height: auto; border: 1px solid #eee; }
                     table { width: 100%; border-collapse: collapse; margin-block: 18px; background: white; }
                     th, td { padding: 8px 10px; border: 1px solid #e7e0ec; text-align: left; }
                     th { background: #f3edf7; }
                     code { overflow-wrap: anywhere; }
                   </style>
                 </head>
                 <body><main>
                   <h1><code>{{WebUtility.HtmlEncode(referenceCase.Id)}}</code></h1>
                   <p class="status"><strong>{{WebUtility.HtmlEncode(status.ToString())}}</strong> —
                     {{WebUtility.HtmlEncode(summary)}}</p>
                   <p>Source: <code>{{WebUtility.HtmlEncode(referenceCase.SourceTest)}}</code> /
                     <code>{{WebUtility.HtmlEncode(referenceCase.SourceGolden)}}</code></p>
                   <div class="images">{{string.Join(Environment.NewLine, figures)}}</div>
                   <h2>Whole-image metrics</h2>
                   {{metricTable}}
                   {{regionTable}}
                 </main></body>
                 </html>
                 """;
    }

    private static string BuildMetricTable(ComposeParityMetrics metrics)
    {
        return $$"""
                 <table>
                   <tr><th>Metric</th><th>Value</th></tr>
                   <tr><td>Alignment</td><td>{{metrics.AlignmentX:+0;-0;0}}, {{metrics.AlignmentY:+0;-0;0}} px</td></tr>
                   <tr><td>Raw / aligned RMSE</td><td>{{metrics.RawRootMeanSquareError:F4}} / {{metrics.AlignedRootMeanSquareError:F4}}</td></tr>
                   <tr><td>Mean absolute channel error</td><td>{{metrics.MeanAbsoluteChannelError:F4}}</td></tr>
                   <tr><td>Mean / p95 / max ΔE76</td><td>{{metrics.MeanDeltaE76:F3}} / {{metrics.Percentile95DeltaE76:F3}} / {{metrics.MaximumDeltaE76:F3}}</td></tr>
                   <tr><td>Background ΔE76</td><td>{{metrics.BackgroundDeltaE76:F3}}</td></tr>
                   <tr><td>Dominant foreground ΔE76</td><td>{{metrics.ForegroundDominantDeltaE76:F3}}</td></tr>
                   <tr><td>Edge precision / recall / F1</td><td>{{metrics.EdgePrecision:F4}} / {{metrics.EdgeRecall:F4}} / {{metrics.EdgeF1:F4}}</td></tr>
                   <tr><td>Compose foreground bounds</td><td>{{WebUtility.HtmlEncode(metrics.ReferenceForegroundBounds?.ToString() ?? "none")}}</td></tr>
                   <tr><td>Avalonia foreground bounds</td><td>{{WebUtility.HtmlEncode(metrics.AvaloniaForegroundBounds?.ToString() ?? "none")}}</td></tr>
                 </table>
                 """;
    }

    private static string BuildRegionTable(
        IReadOnlyList<ComposeParityRegionMetrics> regions)
    {
        var rows = regions.Select(region =>
            $$"""
              <tr>
                <td><code>{{WebUtility.HtmlEncode(region.Id)}}</code></td>
                <td>{{WebUtility.HtmlEncode(region.Kind.ToString())}}</td>
                <td>{{region.MeanDeltaE76:F3}} / {{region.Percentile95DeltaE76:F3}}</td>
                <td>{{region.AverageColorDeltaE76:F3}}</td>
                <td>{{region.EdgeF1:F4}}</td>
                <td>{{WebUtility.HtmlEncode(region.ReferenceInkBounds?.ToString() ?? "none")}}</td>
                <td>{{WebUtility.HtmlEncode(region.AvaloniaInkBounds?.ToString() ?? "none")}}</td>
                <td>{{region.InkBottomDelta?.ToString() ?? "n/a"}}</td>
              </tr>
              """);
        return $$"""
                 <h2>Declared regions</h2>
                 <table>
                   <tr>
                     <th>Region</th><th>Kind</th><th>Mean / p95 ΔE76</th>
                     <th>Average colour ΔE76</th><th>Edge F1</th>
                     <th>Compose ink</th><th>Avalonia ink</th><th>Ink-bottom Δ</th>
                   </tr>
                   {{string.Join(Environment.NewLine, rows)}}
                 </table>
                 """;
    }

    private static string Figure(string caption, string file) =>
        $"<figure><figcaption>{WebUtility.HtmlEncode(caption)}</figcaption>" +
        $"<img src=\"{file}\" alt=\"{WebUtility.HtmlEncode(caption)}\"></figure>";
}
