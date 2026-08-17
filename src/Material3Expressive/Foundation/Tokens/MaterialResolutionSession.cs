using Avalonia.Controls;
using Avalonia.Styling;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>Resolves one Material value relative to a fixed resource-provider placement.</summary>
internal sealed class MaterialResolutionSession
{
    private readonly IResourceHost _placementOwner;
    private readonly ThemeVariant? _queryTheme;
    private readonly IRawTokenCatalog _catalog;
    private readonly Dictionary<MaterialToken, object> _memo =
        new(ReferenceEqualityComparer.Instance);
    private readonly List<MaterialToken> _stack = [];
    private readonly Dictionary<MaterialToken, int> _active =
        new(ReferenceEqualityComparer.Instance);

    internal MaterialResolutionSession(
        IResourceHost placementOwner,
        ThemeVariant? queryTheme,
        IRawTokenCatalog catalog)
    {
        _placementOwner = placementOwner ?? throw new ArgumentNullException(nameof(placementOwner));
        _queryTheme = queryTheme;
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    internal T Resolve<T>(MaterialValueKey<T> key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var context = new RestrictedMaterialValueContext(this, key, key.Plan.Dependencies);
            var result = key.Plan.Evaluate(context);
            return (T)ValidateFinalValue(key, result);
        }
        catch (MaterialValueResolutionException exception)
        {
            throw MaterialResolutionFailures.AttachValueKey(exception, key);
        }
        catch (Exception exception)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.ProjectorFailure,
                $"Material value projector '{key.Path}' failed.",
                valueKey: key,
                theme: _queryTheme,
                path: SnapshotPath(),
                inner: exception);
        }
    }

    internal T ResolveRaw<T>(MaterialToken<T> token) => (T)ResolveRawUntyped(token);

    internal bool HasVisibleResourceDefinition(MaterialToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _placementOwner.TryFindResource(token, _queryTheme, out _);
    }

    private object ResolveRawUntyped(MaterialToken token)
    {
        if (_memo.TryGetValue(token, out var cached))
            return cached;

        if (_active.TryGetValue(token, out var cycleStart))
        {
            var cycle = _stack.Skip(cycleStart).Append(token).ToArray();
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.Cycle,
                $"A Material raw-token cycle was detected: " +
                string.Join(" -> ", cycle.Select(static item => item.Path)) + ".",
                token: token,
                theme: _queryTheme,
                path: Frames(cycle));
        }

        _active.Add(token, _stack.Count);
        _stack.Add(token);
        try
        {
            var raw = FindDefinition(token);
            var result = Evaluate(token, raw);
            ValidateResolvedRawValue(token, result);
            _memo.Add(token, result);
            return result;
        }
        catch (Exception exception) when (exception is IMaterialTokenConfigurationException configuration)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.DomainConfiguration,
                $"Material token domain '{configuration.DomainId}' is not configured for " +
                $"raw token '{token.Path}'.",
                token: token,
                theme: _queryTheme,
                path: SnapshotPath(),
                inner: exception,
                domainId: configuration.DomainId,
                configurationKey: configuration.ConfigurationKey);
        }
        finally
        {
            _active.Remove(token);
            _stack.RemoveAt(_stack.Count - 1);
        }
    }

    private object FindDefinition(MaterialToken token)
    {
        // This intentionally restarts every raw hop at the placement owner. The original query
        // theme is never replaced by the owner's ActualThemeVariant or a previous fallback hit.
        bool found;
        object? resource;
        try
        {
            found = _placementOwner.TryFindResource(token, _queryTheme, out resource);
        }
        catch (MaterialValueResolutionException exception)
        {
            throw MaterialResolutionFailures.AttachRawPath(
                exception,
                token,
                _queryTheme,
                SnapshotPath());
        }
        catch (Exception exception) when (exception is IMaterialTokenConfigurationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.RecipeFailure,
                $"The resource chain failed while looking up raw token '{token.Path}'.",
                token: token,
                theme: _queryTheme,
                path: SnapshotPath(),
                inner: exception);
        }

        if (found)
            return ValidateResourceHit(token, resource);

        RawTokenCatalogLookup catalog;
        try
        {
            catalog = _catalog.Lookup(token, _queryTheme);
        }
        catch (MaterialValueResolutionException exception)
        {
            throw MaterialResolutionFailures.AttachRawPath(
                exception,
                token,
                _queryTheme,
                SnapshotPath());
        }
        catch (Exception exception) when (exception is IMaterialTokenConfigurationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.InvalidCatalogContribution,
                $"The terminal raw-token catalog failed while looking up '{token.Path}'.",
                token: token,
                theme: _queryTheme,
                path: SnapshotPath(),
                inner: exception);
        }

        switch (catalog.Kind)
        {
            case RawTokenCatalogLookupKind.Definition:
                return ValidateDefinition(token, catalog.Definition, catalog.ContributionId);

            case RawTokenCatalogLookupKind.CatalogRequiredMissing:
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.MissingCatalogDefinition,
                    $"Raw token '{token.Path}' is owned by catalog '{catalog.ContributionId}', " +
                    "but its required baseline definition is missing.",
                    token: token,
                    theme: _queryTheme,
                    path: SnapshotPath(),
                    contributionIds: catalog.ContributionId is null ? null : [catalog.ContributionId]);

            case RawTokenCatalogLookupKind.SourceRequiredMissing:
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.MissingTokenDefinition,
                    $"Raw token '{token.Path}' has no visible definition and must be supplied by a source.",
                    token: token,
                    theme: _queryTheme,
                    path: SnapshotPath(),
                    contributionIds: catalog.ContributionId is null ? null : [catalog.ContributionId]);

            default:
                throw new InvalidOperationException("Unknown raw-token catalog result.");
        }
    }

    private object ValidateResourceHit(MaterialToken token, object? resource)
    {
        if (resource is null)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.NullDefinition,
                $"Resource lookup hit null for raw token '{token.Path}'.",
                token: token,
                theme: _queryTheme,
                path: SnapshotPath(),
                expected: token.ValueType);
        }

        if (resource is TokenDefinition definition)
            return ValidateDefinition(token, definition, null);

        if (!token.ValueType.IsInstanceOfType(resource))
            throw WrongType(token, resource.GetType());

        return resource;
    }

    private object ValidateDefinition(
        MaterialToken token,
        TokenDefinition? definition,
        string? contributionId)
    {
        if (definition is null)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.NullDefinition,
                $"Raw token '{token.Path}' has a null definition.",
                token: token,
                theme: _queryTheme,
                path: SnapshotPath(),
                expected: token.ValueType,
                contributionIds: contributionId is null ? null : [contributionId]);
        }

        if (definition.ValueType != token.ValueType)
            throw WrongType(token, definition.ValueType, contributionId);

        return definition;
    }

    private object Evaluate(MaterialToken token, object raw)
    {
        if (raw is not TokenDefinition definition)
            return raw;

        return definition.Accept(new DefinitionEvaluator(this, token));
    }

    private object ValidateFinalValue(MaterialValueKey key, object? result)
    {
        if (result is null)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.NullValue,
                $"Material value '{key.Path}' resolved to null.",
                valueKey: key,
                theme: _queryTheme,
                path: SnapshotPath(),
                expected: key.ValueType);
        }

        if (!key.ValueType.IsInstanceOfType(result))
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.WrongType,
                $"Material value '{key.Path}' produced '{result.GetType().FullName}', " +
                $"but expected '{key.ValueType.FullName}'.",
                valueKey: key,
                theme: _queryTheme,
                path: SnapshotPath(),
                expected: key.ValueType,
                actual: result.GetType());
        }

        return result;
    }

    private void ValidateResolvedRawValue(MaterialToken token, object? result)
    {
        if (result is null)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.NullValue,
                $"Raw token '{token.Path}' resolved to null.",
                token: token,
                theme: _queryTheme,
                path: SnapshotPath(),
                expected: token.ValueType);
        }

        if (!token.ValueType.IsInstanceOfType(result))
            throw WrongType(token, result.GetType());
    }

    private void EnsureDependencyDirection(MaterialToken source, MaterialToken dependency)
    {
        if (dependency.Layer > source.Layer)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.IllegalDependencyDirection,
                $"Raw token '{source.Path}' in layer {source.Layer} cannot depend on " +
                $"downstream {dependency.Layer} token '{dependency.Path}'.",
                token: source,
                theme: _queryTheme,
                path: SnapshotPath());
        }
    }

    private MaterialValueResolutionException WrongType(
        MaterialToken token,
        Type actual,
        string? contributionId = null) =>
        MaterialResolutionFailures.Create(
            MaterialResolutionFailureKind.WrongType,
            $"Raw token '{token.Path}' expected '{token.ValueType.FullName}', " +
            $"but found '{actual.FullName}'.",
            token: token,
            theme: _queryTheme,
            path: SnapshotPath(),
            expected: token.ValueType,
            actual: actual,
            contributionIds: contributionId is null ? null : [contributionId]);

    private IReadOnlyList<MaterialResolutionFrame> SnapshotPath() => Frames(_stack);

    private static IReadOnlyList<MaterialResolutionFrame> Frames(
        IEnumerable<MaterialToken> tokens) =>
        tokens.Select(static token =>
            new MaterialResolutionFrame(token.Path, token.Layer, token.ValueType)).ToArray();

    private sealed class DefinitionEvaluator : ITokenDefinitionVisitor<object>
    {
        private readonly MaterialResolutionSession _session;
        private readonly MaterialToken _source;

        internal DefinitionEvaluator(MaterialResolutionSession session, MaterialToken source)
        {
            _session = session;
            _source = source;
        }

        public object Visit<T>(LiteralTokenDefinition<T> definition) =>
            definition.Value is null
                ? throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.NullValue,
                    $"Literal definition for '{_source.Path}' contains null.",
                    token: _source,
                    theme: _session._queryTheme,
                    path: _session.SnapshotPath(),
                    expected: _source.ValueType)
                : definition.Value;

        public object Visit<T>(AliasTokenDefinition<T> definition)
        {
            _session.EnsureDependencyDirection(_source, definition.Target);
            if (definition.Target.ValueType != _source.ValueType)
                throw _session.WrongType(_source, definition.Target.ValueType);

            return _session.ResolveRawUntyped(definition.Target);
        }

        public object Visit<T>(RecipeTokenDefinition<T> definition)
        {
            foreach (var dependency in definition.Dependencies)
                _session.EnsureDependencyDirection(_source, dependency);

            try
            {
                var context = new RestrictedTokenRecipeContext(
                    _session,
                    definition.Dependencies,
                    _source);
                var result = definition.Recipe.Evaluate(context);
                return result is null
                    ? throw MaterialResolutionFailures.Create(
                        MaterialResolutionFailureKind.NullValue,
                        $"Raw recipe for '{_source.Path}' returned null.",
                        token: _source,
                        theme: _session._queryTheme,
                        path: _session.SnapshotPath(),
                        expected: _source.ValueType)
                    : result;
            }
            catch (MaterialValueResolutionException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IMaterialTokenConfigurationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.RecipeFailure,
                    $"Raw recipe for '{_source.Path}' failed.",
                    token: _source,
                    theme: _session._queryTheme,
                    path: _session.SnapshotPath(),
                    inner: exception);
            }
        }
    }

    private sealed class RestrictedTokenRecipeContext : TokenRecipeContext
    {
        private readonly MaterialResolutionSession _session;
        private readonly HashSet<MaterialToken> _allowed;
        private readonly MaterialToken _source;

        internal RestrictedTokenRecipeContext(
            MaterialResolutionSession session,
            IReadOnlyList<MaterialToken> dependencies,
            MaterialToken source)
        {
            _session = session;
            _source = source;
            _allowed = new HashSet<MaterialToken>(dependencies, ReferenceEqualityComparer.Instance);
        }

        public override T Get<T>(MaterialToken<T> token)
        {
            ArgumentNullException.ThrowIfNull(token);
            if (!_allowed.Contains(token))
            {
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.UndeclaredDependency,
                    $"Raw recipe for '{_source.Path}' requested undeclared dependency '{token.Path}'.",
                    token: _source,
                    theme: _session._queryTheme,
                    path: _session.SnapshotPath());
            }

            return _session.ResolveRaw(token);
        }
    }

    private sealed class RestrictedMaterialValueContext : MaterialValueContext
    {
        private readonly MaterialResolutionSession _session;
        private readonly MaterialValueKey _key;
        private readonly HashSet<MaterialToken> _allowed;

        internal RestrictedMaterialValueContext(
            MaterialResolutionSession session,
            MaterialValueKey key,
            IReadOnlyList<MaterialToken> dependencies)
        {
            _session = session;
            _key = key;
            _allowed = new HashSet<MaterialToken>(dependencies, ReferenceEqualityComparer.Instance);
        }

        public override T Get<T>(MaterialToken<T> token)
        {
            ArgumentNullException.ThrowIfNull(token);
            if (!_allowed.Contains(token))
            {
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.UndeclaredDependency,
                    $"Material value '{_key.Path}' requested undeclared raw dependency '{token.Path}'.",
                    valueKey: _key,
                    token: token,
                    theme: _session._queryTheme,
                    path: _session.SnapshotPath());
            }

            return _session.ResolveRaw(token);
        }
    }
}
