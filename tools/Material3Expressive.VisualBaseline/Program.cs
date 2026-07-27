using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Material3Expressive.VisualTesting;
using Material3Expressive.VisualTesting.Cases;
using Material3Expressive.VisualTesting.Compose;
using Material3Expressive.VisualTesting.Rendering;
using Material3Expressive.VisualTesting.Reporting;
using Material3Expressive.VisualTesting.Storage;
using MaterialColorUtilities.Avalonia;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialColorUtilities.DynamicColors;

return Run(args);

static int Run(string[] arguments)
{
    try
    {
        var options = CommandLine.Parse(arguments);
        if (options.Command is "help" or "--help" or "-h")
        {
            PrintHelp();
            return 0;
        }

        if (options.Command == "list")
            return ListCases(options);
        if (options.Command == "environment")
            return PrintEnvironment();
        if (options.Command == "color-contract")
            return PrintColorContract();
        if (options.Command == "report")
            return WriteReport(options);
        if (options.Command == "review")
            return WriteReview(options);
        if (options.Command == "compose-status")
            return ComposeStatus(options);
        if (options.Command == "compose-import")
            return ComposeImport(options);

        VisualTestAppBuilder.BuildAvaloniaApp().SetupWithoutStarting();
        return options.Command switch
        {
            "record" => Record(options),
            "approve" => Approve(options),
            "verify" => Verify(options),
            "probe" => Probe(options),
            "parity" => Parity(options),
            _ => UnknownCommand(options.Command)
        };
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        return 2;
    }
}

static int ListCases(CommandLine options)
{
    var cases = SelectCases(options);
    foreach (var item in cases)
        Console.WriteLine($"{item.Id} [{string.Join(", ", item.Tags)}]");
    Console.WriteLine($"{cases.Count} visual case(s).");
    return 0;
}

static int PrintEnvironment()
{
    Console.Write(VisualEnvironmentFingerprint.Capture().ToJson());
    return 0;
}

static int PrintColorContract()
{
    var seed = Avalonia.Media.Color.FromRgb(0x67, 0x50, 0xA4);
    var scheme = new TonalSpotScheme(seed)
    {
        SpecVersion = ColorSpec.SpecVersion.Spec2021,
        Platform = DynamicScheme.Platform.Phone
    };
    var resolved = new ResolvedColorScheme(scheme);
    var roles = Enum.GetValues<SysColorToken>()
        .Where(token => token < SysColorToken.Custom)
        .ToDictionary(
            token => token.ToString(),
            token => new
            {
                light = ToHex(resolved.GetColor(token, isDark: false)),
                dark = ToHex(resolved.GetColor(token, isDark: true))
            },
            StringComparer.Ordinal);
    var contract = new
    {
        schemaVersion = 1,
        id = "tonal-spot-spec2021-phone-seed-6750a4",
        seed = "#6750A4",
        variant = "TonalSpot",
        specVersion = "Spec2021",
        platform = "Phone",
        roles
    };

    Console.WriteLine(JsonSerializer.Serialize(
        contract,
        new JsonSerializerOptions { WriteIndented = true }));
    return 0;

    static string ToHex(Avalonia.Media.Color color) =>
        $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}

static int Record(CommandLine options)
{
    var cases = SelectCases(options);
    var runner = new VisualTestRunner();
    foreach (var item in cases)
    {
        runner.Record(item);
        Console.WriteLine($"RECORDED {item.Id}");
    }

    Console.WriteLine($"Recorded {cases.Count} candidate baseline(s) in {runner.Paths.BaselineRoot}");
    return 0;
}

static int Verify(CommandLine options)
{
    var cases = SelectCases(options);
    var runner = new VisualTestRunner();
    var failures = 0;
    var pending = 0;

    foreach (var item in cases)
    {
        var result = runner.Verify(item);
        var label = result.Status switch
        {
            VisualVerificationStatus.Passed => "PASS",
            VisualVerificationStatus.Failed => "FAIL",
            _ => "PENDING"
        };
        Console.WriteLine($"{label} {item.Id}: {result.Summary}");
        if (result.IsEnforcedFailure)
        {
            failures++;
            if (result.ArtifactDirectory is not null)
                Console.WriteLine($"     artifacts: {result.ArtifactDirectory}");
        }
        else if (result.RequiresApproval)
        {
            pending++;
            if (result.ArtifactDirectory is not null)
                Console.WriteLine($"     review artifacts: {result.ArtifactDirectory}");
        }
    }

    if (options.Filter is null && options.Set is null)
    {
        var orphans = runner.FindOrphanedBaselines(VisualCaseCatalog.All);
        foreach (var orphan in orphans)
        {
            failures++;
            Console.WriteLine($"ORPHAN {orphan}");
        }
    }

    Console.WriteLine(
        $"{cases.Count - failures - pending} passed, {pending} awaiting user approval, " +
        $"{failures} enforced failure(s).");
    return failures == 0 ? 0 : 1;
}

static int Approve(CommandLine options)
{
    if (!options.ConfirmReviewed)
    {
        throw new InvalidOperationException(
            "Approval requires --confirm-reviewed. Use 'review --open' and personally inspect " +
            "every selected candidate before approving it.");
    }

    var cases = SelectCases(options);
    var runner = new VisualTestRunner();
    foreach (var item in cases)
    {
        runner.Approve(item);
        Console.WriteLine($"APPROVED {item.Id}");
    }

    Console.WriteLine($"Approved {cases.Count} exact candidate image(s).");
    return 0;
}

static int Probe(CommandLine options)
{
    var selected = SelectCases(options);
    var visualCase = selected.FirstOrDefault()
        ?? throw new InvalidOperationException("The determinism probe needs one matching case.");
    var renderer = new VisualRenderer();
    var hashes = new HashSet<string>(StringComparer.Ordinal);
    string? firstHash = null;

    for (var iteration = 1; iteration <= options.Iterations; iteration++)
    {
        var rendered = renderer.Render(visualCase);
        var hash = Convert.ToHexString(SHA256.HashData(rendered.Pixels));
        hashes.Add(hash);
        firstHash ??= hash;

        if (iteration == 1)
            Console.WriteLine($"reference: {hash}");
        else if (!string.Equals(firstHash, hash, StringComparison.Ordinal))
            Console.WriteLine($"mismatch at capture {iteration}: {hash}");
        else if (iteration % 25 == 0 || iteration == options.Iterations)
            Console.WriteLine($"checked {iteration}/{options.Iterations}");
    }

    if (hashes.Count != 1)
    {
        Console.Error.WriteLine(
            $"Determinism failed: {options.Iterations} captures produced {hashes.Count} hashes.");
        return 1;
    }

    Console.WriteLine(
        $"Determinism passed: {options.Iterations} captures of {visualCase.Id} were identical.");
    return 0;
}

static int WriteReport(CommandLine options)
{
    var paths = new Material3Expressive.VisualTesting.Storage.RepositoryPaths();
    var path = new VisualReportIndexWriter(paths).Write();
    Console.WriteLine(path);

    if (options.Open)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Console.WriteLine("Opened the local report.");
    }

    return 0;
}

static int WriteReview(CommandLine options)
{
    var cases = SelectCases(options);
    var runner = new VisualTestRunner();
    var path = new BaselineReviewReportWriter(runner.Paths).Write(
        cases,
        runner.GetApprovalStates(cases));
    Console.WriteLine(path);

    if (options.Open)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Console.WriteLine("Opened the local candidate review.");
    }

    return 0;
}

static int ComposeStatus(CommandLine options)
{
    var store = new ComposeReferenceStore(new RepositoryPaths());
    var manifest = store.ReadManifest();
    var cases = SelectComposeCases(manifest, options);
    var states = store.GetStates(cases);

    Console.WriteLine(
        $"{manifest.ReferenceSetId}: Material3 {manifest.ComposeMaterial3Version}, " +
        $"Android API {manifest.Environment.AndroidApi}, " +
        $"{manifest.Environment.DensityDpi} dpi, {manifest.Environment.FontFamily}, " +
        $"{manifest.Environment.EmulatorProfile}");
    foreach (var referenceCase in cases)
        Console.WriteLine($"{states[referenceCase.Id].ToString().ToUpperInvariant()} {referenceCase.Id}");

    var invalid = states.Values.Count(
        state => state is ComposeReferenceState.HashMismatch or ComposeReferenceState.MissingIndex);
    var ready = states.Values.Count(state => state == ComposeReferenceState.Ready);
    var missing = states.Values.Count(state => state == ComposeReferenceState.Missing);
    Console.WriteLine($"{ready} ready, {missing} awaiting capture, {invalid} invalid.");
    return invalid == 0 ? 0 : 1;
}

static int ComposeImport(CommandLine options)
{
    if (string.IsNullOrWhiteSpace(options.Source))
        throw new ArgumentException("compose-import requires --source <capture-directory>.");

    var paths = new RepositoryPaths();
    var store = new ComposeReferenceStore(paths);
    var manifest = store.ReadManifest();
    var cases = SelectComposeCases(manifest, options);
    var imported = new ComposeReferenceImporter(store).Import(options.Source, cases);
    foreach (var caseId in imported)
        Console.WriteLine($"IMPORTED {caseId}");
    Console.WriteLine(
        $"Imported {imported.Count} verified Compose reference(s) into " +
        $"{paths.ComposeReferenceRoot}");
    return 0;
}

static int Parity(CommandLine options)
{
    var runner = new ComposeParityRunner();
    var manifest = runner.Manifest;
    var cases = SelectComposeCases(manifest, options);
    var results = new List<ComposeParityResult>();
    foreach (var referenceCase in cases)
    {
        var result = runner.Compare(referenceCase);
        results.Add(result);
        Console.WriteLine($"{result.Status.ToString().ToUpperInvariant()} {result.CaseId}: {result.Summary}");
    }

    var report = runner.WriteIndex(results, manifest);
    Console.WriteLine($"Parity report: {report}");
    if (options.Open)
    {
        Process.Start(new ProcessStartInfo(report) { UseShellExecute = true });
        Console.WriteLine("Opened the local parity report.");
    }

    var invalid = results.Count(item => item.HasInvalidInput);
    Console.WriteLine(
        $"{results.Count(item => item.HasComparison)} compared, " +
        $"{results.Count(item => item.Status == ComposeParityStatus.ReferenceMissing)} awaiting capture, " +
        $"{invalid} invalid. No automatic cross-renderer threshold is applied.");
    return invalid == 0 ? 0 : 1;
}

static IReadOnlyList<Material3Expressive.VisualTesting.Model.VisualCase> SelectCases(
    CommandLine options)
{
    var cases = VisualCaseCatalog.Filter(options.Filter, options.Set);
    if (cases.Count == 0)
        throw new InvalidOperationException("No visual cases matched the supplied filter.");
    return cases;
}

static IReadOnlyList<ComposeReferenceCase> SelectComposeCases(
    ComposeReferenceManifest manifest,
    CommandLine options)
{
    IEnumerable<ComposeReferenceCase> cases = manifest.Cases;
    if (!string.IsNullOrWhiteSpace(options.Filter))
    {
        cases = cases.Where(item =>
            options.Filter.EndsWith('*')
                ? item.Id.StartsWith(options.Filter[..^1], StringComparison.OrdinalIgnoreCase)
                : item.Id.Contains(options.Filter, StringComparison.OrdinalIgnoreCase));
    }

    var selected = cases.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
    if (selected.Length == 0)
        throw new InvalidOperationException("No Compose reference cases matched the supplied filter.");
    return selected;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintHelp();
    return 2;
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        Material3Expressive visual baseline tool

          list        [--filter pattern] [--set tag]
          environment
          color-contract
          record      [--filter pattern] [--set tag]
          review      [--filter pattern] [--set tag] [--open]
          approve     [--filter pattern] [--set tag] --confirm-reviewed
          verify      [--filter pattern] [--set tag]
          probe       [--filter pattern] [--iterations 100]
          report      [--open]
          compose-status [--filter pattern]
          compose-import --source directory [--filter pattern]
          parity      [--filter pattern] [--open]

        Common sets are smoke, full, interaction, parity, and motion; component tags also work.
        Patterns ending in '*' match a case-ID prefix. Recording writes unapproved candidates.
        Approval is bound to the exact PNG hash and requires explicit confirmation. Verify and
        probe never modify candidates or approvals. Compose imports require capture metadata and
        exact hashes. Parity is diagnostic until reviewed thresholds are established.
        """);
}

internal sealed record CommandLine(
    string Command,
    string? Filter,
    string? Set,
    int Iterations,
    bool Open,
    bool ConfirmReviewed,
    string? Source)
{
    public static CommandLine Parse(string[] arguments)
    {
        var command = arguments.FirstOrDefault() ?? "help";
        string? filter = null;
        string? set = null;
        var iterations = 100;
        var open = false;
        var confirmReviewed = false;
        string? source = null;

        for (var index = 1; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "--filter" when index + 1 < arguments.Length:
                    filter = arguments[++index];
                    break;
                case "--set" when index + 1 < arguments.Length:
                    set = arguments[++index];
                    break;
                case "--iterations" when index + 1 < arguments.Length:
                    if (!int.TryParse(arguments[++index], out iterations) || iterations <= 0)
                        throw new ArgumentException("--iterations must be a positive integer.");
                    break;
                case "--open":
                    open = true;
                    break;
                case "--confirm-reviewed":
                    confirmReviewed = true;
                    break;
                case "--source" when index + 1 < arguments.Length:
                    source = arguments[++index];
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arguments[index]}'.");
            }
        }

        return new CommandLine(
            command.ToLowerInvariant(),
            filter,
            set,
            iterations,
            open,
            confirmReviewed,
            source);
    }
}
