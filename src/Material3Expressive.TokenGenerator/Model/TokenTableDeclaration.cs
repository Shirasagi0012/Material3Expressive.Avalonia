using System;
using System.Collections.Generic;
using System.Linq;

namespace Material3Expressive.TokenGenerator.Model;

/// <summary>
/// The part of a parsed table that can affect cross-table validation. Literal expressions are
/// deliberately omitted, so changing a number or colour does not invalidate the global catalog.
/// References, member names, types and record shape remain part of the declaration.
/// </summary>
internal sealed class TokenTableDeclaration : IEquatable<TokenTableDeclaration>
{
    public TokenTableDeclaration(TokenTable table)
    {
        Name = table.Name;
        Category = table.Category;
        Layer = table.Layer;
        IsTokenSet = table.IsTokenSet;

        Symbols = table.IsTokenSet
            ? table.Tokens
                .Select(token => new TokenSymbolDeclaration(
                    token.Name,
                    token.Type,
                    token.EnumType,
                    token.Value.Reference))
                .ToArray()
            : Array.Empty<TokenSymbolDeclaration>();

        Values = ValuesOf(table)
            .Select(value => new TokenValueDeclaration(value.Type, value.EnumType, value.Reference))
            .ToArray();

        // A component accessor depends on names and ordering, but not on the literal value of a
        // set. Include the record shape here for the same reason.
        Properties = table.Properties
            .Select(property => new TokenPropertyDeclaration(
                property.Name,
                property.Type,
                property.EnumType,
                property.OpacityProperty))
            .ToArray();
        SetNames = table.Sets.Select(set => set.Name).ToArray();
        VariantEnum = table.VariantEnum;
        ResolvedName = table.ResolvedName;
    }

    public string Name { get; }

    public TokenTableCategory Category { get; }

    public TokenTableLayer Layer { get; }

    public bool IsTokenSet { get; }

    public IReadOnlyList<TokenSymbolDeclaration> Symbols { get; }

    public IReadOnlyList<TokenValueDeclaration> Values { get; }

    public IReadOnlyList<TokenPropertyDeclaration> Properties { get; }

    public IReadOnlyList<string> SetNames { get; }

    public string? VariantEnum { get; }

    public string? ResolvedName { get; }

    public bool Equals(TokenTableDeclaration? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || !string.Equals(Name, other.Name, StringComparison.Ordinal) ||
            Category != other.Category || Layer != other.Layer || IsTokenSet != other.IsTokenSet ||
            !string.Equals(VariantEnum, other.VariantEnum, StringComparison.Ordinal) ||
            !string.Equals(ResolvedName, other.ResolvedName, StringComparison.Ordinal))
            return false;

        return SequenceEqual(Symbols, other.Symbols) &&
               SequenceEqual(Values, other.Values) &&
               SequenceEqual(Properties, other.Properties) &&
               SetNames.SequenceEqual(other.SetNames, StringComparer.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TokenTableDeclaration);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Name.GetHashCode();
            hash = hash * 397 + (int)Category;
            hash = hash * 397 + (int)Layer;
            hash = hash * 397 + IsTokenSet.GetHashCode();
            hash = hash * 397 + (VariantEnum?.GetHashCode() ?? 0);
            hash = hash * 397 + (ResolvedName?.GetHashCode() ?? 0);
            foreach (var symbol in Symbols)
                hash = hash * 397 + symbol.GetHashCode();
            foreach (var value in Values)
                hash = hash * 397 + value.GetHashCode();
            foreach (var property in Properties)
                hash = hash * 397 + property.GetHashCode();
            foreach (var setName in SetNames)
                hash = hash * 397 + setName.GetHashCode();
            return hash;
        }
    }

    private static IEnumerable<TokenValue> ValuesOf(TokenTable table)
    {
        if (table.IsTokenSet)
            return table.Tokens.Select(token => token.Value);

        return table.Sets
            .SelectMany(set => set.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            .Select(pair => pair.Value);
    }

    private static bool SequenceEqual<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        where T : IEquatable<T>
    {
        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!left[i].Equals(right[i]))
                return false;
        }

        return true;
    }
}

internal sealed class TokenSymbolDeclaration : IEquatable<TokenSymbolDeclaration>
{
    public TokenSymbolDeclaration(string name, TokenType type, string? enumType, TokenReference? reference)
    {
        Name = name;
        Type = type;
        EnumType = enumType;
        Reference = reference;
    }

    public string Name { get; }

    public TokenType Type { get; }

    public string? EnumType { get; }

    public TokenReference? Reference { get; }

    public bool Equals(TokenSymbolDeclaration? other)
    {
        return other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               Type == other.Type && string.Equals(EnumType, other.EnumType, StringComparison.Ordinal) &&
               TokenReferenceEquals(Reference, other.Reference);
    }

    public override bool Equals(object? obj) => Equals(obj as TokenSymbolDeclaration);

    public override int GetHashCode() => Hash(Name, Type, EnumType, Reference);

    private static int Hash(string name, TokenType type, string? enumType, TokenReference? reference)
    {
        unchecked
        {
            var hash = name.GetHashCode() * 397 + (int)type;
            hash = hash * 397 + (enumType?.GetHashCode() ?? 0);
            hash = hash * 397 + (reference?.TableName.GetHashCode() ?? 0);
            hash = hash * 397 + (reference?.MemberName.GetHashCode() ?? 0);
            return hash;
        }
    }

    internal static bool TokenReferenceEquals(TokenReference? left, TokenReference? right)
    {
        return (left is null && right is null) ||
               (left is not null && right is not null &&
                string.Equals(left.TableName, right.TableName, StringComparison.Ordinal) &&
                string.Equals(left.MemberName, right.MemberName, StringComparison.Ordinal));
    }
}

internal sealed class TokenValueDeclaration : IEquatable<TokenValueDeclaration>
{
    public TokenValueDeclaration(TokenType type, string? enumType, TokenReference? reference)
    {
        Type = type;
        EnumType = enumType;
        Reference = reference;
    }

    public TokenType Type { get; }

    public string? EnumType { get; }

    public TokenReference? Reference { get; }

    public bool Equals(TokenValueDeclaration? other)
    {
        return other is not null && Type == other.Type &&
               string.Equals(EnumType, other.EnumType, StringComparison.Ordinal) &&
               TokenSymbolDeclaration.TokenReferenceEquals(Reference, other.Reference);
    }

    public override bool Equals(object? obj) => Equals(obj as TokenValueDeclaration);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Type * 397 + (EnumType?.GetHashCode() ?? 0);
            hash = hash * 397 + (Reference?.TableName.GetHashCode() ?? 0);
            hash = hash * 397 + (Reference?.MemberName.GetHashCode() ?? 0);
            return hash;
        }
    }
}

internal sealed class TokenPropertyDeclaration : IEquatable<TokenPropertyDeclaration>
{
    public TokenPropertyDeclaration(string name, TokenType type, string? enumType, string? opacityProperty)
    {
        Name = name;
        Type = type;
        EnumType = enumType;
        OpacityProperty = opacityProperty;
    }

    public string Name { get; }

    public TokenType Type { get; }

    public string? EnumType { get; }

    public string? OpacityProperty { get; }

    public bool Equals(TokenPropertyDeclaration? other)
    {
        return other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               Type == other.Type && string.Equals(EnumType, other.EnumType, StringComparison.Ordinal) &&
               string.Equals(OpacityProperty, other.OpacityProperty, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as TokenPropertyDeclaration);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Name.GetHashCode() * 397 + (int)Type;
            hash = hash * 397 + (EnumType?.GetHashCode() ?? 0);
            return hash * 397 + (OpacityProperty?.GetHashCode() ?? 0);
        }
    }
}
