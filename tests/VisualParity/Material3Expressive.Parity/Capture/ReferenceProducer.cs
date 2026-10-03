using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Material3Expressive.Parity;

// One sequential producer process per preparation/self-test session. Every request creates a new fixture and host.
public sealed class ReferenceProducer : IAsyncDisposable
{
    readonly Process process;
    readonly StringBuilder errors = new();
    readonly Task drainErrors;
    public ScenarioIdentity[] Scenarios { get; private set; } = [];
    public Dictionary<string, string> Environment { get; private set; } = [];
    static readonly JsonSerializerOptions Wire = new(ParityJson.Options) { WriteIndented = false };

    ReferenceProducer(Process process)
    {
        this.process = process;
        drainErrors = Task.Run(async () =>
        {
            while (await process.StandardError.ReadLineAsync() is { } line)
                lock (errors)
                {
                    errors.AppendLine(line);
                    if (errors.Length > 32_768) errors.Remove(0, errors.Length - 32_768);
                }
        });
    }

    public static async Task<ReferenceProducer> Start(string root)
    {
        var library = Path.Combine(root, "tests/VisualParity/reference/build/install/Material3ParityReference/lib");
        var frozen = BuildSnapshot.Create(root, library, "compose");
        var start = new ProcessStartInfo("java")
        {
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var arg in new[] { "-Djava.awt.headless=true", "-cp", Path.Combine(frozen, "*"), "parity.MainKt", "--worker", root })
            start.ArgumentList.Add(arg);
        var producer = new ReferenceProducer(Process.Start(start) ?? throw new IOException("Could not start Compose producer."));
        try
        {
            var hello = await producer.ReadResponse();
            producer.Scenarios = hello.GetProperty("scenarios").Deserialize<ScenarioIdentity[]>(Wire)!;
            producer.Environment = hello.GetProperty("environment").Deserialize<Dictionary<string, string>>(Wire)!;
            return producer;
        }
        catch { await producer.DisposeAsync(); throw; }
    }

    public async Task<Capture> Capture(string input, string output, bool extraImages = false)
    {
        await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new { input, output, extraImages }, Wire));
        await process.StandardInput.FlushAsync();
        await ReadResponse();
        return ParityJson.Verify(output, decodePixels: false);
    }

    async Task<JsonElement> ReadResponse()
    {
        try
        {
            var line = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromMinutes(2));
            if (line is null)
            {
                await drainErrors;
                throw new IOException("Compose producer exited: " + errors);
            }
            var reply = JsonSerializer.Deserialize<JsonElement>(line, Wire);
            if (reply.TryGetProperty("error", out var error)) throw new InvalidOperationException("Compose producer: " + error.GetString());
            return reply;
        }
        catch
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        process.StandardInput.Close();
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException) { if (!process.HasExited) process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
        await drainErrors;
        process.Dispose();
    }
}
