using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using System.ComponentModel;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>Resolves strongly typed tokens against the resource scope of an actual consumer.</summary>
[Obsolete(
    "Requester-relative token resolution is legacy. Use a MaterialTheme placement and " +
    "MaterialValueKey<T> DynamicResource endpoints.",
    DiagnosticId = "M3E1002")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class MaterialTokenResolver
{
    /// <summary>Resolves one token in a new resolution session.</summary>
    public T Resolve<T>(IResourceHost origin, MaterialToken<T> token, ThemeVariant? theme = null)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(token);
        return new Session(origin, EffectiveTheme(origin, theme)).Resolve(token);
    }

    /// <summary>Creates a session that memoizes shared dependencies across several reads.</summary>
    public TokenReader BeginRead(IResourceHost origin, ThemeVariant? theme = null)
    {
        ArgumentNullException.ThrowIfNull(origin);
        return new TokenReader(new Session(origin, EffectiveTheme(origin, theme)));
    }

    private static ThemeVariant? EffectiveTheme(IResourceHost origin, ThemeVariant? requested) =>
        requested ?? (origin as StyledElement)?.ActualThemeVariant;

    internal sealed class Session
    {
        private readonly IResourceHost _origin;
        private readonly ThemeVariant? _theme;
        private readonly Dictionary<MaterialToken, object?> _memo = [];
        private readonly List<MaterialToken> _stack = [];

        internal Session(IResourceHost origin, ThemeVariant? theme)
        {
            _origin = origin;
            _theme = theme;
        }

        internal T Resolve<T>(MaterialToken<T> token) => (T)ResolveUntyped(token)!;

        internal bool IsDefined(MaterialToken token) =>
            _origin.TryFindResource(token, _theme, out _);

        private object? ResolveUntyped(MaterialToken token)
        {
            if (_memo.TryGetValue(token, out var cached))
                return cached;

            var cycleStart = _stack.IndexOf(token);
            if (cycleStart >= 0)
            {
                var cycle = _stack.Skip(cycleStart).Append(token).Select(static item => item.Path);
                throw new MaterialTokenResolutionException(
                    $"A Material token alias or recipe cycle was detected: {string.Join(" -> ", cycle)}.");
            }

            _stack.Add(token);
            try
            {
                var raw = FindDefinition(token);
                var value = Evaluate(token, raw);
                _memo.Add(token, value);
                return value;
            }
            finally
            {
                _stack.RemoveAt(_stack.Count - 1);
            }
        }

        private object? FindDefinition(MaterialToken token)
        {
            if (_origin.TryFindResource(token, _theme, out var resource))
                return resource;

            return token.UntypedDefaultDefinition;
        }

        private object? Evaluate(MaterialToken token, object? raw)
        {
            // A value of T is accepted as an ergonomic ResourceDictionary literal shorthand.
            if (raw is not ITokenDefinition definition)
            {
                if (raw is null && (!token.ValueType.IsValueType || Nullable.GetUnderlyingType(token.ValueType) is not null))
                    return null;
                if (raw is not null && token.ValueType.IsInstanceOfType(raw))
                    return raw;

                throw WrongType(token, raw?.GetType());
            }

            if (definition.ValueType != token.ValueType)
                throw WrongType(token, definition.ValueType);

            switch (definition.Kind)
            {
                case TokenDefinitionKind.Literal:
                    return definition.LiteralValue;

                case TokenDefinitionKind.Alias:
                {
                    var target = definition.AliasTarget!;
                    EnsureDependencyDirection(token, target);
                    return ResolveUntyped(target);
                }

                case TokenDefinitionKind.Recipe:
                {
                    var dependencies = definition.Dependencies;
                    foreach (var dependency in dependencies)
                        EnsureLayerDirection(token, dependency);

                    var context = new TokenRecipeContext(dependencies, ResolveUntyped);
                    return definition.EvaluateRecipe(context);
                }
            }

            throw new MaterialTokenResolutionException(
                $"Resource '{token.Path}' contains unsupported definition type '{definition.GetType().FullName}'.");
        }

        private static void EnsureDependencyDirection(MaterialToken source, MaterialToken target)
        {
            EnsureLayerDirection(source, target);
            if (target.ValueType != source.ValueType)
                throw new MaterialTokenResolutionException(
                    $"Token '{source.Path}' produces {source.ValueType.Name}, but its dependency " +
                    $"'{target.Path}' produces {target.ValueType.Name}.");
        }

        private static void EnsureLayerDirection(MaterialToken source, MaterialToken target)
        {
            if (target.Layer > source.Layer)
                throw new MaterialTokenResolutionException(
                    $"Token '{source.Path}' in the {source.Layer} layer cannot depend on downstream " +
                    $"{target.Layer} token '{target.Path}'.");
        }

        private static MaterialTokenResolutionException WrongType(MaterialToken token, Type? actual) => new(
            $"Resource '{token.Path}' must contain {token.ValueType.FullName} or " +
            $"TokenDefinition<{token.ValueType.Name}>, but contained {actual?.FullName ?? "null"}.");
    }
}
