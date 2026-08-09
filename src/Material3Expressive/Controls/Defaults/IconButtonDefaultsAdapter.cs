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
    private bool _refreshPending;
    private MaterialThemeState _themeState = MaterialThemeState.Default;
    private IDisposable? _themeSubscription;

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

        _owner.ClearValue(M3IconButton.DefaultColorsProperty);
        _owner.ClearValue(M3IconButton.DefaultMetricsProperty);
        _owner.ClearValue(M3IconButton.DefaultShapesProperty);
        _owner.ClearValue(M3IconButton.DefaultShapeMotionProperty);
        _owner.ClearValue(M3IconButton.DefaultOutlineThicknessProperty);
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == M3IconButton.VariantProperty
            || change.Property == M3IconButton.SizeProperty
            || change.Property == M3IconButton.ButtonWidthProperty)
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

                M3IconButton.SetDefaultColors(
                    _owner,
                    IconButtonDefaults.Colors(_themeState.Theme, variant, _themeState.IsDark));
                M3IconButton.SetDefaultMetrics(
                    _owner,
                    IconButtonDefaults.Metrics(size, width));
                M3IconButton.SetDefaultShapes(
                    _owner,
                    IconButtonDefaults.Shapes(size));
                M3IconButton.SetDefaultShapeMotion(
                    _owner,
                    IconButtonDefaults.ShapeMotion(_themeState.Theme));
                M3IconButton.SetDefaultOutlineThickness(
                    _owner,
                    IconButtonDefaults.OutlineThickness(variant, size));
            }
            while (_refreshPending);
        }
        finally
        {
            _refreshing = false;
        }
    }
}
