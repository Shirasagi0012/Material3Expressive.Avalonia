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
    private bool _refreshPending;
    private MaterialThemeState _themeState = MaterialThemeState.Default;
    private IDisposable? _themeSubscription;

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
            RequestRefresh();
            return;
        }

        _active = true;
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        _themeSubscription = MaterialElementContext.Ensure(_owner).Theme.Subscribe(OnThemeChanged);
    }

    internal void Deactivate()
    {
        if (!_active)
            return;

        _active = false;
        _owner.PropertyChanged -= OnOwnerPropertyChanged;
        _themeSubscription?.Dispose();
        _themeSubscription = null;

        _owner.ClearValue(M3ToggleIconButton.DefaultColorsProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultMetricsProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultShapesProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultShapeMotionProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultOutlineThicknessProperty);
        _owner.ClearValue(M3ToggleIconButton.DefaultStateLayerBrushProperty);
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == M3IconButton.VariantProperty
            || change.Property == M3IconButton.SizeProperty
            || change.Property == M3IconButton.ButtonWidthProperty
            || change.Property == ToggleButton.IsCheckedProperty)
        {
            RequestRefresh();
        }
    }

    private void OnThemeChanged(MaterialThemeState state)
    {
        _themeState = state;
        RequestRefresh();
    }

    private void RequestRefresh()
    {
        if (!_active)
            return;

        if (_refreshing)
        {
            _refreshPending = true;
            return;
        }

        _refreshing = true;
        try
        {
            do
            {
                _refreshPending = false;
                var variant = M3IconButton.GetVariant(_owner);
                var size = M3IconButton.GetSize(_owner);
                var width = M3IconButton.GetButtonWidth(_owner);
                var isChecked = _owner.IsChecked == true;
                var colors = ToggleIconButtonDefaults.Colors(
                    _themeState.Theme,
                    variant,
                    _themeState.IsDark);

                M3ToggleIconButton.SetDefaultColors(_owner, colors);
                M3ToggleIconButton.SetDefaultMetrics(
                    _owner,
                    ToggleIconButtonDefaults.Metrics(size, width));
                M3ToggleIconButton.SetDefaultShapes(
                    _owner,
                    ToggleIconButtonDefaults.Shapes(size));
                M3ToggleIconButton.SetDefaultShapeMotion(
                    _owner,
                    ToggleIconButtonDefaults.ShapeMotion(_themeState.Theme));
                M3ToggleIconButton.SetDefaultOutlineThickness(
                    _owner,
                    ToggleIconButtonDefaults.OutlineThickness(colors, size, isChecked));
                M3ToggleIconButton.SetDefaultStateLayerBrush(
                    _owner,
                    colors.StateLayer(isChecked));
            }
            while (_refreshPending);
        }
        finally
        {
            _refreshing = false;
        }
    }
}
