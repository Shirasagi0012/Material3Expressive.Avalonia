using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Owns the live projection from a native ToggleButton's theme, axes and checked state to its
/// internal semantic defaults.
/// </summary>
internal sealed class ToggleButtonDefaultsAdapter
{
    private static readonly AttachedProperty<ToggleButtonDefaultsAdapter?> AdapterProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleButtonDefaultsAdapter?>(
            "__ToggleButtonDefaultsAdapter", typeof(ToggleButtonDefaultsAdapter));

    private readonly ToggleButton _owner;
    private bool _active;
    private bool _refreshing;

    private ToggleButtonDefaultsAdapter(ToggleButton owner)
    {
        _owner = owner;
    }

    internal static ToggleButtonDefaultsAdapter GetOrCreate(ToggleButton owner)
    {
        if (owner.GetValue(AdapterProperty) is { } existing)
            return existing;

        var created = new ToggleButtonDefaultsAdapter(owner);
        owner.SetValue(AdapterProperty, created);
        return created;
    }

    internal static bool TryGet(ToggleButton owner, out ToggleButtonDefaultsAdapter? adapter)
    {
        adapter = owner.GetValue(AdapterProperty);
        return adapter is not null;
    }

    internal void Activate()
    {
        if (_active)
        {
            Refresh();
            return;
        }

        _active = true;
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        _owner.AttachedToLogicalTree += OnAttachedToLogicalTree;
        _owner.DetachedFromLogicalTree += OnDetachedFromLogicalTree;
        Refresh();
    }

    internal void Deactivate()
    {
        if (!_active)
            return;

        _active = false;
        _owner.PropertyChanged -= OnOwnerPropertyChanged;
        _owner.AttachedToLogicalTree -= OnAttachedToLogicalTree;
        _owner.DetachedFromLogicalTree -= OnDetachedFromLogicalTree;

        // These are adapter-owned local values. Clear them instead of assigning null so a
        // replacement theme/style can provide the attached properties after deactivation.
        _owner.ClearValue(M3ToggleButton.DefaultColorsProperty);
        _owner.ClearValue(M3ToggleButton.DefaultMetricsProperty);
        _owner.ClearValue(M3ToggleButton.DefaultShapesProperty);
        _owner.ClearValue(M3ToggleButton.DefaultShapeMotionProperty);
        _owner.ClearValue(M3ToggleButton.DefaultOutlineThicknessProperty);
        _owner.ClearValue(M3ToggleButton.DefaultStateLayerBrushProperty);
    }

    private void OnAttachedToLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialThemeScope.ResolvedThemeProperty
            || change.Property == ThemeVariantScope.ActualThemeVariantProperty
            || change.Property == M3Button.VariantProperty
            || change.Property == M3Button.SizeProperty
            || change.Property == ToggleButton.IsCheckedProperty)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (!_active || _refreshing)
            return;

        _refreshing = true;
        try
        {
            var theme = MaterialThemeScope.GetResolvedTheme(_owner);
            var isDark = ColorScheme.IsDark(_owner.ActualThemeVariant ?? ThemeVariant.Default);
            var variant = M3Button.GetVariant(_owner);
            var size = M3Button.GetSize(_owner);
            var isChecked = _owner.IsChecked == true;
            var colors = ToggleButtonDefaults.Colors(theme, variant, isDark);

            M3ToggleButton.SetDefaultColors(_owner, colors);
            M3ToggleButton.SetDefaultMetrics(_owner, ToggleButtonDefaults.Metrics(size));
            M3ToggleButton.SetDefaultShapes(_owner, ToggleButtonDefaults.Shapes(size));
            M3ToggleButton.SetDefaultShapeMotion(_owner, ToggleButtonDefaults.ShapeMotion(theme));
            M3ToggleButton.SetDefaultOutlineThickness(
                _owner,
                ToggleButtonDefaults.OutlineThickness(colors, size, isChecked));
            M3ToggleButton.SetDefaultStateLayerBrush(_owner, colors.StateLayer(isChecked));
        }
        finally
        {
            _refreshing = false;
        }
    }
}
