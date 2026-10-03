using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

/// <summary>A non-interactive Material badge, with an optional content slot.</summary>
public class Badge : ContentControl
{
    public Badge() => UpdateContent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty || change.Property == ContentTemplateProperty)
            UpdateContent();
    }

    // An explicitly supplied empty template is still a content lambda upstream.
    private void UpdateContent() => PseudoClasses.Set(":has-content", Content is not null || ContentTemplate is not null);
}
