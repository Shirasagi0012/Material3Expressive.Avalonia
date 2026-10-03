namespace Material3Expressive.Parity;

public sealed class CaptureWriter(
    CaseMetadata metadata,
    string input,
    string output,
    string root,
    bool extraImages = false,
    Dictionary<string, string>? environment = null
)
{
    readonly List<Sample> samples = [];
    readonly List<TraceEvent> trace = [];
    public List<string> Errors { get; } = [];
    public string Output { get; } = output;
    public CaseMetadata Metadata { get; } = metadata;
    public bool ExtraImages { get; } = extraImages;

    public void Record(long timeUs, string operation, string detail = "", object? data = null) =>
        trace.Add(new(trace.Count, timeUs, operation, detail,
            data is null ? null : System.Text.Json.JsonSerializer.SerializeToElement(data, ParityJson.Options)));

    public void Submit(Sample sample)
    {
        samples.Add(System.Text.Json.JsonSerializer.Deserialize<Sample>(
            System.Text.Json.JsonSerializer.Serialize(sample, ParityJson.Options), ParityJson.Options)!);
        Record(sample.TimeUs, "sample", sample.Id, new { sampleId = sample.Id });
    }

    public Capture Finish()
    {
        var capture = new Capture(4, "avalonia", Metadata.Id, ParityJson.Hash(input),
            ParityJson.Hash(Path.Combine(root, "docs/upstream-baseline.json")),
            environment is null ? EnvironmentInfo.Read(root) : new(environment),
            samples.ToArray(), trace.ToArray(), Errors.ToArray());
        capture.Validate();
        ParityJson.Write(Path.Combine(Output, "observations.json"), capture);
        ParityJson.Seal(Output, capture.Runner, capture.Environment);
        return capture;
    }
}

