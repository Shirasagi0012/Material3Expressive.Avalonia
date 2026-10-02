using System.Text;

namespace Material3Expressive.Parity;

// A small, atomically published navigation index. Detailed facts remain in sample YAML.
public sealed class RunResults(string directory, int selectedCases, string command)
{
    readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;
    readonly List<object> cases = [];
    readonly StringBuilder summary = new("# Visual parity\n\nCounts are observations, not defects or completion percentages.\n\n| Case | Status | Samples | Equal | Within tolerance | Different | Not compared | Errors | Overview |\n|---|---|---:|---:|---:|---:|---:|---:|---|\n");

    public void Add(CaseResult result)
    {
        var c = result.Comparison;
        cases.Add(new
        {
            id = c.CaseId, result.Metadata.Title, result.Metadata.Suite, c.Status, c.Coverage,
            samples = c.Samples.Length, errors = c.Errors.Length,
            repeatability = result.Repeatability is null ? "notChecked"
                : result.Repeatability["results"]!.AsObject().Any(p => p.Value!.AsArray().Count != 0) ? "unstable" : "stable"
        });
        summary.AppendLine($"| {c.CaseId} | {c.Status} | {c.Samples.Length} | {c.Coverage.Equal} | {c.Coverage.WithinTolerance} | {c.Coverage.Different} | {c.Coverage.NotCompared} | {c.Errors.Length} | [Read](cases/{c.CaseId}/overview.md) |");
    }

    public void Save(string state)
    {
        var temporary = Path.Combine(directory, "run.yaml.tmp");
        ResultYaml.Write(temporary, ResultYaml.Node(new
        {
            schemaVersion = 8, id = new DirectoryInfo(directory).Name, command, state, startedAt,
            finishedAt = state == "completed" ? (DateTimeOffset?)DateTimeOffset.UtcNow : null,
            selectedCases, cases
        })!);
        File.Move(temporary, Path.Combine(directory, "run.yaml"), overwrite: true);
        File.WriteAllText(Path.Combine(directory, "summary.md"), summary.ToString());
    }
}
