using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Material3Expressive.TokenGenerator;

namespace Material3Expressive.TokenGenerator.Tests;

internal sealed class StringAdditionalText : AdditionalText
{
    private readonly SourceText _text;

    public StringAdditionalText(string path, string content)
    {
        Path = path;
        _text = SourceText.From(content, Encoding.UTF8);
    }

    public override string Path { get; }

    public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
    {
        return _text;
    }
}

internal sealed record GeneratorRun(
    ImmutableArray<GeneratedSourceResult> Sources,
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> TrackedSteps)
{
    public string Source(string hintName)
    {
        var result = Sources.Single(source => source.HintName == hintName);
        return result.SourceText.ToString();
    }
}

internal static class GeneratorTestHost
{
    internal const string CanonicalManifestPath = "MaterialColorUtilities.Avalonia.canonical-tokens.json";

    internal const string CanonicalManifest = """
        {
          "schema": "material-design-token-canonical-declarations",
          "schemaVersion": 1,
          "owner": "MaterialColorUtilities.Avalonia",
          "contribution": {
            "id": "MaterialColorUtilities.Avalonia.ColorTokens",
            "schemaVersion": 1
          },
          "declarations": [
            {
              "path": "System.Color.Primary",
              "layer": "System",
              "valueType": "Avalonia.Media.Color",
              "role": "Primary",
              "canonicalSymbol": "global::MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens.Primary",
              "provenance": "material-color-utilities",
              "family": "MaterialColorUtilities.Avalonia.SystemColor"
            },
            {
              "path": "System.Color.Custom.*",
              "layer": "System",
              "valueType": "Avalonia.Media.Color",
              "role": "Custom",
              "canonicalFactory": "global::MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens.Get(global::MaterialColorUtilities.Avalonia.Tokens.SysColorToken.Custom, {customName})",
              "provenance": "material-color-utilities",
              "family": "MaterialColorUtilities.Avalonia.SystemColor"
            }
          ]
        }
        """;

    public static GeneratorRun Run(params (string Path, string Content)[] tables)
    {
        return RunCore(WithManifest(tables));
    }

    public static GeneratorRun RunWithoutManifest(params (string Path, string Content)[] tables)
    {
        return RunCore(tables);
    }

    private static GeneratorRun RunCore(IEnumerable<(string Path, string Content)> tables)
    {
        var compilation = CreateCompilation();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TokenTableGenerator());
        driver = driver.AddAdditionalTexts(
            tables.Select(table => (AdditionalText)new StringAdditionalText(table.Path, table.Content))
                .ToImmutableArray());
        driver = driver.RunGenerators(compilation);

        return ReadRun(driver);
    }

    public static (GeneratorRun Before, GeneratorRun After) RunIncremental(
        (string Path, string Content)[] initial,
        (string Path, string Content) replacement)
    {
        var compilation = CreateCompilation();
        var initialTexts = WithManifest(initial)
            .Select(table => (AdditionalText)new StringAdditionalText(table.Path, table.Content))
            .ToImmutableArray();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new TokenTableGenerator().AsSourceGenerator() },
            initialTexts,
            CSharpParseOptions.Default,
            optionsProvider: null,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true,
                baseDirectory: null));

        driver = driver.RunGenerators(compilation);
        var before = ReadRun(driver);

        var oldText = initialTexts.Single(text => text.Path == replacement.Path);
        driver = driver.ReplaceAdditionalText(
            oldText,
            new StringAdditionalText(replacement.Path, replacement.Content));
        driver = driver.RunGenerators(compilation);
        return (before, ReadRun(driver));
    }

    private static IEnumerable<(string Path, string Content)> WithManifest(
        IEnumerable<(string Path, string Content)> inputs)
    {
        var materialized = inputs.ToArray();
        if (materialized.Any(static input => input.Path.EndsWith(
                ".canonical-tokens.json",
                StringComparison.OrdinalIgnoreCase)))
            return materialized;

        return materialized.Append((CanonicalManifestPath, CanonicalManifest));
    }

    private static CSharpCompilation CreateCompilation()
    {
        return CSharpCompilation.Create(
            assemblyName: "TokenGeneratorTestAssembly",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText("public static class Placeholder { }") },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorRun ReadRun(GeneratorDriver driver)
    {
        var result = driver.GetRunResult();
        var generatorResult = result.Results.Single();
        return new GeneratorRun(
            generatorResult.GeneratedSources,
            generatorResult.Diagnostics,
            generatorResult.TrackedSteps);
    }
}
