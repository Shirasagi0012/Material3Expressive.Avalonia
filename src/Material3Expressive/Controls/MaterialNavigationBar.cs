using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Navigation surface orientation.</summary>
public enum NavigationVariant { Bar, Rail }

/// <summary>
/// A navigation bar along the bottom of the window, or a rail up its side.
/// </summary>
/// <remarks>
/// One control for both, because they are the same thing turned ninety degrees: the same items,
/// the same indicator, the same selection rule. Only the surface colour, the elevation and the
/// axis differ, and all three are tokens.
/// <para>
/// Selection is exclusive and lives here rather than on the items. An item that decided its own
/// selection would have to know about its siblings, and a group of toggles with no owner is how
/// you end up with two selected at once.
/// </para>
/// </remarks>
public class MaterialNavigationBar : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.IsDark ?? false;
    private MaterialMotionScope Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    private void OnMotionTick() => InvalidateVisual();

    public static readonly StyledProperty<NavigationVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialNavigationBar, NavigationVariant>(nameof(Variant));

    /// <inheritdoc cref="SelectedIndex" />
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MaterialNavigationBar, int>(
            nameof(SelectedIndex), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private NavigationMetrics _tokens = NavigationDefaults.Metrics(NavigationVariant.Bar);
    private NavigationVisualColors _colors = NavigationDefaults.Colors(Foundation.ResolvedTheme.Default, NavigationVariant.Bar, false);
    private bool _updatingSelection;

    public MaterialNavigationBar()
    {
        _material = MaterialElementContext.GetOrCreate(this);
        Items.CollectionChanged += (_, _) => AttachItems();
        ApplyTokens();
    }

    static MaterialNavigationBar()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialNavigationBar>(false);
        AffectsRender<MaterialNavigationBar>(VariantProperty);
    }

    /// <summary>Bar or rail.</summary>
    public NavigationVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Which destination is selected. -1 for none.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>The destinations.</summary>
    [Content]
    public AvaloniaList<MaterialNavigationItem> Items { get; } = new();

    /// <summary>The gap between items, from the token table.</summary>
    public double ItemSpacing => _tokens.ItemBetweenSpace;

    /// <summary>True when the items stack down the side rather than across the bottom.</summary>
    public bool IsVertical => _tokens.IsVertical;

    /// <summary>Rows requested by the template's uniform item grid.</summary>
    public int ItemRows => IsVertical ? 0 : 1;

    /// <summary>Columns requested by the template's uniform item grid.</summary>
    public int ItemColumns => IsVertical ? 1 : 0;

    public static readonly DirectProperty<MaterialNavigationBar, double> ItemSpacingProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationBar, double>(nameof(ItemSpacing), o => o.ItemSpacing);

    public static readonly DirectProperty<MaterialNavigationBar, bool> IsVerticalProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationBar, bool>(nameof(IsVertical), o => o.IsVertical);

    public static readonly DirectProperty<MaterialNavigationBar, int> ItemRowsProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationBar, int>(nameof(ItemRows), o => o.ItemRows);

    public static readonly DirectProperty<MaterialNavigationBar, int> ItemColumnsProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationBar, int>(nameof(ItemColumns), o => o.ItemColumns);

    public override void Render(DrawingContext context)
    {
        MaterialContainer.Draw(
            context,
            Bounds.Size,
            default,
            _colors.ContainerColor);

        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);

        return _tokens.IsVertical
            ? new Size(Math.Max(size.Width, _tokens.ContainerSize), size.Height)
            : new Size(size.Width, Math.Max(size.Height, _tokens.ContainerSize));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty)
            ApplyTokens();
        else if (change.Property == SelectedIndexProperty)
            SyncSelection();
    }

    /// <summary>
    /// Keeps exactly one item checked, and reflects a click back into
    /// <see cref="SelectedIndex" />.
    /// </summary>
    private void AttachItems()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            item.Surface = Variant;
            item.ApplyTokens();

            // Re-subscribing is safe: the same handler removed then added leaves one subscription.
            item.IsCheckedChanged -= OnItemCheckedChanged;
            item.IsCheckedChanged += OnItemCheckedChanged;
        }

        SyncSelection();
        InvalidateMeasure();
    }

    private void OnItemCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_updatingSelection || sender is not MaterialNavigationItem item)
            return;

        if (item.IsChecked != true)
        {
            // Clicking the selected item again must not leave the bar with nothing selected.
            SyncSelection();
            return;
        }

        SelectedIndex = Items.IndexOf(item);
    }

    private void SyncSelection()
    {
        if (_updatingSelection)
            return;

        _updatingSelection = true;

        try
        {
            for (var i = 0; i < Items.Count; i++)
                Items[i].IsChecked = i == SelectedIndex;
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    private void ApplyTokens()
    {
        var previousSpacing = ItemSpacing;
        var previousVertical = IsVertical;
        var previousRows = ItemRows;
        var previousColumns = ItemColumns;

        _tokens = NavigationDefaults.Metrics(Variant);
        _colors = NavigationDefaults.Colors(ResolvedTheme, Variant, IsDarkVariant);

        RaisePropertyChanged(ItemSpacingProperty, previousSpacing, ItemSpacing);
        RaisePropertyChanged(IsVerticalProperty, previousVertical, IsVertical);
        RaisePropertyChanged(ItemRowsProperty, previousRows, ItemRows);
        RaisePropertyChanged(ItemColumnsProperty, previousColumns, ItemColumns);

        AttachItems();
        InvalidateVisual();
    }
}
