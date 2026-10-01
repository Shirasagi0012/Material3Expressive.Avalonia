using System.Text;
using System.Text.Json;

namespace Material3Expressive.Parity;

public static class Report
{
    // Read only the public result files; the private capture cache is not needed to render a report.
    public static CaseResult Write(string directory)
    {
        var result = ResultFiles.Read(directory);
        var buffers = new Dictionary<string, string>();
        foreach (var (side, capture) in new[] { ("expected", result.Expected), ("actual", result.Actual) })
            if (capture is not null)
                foreach (var sample in capture.Samples)
                foreach (var raster in sample.Rasters)
                    buffers[side + "/" + sample.Id + "/" + raster.Id] = Convert.ToBase64String(PixelData.Read(directory, raster));
        var data = JsonSerializer.Serialize(new
        {
            metadata = result.Metadata, comparison = result.Comparison, expected = result.Expected, actual = result.Actual,
            buffers, repeatability = result.Repeatability, sampleFiles = result.Samples,
            suiteHref = new DirectoryInfo(directory).Parent?.Name == "cases" ? "../../report.html" : null
        }, ParityJson.Options);
        File.WriteAllText(Path.Combine(directory, "overview.md"), ResultFiles.Overview(result));
        WriteTemplate(directory, "Report.template.html", data);
        return result;
    }

    public static void Suite(string directory)
    {
        var results = Directory.EnumerateDirectories(Path.Combine(directory, "cases")).Order().Select(ResultFiles.Read).ToArray();
        var entries = results.Select(result =>
        {
            var c = result.Comparison;
            return new
            {
                c.CaseId, result.Metadata.Title, result.Metadata.Suite, c.Status, c.Coverage,
                ErrorCount = c.Errors.Length, SampleCount = c.Samples.Length
            };
        }).ToArray();
        var summary = new StringBuilder("# Visual parity\n\nCounts are observations, not defects or completion percentages.\n\n");
        summary.AppendLine("| Case | Status | Samples | Equal | Different | Not compared | Errors | Overview |");
        summary.AppendLine("|---|---|---:|---:|---:|---:|---:|---|");
        foreach (var c in entries)
            summary.AppendLine($"| {c.CaseId} | {c.Status} | {c.SampleCount} | {c.Coverage.Equal} | {c.Coverage.Different} | {c.Coverage.NotCompared} | {c.ErrorCount} | [Read](cases/{c.CaseId}/overview.md) |");
        File.WriteAllText(Path.Combine(directory, "summary.md"), summary.ToString());
        WriteTemplate(directory, "Report.suite.template.html", JsonSerializer.Serialize(entries, ParityJson.Options));
    }

    static void WriteTemplate(string directory, string templateName, string data)
    {
        var template = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Results", "Templates", templateName));
        var styles = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Results", "Templates", "Report.styles.css"));
        File.WriteAllText(Path.Combine(directory, "report.html"), template
            .Replace("/*PARITY_STYLES*/", styles).Replace("/*PARITY_DATA*/", data));
    }
}
