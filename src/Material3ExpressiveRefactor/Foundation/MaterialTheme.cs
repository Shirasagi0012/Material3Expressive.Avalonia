using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Material3ExpressiveRefactor.Foundation;

public class MaterialTheme : ResourceProvider
{
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        throw new NotImplementedException();
    }

    public override bool HasResources => true;

    [Content]
    public AvaloniaList<ITokenSource> Children { get; } = [];
}
