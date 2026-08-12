using Avalonia.Controls;
using Avalonia.Styling;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>A sparse resource provider for local, strongly typed token overrides.</summary>
public class MaterialTokenOverrides : ResourceProvider
{
    private readonly Dictionary<MaterialToken, object> _definitions = [];
    private int _updateDepth;
    private bool _changedDuringUpdate;

    /// <inheritdoc />
    public override bool HasResources => _definitions.Count != 0;

    /// <inheritdoc />
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is MaterialToken token && _definitions.TryGetValue(token, out var definition))
        {
            value = definition;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Gets an explicitly set definition, or <see langword="null" /> when unset.</summary>
    public TokenDefinition<T>? GetDefinition<T>(MaterialToken<T> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _definitions.TryGetValue(token, out var value) ? (TokenDefinition<T>)value : null;
    }

    /// <summary>Sets an explicit definition.</summary>
    public void SetDefinition<T>(MaterialToken<T> token, TokenDefinition<T> definition)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(definition);
        _definitions[token] = definition;
        Changed();
    }

    /// <summary>Sets a literal override.</summary>
    public void Set<T>(MaterialToken<T> token, T value) => SetDefinition(token, Token.Literal(value));

    /// <summary>Sets a literal, alias, or recipe definition.</summary>
    public void Set<T>(MaterialToken<T> token, TokenDefinition<T> definition) =>
        SetDefinition(token, definition);

    /// <summary>Sets an alias override.</summary>
    public void Alias<T>(MaterialToken<T> token, MaterialToken<T> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        SetDefinition(token, Token.Alias(target));
    }

    /// <summary>Removes an override so lookup continues toward ancestor resources and defaults.</summary>
    public bool Clear(MaterialToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!_definitions.Remove(token))
            return false;

        Changed();
        return true;
    }

    /// <summary>Coalesces several mutations into one resource-change notification.</summary>
    public IDisposable BeginUpdate()
    {
        _updateDepth++;
        return new UpdateScope(this);
    }

    private void Changed()
    {
        if (_updateDepth > 0)
            _changedDuringUpdate = true;
        else
            RaiseResourcesChanged();
    }

    private void EndUpdate()
    {
        if (_updateDepth == 0)
            return;

        _updateDepth--;
        if (_updateDepth == 0 && _changedDuringUpdate)
        {
            _changedDuringUpdate = false;
            RaiseResourcesChanged();
        }
    }

    private sealed class UpdateScope(MaterialTokenOverrides owner) : IDisposable
    {
        private MaterialTokenOverrides? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.EndUpdate();
    }
}
