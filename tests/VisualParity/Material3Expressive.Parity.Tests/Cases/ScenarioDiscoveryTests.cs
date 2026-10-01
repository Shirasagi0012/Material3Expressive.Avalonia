using System.Text.Json;
using Material3Expressive.Parity;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public class ScenarioDiscoveryTests
{
    [Fact]
    public void ProductionCasesAreStableAndConfigurationsShareScenarios()
    {
        var types = typeof(Program).Assembly.GetTypes();
        var first = ScenarioDiscovery.Expand(ScenarioDiscovery.Discover(types).Values);
        var reversed = ScenarioDiscovery.Expand(ScenarioDiscovery.Discover(types.Reverse()).Values);
        Assert.Equal(new[] { "button.filled.disabled", "button.filled.enabled", "button.filled.press", "motion.interrupted",
            "protocol.direct", "surface.static", "text.paragraph-breaks", "text.wrapped" }, first.Select(m => m.Id));
        Assert.Equal(JsonSerializer.Serialize(first, ParityJson.Options), JsonSerializer.Serialize(reversed, ParityJson.Options));
        var buttons = first.Where(m => m.Scenario == "static" && m.Suite == "button").ToArray();
        Assert.Equal(new[] { false, true }, buttons.Select(m => m.ReadConfig<ButtonConfig>().Enabled));
    }

    [Fact]
    public async Task DiscoveryAndExpansionDoNotConstructFixturesAndExecutionUsesFreshInstances()
    {
        IsolationSuite.Constructors = 0;
        IsolationSuite.SourceCalls = 0;
        var scenarios = ScenarioDiscovery.Discover([typeof(IsolationSuite)]);
        Assert.Equal(0, IsolationSuite.SourceCalls);
        var metadata = Assert.Single(ScenarioDiscovery.Expand(scenarios.Values));
        Assert.Equal(1, IsolationSuite.SourceCalls);
        Assert.Equal(0, IsolationSuite.Constructors);
        var scenario = Assert.Single(scenarios.Values);
        for (var i = 0; i < 2; i++)
        {
            var writer = Writer(metadata);
            await scenario.Execute(writer);
            Assert.Equal(1, writer.Finish().Samples.Single().Points.Single().Measurements.Single().Numbers!.Single());
        }
        Assert.Equal(2, IsolationSuite.Constructors);
    }

    [Theory]
    [InlineData(typeof(MissingSource))]
    [InlineData(typeof(WrongSourceType))]
    [InlineData(typeof(InstanceSource))]
    [InlineData(typeof(InvalidSignature))]
    [InlineData(typeof(NoConstructor))]
    [InlineData(typeof(NoScenarios))]
    [InlineData(typeof(DuplicateScenario))]
    public void InvalidFixtureContractsFailBeforeExecution(Type fixture) =>
        Assert.Throws<InvalidDataException>(() => ScenarioDiscovery.Discover([fixture]));

    [Fact]
    public void DuplicateSuiteAndCrossScenarioCaseIdsAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => ScenarioDiscovery.Discover([typeof(IsolationSuite), typeof(SameSuite)]));
        var scenarios = ScenarioDiscovery.Discover([typeof(DuplicateCase)]);
        Assert.Throws<InvalidDataException>(() => ScenarioDiscovery.Expand(scenarios.Values));
    }

    [Fact]
    public void InvalidRowsAndNullDataAreRejectedDuringExpansion()
    {
        Assert.Throws<InvalidDataException>(() => ScenarioDiscovery.Expand(ScenarioDiscovery.Discover([typeof(InvalidRow)]).Values));
        Assert.Throws<InvalidDataException>(() => ScenarioDiscovery.Expand(ScenarioDiscovery.Discover([typeof(NullData)]).Values));
    }

    [Fact]
    public async Task InvocationUnwrapsFailuresAndRejectsMismatchedMetadata()
    {
        var scenarios = ScenarioDiscovery.Discover([typeof(ThrowingSuite)]);
        var metadata = Assert.Single(ScenarioDiscovery.Expand(scenarios.Values));
        var scenario = Assert.Single(scenarios.Values);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.Execute(Writer(metadata)));
        Assert.Equal("scenario failure", error.Message);
        await Assert.ThrowsAsync<InvalidDataException>(() => scenario.Execute(Writer(metadata with { Scenario = "other" })));
    }

    [Fact]
    public void PreflightChecksOnlySelectedScenariosAndNamesMissingOnes()
    {
        var selected = new[] { CaseMetadata.Create("button.fixture", "Fixture", "button", "static", new EmptyConfig()) };
        ScenarioDiscovery.RequireReferenceScenarios(selected, [new("button", "static")]);
        var error = Assert.Throws<InvalidDataException>(() => ScenarioDiscovery.RequireReferenceScenarios(selected, [new("button", "press")]));
        Assert.Contains("button/static", error.Message);
    }

    static CaptureWriter Writer(CaseMetadata metadata)
    {
        var directory = Path.Combine(Path.GetTempPath(), "m3-parity-discovery", Guid.NewGuid().ToString("N"));
        var input = Path.Combine(directory, "case.json");
        ParityJson.Write(input, metadata);
        return new(metadata, input, Path.Combine(directory, "actual"), Program.FindRoot());
    }

    [ParitySuite("isolation")]
    public sealed class IsolationSuite
    {
        public static int Constructors;
        public static int SourceCalls;
        int executions;
        public IsolationSuite() => Constructors++;
        public static IEnumerable<CaseData<EmptyConfig>> Data()
        {
            SourceCalls++;
            return [new("sample", "Isolation", new())];
        }
        [ParityScenario("run", Data = nameof(Data))]
        public async Task Run(CaptureWriter writer)
        {
            await Task.Yield();
            writer.Submit(new("initial", 0, "fixture", [new("instance", "fixture", null,
                [Measurement.Numeric("executions", ++executions)])], []));
        }
    }

    [ParitySuite("isolation")]
    public sealed class SameSuite;

    [ParitySuite("missing")]
    public sealed class MissingSource
    {
        [ParityScenario("run", Data = "Unknown")]
        public Task Run(CaptureWriter writer) => Task.CompletedTask;
    }

    [ParitySuite("wrong")]
    public sealed class WrongSourceType
    {
        public static IEnumerable<int> Data() => [1];
        [ParityScenario("run", Data = nameof(Data))]
        public Task Run(CaptureWriter writer) => Task.CompletedTask;
    }

    [ParitySuite("instance")]
    public sealed class InstanceSource
    {
        public IEnumerable<CaseData<EmptyConfig>> Data() => [];
        [ParityScenario("run", Data = nameof(Data))]
        public Task Run(CaptureWriter writer) => Task.CompletedTask;
    }

    [ParitySuite("signature")]
    public sealed class InvalidSignature
    {
        public static IEnumerable<CaseData<EmptyConfig>> Data() => [];
        [ParityScenario("run", Data = nameof(Data))]
        public static Task Run(CaptureWriter writer) => Task.CompletedTask;
    }

    [ParitySuite("constructor")]
    public sealed class NoConstructor { public NoConstructor(int value) { } }

    [ParitySuite("empty")]
    public sealed class NoScenarios;

    [ParitySuite("duplicate")]
    public sealed class DuplicateScenario
    {
        public static IEnumerable<CaseData<EmptyConfig>> Data() => [];
        [ParityScenario("run", Data = nameof(Data))]
        public Task First(CaptureWriter writer) => Task.CompletedTask;
        [ParityScenario("run", Data = nameof(Data))]
        public Task Second(CaptureWriter writer) => Task.CompletedTask;
    }

    [ParitySuite("duplicate")]
    public sealed class DuplicateCase
    {
        public static IEnumerable<CaseData<EmptyConfig>> Data() => [new("same", "Same", new())];
        [ParityScenario("first", Data = nameof(Data))]
        public Task First(CaptureWriter writer) => Task.CompletedTask;
        [ParityScenario("second", Data = nameof(Data))]
        public Task Second(CaptureWriter writer) => Task.CompletedTask;
    }

    [ParitySuite("invalid")]
    public sealed class InvalidRow
    {
        public static IEnumerable<CaseData<EmptyConfig>> Data() => [new("../escape", "Invalid", new())];
        [ParityScenario("run", Data = nameof(Data))]
        public Task Run(CaptureWriter writer) => Task.CompletedTask;
    }

    [ParitySuite("null")]
    public sealed class NullData
    {
        public static IEnumerable<CaseData<EmptyConfig>> Data() => null!;
        [ParityScenario("run", Data = nameof(Data))]
        public Task Run(CaptureWriter writer) => Task.CompletedTask;
    }

    [ParitySuite("throwing")]
    public sealed class ThrowingSuite
    {
        public static IEnumerable<CaseData<EmptyConfig>> Data() => [new("sample", "Failure", new())];
        [ParityScenario("run", Data = nameof(Data))]
        public Task Run(CaptureWriter writer) => throw new InvalidOperationException("scenario failure");
    }
}
