using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens.Runtime;
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
[Obsolete(
    "Legacy component-defaults adapter. Consume typed MaterialValueKey<T> resources.",
    DiagnosticId = "M3E1004")]
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
        _owner.AttachedToLogicalTree += OnLogicalTreeChanged;
        _owner.DetachedFromLogicalTree += OnLogicalTreeChanged;
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        ((IResourceHost)_owner).ResourcesChanged += OnResourcesChanged;
        _themeSubscription = MaterialElementContext.Ensure(_owner).Theme.Subscribe(OnThemeChanged);
    }

    internal void Deactivate()
    {
        if (!_active)
            return;

        _active = false;
        _owner.AttachedToLogicalTree -= OnLogicalTreeChanged;
        _owner.DetachedFromLogicalTree -= OnLogicalTreeChanged;
        _owner.PropertyChanged -= OnOwnerPropertyChanged;
        ((IResourceHost)_owner).ResourcesChanged -= OnResourcesChanged;
        _themeSubscription?.Dispose();
        _themeSubscription = null;

        // These are adapter-owned local values rather than public presentation properties, so
        // clearing them cannot disturb an application's Style or local value.
        _owner.ClearValue(M3Button.DefaultColorsProperty);
        _owner.ClearValue(M3Button.DefaultMetricsProperty);
        _owner.ClearValue(M3Button.DefaultElevationProperty);
        _owner.ClearValue(M3Button.DefaultShapeMotionProperty);
        _owner.ClearValue(M3Button.DefaultOutlineThicknessProperty);
    }

    private void OnLogicalTreeChanged(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        // A detached button can still receive resource notifications through its stale logical
        // ancestors. Pause while detached and resume on re-attach if the theme still opts in.
        if (((ILogical)_owner).IsAttachedToLogicalTree)
        {
            if (M3Button.GetUseMaterialDefaults(_owner))
                Activate();
        }
        else
        {
            Deactivate();
        }
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

    private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs e) => RequestRefresh();

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
                var tokens = TokenReader.Begin(
                    _owner,
                    _owner.ActualThemeVariant ?? ThemeVariant.Default);
                var metrics = ButtonDefaults.UnmigratedMetrics(tokens, size);

                M3Button.SetDefaultColors(
                    _owner,
                    ButtonDefaults.UnmigratedColors(tokens, variant));
                M3Button.SetDefaultMetrics(_owner, metrics);
                M3Button.SetDefaultElevation(_owner, ButtonDefaults.Elevation(tokens, variant));
                M3Button.SetDefaultShapeMotion(_owner, ButtonDefaults.ShapeMotion(_themeState.Theme));
                M3Button.SetDefaultOutlineThickness(
                    _owner,
                    ButtonDefaults.OutlineThickness(variant, metrics));
            }
            while (_refreshPending);
        }
        finally
        {
            _refreshing = false;
        }
    }
}
