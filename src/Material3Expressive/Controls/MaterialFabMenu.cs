using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A floating action button that expands into a column of labelled items.
/// </summary>
/// <remarks>
/// The items are extended FABs — <see cref="MaterialFab" /> with a label — so each one already
/// carries its own state layer, ripple, elevation and press morph. The menu contributes the
/// arrangement and the reveal.
/// <para>
/// Items appear in sequence rather than together, each one a fraction of a second behind the last,
/// which is what makes the menu read as unfolding rather than as a block appearing. The stagger is
/// a phase offset into one shared spring, so closing reverses the order for free.
/// </para>
/// </remarks>
public class MaterialFabMenu : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.Theme.Current.IsDark ?? false;
    private MaterialMotionController Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    /// <inheritdoc cref="IsExpanded" />
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MaterialFabMenu, bool>(
            nameof(IsExpanded), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <inheritdoc cref="Icon" />
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<MaterialFabMenu, object?>(nameof(Icon));

    public static readonly StyledProperty<FabVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialFabMenu, FabVariant>(nameof(Variant));

    /// <summary>How far apart in the cycle two neighbouring items start moving.</summary>
    private const double StaggerFraction = 0.12;

    private SpringAnimation? _reveal;
    private FabMenuMetrics _metrics = FabMenuDefaults.Metrics();

    static MaterialFabMenu()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialFabMenu>(false);
    }

    public MaterialFabMenu()
    {
        _material = MaterialElementContext.Ensure(this);
        Items.CollectionChanged += (_, _) => InvalidateMeasure();

        ApplyTokens();
    }

    /// <summary>Whether the menu is showing its items.</summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>The toggle's icon.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The colour variant the toggle and every item take.</summary>
    public FabVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>The menu's items, which are extended FABs.</summary>
    [Content]
    public AvaloniaList<MaterialFab> Items { get; } = new();

    /// <summary>The gap between items, from the token table.</summary>
    public double ItemSpacing => _metrics.ItemBetweenSpace;

    /// <summary>The gap between the last item and the toggle.</summary>
    public double CloseButtonSpacing => _metrics.CloseButtonBetweenSpace;

    // Read-only outputs of token resolution; the template binds to them.
    public static readonly DirectProperty<MaterialFabMenu, double> ItemSpacingProperty =
        AvaloniaProperty.RegisterDirect<MaterialFabMenu, double>(nameof(ItemSpacing), o => o.ItemSpacing);

    public static readonly DirectProperty<MaterialFabMenu, double> CloseButtonSpacingProperty =
        AvaloniaProperty.RegisterDirect<MaterialFabMenu, double>(
            nameof(CloseButtonSpacing), o => o.CloseButtonSpacing);

    private SpringAnimation Reveal =>
        _reveal ??= Motion.AddSpring(IsExpanded ? 1 : 0, Spring(MotionRole.FastSpatial));

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The scheduler no longer performs an owner-wide lifecycle settle. Synchronize this
        // component-owned semantic track when its presenter becomes visible instead.
        UpdateItems();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsExpandedProperty)
        {
            Reveal.AnimateTo(IsExpanded ? 1 : 0, Spring(MotionRole.FastSpatial));
            Motion.RequestFrame();
            UpdateItems();
        }
        else if (change.Property == VariantProperty)
        {
            foreach (var item in Items)
                item.Variant = Variant;
        }
    }

    private void OnMotionTick()
    {
        UpdateItems();
    }

    /// <summary>
    /// Applies the reveal to each item, offset so they arrive in sequence.
    /// </summary>
    /// <remarks>
    /// The item nearest the toggle leads and the furthest trails, which is the direction the menu
    /// grows. Reading the offsets off one spring rather than running one per item means a menu
    /// closed halfway through opening unwinds in the right order without any extra state.
    /// </remarks>
    private void UpdateItems()
    {
        var progress = Reveal.Value;
        var count = Items.Count;

        for (var i = 0; i < count; i++)
        {
            // Items are laid out top to bottom but revealed bottom-up, from the toggle outwards.
            var position = count - 1 - i;
            var start = position * StaggerFraction;
            var span = Math.Max(0.0001, 1 - start);
            var local = Math.Clamp((progress - start) / span, 0, 1);

            var item = Items[i];
            item.Opacity = local;
            item.IsVisible = local > 0.001;
            item.RenderTransform = new Avalonia.Media.TranslateTransform(0, (1 - local) * 12);
        }
    }

    private void ApplyTokens()
    {
        var oldItemSpacing = ItemSpacing;
        var oldCloseButtonSpacing = CloseButtonSpacing;
        _metrics = FabMenuDefaults.Metrics();

        foreach (var item in Items)
        {
            item.Variant = Variant;
            item.HorizontalAlignment = HorizontalAlignment.Right;
        }

        UpdateItems();
        RaisePropertyChanged(ItemSpacingProperty, oldItemSpacing, ItemSpacing);
        RaisePropertyChanged(CloseButtonSpacingProperty, oldCloseButtonSpacing, CloseButtonSpacing);
        InvalidateMeasure();
    }
}
