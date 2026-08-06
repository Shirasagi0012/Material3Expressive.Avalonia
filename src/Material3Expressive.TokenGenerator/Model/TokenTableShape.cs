using System;
using System.Collections.Generic;
using System.Linq;

namespace Material3Expressive.TokenGenerator.Model;

/// <summary>
/// The declaration-shaped information needed by the generated theme accessor file. It excludes
/// literal values and provenance text, allowing that aggregate source to remain cached when a
/// component changes only a default number or colour.
/// </summary>
internal sealed class TokenTableShape : IEquatable<TokenTableShape>
{
    public TokenTableShape(TokenTable table)
    {
        Name = table.Name;
        Namespace = table.Namespace;
        Layer = table.Layer;
        IsTokenSet = table.IsTokenSet;
        SetNames = table.Sets.Select(set => set.Name).ToArray();
        VariantEnum = table.VariantEnum;
        ResolvedName = table.ResolvedName;
        Overridable = table.Overridable;
    }

    public string Name { get; }

    public string Namespace { get; }

    public TokenTableLayer Layer { get; }

    public bool IsTokenSet { get; }

    public IReadOnlyList<string> SetNames { get; }

    public string? VariantEnum { get; }

    public string? ResolvedName { get; }

    public bool Overridable { get; }

    public bool Equals(TokenTableShape? other)
    {
        return other is not null &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               string.Equals(Namespace, other.Namespace, StringComparison.Ordinal) &&
               Layer == other.Layer && IsTokenSet == other.IsTokenSet &&
               SetNames.SequenceEqual(other.SetNames, StringComparer.Ordinal) &&
               string.Equals(VariantEnum, other.VariantEnum, StringComparison.Ordinal) &&
               string.Equals(ResolvedName, other.ResolvedName, StringComparison.Ordinal) &&
               Overridable == other.Overridable;
    }

    public override bool Equals(object? obj) => Equals(obj as TokenTableShape);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Name.GetHashCode() * 397 + Namespace.GetHashCode();
            hash = hash * 397 + (int)Layer;
            hash = hash * 397 + IsTokenSet.GetHashCode();
            foreach (var setName in SetNames)
                hash = hash * 397 + setName.GetHashCode();
            hash = hash * 397 + (VariantEnum?.GetHashCode() ?? 0);
            hash = hash * 397 + (ResolvedName?.GetHashCode() ?? 0);
            return hash * 397 + Overridable.GetHashCode();
        }
    }
}
