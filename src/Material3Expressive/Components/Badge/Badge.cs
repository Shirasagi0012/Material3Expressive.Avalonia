using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

/// <summary>A non-interactive Material badge, with an optional content slot.</summary>
public class Badge : ContentControl
{
    internal static readonly StyledProperty<Thickness> DefaultContentPaddingProperty = AvaloniaProperty.Register<Badge, Thickness>(nameof(DefaultContentPadding));
    internal Thickness DefaultContentPadding { get => GetValue(DefaultContentPaddingProperty); set => SetValue(DefaultContentPaddingProperty, value); }
    internal static readonly StyledProperty<CornerRadius> DefaultShapeProperty = AvaloniaProperty.Register<Badge, CornerRadius>(nameof(DefaultShape));
    internal CornerRadius DefaultShape { get => GetValue(DefaultShapeProperty); set => SetValue(DefaultShapeProperty, value); }
    internal static readonly StyledProperty<TextStyle?> DefaultLabelStyleProperty = AvaloniaProperty.Register<Badge, TextStyle?>(nameof(DefaultLabelStyle));
    internal TextStyle? DefaultLabelStyle { get => GetValue(DefaultLabelStyleProperty); set => SetValue(DefaultLabelStyleProperty, value); }

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
