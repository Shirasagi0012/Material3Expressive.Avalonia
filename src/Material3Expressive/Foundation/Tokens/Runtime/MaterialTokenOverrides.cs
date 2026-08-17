using Avalonia.Controls;
using Avalonia.Styling;
using System.ComponentModel;
using MaterialDesignTokens.Avalonia;
using SharedMaterialToken = MaterialDesignTokens.Avalonia.MaterialToken;
using SharedToken = MaterialDesignTokens.Avalonia.Token;
using SharedTokenDefinition = MaterialDesignTokens.Avalonia.TokenDefinition;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>A sparse resource provider for local, strongly typed token overrides.</summary>
public class MaterialTokenOverrides : ResourceProvider, IMaterialTokenSource
{
    private readonly Dictionary<MaterialToken, object> _definitions = [];
    private readonly Dictionary<SharedMaterialToken, SharedTokenDefinition> _rawDefinitions =
        new(ReferenceEqualityComparer.Instance);
    private event EventHandler? SourceChanged;
    private int _updateDepth;
    private bool _changedDuringUpdate;

    /// <inheritdoc />
    public override bool HasResources => _definitions.Count != 0 || _rawDefinitions.Count != 0;

    /// <inheritdoc />
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is MaterialToken token && _definitions.TryGetValue(token, out var definition))
        {
            value = definition;
            return true;
        }

        if (key is SharedMaterialToken rawToken &&
            _rawDefinitions.TryGetValue(rawToken, out var rawDefinition))
        {
            value = rawDefinition;
            return true;
        }

        value = null;
        return false;
    }

    bool IMaterialTokenSource.TryGetRawToken(
        SharedMaterialToken token,
        ThemeVariant? queryTheme,
        out object? definition)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (_rawDefinitions.TryGetValue(token, out var typedDefinition))
        {
            definition = typedDefinition;
            return true;
        }

        definition = null;
        return false;
    }

    event EventHandler? IMaterialTokenSource.Changed
    {
        add => SourceChanged += value;
        remove => SourceChanged -= value;
    }

    /// <summary>Gets an explicitly set definition, or <see langword="null" /> when unset.</summary>
    [Obsolete(
        "Legacy Runtime.MaterialToken override. Use the MaterialDesignTokens.Avalonia overload.",
        DiagnosticId = "M3E1001")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TokenDefinition<T>? GetDefinition<T>(MaterialToken<T> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _definitions.TryGetValue(token, out var value) ? (TokenDefinition<T>)value : null;
    }

    /// <summary>Sets an explicit definition.</summary>
    [Obsolete(
        "Legacy Runtime.MaterialToken override. Use the MaterialDesignTokens.Avalonia overload.",
        DiagnosticId = "M3E1001")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void SetDefinition<T>(MaterialToken<T> token, TokenDefinition<T> definition)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[token] = definition;
        NotifyChanged();
    }

    /// <summary>Sets a literal override.</summary>
    [Obsolete(
        "Legacy Runtime.MaterialToken override. Use the MaterialDesignTokens.Avalonia overload.",
        DiagnosticId = "M3E1001")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Set<T>(MaterialToken<T> token, T value) => SetDefinition(token, Token.Literal(value));

    /// <summary>Sets a literal, alias, or recipe definition.</summary>
    [Obsolete(
        "Legacy Runtime.MaterialToken override. Use the MaterialDesignTokens.Avalonia overload.",
        DiagnosticId = "M3E1001")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Set<T>(MaterialToken<T> token, TokenDefinition<T> definition) =>
        SetDefinition(token, definition);

    /// <summary>Sets an alias override.</summary>
    [Obsolete(
        "Legacy Runtime.MaterialToken override. Use the MaterialDesignTokens.Avalonia overload.",
        DiagnosticId = "M3E1001")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Alias<T>(MaterialToken<T> token, MaterialToken<T> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        SetDefinition(token, Token.Alias(target));
    }

    /// <summary>Removes an override so lookup continues toward ancestor resources and defaults.</summary>
    [Obsolete(
        "Legacy Runtime.MaterialToken override. Use the MaterialDesignTokens.Avalonia overload.",
        DiagnosticId = "M3E1001")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Clear(MaterialToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!_definitions.Remove(token))
            return false;

        NotifyChanged();
        return true;
    }

    /// <summary>Gets an explicitly set shared Raw definition, or null when unset.</summary>
    public MaterialDesignTokens.Avalonia.TokenDefinition<T>? GetDefinition<T>(
        MaterialDesignTokens.Avalonia.MaterialToken<T> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _rawDefinitions.TryGetValue(token, out var value)
            ? (MaterialDesignTokens.Avalonia.TokenDefinition<T>)value
            : null;
    }

    /// <summary>Sets an explicit shared Raw definition.</summary>
    public void SetDefinition<T>(
        MaterialDesignTokens.Avalonia.MaterialToken<T> token,
        MaterialDesignTokens.Avalonia.TokenDefinition<T> definition)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(definition);
        _rawDefinitions[token] = definition;
        NotifyChanged();
    }

    /// <summary>Sets a shared Raw literal override.</summary>
    public void Set<T>(MaterialDesignTokens.Avalonia.MaterialToken<T> token, T value) =>
        SetDefinition(token, SharedToken.Literal(value));

    /// <summary>Sets a shared Raw literal, alias, or recipe definition.</summary>
    public void Set<T>(
        MaterialDesignTokens.Avalonia.MaterialToken<T> token,
        MaterialDesignTokens.Avalonia.TokenDefinition<T> definition) =>
        SetDefinition(token, definition);

    /// <summary>Sets a shared Raw alias override.</summary>
    public void Alias<T>(
        MaterialDesignTokens.Avalonia.MaterialToken<T> token,
        MaterialDesignTokens.Avalonia.MaterialToken<T> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        SetDefinition(token, SharedToken.Alias(target));
    }

    /// <summary>Removes a shared Raw override so lookup continues outward.</summary>
    public bool Clear(SharedMaterialToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!_rawDefinitions.Remove(token))
            return false;

        NotifyChanged();
        return true;
    }

    /// <summary>Coalesces several mutations into one resource-change notification.</summary>
    public IDisposable BeginUpdate()
    {
        _updateDepth++;
        return new UpdateScope(this);
    }

    private void NotifyChanged()
    {
        if (_updateDepth > 0)
            _changedDuringUpdate = true;
        else
            PublishChanged();
    }

    private void EndUpdate()
    {
        if (_updateDepth == 0)
            return;

        _updateDepth--;
        if (_updateDepth == 0 && _changedDuringUpdate)
        {
            _changedDuringUpdate = false;
            PublishChanged();
        }
    }

    private void PublishChanged()
    {
        RaiseResourcesChanged();
        SourceChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class UpdateScope(MaterialTokenOverrides owner) : IDisposable
    {
        private MaterialTokenOverrides? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.EndUpdate();
    }
}
