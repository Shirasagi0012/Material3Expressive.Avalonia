using Avalonia;

namespace Material3Expressive.Foundation.Rendering;

/// <summary>Defaults supplied by an icon slot to symbols without explicit dimensions.</summary>
public sealed class MaterialIcon : AvaloniaObject
{
    private MaterialIcon() { }

    public static readonly AttachedProperty<double> DefaultSizeProperty =
        AvaloniaProperty.RegisterAttached<MaterialIcon, StyledElement, double>(
            "DefaultSize", 24d, inherits: true,
            validate: value => double.IsFinite(value) && value >= 0);

    public static double GetDefaultSize(StyledElement element) => element.GetValue(DefaultSizeProperty);
    public static void SetDefaultSize(StyledElement element, double value) => element.SetValue(DefaultSizeProperty, value);
}
