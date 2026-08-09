using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Owns the live projection from a native icon-button owner's theme and axes to semantic defaults.
/// </summary>
/// <remarks>
/// The adapter writes only private fallback properties on <see cref="M3IconButton"/>. The
/// Material ControlTheme binds those properties to ordinary Avalonia presentation properties at
/// theme priority, leaving application styles and local values free to win.
/// </remarks>
internal sealed class IconButtonDefaultsAdapter
{
    private static readonly AttachedProperty<IconButtonDefaultsAdapter?> AdapterProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonDefaultsAdapter?>(
            "__IconButtonDefaultsAdapter", typeof(IconButtonDefaultsAdapter));

    private readonly Button _owner;
    private bool _active;
    private bool _refreshing;

    private IconButtonDefaultsAdapter(Button owner)
    {
        _owner = owner;
    }

    internal static IconButtonDefaultsAdapter GetOrCreate(Button owner)
    {
        if (owner.GetValue(AdapterProperty) is { } existing)
            return existing;

        var created = new IconButtonDefaultsAdapter(owner);
        owner.SetValue(AdapterProperty, created);
        return created;
    }

    internal static bool TryGet(Button owner, out IconButtonDefaultsAdapter? adapter)
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

        _owner.ClearValue(M3IconButton.DefaultColorsProperty);
        _owner.ClearValue(M3IconButton.DefaultMetricsProperty);
        _owner.ClearValue(M3IconButton.DefaultShapesProperty);
        _owner.ClearValue(M3IconButton.DefaultShapeMotionProperty);
        _owner.ClearValue(M3IconButton.DefaultOutlineThicknessProperty);
    }

    private void OnAttachedToLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == MaterialThemeScope.ResolvedThemeProperty
            || change.Property == ThemeVariantScope.ActualThemeVariantProperty
            || change.Property == M3IconButton.VariantProperty
            || change.Property == M3IconButton.SizeProperty
            || change.Property == M3IconButton.ButtonWidthProperty)
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

            M3IconButton.SetDefaultColors(
                _owner,
                IconButtonDefaults.Colors(theme, variant, isDark));
            M3IconButton.SetDefaultMetrics(
                _owner,
                IconButtonDefaults.Metrics(size, width));
            M3IconButton.SetDefaultShapes(
                _owner,
                IconButtonDefaults.Shapes(size));
            M3IconButton.SetDefaultShapeMotion(
                _owner,
                IconButtonDefaults.ShapeMotion(theme));
            M3IconButton.SetDefaultOutlineThickness(
                _owner,
                IconButtonDefaults.OutlineThickness(variant, size));
        }
        finally
        {
            _refreshing = false;
        }
    }
}
