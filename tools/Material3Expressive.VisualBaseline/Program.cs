using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Material3Expressive.VisualTesting.Compose;
using Material3Expressive.VisualTesting.Rendering;
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

        return options.Command switch
        {
            "color-contract" => PrintColorContract(),
            "compose-status" => ComposeStatus(options),
            "compose-import" => ComposeImport(options),
            "parity" => RunParity(options),
            _ => UnknownCommand(options.Command)
        };
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        return 2;
    }
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

static int RunParity(CommandLine options)
{
    VisualTestAppBuilder.BuildAvaloniaApp().SetupWithoutStarting();

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
        Material3Expressive Compose parity tool

          color-contract
          compose-status [--filter pattern]
          compose-import --source directory [--filter pattern]
          parity      [--filter pattern] [--open]

        Patterns ending in '*' match a case-ID prefix. Compose imports require capture metadata
        and exact hashes. Parity compares the current Avalonia rendering directly with the pinned
        Compose reference; it does not maintain Avalonia screenshot baselines.
        """);
}

internal sealed record CommandLine(
    string Command,
    string? Filter,
    bool Open,
    string? Source)
{
    public static CommandLine Parse(string[] arguments)
    {
        var command = arguments.FirstOrDefault() ?? "help";
        string? filter = null;
        var open = false;
        string? source = null;

        for (var index = 1; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "--filter" when index + 1 < arguments.Length:
                    filter = arguments[++index];
                    break;
                case "--open":
                    open = true;
                    break;
                case "--source" when index + 1 < arguments.Length:
                    source = arguments[++index];
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arguments[index]}'.");
            }
        }

        return new CommandLine(command.ToLowerInvariant(), filter, open, source);
    }
}
