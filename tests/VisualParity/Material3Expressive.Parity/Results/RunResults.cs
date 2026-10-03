using System.Text.Json;
using System.Text.Json.Nodes;

namespace Material3Expressive.Parity;

// Progress is append-only. Only compact case summaries survive a case; full facts and pixels do not.
public sealed class RunResults(string directory, int selectedCases, string command)
{
    readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;
    int completed;
    public Dictionary<string, double> TimingsMs { get; } = [];
    static readonly JsonSerializerOptions Compact = new(ParityJson.Options) { WriteIndented = false };
    const string Header = "# Visual parity\n\nCounts are observations, not defects or completion percentages.\n\n| Case | Status | Samples | Equal | Within tolerance | Different | Not compared | Errors | Overview |\n|---|---|---:|---:|---:|---:|---:|---:|---|\n";

    public void Add(CaseResult result, Dictionary<string, double>? timingsMs = null)
    {
        var c = result.Comparison;
        var entry = new
        {
            id = c.CaseId, result.Metadata.Title, result.Metadata.Suite, c.Status, c.Coverage,
            samples = c.Samples.Length, errors = c.Errors.Length,
            repeatability = result.Repeatability is null ? "notChecked"
                : result.Repeatability["results"]!.AsObject().Any(p => p.Value!.AsArray().Count != 0) ? "unstable" : "stable",
            timingsMs
        };
        File.AppendAllText(Path.Combine(directory, "progress.jsonl"), JsonSerializer.Serialize(entry, Compact) + "\n");
        File.AppendAllText(Path.Combine(directory, "summary.md"),
            $"| {c.CaseId} | {c.Status} | {c.Samples.Length} | {c.Coverage.Equal} | {c.Coverage.WithinTolerance} | {c.Coverage.Different} | {c.Coverage.NotCompared} | {c.Errors.Length} | [Read](cases/{c.CaseId}/overview.md) |\n");
        foreach (var (phase, duration) in timingsMs ?? []) TimingsMs[phase] = TimingsMs.GetValueOrDefault(phase) + duration;
        completed++;
    }

    public void Save(string state)
    {
        Directory.CreateDirectory(directory);
        if (!File.Exists(Path.Combine(directory, "summary.md"))) File.WriteAllText(Path.Combine(directory, "summary.md"), Header);
        var temporary = Path.Combine(directory, "run.yaml.tmp");
        // The on-disk running index has a fixed size; readers use the committed progress journal.
        var cases = state == "completed" && completed != 0
            ? File.ReadLines(Path.Combine(directory, "progress.jsonl")).Select(line => JsonNode.Parse(line)!).ToArray() : [];
        ResultYaml.Write(temporary, ResultYaml.Node(new
        {
            schemaVersion = 8, id = new DirectoryInfo(directory).Name, command, state, startedAt,
            finishedAt = state == "completed" ? (DateTimeOffset?)DateTimeOffset.UtcNow : null,
            selectedCases, completedCases = completed, progress = "progress.jsonl", timingsMs = TimingsMs, cases
        })!);
        File.Move(temporary, Path.Combine(directory, "run.yaml"), overwrite: true);
    }
}
