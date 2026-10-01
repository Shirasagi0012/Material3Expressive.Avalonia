using Material3Expressive.Parity;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    static CaseMetadata Metadata => CaseMetadata.Create("test.fixture", "Fixture", "test", "direct", new NumericConfig(42));
    static string Temp() => Path.Combine(Path.GetTempPath(), "m3-parity-v2", Guid.NewGuid().ToString("N"));
    static Capture Capture(string runner, params ProbePoint[] points) => new(2, runner, Metadata.Id, "metadata", "baseline", new() { ["density"] = "1" },
        [new("initial", 0, "mounted", points, [])], [], []);
    static ProbePoint Point(params Measurement[] fields) => new("target", "container", null, fields);
    static Comparison Compare(Capture e, Capture a) => ParityComparison.Run(Metadata, e, a, "", "", Temp());
    static async Task<(Capture Capture, string Directory)> Actual(Func<CaptureWriter, Task> execute, bool extra = false)
    {
        var root = Program.FindRoot(); var m = Metadata;
        var input = Path.Combine(Temp(), "case.json"); ParityJson.Write(input, m); var d = Temp();
        var writer = new CaptureWriter(ParityJson.Read<CaseMetadata>(input), input, d, root, extra);
        await execute(writer); var capture = writer.Finish(); ParityJson.Verify(d); return (capture, d);
    }
}
