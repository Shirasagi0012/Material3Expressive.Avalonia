using System;
using System.Collections.Generic;
using System.Linq;
using Material3Expressive.TokenGenerator.Model;

namespace Material3Expressive.TokenGenerator.Validation;

/// <summary>
/// The cross-table symbol catalog. Parsing validates one table in isolation; this pass validates
/// every <c>@Table.Member</c> reference once all declarations are known.
/// </summary>
internal sealed class TokenCatalog
{
    private readonly IReadOnlyDictionary<string, TokenSymbol> _symbols;

    private TokenCatalog(IReadOnlyDictionary<string, TokenSymbol> symbols)
    {
        _symbols = symbols;
    }

    public static TokenCatalog Create(IEnumerable<TokenTableDeclaration> tables)
    {
        var symbols = new Dictionary<string, TokenSymbol>(StringComparer.Ordinal);

        foreach (var table in tables)
        {
            if (!table.IsTokenSet)
                continue;

            foreach (var token in table.Symbols)
            {
                var key = Key(table.Name, token.Name);
                symbols[key] = new TokenSymbol(
                    key,
                    table.Category,
                    table.Layer,
                    token.Type,
                    token.EnumType,
                    token.Reference);
            }
        }

        return new TokenCatalog(symbols);
    }

    public IReadOnlyList<TokenReferenceIssue> Validate(IEnumerable<TokenTableDeclaration> tables)
    {
        var issues = new List<TokenReferenceIssue>();

        foreach (var table in tables)
        {
            foreach (var value in table.Values)
            {
                if (value.Reference is not { } reference)
                    continue;

                var referenceText = $"{reference.TableName}.{reference.MemberName}";
                var targetKey = Key(reference.TableName, reference.MemberName);

                if (!_symbols.TryGetValue(targetKey, out var target))
                {
                    issues.Add(new TokenReferenceIssue(
                        table.Name,
                        referenceText,
                        $"'{referenceText}' does not name a known token-set member."));
                    continue;
                }

                if (value.Type != target.Type ||
                    value.Type == TokenType.Enum && !string.Equals(value.EnumType, target.EnumType, StringComparison.Ordinal))
                {
                    issues.Add(new TokenReferenceIssue(
                        table.Name,
                        referenceText,
                        $"'{referenceText}' is {Describe(target.Type, target.EnumType)}, but the value requires " +
                        $"{Describe(value.Type, value.EnumType)}."));
                }

                if (!IsAllowed(table, target))
                {
                    issues.Add(new TokenReferenceIssue(
                        table.Name,
                        referenceText,
                        $"a {table.Layer} table cannot depend on a {target.Layer} table."));
                }
            }
        }

        FindCycles(issues);
        return issues;
    }

    private void FindCycles(ICollection<TokenReferenceIssue> issues)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var active = new HashSet<string>(StringComparer.Ordinal);
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var symbol in _symbols.Values)
            Visit(symbol, new List<string>(), visited, active, reported, issues);
    }

    private void Visit(
        TokenSymbol symbol,
        List<string> path,
        ISet<string> visited,
        ISet<string> active,
        ISet<string> reported,
        ICollection<TokenReferenceIssue> issues)
    {
        if (active.Contains(symbol.Key))
        {
            var start = path.IndexOf(symbol.Key);
            var cycle = start < 0
                ? symbol.Key
                : string.Join(" -> ", path.Skip(start).Concat(new[] { symbol.Key }));

            if (reported.Add(cycle))
                issues.Add(new TokenReferenceIssue(symbol.Key.Split('.')[0], symbol.Key, $"reference cycle: {cycle}."));

            return;
        }

        if (!visited.Add(symbol.Key))
            return;

        active.Add(symbol.Key);
        path.Add(symbol.Key);

        if (symbol.Reference is { } reference &&
            _symbols.TryGetValue(Key(reference.TableName, reference.MemberName), out var target))
        {
            Visit(target, path, visited, active, reported, issues);
        }

        path.RemoveAt(path.Count - 1);
        active.Remove(symbol.Key);
    }

    private static bool IsAllowed(TokenTableDeclaration source, TokenSymbol target)
    {
        return source.Layer switch
        {
            TokenTableLayer.Reference => target.Layer == TokenTableLayer.Reference,
            TokenTableLayer.System => target.Layer is TokenTableLayer.Reference or TokenTableLayer.System,
            TokenTableLayer.Component => target.Layer is TokenTableLayer.Reference or TokenTableLayer.System
                or TokenTableLayer.Component,
            _ => true
        };
    }

    private static string Describe(TokenType type, string? enumType)
    {
        return type == TokenType.Enum ? $"enum {enumType}" : type.ToString();
    }

    private static string Key(string tableName, string memberName)
    {
        return tableName + "." + memberName;
    }

    private sealed class TokenSymbol
    {
        public TokenSymbol(string key, TokenTableCategory category, TokenTableLayer layer, TokenType type, string? enumType,
            TokenReference? reference)
        {
            Key = key;
            Category = category;
            Layer = layer;
            Type = type;
            EnumType = enumType;
            Reference = reference;
        }

        public string Key { get; }

        public TokenTableCategory Category { get; }

        public TokenTableLayer Layer { get; }

        public TokenType Type { get; }

        public string? EnumType { get; }

        public TokenReference? Reference { get; }
    }
}

internal sealed class TokenReferenceIssue
{
    public TokenReferenceIssue(string tableName, string reference, string message)
    {
        TableName = tableName;
        Reference = reference;
        Message = message;
    }

    public string TableName { get; }

    public string Reference { get; }

    public string Message { get; }
}
