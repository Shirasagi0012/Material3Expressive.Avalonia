using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Owns the live projection from a native toggle icon button's theme, axes and checked state to
/// semantic defaults.
/// </summary>
internal sealed class ToggleIconButtonDefaultsAdapter
{
    private static readonly AttachedProperty<ToggleIconButtonDefaultsAdapter?> AdapterProperty =
        AvaloniaProperty.RegisterAttached<ToggleButton, ToggleIconButtonDefaultsAdapter?>(
            "__ToggleIconButtonDefaultsAdapter", typeof(ToggleIconButtonDefaultsAdapter));

    private readonly ToggleButton _owner;
    private bool _active;
    private bool _refreshing;

    private ToggleIconButtonDefaultsAdapter(ToggleButton owner)
    {
        _owner = owner;
    }

    internal static ToggleIconButtonDefaultsAdapter GetOrCreate(ToggleButton owner)
    {
        if (owner.GetValue(AdapterProperty) is { } existing)
            return existing;

        var created = new ToggleIconButtonDefaultsAdapter(owner);
        owner.SetValue(AdapterProperty, created);
        return created;
    }

    internal static bool TryGet(ToggleButton owner, out ToggleIconButtonDefaultsAdapter? adapter)
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

        _owner.ClearValue(M3ToggleIconButton.DefaultColorsProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultMetricsProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultShapesProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultShapeMotionProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultOutlineThicknessProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultStateLayerBrushProperty);
    }

    private void OnAttachedToLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialThemeScope.ResolvedThemeProperty
            || change.Property == ThemeVariantScope.ActualThemeVariantProperty
            || change.Property == M3IconButton.VariantProperty
            || change.Property == M3IconButton.SizeProperty
            || change.Property == M3IconButton.ButtonWidthProperty
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
            var variant = M3IconButton.GetVariant(_owner);
            var size = M3IconButton.GetSize(_owner);
            var width = M3IconButton.GetButtonWidth(_owner);
            var isChecked = _owner.IsChecked == true;
            var colors = ToggleIconButtonDefaults.Colors(theme, variant, isDark);

            M3ToggleIconButton.SetDefaultColors(_owner, colors);
            M3ToggleIconButton.SetDefaultMetrics(
                _owner,
                ToggleIconButtonDefaults.Metrics(size, width));
            M3ToggleIconButton.SetDefaultShapes(
                _owner,
                ToggleIconButtonDefaults.Shapes(size));
            M3ToggleIconButton.SetDefaultShapeMotion(
                _owner,
                ToggleIconButtonDefaults.ShapeMotion(theme));
            M3ToggleIconButton.SetDefaultOutlineThickness(
                _owner,
                ToggleIconButtonDefaults.OutlineThickness(colors, size, isChecked));
            M3ToggleIconButton.SetDefaultStateLayerBrush(
                _owner,
                colors.StateLayer(isChecked));
        }
        finally
        {
            _refreshing = false;
        }
    }
}
