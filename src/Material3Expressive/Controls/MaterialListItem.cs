using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Maps Compose ListItem's overload families to one Avalonia/XAML interaction surface.</summary>
public enum ListItemInteractionMode
{
    None,
    Click,
    SingleSelection,
    MultiSelection
}

// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25, ListItem.kt and
// tokens/ListTokens.kt (VERSION 29.0.0). Compose provides separate non-interactive, clickable,
// radio-like and checkbox-like overloads. Avalonia consolidates those overloads into
// InteractionMode while retaining IsSelected as the bindable selection state.

/// <summary>A Material list row with leading, headline, overline, supporting and trailing slots.</summary>
public sealed class MaterialListItem : ToggleButton
{
    public static readonly StyledProperty<ListItemInteractionMode> InteractionModeProperty =
        AvaloniaProperty.Register<MaterialListItem, ListItemInteractionMode>(nameof(InteractionMode));
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsSelected), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    public static readonly StyledProperty<object?> LeadingContentProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(LeadingContent));
    public static readonly StyledProperty<object?> TrailingContentProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(TrailingContent));
    public static readonly StyledProperty<object?> OverlineContentProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(OverlineContent));
    public static readonly StyledProperty<object?> SupportingContentProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(SupportingContent));

    public static readonly DirectProperty<MaterialListItem, IBrush> SupportingBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialListItem, IBrush>(nameof(SupportingBrush), o => o.SupportingBrush);
    public static readonly DirectProperty<MaterialListItem, IBrush> LeadingBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialListItem, IBrush>(nameof(LeadingBrush), o => o.LeadingBrush);
    public static readonly DirectProperty<MaterialListItem, IBrush> TrailingBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialListItem, IBrush>(nameof(TrailingBrush), o => o.TrailingBrush);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;
    private bool _syncing;
    private IBrush _supportingBrush = Brushes.Gray;
    private IBrush _leadingBrush = Brushes.Gray;
    private IBrush _trailingBrush = Brushes.Gray;

    public MaterialListItem()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);
        _theme.Changed += (_, _) => ApplyTheme();
        _interaction.InteractionChanged += (_, _) => InvalidateVisual();
        ApplyTheme();
    }

    public ListItemInteractionMode InteractionMode { get => GetValue(InteractionModeProperty); set => SetValue(InteractionModeProperty, value); }
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public object? LeadingContent { get => GetValue(LeadingContentProperty); set => SetValue(LeadingContentProperty, value); }
    public object? TrailingContent { get => GetValue(TrailingContentProperty); set => SetValue(TrailingContentProperty, value); }
    public object? OverlineContent { get => GetValue(OverlineContentProperty); set => SetValue(OverlineContentProperty, value); }
    public object? SupportingContent { get => GetValue(SupportingContentProperty); set => SetValue(SupportingContentProperty, value); }
    public IBrush SupportingBrush => _supportingBrush;
    public IBrush LeadingBrush => _leadingBrush;
    public IBrush TrailingBrush => _trailingBrush;

    static MaterialListItem()
    {
        AffectsRender<MaterialListItem>(InteractionModeProperty, IsSelectedProperty, IsEnabledProperty);
        AffectsMeasure<MaterialListItem>(OverlineContentProperty, SupportingContentProperty);
    }

    public override void Render(DrawingContext context)
    {
        var scheme = _theme.Theme.ColorScheme;
        var enabled = IsEffectivelyEnabled;
        var selected = IsSelected;
        var fill = new SolidColorBrush(scheme?.GetColor(selected ? SysColorToken.SecondaryContainer : SysColorToken.Surface, _theme.IsDark)
            ?? (selected ? Colors.LightGray : Colors.White));
        var corners = _theme.Theme.Shapes.GetCornerRadius(selected ? ShapeRole.CornerLarge : ShapeRole.CornerNone, Bounds.Size);
        MaterialContainer.Draw(context, Bounds.Size, corners, fill);
        if (InteractionMode != ListItemInteractionMode.None)
            _interaction.DrawLayers(context, new SolidColorBrush(scheme?.GetColor(SysColorToken.OnSurface, _theme.IsDark) ?? Colors.Black), corners);
        base.Render(context);
    }

    protected override void OnClick()
    {
        if (InteractionMode == ListItemInteractionMode.None)
            return;

        // ToggleButton supplies pointer/keyboard Click routing and focus behavior. Its native
        // toggling does not describe the Click or radio-like overloads, so restore/resolve the
        // public state only after it has raised the routed Click event.
        var wasSelected = IsSelected;
        base.OnClick();

        IsSelected = InteractionMode switch
        {
            ListItemInteractionMode.Click => wasSelected,
            ListItemInteractionMode.SingleSelection => true,
            ListItemInteractionMode.MultiSelection => !wasSelected,
            _ => IsSelected
        };
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_syncing)
            return;

        if (change.Property == IsSelectedProperty)
            SyncSelectedToChecked();
        else if (change.Property == IsCheckedProperty)
            SyncCheckedToSelected();
        else if (change.Property == InteractionModeProperty || change.Property == IsEffectivelyEnabledProperty)
            ApplyTheme();
    }

    private void ApplyTheme()
    {
        var scheme = _theme.Theme.ColorScheme;
        var selected = IsSelected;
        var foreground = new SolidColorBrush(scheme?.GetColor(selected ? SysColorToken.OnSecondaryContainer : SysColorToken.OnSurface, _theme.IsDark) ?? Colors.Black);
        var secondary = new SolidColorBrush(scheme?.GetColor(selected ? SysColorToken.OnSecondaryContainer : SysColorToken.OnSurfaceVariant, _theme.IsDark) ?? Colors.Gray);
        Foreground = foreground;
        SetBrushes(secondary);
        MinHeight = OverlineContent is not null || SupportingContent is not null ? 72 : 56;
        Focusable = InteractionMode != ListItemInteractionMode.None;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void SetBrushes(IBrush secondary)
    {
        RaisePropertyChanged(SupportingBrushProperty, _supportingBrush, _supportingBrush = secondary);
        RaisePropertyChanged(LeadingBrushProperty, _leadingBrush, _leadingBrush = secondary);
        RaisePropertyChanged(TrailingBrushProperty, _trailingBrush, _trailingBrush = secondary);
    }

    private void SyncSelectedToChecked()
    {
        _syncing = true;
        try { IsChecked = IsSelected; }
        finally { _syncing = false; }
        ApplyTheme();
    }

    private void SyncCheckedToSelected()
    {
        _syncing = true;
        try { IsSelected = IsChecked == true; }
        finally { _syncing = false; }
        ApplyTheme();
    }
}
