using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Material3Expressive.TokenGenerator.Emit;
using Material3Expressive.TokenGenerator.Json;
using Material3Expressive.TokenGenerator.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Material3Expressive.TokenGenerator;

/// <summary>
/// Generates the token layer from the JSON tables under <c>Tokens/</c>.
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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Carry plain strings through the pipeline rather than the parsed model: strings compare
        // structurally, so an unrelated edit elsewhere does not invalidate the cached output.
        var tables = context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(TableSuffix, StringComparison.OrdinalIgnoreCase))
            .Select((text, token) => new TableSource(
                Path.GetFileName(text.Path),
                text.GetText(token)?.ToString() ?? string.Empty))
            .Collect();

        context.RegisterSourceOutput(tables, Execute);
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<TableSource> sources)
    {
        var parsed = new List<TokenTable>();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var source in sources.OrderBy(s => s.FileName, StringComparer.Ordinal))
        {
            TokenTable table;

            try
            {
                table = TokenTable.Parse(source.Content);
            }
            catch (Exception exception) when (exception is InvalidTableException or JsonException)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidTable, Location.None, source.FileName, exception.Message));
                continue;
            }

            if (seen.TryGetValue(table.Name, out _))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DuplicateTable, Location.None, source.FileName, table.Name));
                continue;
            }

            seen[table.Name] = source.FileName;
            parsed.Add(table);
        }

        foreach (var table in parsed)
        {
            context.AddSource(
                $"{table.Name}.Tokens.g.cs",
                SourceText.From(TableEmitter.Emit(table), System.Text.Encoding.UTF8));
        }

        context.AddSource(
            "ResolvedTheme.Components.g.cs",
            SourceText.From(TableEmitter.EmitThemeAccessors(parsed), System.Text.Encoding.UTF8));
    }

    /// <summary>A table's file name and contents, both structurally comparable.</summary>
    private readonly struct TableSource : IEquatable<TableSource>
    {
        public TableSource(string fileName, string content)
        {
            FileName = fileName;
            Content = content;
        }

        public string FileName { get; }

        public string Content { get; }

        public bool Equals(TableSource other)
        {
            return string.Equals(FileName, other.FileName, StringComparison.Ordinal)
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
                return (FileName.GetHashCode() * 397) ^ Content.GetHashCode();
            }
        }

        public override string ToString()
        {
            return FileName + " (" + Content.Length.ToString(CultureInfo.InvariantCulture) + " chars)";
        }
    }
}
