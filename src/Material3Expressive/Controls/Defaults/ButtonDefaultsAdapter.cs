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
/// fallback properties on <see cref="M3Button" />; the Material ControlTheme remains
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
    private bool _refreshPending;
    private MaterialThemeState _themeState = MaterialThemeState.Default;
    private IDisposable? _themeSubscription;

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

        // These are adapter-owned local values rather than public presentation properties, so
        // clearing them cannot disturb an application's Style or local value.
        _owner.ClearValue(M3Button.DefaultColorsProperty);
        _owner.ClearValue(M3Button.DefaultMetricsProperty);
        _owner.ClearValue(M3Button.DefaultShapesProperty);
        _owner.ClearValue(M3Button.DefaultElevationProperty);
        _owner.ClearValue(M3Button.DefaultShapeMotionProperty);
        _owner.ClearValue(M3Button.DefaultOutlineThicknessProperty);
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == M3Button.VariantProperty
            || change.Property == M3Button.SizeProperty)
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
                var variant = M3Button.GetVariant(_owner);
                var size = M3Button.GetSize(_owner);
                var metrics = ButtonDefaults.Metrics(size);

                M3Button.SetDefaultColors(
                    _owner,
                    ButtonDefaults.Colors(_themeState.Theme, variant, _themeState.IsDark));
                M3Button.SetDefaultMetrics(_owner, metrics);
                M3Button.SetDefaultShapes(_owner, ButtonDefaults.Shapes(size));
                M3Button.SetDefaultElevation(_owner, ButtonDefaults.Elevation(variant));
                M3Button.SetDefaultShapeMotion(_owner, ButtonDefaults.ShapeMotion(_themeState.Theme));
                M3Button.SetDefaultOutlineThickness(
                    _owner,
                    ButtonDefaults.OutlineThickness(variant, size));
            }
            while (_refreshPending);
        }
        finally
        {
            _refreshing = false;
        }
    }
}
