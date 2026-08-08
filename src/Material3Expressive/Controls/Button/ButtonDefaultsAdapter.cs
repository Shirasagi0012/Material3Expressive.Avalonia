using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Owns the live projection from a native Button's theme/axes to its internal semantic defaults.
/// </summary>
/// <remarks>
/// This adapter is stored on its owner, not in a global registry. It writes only the private
/// fallback properties on <see cref="ButtonAssist" />; the Material ControlTheme remains
/// responsible for binding those fallbacks to public Avalonia properties at style priority.
/// </remarks>
internal sealed class ButtonDefaultsAdapter
{
    private static readonly AttachedProperty<ButtonDefaultsAdapter?> AdapterProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonDefaultsAdapter?>(
            "__ButtonDefaultsAdapter", typeof(ButtonDefaultsAdapter));

    private readonly Button _owner;
    private bool _active;
    private bool _refreshing;

    private ButtonDefaultsAdapter(Button owner)
    {
        _owner = owner;
    }

    internal static ButtonDefaultsAdapter GetOrCreate(Button owner)
    {
        if (owner.GetValue(AdapterProperty) is { } existing)
            return existing;

        var created = new ButtonDefaultsAdapter(owner);
        owner.SetValue(AdapterProperty, created);
        return created;
    }

    internal static bool TryGet(Button owner, out ButtonDefaultsAdapter? adapter)
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

        // These are adapter-owned local values rather than public presentation properties, so
        // clearing them cannot disturb an application's Style or local value.
        _owner.ClearValue(ButtonAssist.DefaultColorsProperty);
        _owner.ClearValue(ButtonAssist.DefaultMetricsProperty);
        _owner.ClearValue(ButtonAssist.DefaultShapesProperty);
        _owner.ClearValue(ButtonAssist.DefaultElevationProperty);
        _owner.ClearValue(ButtonAssist.DefaultShapeMotionProperty);
        _owner.ClearValue(ButtonAssist.DefaultOutlineThicknessProperty);
    }

    private void OnAttachedToLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialThemeScope.ResolvedThemeProperty
            || change.Property == ThemeVariantScope.ActualThemeVariantProperty
            || change.Property == ButtonAssist.VariantProperty
            || change.Property == ButtonAssist.SizeProperty)
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
            var variant = ButtonAssist.GetVariant(_owner);
            var size = ButtonAssist.GetSize(_owner);
            var metrics = ButtonDefaults.Metrics(size);

            ButtonAssist.SetDefaultColors(_owner, ButtonDefaults.Colors(theme, variant, isDark));
            ButtonAssist.SetDefaultMetrics(_owner, metrics);
            ButtonAssist.SetDefaultShapes(_owner, ButtonDefaults.Shapes(size));
            ButtonAssist.SetDefaultElevation(_owner, ButtonDefaults.Elevation(variant));
            ButtonAssist.SetDefaultShapeMotion(_owner, ButtonDefaults.ShapeMotion(theme));
            ButtonAssist.SetDefaultOutlineThickness(
                _owner,
                ButtonDefaults.OutlineThickness(variant, size));
        }
        finally
        {
            _refreshing = false;
        }
    }
}
