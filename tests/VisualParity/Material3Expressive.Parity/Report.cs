using System.Text.Json;
namespace Material3Expressive.Parity;

public static class Report
{
    public static void Write(string directory, CaseMetadata metadata, Comparison comparison, Capture? expected, Capture? actual)
    {
        var buffers = new Dictionary<string, string>();
        foreach (var (side, capture) in new[] { ("expected", expected), ("actual", actual) })
            if (capture is not null) foreach (var sample in capture.Samples) foreach (var raster in sample.Rasters)
            {
                var path = Path.Combine(directory, side, raster.Pixels);
                if (File.Exists(path)) buffers[side + "/" + sample.Id + "/" + raster.Id] = Convert.ToBase64String(File.ReadAllBytes(path));
            }
        JsonElement? repeatability = File.Exists(Path.Combine(directory, "repeatability.json"))
            ? JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(directory, "repeatability.json"))) : null;
        var data = JsonSerializer.Serialize(new { metadata, comparison, expected, actual, buffers, repeatability }, ParityJson.Options);
        WriteTemplate(directory, "Report.template.html", data);
    }
    public static void Suite(string directory, List<Comparison> outcomes)
    {
        var cases = outcomes.Select(comparison =>
        {
            var path = Path.Combine(directory, comparison.CaseId, "case.json");
            var metadata = File.Exists(path) ? ParityJson.Read<CaseMetadata>(path) : null;
            return new { comparison.CaseId, metadata?.Title, metadata?.Family, comparison.Status, comparison.Coverage,
                ErrorCount = comparison.Errors.Length, SampleCount = comparison.Samples.Length };
        });
        WriteTemplate(directory, "Report.suite.template.html", JsonSerializer.Serialize(cases, ParityJson.Options));
    }

    static void WriteTemplate(string directory, string templateName, string data)
    {
        var template = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, templateName));
        var styles = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Report.styles.css"));
        File.WriteAllText(Path.Combine(directory, "report.html"), template
            .Replace("/*PARITY_STYLES*/", styles).Replace("/*PARITY_DATA*/", data));
    }
}
