// Foundation WindowInsetsPadding.kt / InsetsPaddingModifierNode, pinned AndroidX baseline; Apache-2.0.
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Foundation.Rendering;

/// <summary>Applies only physical window insets not consumed by ancestors, then publishes consumption to its child.</summary>
public sealed class MaterialWindowInsetsPadding : Decorator
{
    public static readonly StyledProperty<Thickness> InsetsProperty =
        AvaloniaProperty.Register<MaterialWindowInsetsPadding, Thickness>(nameof(Insets));
    public Thickness Insets { get => GetValue(InsetsProperty); set => SetValue(InsetsProperty, value); }

    public MaterialWindowInsetsPadding() => MaterialWindowInsets.EnsureScope(this);
    protected override bool BypassFlowDirectionPolicies => true;
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InsetsProperty) SetValue(MaterialWindowInsets.AppliedInsetsProperty, Insets);
        if (change.Property == MaterialWindowInsets.UnconsumedInsetsProperty) InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        // Density is a layout input; recompute physical rounding when measuring in a new root scale.
        MaterialWindowInsets.Refresh(this);
        var padding = GetValue(MaterialWindowInsets.UnconsumedInsetsProperty);
        var horizontal = padding.Left + padding.Right;
        var vertical = padding.Top + padding.Bottom;
        Child?.Measure(new Size(Math.Max(0, availableSize.Width - horizontal), Math.Max(0, availableSize.Height - vertical)));
        var child = Child?.DesiredSize ?? default;
        return new Size(Math.Min(availableSize.Width, child.Width + horizontal), Math.Min(availableSize.Height, child.Height + vertical));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var padding = GetValue(MaterialWindowInsets.UnconsumedInsetsProperty);
        Child?.Arrange(new Rect(padding.Left, padding.Top,
            Math.Max(0, finalSize.Width - padding.Left - padding.Right),
            Math.Max(0, finalSize.Height - padding.Top - padding.Bottom)));
        return finalSize;
    }
}
