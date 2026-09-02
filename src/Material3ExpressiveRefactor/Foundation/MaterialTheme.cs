using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;

using System.Collections.Specialized;

namespace Material3ExpressiveRefactor.Foundation;

public class MaterialTheme : ResourceProvider
{
    private readonly AvaloniaList<ITokenSource> _children = new()
    {
        ResetBehavior = ResetBehavior.Remove,
    };

    public MaterialTheme()
    {
        _children.CollectionChanged += OnChildrenChanged;
    }

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is not MaterialTokenKey tokenKey)
        {
            value = null;
            return false;
        }

        for (var index = _children.Count - 1; index >= 0; index--)
        {
            var source = _children[index];
            if (source.TryGetToken(tokenKey, theme, out var sourceValue))
            {
                value = sourceValue;
                return true;
            }
        }

        value = null;
        return false;
    }

    public override bool HasResources => true;

    [Content]
    public AvaloniaList<ITokenSource> Children => _children;

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSourceSubscriptions(e);
        RaiseResourcesChanged();
    }

    private void UpdateSourceSubscriptions(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is ITokenSource source)
                    source.Changed -= OnSourceChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is ITokenSource source)
                    source.Changed += OnSourceChanged;
            }
        }
    }

    private void OnSourceChanged(object? sender, EventArgs e) => RaiseResourcesChanged();
}
