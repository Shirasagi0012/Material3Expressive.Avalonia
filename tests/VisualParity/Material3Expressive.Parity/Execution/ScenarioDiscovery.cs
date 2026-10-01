using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Material3Expressive.Parity;

public sealed record ScenarioIdentity(string Suite, string Scenario);

public sealed class DiscoveredScenario(Type fixture, MethodInfo method, MethodInfo source, ScenarioIdentity identity)
{
    public ScenarioIdentity Identity { get; } = identity;

    internal IEnumerable<CaseMetadata> Expand()
    {
        var rows = (IEnumerable<CaseData>?)Invoke(source, null, [])
                   ?? throw new InvalidDataException($"Null data source: {fixture.Name}.{source.Name}");
        foreach (var row in rows)
        {
            if (row is null || !CaseMetadata.ValidId(row.Id))
                throw new InvalidDataException($"Invalid case data: {Identity}");
            var metadata = row.Expand(Identity.Suite, Identity.Scenario);
            metadata.Validate();
            yield return metadata;
        }
    }

    public async Task Execute(CaptureWriter writer)
    {
        writer.Metadata.Validate();
        if (writer.Metadata.Suite != Identity.Suite || writer.Metadata.Scenario != Identity.Scenario)
            throw new InvalidDataException("Case identity does not match the selected scenario.");
        // Fixture state belongs to exactly one execution, including repeatability captures.
        var instance = Activator.CreateInstance(fixture)!;
        var task = Invoke(method, instance, [writer]) as Task
                   ?? throw new InvalidOperationException($"Scenario returned no task: {Identity}");
        await task;
    }

    static object? Invoke(MethodInfo method, object? instance, object?[] arguments)
    {
        try { return method.Invoke(instance, arguments); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}

public static class ScenarioDiscovery
{
    // The caller explicitly selects the assembly/types; dependency assemblies are never scanned.
    public static IReadOnlyDictionary<ScenarioIdentity, DiscoveredScenario> Discover(IEnumerable<Type> types)
    {
        var suites = new HashSet<string>(StringComparer.Ordinal);
        var scenarios = new Dictionary<ScenarioIdentity, DiscoveredScenario>();
        foreach (var type in types.OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            var suite = type.GetCustomAttribute<ParitySuiteAttribute>();
            if (suite is null) continue;
            if (!CaseMetadata.ValidId(suite.Id) || !suites.Add(suite.Id))
                throw new InvalidDataException("Invalid or duplicate suite: " + suite.Id);
            if (!type.IsClass || !type.IsVisible || type.IsAbstract || type.ContainsGenericParameters
                || type.GetConstructor(Type.EmptyTypes) is null)
                throw new InvalidDataException($"Suite requires a public concrete class and parameterless constructor: {type}");

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                          | BindingFlags.Static | BindingFlags.DeclaredOnly);
            var count = 0;
            foreach (var method in methods)
            {
                var scenario = method.GetCustomAttribute<ParityScenarioAttribute>();
                if (scenario is null) continue;
                var identity = new ScenarioIdentity(suite.Id, scenario.Id);
                if (!CaseMetadata.ValidId(scenario.Id) || scenarios.ContainsKey(identity))
                    throw new InvalidDataException("Invalid or duplicate scenario: " + identity);
                if (!method.IsPublic || method.IsStatic || method.IsGenericMethod || method.ReturnType != typeof(Task)
                    || !method.GetParameters().Select(p => p.ParameterType).SequenceEqual([typeof(CaptureWriter)]))
                    throw new InvalidDataException($"Scenario must be public instance Task Method(CaptureWriter): {type.Name}.{method.Name}");
                var sources = methods.Where(m => m.Name == scenario.Data).ToArray();
                if (sources.Length != 1 || !sources[0].IsPublic || !sources[0].IsStatic || sources[0].IsGenericMethod
                    || sources[0].GetParameters().Length != 0
                    || !typeof(IEnumerable<CaseData>).IsAssignableFrom(sources[0].ReturnType))
                    throw new InvalidDataException($"Data source must be a unique public static IEnumerable<CaseData<T>> method: {type.Name}.{scenario.Data}");
                scenarios.Add(identity, new(type, method, sources[0], identity));
                count++;
            }
            if (count == 0) throw new InvalidDataException("Suite has no scenarios: " + suite.Id);
        }
        return scenarios;
    }

    public static CaseMetadata[] Expand(IEnumerable<DiscoveredScenario> scenarios)
    {
        var cases = scenarios.SelectMany(s => s.Expand()).OrderBy(m => m.Id, StringComparer.Ordinal).ToArray();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var metadata in cases)
            if (!ids.Add(metadata.Id)) throw new InvalidDataException("Duplicate case id: " + metadata.Id);
        return cases;
    }

    public static void RequireReferenceScenarios(IEnumerable<CaseMetadata> selected, IEnumerable<ScenarioIdentity> reference)
    {
        var available = reference.ToHashSet();
        var missing = selected.Select(m => new ScenarioIdentity(m.Suite, m.Scenario)).Distinct().Where(s => !available.Contains(s)).ToArray();
        if (missing.Length != 0)
            throw new InvalidDataException("Missing Compose scenarios: " + string.Join(", ", missing.Select(s => $"{s.Suite}/{s.Scenario}")));
    }
}
