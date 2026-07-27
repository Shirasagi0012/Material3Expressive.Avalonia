using System.Net;
using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting.Reporting;

public sealed class VisualReportIndexWriter(RepositoryPaths paths)
{
    public string Write()
    {
        Directory.CreateDirectory(paths.ArtifactRoot);
        var reports = Directory.EnumerateFiles(
                paths.ArtifactRoot,
                "index.html",
                SearchOption.AllDirectories)
            .Where(path => !string.Equals(
                Path.GetFullPath(path),
                Path.GetFullPath(Path.Combine(paths.ArtifactRoot, "index.html")),
                StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var items = reports.Length == 0
            ? "<p>No visual differences have been written.</p>"
            : "<ul>" + string.Join(
                Environment.NewLine,
                reports.Select(path =>
                {
                    var relative = Path.GetRelativePath(paths.ArtifactRoot, path).Replace('\\', '/');
                    var caseId = Path.GetDirectoryName(relative)?.Replace('\\', '/') ?? relative;
                    return $"<li><a href=\"{WebUtility.HtmlEncode(relative)}\">" +
                           $"{WebUtility.HtmlEncode(caseId)}</a></li>";
                })) + "</ul>";

        var indexPath = Path.Combine(paths.ArtifactRoot, "index.html");
        File.WriteAllText(
            indexPath,
            $$"""
              <!doctype html>
              <html lang="en">
              <head>
                <meta charset="utf-8">
                <title>Material3Expressive visual differences</title>
                <style>
                  body { font: 15px Roboto, sans-serif; margin: 32px auto; max-width: 1000px; background: #f7f2fa; color: #1d1b20; }
                  a { color: #6750a4; }
                  li { margin: 8px 0; }
                </style>
              </head>
              <body>
                <h1>Material3Expressive visual differences</h1>
                {{items}}
              </body>
              </html>
              """);
        return indexPath;
    }
}
