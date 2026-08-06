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
/// Generates the token layer from the reference/system/component tables and component defaults
/// under <c>Tokens/</c> and <c>Defaults/</c>.
/// </summary>
/// <remarks>
/// The tables are the single source of truth. Before this generator existed, every component token
/// default was written three times — once in the authoring class's constructor, once in the
/// resolver, and once as a markup extension's fallback — and the three drifted. Now a token exists
/// in exactly one place, and adding a component domain is one table file.
/// </remarks>
[Generator]
public sealed class TokenTableGenerator : IIncrementalGenerator
{
    /// <summary>Only files named <c>*.tokens.json</c> are tables; other AdditionalFiles are ignored.</summary>
    private const string TableSuffix = ".tokens.json";

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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Carry one source through the pipeline at a time. The parsed result compares by path and
        // content, so an unrelated edit reuses every other table's parse result before the global
        // catalog/accessor pass runs.
        var tableItems = context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(TableSuffix, StringComparison.OrdinalIgnoreCase))
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

        var shapes = tableItems
            .Select((result, _) => new TableShapeSource(
                result.Source.Path,
                result.Table is { } table ? new TokenTableShape(table) : null))
            .WithTrackingName("TokenTable.Shape")
            .Collect();

        var validation = declarations
            .Collect()
            .Select((items, _) => BuildValidation(items))
            .WithTrackingName("TokenTable.Validation");

        var allTables = tableItems.Collect();

        // Each table has its own source-output node. Roslyn can now reuse every unchanged table's
        // emitted source even though the aggregate theme accessor still comes from the global pass.
        context.RegisterSourceOutput(
            tableItems.Combine(validation),
            static (production, pair) => EmitTable(production, pair.Left, pair.Right));

        context.RegisterSourceOutput(
            allTables.Combine(validation),
            static (production, pair) => ReportDiagnostics(production, pair.Left, pair.Right));

        context.RegisterSourceOutput(
            shapes.Combine(validation),
            static (production, pair) => EmitThemeAccessors(production, pair.Left, pair.Right));
    }

    private static ParsedTable Parse(TableSource source)
    {
        try
        {
            return new ParsedTable(source, TokenTable.Parse(source.Content), error: null);
        }
        catch (Exception exception) when (exception is InvalidTableException or JsonException)
        {
            return new ParsedTable(source, table: null, exception.Message);
        }
    }

    private static void EmitTable(SourceProductionContext context, ParsedTable result,
        ValidationSnapshot validation)
    {
        if (result.Table is not { } table || validation.InvalidPaths.Contains(result.Source.Path))
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

    }

    private static void EmitThemeAccessors(SourceProductionContext context,
        ImmutableArray<TableShapeSource> shapes, ValidationSnapshot validation)
    {
        var validShapes = shapes
            .Where(item => item.Shape is not null && !validation.InvalidPaths.Contains(item.Path))
            .Select(item => item.Shape!)
            .ToArray();

        context.AddSource(
            "ResolvedTheme.Components.g.cs",
            SourceText.From(
                TableEmitter.EmitThemeAccessors(validShapes),
                System.Text.Encoding.UTF8));
    }

    private static ValidationSnapshot BuildValidation(ImmutableArray<TableDeclarationSource> declarations)
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

        foreach (var issue in issues)
        {
            foreach (var item in accepted.Where(item => item.Declaration!.Name == issue.TableName))
                invalidPaths.Add(item.Path);
        }

        return new ValidationSnapshot(
            invalidPaths.OrderBy(path => path, StringComparer.Ordinal).ToImmutableArray(),
            issues.ToImmutableArray());
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

    /// <summary>One source's parse result, detached from the incremental JSON DOM.</summary>
    private readonly struct ParsedTable : IEquatable<ParsedTable>
    {
        public ParsedTable(TableSource source, TokenTable? table, string? error)
        {
            Source = source;
            Table = table;
            Error = error;
        }

        public TableSource Source { get; }

        public TokenTable? Table { get; }

        public string? Error { get; }

        public bool Equals(ParsedTable other)
        {
            return Source.Equals(other.Source) && string.Equals(Error, other.Error, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is ParsedTable other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Source.GetHashCode() * 397) ^ (Error?.GetHashCode() ?? 0);
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

    /// <summary>A shape keyed by path; literal values are intentionally absent from equality.</summary>
    private readonly struct TableShapeSource : IEquatable<TableShapeSource>
    {
        public TableShapeSource(string path, TokenTableShape? shape)
        {
            Path = path;
            Shape = shape;
        }

        public string Path { get; }

        public TokenTableShape? Shape { get; }

        public bool Equals(TableShapeSource other)
        {
            return string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   (Shape?.Equals(other.Shape) ?? other.Shape is null);
        }

        public override bool Equals(object? obj)
        {
            return obj is TableShapeSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Path.GetHashCode() * 397) ^ (Shape?.GetHashCode() ?? 0);
            }
        }
    }

    private sealed class ValidationSnapshot : IEquatable<ValidationSnapshot>
    {
        public ValidationSnapshot(ImmutableArray<string> invalidPaths,
            ImmutableArray<TokenReferenceIssue> issues)
        {
            InvalidPaths = invalidPaths;
            Issues = issues;
        }

        public ImmutableArray<string> InvalidPaths { get; }

        public ImmutableArray<TokenReferenceIssue> Issues { get; }

        public bool Equals(ValidationSnapshot? other)
        {
            if (other is null || !InvalidPaths.SequenceEqual(other.InvalidPaths, StringComparer.Ordinal) ||
                Issues.Length != other.Issues.Length)
                return false;

            for (var i = 0; i < Issues.Length; i++)
            {
                if (!string.Equals(Issues[i].TableName, other.Issues[i].TableName, StringComparison.Ordinal) ||
                    !string.Equals(Issues[i].Reference, other.Issues[i].Reference, StringComparison.Ordinal) ||
                    !string.Equals(Issues[i].Message, other.Issues[i].Message, StringComparison.Ordinal))
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

                return hash;
            }
        }
    }
}
