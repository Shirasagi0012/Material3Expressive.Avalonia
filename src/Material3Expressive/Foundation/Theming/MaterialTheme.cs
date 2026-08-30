using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// A placement-local Raw-token source and Material-value resolution boundary.
/// </summary>
public sealed class MaterialTheme : ResourceProvider
{
    private readonly AvaloniaList<IMaterialTokenSource> _children = new()
    {
        ResetBehavior = ResetBehavior.Reset,
    };
    private readonly Dictionary<IMaterialTokenSource, int> _sourceSubscriptions =
        new(ReferenceEqualityComparer.Instance);
    private bool _subscriptionsActive;

    public MaterialTheme()
    {
        _children.CollectionChanged += OnChildrenChanged;
        _children.Validate = ValidateChildSourceForInsertion;
    }

    /// <summary>
    /// Gets the local sparse Raw-token sources. Later children have higher priority.
    /// </summary>
    /// <remarks>
    /// Colour schemes, single-token overrides, and generated domain sources all participate
    /// through the same list; this provider has no domain-specific compatibility branches.
    /// </remarks>
    [Content]
    public AvaloniaList<IMaterialTokenSource> Children => _children;

    /// <inheritdoc />
    public override bool HasResources => true;

    /// <inheritdoc />
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is MaterialToken raw)
            return TryGetLocalRaw(raw, theme, out value);

        if (key is MaterialValueKey flat)
        {
            if (Owner is not { } owner)
            {
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.UnownedPlacement,
                    $"Material value '{flat.Path}' cannot be resolved because its MaterialTheme " +
                    "is not attached to a resource host.",
                    valueKey: flat,
                    theme: theme);
            }

            ValidateChildSourcePlacements();
            var session = new MaterialResolutionSession(owner, theme, DefaultRawTokens.Catalog);
            value = flat.Resolve(session);
            return true;
        }

        value = null;
        return false;
    }

    public object ProvideValue() => this;

    protected override void OnAddOwner(IResourceHost owner)
    {
        ValidateChildSourcePlacements();
        ActivateSubscriptions();
        try
        {
            base.OnAddOwner(owner);
        }
        catch
        {
            DeactivateSubscriptions();
            throw;
        }
    }

    protected override void OnRemoveOwner(IResourceHost owner)
    {
        DeactivateSubscriptions();
        base.OnRemoveOwner(owner);
    }

    private bool TryGetLocalRaw(
        MaterialToken token,
        ThemeVariant? queryTheme,
        out object? definition)
    {
        for (var index = _children.Count - 1; index >= 0; index--)
        {
            var source = _children[index];
            if (source is null)
                continue;

            ValidateChildSourceForInsertion(source);
            var found = source.TryGetRawToken(token, queryTheme, out definition);
            if (!found)
            {
                ValidateLocalSourceMiss(source, token, definition, queryTheme);
                continue;
            }

            ValidateLocalDefinitionHit(source, token, definition, queryTheme);
            return true;
        }

        definition = null;
        return false;
    }

    private static void ValidateLocalSourceMiss(
        IMaterialTokenSource source,
        MaterialToken token,
        object? definition,
        ThemeVariant? queryTheme)
    {
        if (definition is null)
            return;

        throw MaterialResolutionFailures.Create(
            MaterialResolutionFailureKind.RecipeFailure,
            $"Material token source '{source.GetType().FullName}' returned false but also " +
            $"provided a definition for raw token '{token.Path}'.",
            token: token,
            theme: queryTheme,
            path: [new MaterialResolutionFrame(token.Path, token.Layer, token.ValueType)],
            expected: token.ValueType,
            actual: definition.GetType());
    }

    private static void ValidateLocalDefinitionHit(
        IMaterialTokenSource source,
        MaterialToken token,
        object? definition,
        ThemeVariant? queryTheme)
    {
        var path = new[] { new MaterialResolutionFrame(token.Path, token.Layer, token.ValueType) };
        if (definition is null)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.NullDefinition,
                $"Material token source '{source.GetType().FullName}' returned a null definition " +
                $"for raw token '{token.Path}'.",
                token: token,
                theme: queryTheme,
                path: path,
                expected: token.ValueType);
        }

        var actualType = definition is TokenDefinition typedDefinition
            ? typedDefinition.ValueType
            : definition.GetType();
        var valid = definition is TokenDefinition
            ? actualType == token.ValueType
            : token.ValueType.IsInstanceOfType(definition);
        if (!valid)
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.WrongType,
                $"Material token source '{source.GetType().FullName}' returned " +
                $"'{actualType.FullName}' for raw token '{token.Path}', but " +
                $"'{token.ValueType.FullName}' is required.",
                token: token,
                theme: queryTheme,
                path: path,
                expected: token.ValueType,
                actual: actualType);
        }
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_subscriptionsActive)
            UpdateSourceSubscriptions();
        Invalidate();
    }

    private static void ValidateChildSourceForInsertion(IMaterialTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is IResourceProvider { Owner: not null } provider)
        {
            throw new InvalidOperationException(
                $"Material token source '{source.GetType().FullName}' is already installed as an " +
                $"independent resource provider on '{provider.Owner.GetType().FullName}'. A source " +
                "instance cannot be installed independently and in MaterialTheme.Children at the same time.");
        }
    }

    private void ValidateChildSourcePlacements()
    {
        foreach (var source in _children)
            ValidateChildSourceForInsertion(source);
    }

    private void ActivateSubscriptions()
    {
        if (_subscriptionsActive)
            return;

        _subscriptionsActive = true;
        try
        {
            UpdateSourceSubscriptions();
        }
        catch
        {
            DeactivateSubscriptions();
            throw;
        }
    }

    private void DeactivateSubscriptions()
    {
        if (!_subscriptionsActive)
            return;

        _subscriptionsActive = false;
        foreach (var source in _sourceSubscriptions.Keys)
            source.Changed -= OnTokenSourceChanged;
        _sourceSubscriptions.Clear();
    }

    private void UpdateSourceSubscriptions()
    {
        var desired = new Dictionary<IMaterialTokenSource, int>(ReferenceEqualityComparer.Instance);
        foreach (var source in _children)
        {
            ValidateChildSourceForInsertion(source);
            desired.TryGetValue(source, out var count);
            desired[source] = count + 1;
        }

        foreach (var source in _sourceSubscriptions.Keys.ToArray())
        {
            if (!desired.ContainsKey(source))
                source.Changed -= OnTokenSourceChanged;
        }

        foreach (var source in desired.Keys)
        {
            if (!_sourceSubscriptions.ContainsKey(source))
                source.Changed += OnTokenSourceChanged;
        }

        _sourceSubscriptions.Clear();
        foreach (var pair in desired)
            _sourceSubscriptions.Add(pair.Key, pair.Value);
    }

    private void OnTokenSourceChanged(object? sender, EventArgs e)
    {
        if (sender is IMaterialTokenSource source)
            ValidateChildSourceForInsertion(source);
        Invalidate();
    }

    private void Invalidate() => RaiseResourcesChanged();
}
