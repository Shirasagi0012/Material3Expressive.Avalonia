using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Material3Expressive.TokenGenerator.Emit;
using Material3Expressive.TokenGenerator.Json;
using Material3Expressive.TokenGenerator.Model;
using Material3Expressive.TokenGenerator.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Material3Expressive.TokenGenerator;

/// <summary>
/// Generates the token layer from the reference, system and raw component tables under
/// <c>Tokens/</c>.
/// </summary>
/// <remarks>
/// The tables are the single source of truth for raw token facts. Component-owned C# Defaults
/// select and resolve those facts into semantic values; the generator deliberately does not model
/// component state, variants, or template bindings.
/// </remarks>
[Generator]
public sealed class TokenTableGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Only files with one of the three layer suffixes are tables; other AdditionalFiles are
    /// ignored. The root <c>kind</c> remains the semantic authority, while the suffix provides a
    /// discoverable, human-readable classification and is checked against that kind below.
    /// </summary>
    private static readonly string[] TableSuffixes =
    {
        ".ref.jsonc",
        ".sys.jsonc",
        ".comp.jsonc"
    };

    private static readonly DiagnosticDescriptor InvalidTable = new(
        "M3ETOK001",
        "Invalid token table",
        "Token table '{0}' could not be read: {1}",
        "Material3Expressive",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor DuplicateTable = new(
        "M3ETOK002",
        "Duplicate token table",
        "Token table '{0}' declares the name '{1}', which another table already declares",
        "Material3Expressive",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidReference = new(
        "M3ETOK003",
        "Invalid token reference",
        "Token table '{0}' has invalid reference '{1}': {2}",
        "Material3Expressive",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor FileKindMismatch = new(
        "M3ETOK004",
        "Token filename kind mismatch",
        "Token table '{0}' has kind '{1}', but suffix '{2}' requires kind '{3}'",
        "Material3Expressive",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor InvalidExternalDeclarations = new(
        "M3ETOK005",
        "Invalid external canonical declarations",
        "External canonical declarations '{0}' are unavailable or incompatible: {1}",
        "Material3Expressive",
        DiagnosticSeverity.Error,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Carry one source through the pipeline at a time. The parsed result compares by path and
        // content, so an unrelated edit reuses every other table's parse result before the global
        // catalog/accessor pass runs.
        var tableItems = context.AdditionalTextsProvider
            .Where(text => TableSuffixes.Any(suffix =>
                text.Path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            .Select((text, token) => new TableSource(
                text.Path,
                text.GetText(token)?.ToString() ?? string.Empty))
            .WithTrackingName("TokenTable.Source")
            .Select((source, _) => Parse(source))
            .WithTrackingName("TokenTable.Parse");

        // The catalog only needs declarations: member names, types, table ownership and
        // cross-table references. Literal body changes therefore keep this global step cached.
        var declarations = tableItems
            .Select((result, _) => new TableDeclarationSource(
                result.Source.Path,
                result.Table is { } table ? new TokenTableDeclaration(table) : null))
            .WithTrackingName("TokenTable.Declaration");

        var manifests = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(
                ExternalCanonicalManifest.FileSuffix,
                StringComparison.OrdinalIgnoreCase))
            .Select((text, token) => new ManifestSource(
                text.Path,
                text.GetText(token)?.ToString() ?? string.Empty))
            .WithTrackingName("ExternalCanonicalManifest.Source")
            .Select((source, _) => ParseManifest(source))
            .WithTrackingName("ExternalCanonicalManifest.Parse")
            .Collect()
            .Select((items, _) => SelectManifest(items))
            .WithTrackingName("ExternalCanonicalManifest.Validation");

        var validation = declarations
            .Collect()
            .Combine(manifests)
            .Select((pair, _) => BuildValidation(pair.Left, pair.Right))
            .WithTrackingName("TokenTable.Validation");

        var allTables = tableItems.Collect();

        // Each table has its own source-output node. Roslyn can now reuse every unchanged table's
        // emitted source without a generated global component snapshot.
        context.RegisterSourceOutput(
            tableItems.Combine(validation),
            static (production, pair) => EmitTable(production, pair.Left, pair.Right));

        context.RegisterSourceOutput(
            allTables.Combine(validation),
            static (production, pair) => ReportDiagnostics(production, pair.Left, pair.Right));

        context.RegisterSourceOutput(
            allTables.Combine(validation),
            static (production, pair) => EmitCatalog(production, pair.Left, pair.Right));

    }

    private static ParsedManifest ParseManifest(ManifestSource source)
    {
        try
        {
            return new ParsedManifest(source, ExternalCanonicalManifest.Parse(source.Content), error: null);
        }
        catch (Exception exception) when (exception is InvalidManifestException or JsonException)
        {
            return new ParsedManifest(source, manifest: null, exception.Message);
        }
    }

    private static ManifestSnapshot SelectManifest(ImmutableArray<ParsedManifest> items)
    {
        if (items.Length == 0)
            return new ManifestSnapshot(
                manifest: null,
                "<missing>",
                $"exactly one '*{ExternalCanonicalManifest.FileSuffix}' AdditionalFile is required.");

        if (items.Length != 1)
        {
            var names = string.Join(", ", items
                .OrderBy(static item => item.Source.Path, StringComparer.Ordinal)
                .Select(static item => item.Source.DisplayName));
            return new ManifestSnapshot(manifest: null, names, "exactly one manifest is allowed.");
        }

        var item = items[0];
        return item.Manifest is { } manifest
            ? new ManifestSnapshot(manifest, item.Source.DisplayName, error: null)
            : new ManifestSnapshot(manifest: null, item.Source.DisplayName, item.Error);
    }

    private static ParsedTable Parse(TableSource source)
    {
        try
        {
            var table = TokenTable.Parse(source.Content);
            if (TryGetFileKind(source.Path, out var fileKind) &&
                !IsMatchingKind(fileKind, table.Category))
            {
                return new ParsedTable(
                    source,
                    table: null,
                    error: null,
                    kindMismatch: new FileKindMismatchInfo(
                        table.Kind,
                        fileKind.Suffix,
                        fileKind.Kind));
            }

            return new ParsedTable(source, table, error: null);
        }
        catch (Exception exception) when (exception is InvalidTableException or JsonException)
        {
            return new ParsedTable(source, table: null, exception.Message);
        }
    }

    private static bool TryGetFileKind(string path, out FileKindInfo fileKind)
    {
        foreach (var suffix in TableSuffixes)
        {
            if (!path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var kind = suffix switch
            {
                ".ref.jsonc" => "referenceTokens",
                ".sys.jsonc" => "systemTokens",
                ".comp.jsonc" => "componentTokens",
                _ => throw new InvalidOperationException("Unknown token table suffix.")
            };

            fileKind = new FileKindInfo(suffix, kind);
            return true;
        }

        fileKind = default;
        return false;
    }

    private static bool IsMatchingKind(FileKindInfo fileKind, TokenTableCategory category)
    {
        return fileKind.Kind switch
        {
            "referenceTokens" => category == TokenTableCategory.ReferenceTokens,
            "systemTokens" => category == TokenTableCategory.SystemTokens,
            "componentTokens" => category == TokenTableCategory.ComponentTokens,
            _ => false
        };
    }

    private static void EmitTable(SourceProductionContext context, ParsedTable result,
        ValidationSnapshot validation)
    {
        if (validation.Manifest is null || result.Table is not { } table ||
            validation.InvalidPaths.Contains(result.Source.Path))
            return;

        context.AddSource(
            $"{table.Name}.Tokens.g.cs",
            SourceText.From(TableEmitter.Emit(table), System.Text.Encoding.UTF8));
    }

    private static void ReportDiagnostics(SourceProductionContext context, ImmutableArray<ParsedTable> sources,
        ValidationSnapshot validation)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var result in sources.OrderBy(s => s.Source.Path, StringComparer.Ordinal))
        {
            if (result.KindMismatch is { } mismatch)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    FileKindMismatch,
                    Location.None,
                    result.Source.DisplayName,
                    mismatch.Kind,
                    mismatch.Suffix,
                    mismatch.ExpectedKind));
                continue;
            }

            if (result.Table is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidTable, Location.None, result.Source.DisplayName, result.Error));
                continue;
            }

            var table = result.Table;
            if (seen.TryGetValue(table.Name, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DuplicateTable, Location.None, result.Source.DisplayName, table.Name));
                continue;
            }

            seen[table.Name] = result.Source.DisplayName;
        }

        foreach (var issue in validation.Issues)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidReference,
                Location.None,
                issue.TableName,
                issue.Reference,
                issue.Message));
        }

        if (validation.ManifestError is { } manifestError)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidExternalDeclarations,
                Location.None,
                validation.ManifestDisplayName,
                manifestError));
        }

        foreach (var issue in validation.ExternalIssues)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidExternalDeclarations,
                Location.None,
                validation.ManifestDisplayName,
                $"token table '{issue.TableName}' {issue.Kind} '{issue.Reference}': {issue.Message}"));
        }

    }

    private static void EmitCatalog(
        SourceProductionContext context,
        ImmutableArray<ParsedTable> sources,
        ValidationSnapshot validation)
    {
        if (validation.Manifest is not { } manifest ||
            validation.InvalidPaths.Length != 0 ||
            validation.Issues.Length != 0 ||
            validation.ExternalIssues.Length != 0 ||
            !sources.Any(static source => source.Table?.IsTokenSet == true) ||
            sources.Any(static source => source.Table is null || source.KindMismatch is not null))
            return;

        var tables = sources
            .Select(static source => source.Table!)
            .OrderBy(static table => table.Name, StringComparer.Ordinal)
            .ThenBy(static table => table.Namespace, StringComparer.Ordinal)
            .ToArray();
        context.AddSource(
            CatalogEmitter.HintName,
            SourceText.From(CatalogEmitter.Emit(tables, manifest), System.Text.Encoding.UTF8));
    }

    private static ValidationSnapshot BuildValidation(
        ImmutableArray<TableDeclarationSource> declarations,
        ManifestSnapshot manifestSnapshot)
    {
        var invalidPaths = new HashSet<string>(StringComparer.Ordinal);
        var accepted = new List<TableDeclarationSource>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in declarations.Where(item => item.Declaration is not null)
                     .OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            if (!seen.Add(item.Declaration!.Name))
            {
                invalidPaths.Add(item.Path);
                continue;
            }

            accepted.Add(item);
        }

        var acceptedTables = accepted.Select(item => item.Declaration!).ToArray();
        var catalog = TokenCatalog.Create(acceptedTables);
        var issues = catalog.Validate(acceptedTables);
        var externalIssues = new List<ExternalReferenceIssue>();

        if (manifestSnapshot.Manifest is { } manifest)
        {
            foreach (var table in acceptedTables)
            {
                var tablePath = accepted.Single(item => ReferenceEquals(item.Declaration, table)).Path;
                foreach (var symbol in table.Symbols)
                {
                    var descriptorPath = table.DescriptorPath(symbol.Name);
                    if (manifest.TryFindOwnershipOverlap(descriptorPath, out var externalOwner))
                    {
                        externalIssues.Add(new ExternalReferenceIssue(
                            table.Name,
                            "generates descriptor path",
                            descriptorPath,
                            $"external owner '{manifest.Owner}' already owns canonical declaration " +
                            $"'{externalOwner.Path}' in family '{externalOwner.Family}'."));
                        invalidPaths.Add(tablePath);
                    }

                    if (symbol.ColorRole is not { } colorRole)
                        continue;

                    if (table.Layer != TokenTableLayer.Component)
                    {
                        externalIssues.Add(new ExternalReferenceIssue(
                            table.Name,
                            "uses color role",
                            Display(colorRole),
                            "external color shorthand is only valid in Component token tables."));
                        invalidPaths.Add(tablePath);
                        continue;
                    }

                    if (!manifest.TryResolve(colorRole, out _))
                    {
                        externalIssues.Add(new ExternalReferenceIssue(
                            table.Name,
                            "uses color role",
                            Display(colorRole),
                            "the manifest does not declare a compatible canonical singleton or factory."));
                        invalidPaths.Add(tablePath);
                    }
                }
            }
        }
        else
        {
            foreach (var item in accepted)
                invalidPaths.Add(item.Path);
        }

        foreach (var issue in issues)
        {
            foreach (var item in accepted.Where(item => item.Declaration!.Name == issue.TableName))
                invalidPaths.Add(item.Path);
        }

        return new ValidationSnapshot(
            invalidPaths.OrderBy(path => path, StringComparer.Ordinal).ToImmutableArray(),
            issues.ToImmutableArray(),
            manifestSnapshot.Manifest,
            manifestSnapshot.DisplayName,
            manifestSnapshot.Error,
            externalIssues.ToImmutableArray());
    }

    private static string Display(ColorRoleReference reference)
    {
        return reference.CustomName is null ? reference.Role : reference.Role + ":" + reference.CustomName;
    }

    /// <summary>A table's path and contents, both structurally comparable.</summary>
    private readonly struct TableSource : IEquatable<TableSource>
    {
        public TableSource(string path, string content)
        {
            Path = path;
            Content = content;
        }

        public string Path { get; }

        public string Content { get; }

        public string DisplayName => System.IO.Path.GetFileName(Path);

        public bool Equals(TableSource other)
        {
            return string.Equals(Path, other.Path, StringComparison.Ordinal)
                   && string.Equals(Content, other.Content, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is TableSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Path.GetHashCode() * 397) ^ Content.GetHashCode();
            }
        }

        public override string ToString()
        {
            return DisplayName + " (" + Content.Length.ToString(CultureInfo.InvariantCulture) + " chars)";
        }
    }

    private readonly struct ManifestSource : IEquatable<ManifestSource>
    {
        public ManifestSource(string path, string content)
        {
            Path = path;
            Content = content;
        }

        public string Path { get; }
        public string Content { get; }
        public string DisplayName => System.IO.Path.GetFileName(Path);

        public bool Equals(ManifestSource other) =>
            string.Equals(Path, other.Path, StringComparison.Ordinal) &&
            string.Equals(Content, other.Content, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is ManifestSource other && Equals(other);
        public override int GetHashCode() => unchecked((Path.GetHashCode() * 397) ^ Content.GetHashCode());
    }

    private readonly struct ParsedManifest : IEquatable<ParsedManifest>
    {
        public ParsedManifest(ManifestSource source, ExternalCanonicalManifest? manifest, string? error)
        {
            Source = source;
            Manifest = manifest;
            Error = error;
        }

        public ManifestSource Source { get; }
        public ExternalCanonicalManifest? Manifest { get; }
        public string? Error { get; }

        public bool Equals(ParsedManifest other) =>
            Source.Equals(other.Source) &&
            (Manifest?.Equals(other.Manifest) ?? other.Manifest is null) &&
            string.Equals(Error, other.Error, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is ParsedManifest other && Equals(other);
        public override int GetHashCode() => unchecked((Source.GetHashCode() * 397) ^ (Error?.GetHashCode() ?? 0));
    }

    private sealed class ManifestSnapshot : IEquatable<ManifestSnapshot>
    {
        public ManifestSnapshot(ExternalCanonicalManifest? manifest, string displayName, string? error)
        {
            Manifest = manifest;
            DisplayName = displayName;
            Error = error;
        }

        public ExternalCanonicalManifest? Manifest { get; }
        public string DisplayName { get; }
        public string? Error { get; }

        public bool Equals(ManifestSnapshot? other) =>
            other is not null &&
            (Manifest?.Equals(other.Manifest) ?? other.Manifest is null) &&
            string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal) &&
            string.Equals(Error, other.Error, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as ManifestSnapshot);
        public override int GetHashCode() => unchecked(
            ((Manifest?.GetHashCode() ?? 0) * 397 ^ DisplayName.GetHashCode()) * 397 ^
            (Error?.GetHashCode() ?? 0));
    }

    /// <summary>One source's parse result, detached from the incremental JSON DOM.</summary>
    private readonly struct ParsedTable : IEquatable<ParsedTable>
    {
        public ParsedTable(TableSource source, TokenTable? table, string? error,
            FileKindMismatchInfo? kindMismatch = null)
        {
            Source = source;
            Table = table;
            Error = error;
            KindMismatch = kindMismatch;
        }

        public TableSource Source { get; }

        public TokenTable? Table { get; }

        public string? Error { get; }

        public FileKindMismatchInfo? KindMismatch { get; }

        public bool Equals(ParsedTable other)
        {
            return Source.Equals(other.Source) &&
                   string.Equals(Error, other.Error, StringComparison.Ordinal) &&
                   Nullable.Equals(KindMismatch, other.KindMismatch);
        }

        public override bool Equals(object? obj)
        {
            return obj is ParsedTable other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source.GetHashCode() * 397) ^ (Error?.GetHashCode() ?? 0)) * 397 ^
                       (KindMismatch?.GetHashCode() ?? 0);
            }
        }
    }

    private readonly struct FileKindInfo
    {
        public FileKindInfo(string suffix, string kind)
        {
            Suffix = suffix;
            Kind = kind;
        }

        public string Suffix { get; }

        public string Kind { get; }
    }

    private readonly struct FileKindMismatchInfo : IEquatable<FileKindMismatchInfo>
    {
        public FileKindMismatchInfo(string kind, string suffix, string expectedKind)
        {
            Kind = kind;
            Suffix = suffix;
            ExpectedKind = expectedKind;
        }

        public string Kind { get; }

        public string Suffix { get; }

        public string ExpectedKind { get; }

        public bool Equals(FileKindMismatchInfo other)
        {
            return string.Equals(Kind, other.Kind, StringComparison.Ordinal) &&
                   string.Equals(Suffix, other.Suffix, StringComparison.Ordinal) &&
                   string.Equals(ExpectedKind, other.ExpectedKind, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => obj is FileKindMismatchInfo other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Kind.GetHashCode() * 397) ^ Suffix.GetHashCode()) * 397 ^
                       ExpectedKind.GetHashCode();
            }
        }
    }

    /// <summary>A declaration keyed by path; equality intentionally ignores literal table bodies.</summary>
    private readonly struct TableDeclarationSource : IEquatable<TableDeclarationSource>
    {
        public TableDeclarationSource(string path, TokenTableDeclaration? declaration)
        {
            Path = path;
            Declaration = declaration;
        }

        public string Path { get; }

        public TokenTableDeclaration? Declaration { get; }

        public bool Equals(TableDeclarationSource other)
        {
            return string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   (Declaration?.Equals(other.Declaration) ?? other.Declaration is null);
        }

        public override bool Equals(object? obj)
        {
            return obj is TableDeclarationSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Path.GetHashCode() * 397) ^ (Declaration?.GetHashCode() ?? 0);
            }
        }
    }

    private sealed class ValidationSnapshot : IEquatable<ValidationSnapshot>
    {
        public ValidationSnapshot(ImmutableArray<string> invalidPaths,
            ImmutableArray<TokenReferenceIssue> issues,
            ExternalCanonicalManifest? manifest,
            string manifestDisplayName,
            string? manifestError,
            ImmutableArray<ExternalReferenceIssue> externalIssues)
        {
            InvalidPaths = invalidPaths;
            Issues = issues;
            Manifest = manifest;
            ManifestDisplayName = manifestDisplayName;
            ManifestError = manifestError;
            ExternalIssues = externalIssues;
        }

        public ImmutableArray<string> InvalidPaths { get; }

        public ImmutableArray<TokenReferenceIssue> Issues { get; }

        public ExternalCanonicalManifest? Manifest { get; }

        public string ManifestDisplayName { get; }

        public string? ManifestError { get; }

        public ImmutableArray<ExternalReferenceIssue> ExternalIssues { get; }

        public bool Equals(ValidationSnapshot? other)
        {
            if (other is null || !InvalidPaths.SequenceEqual(other.InvalidPaths, StringComparer.Ordinal) ||
                Issues.Length != other.Issues.Length ||
                !(Manifest?.Equals(other.Manifest) ?? other.Manifest is null) ||
                !string.Equals(ManifestDisplayName, other.ManifestDisplayName, StringComparison.Ordinal) ||
                !string.Equals(ManifestError, other.ManifestError, StringComparison.Ordinal) ||
                ExternalIssues.Length != other.ExternalIssues.Length)
                return false;

            for (var i = 0; i < Issues.Length; i++)
            {
                if (!string.Equals(Issues[i].TableName, other.Issues[i].TableName, StringComparison.Ordinal) ||
                    !string.Equals(Issues[i].Reference, other.Issues[i].Reference, StringComparison.Ordinal) ||
                    !string.Equals(Issues[i].Message, other.Issues[i].Message, StringComparison.Ordinal))
                    return false;
            }

            for (var i = 0; i < ExternalIssues.Length; i++)
            {
                if (!ExternalIssues[i].Equals(other.ExternalIssues[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as ValidationSnapshot);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var path in InvalidPaths)
                    hash = hash * 397 + path.GetHashCode();
                foreach (var issue in Issues)
                {
                    hash = hash * 397 + issue.TableName.GetHashCode();
                    hash = hash * 397 + issue.Reference.GetHashCode();
                    hash = hash * 397 + issue.Message.GetHashCode();
                }

                hash = hash * 397 + (Manifest?.GetHashCode() ?? 0);
                hash = hash * 397 + ManifestDisplayName.GetHashCode();
                hash = hash * 397 + (ManifestError?.GetHashCode() ?? 0);
                foreach (var issue in ExternalIssues)
                    hash = hash * 397 + issue.GetHashCode();

                return hash;
            }
        }
    }

    private readonly struct ExternalReferenceIssue : IEquatable<ExternalReferenceIssue>
    {
        public ExternalReferenceIssue(string tableName, string kind, string reference, string message)
        {
            TableName = tableName;
            Kind = kind;
            Reference = reference;
            Message = message;
        }

        public string TableName { get; }
        public string Kind { get; }
        public string Reference { get; }
        public string Message { get; }

        public bool Equals(ExternalReferenceIssue other) =>
            string.Equals(TableName, other.TableName, StringComparison.Ordinal) &&
            string.Equals(Kind, other.Kind, StringComparison.Ordinal) &&
            string.Equals(Reference, other.Reference, StringComparison.Ordinal) &&
            string.Equals(Message, other.Message, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is ExternalReferenceIssue other && Equals(other);
        public override int GetHashCode() => unchecked(
            (((TableName.GetHashCode() * 397) ^ Kind.GetHashCode()) * 397 ^ Reference.GetHashCode()) * 397 ^
            Message.GetHashCode());
    }
}
