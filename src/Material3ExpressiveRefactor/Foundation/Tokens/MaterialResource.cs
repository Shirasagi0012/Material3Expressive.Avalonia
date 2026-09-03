using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens.Sources;

namespace Material3ExpressiveRefactor.Foundation.Tokens;

public class MaterialResource : ResourceProvider
{
    public MaterialResource()
    {
        Children.CollectionChanged += OnChildrenChanged;
    }

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is not MaterialTokenKey tokenKey)
        {
            value = null;
            return false;
        }

        for (var index = Children.Count - 1; index >= 0; index--)
        {
            var source = Children[index];
            if (source.TryGetToken(tokenKey, theme, out var sourceValue))
            {
                value = sourceValue;
                return true;
            }
        }

        value = null;
        return false;
    }

    public override bool HasResources => Children.Count != 0;

    [Content]
    public AvaloniaList<ITokenSource> Children { get; } = new()
    {
        ResetBehavior = ResetBehavior.Remove,
    };

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSourceSubscriptions(e);
        RaiseResourcesChanged();
    }

    private void UpdateSourceSubscriptions(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is { })
        {
            foreach (var item in e.OldItems)
            {
                if (item is ITokenSource source)
                    source.Changed -= OnSourceChanged;
            }
        }

        if (e.NewItems is { })
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
