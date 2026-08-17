using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Primary tabs sit under an app bar; secondary tabs sit inside content.</summary>
public enum TabVariant { Primary, Secondary }

// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// TabRow.kt (ScrollableTabData.calculateTabOffset and ScrollableTabRowWithSubcomposeImpl).
// The executable Compose parity reference is alpha22, whose matching tab-row behavior is used
// only as a visual conformance observation; alpha25 remains the implementation source baseline.

/// <summary>One tab in a <see cref="MaterialTabs" />.</summary>
public class MaterialTab : ToggleButton, IMaterialInteractionConsumer
{
    // Compose's pinned Roboto reference shapes the Material tab label without discretionary
    // standard/contextual ligatures. Retain a caller-provided feature set, but make Avalonia's
    // otherwise implicit liga/clig defaults explicit for the Material default. Source: AndroidX
    // ff0a0a99 / Material3 1.5.0-alpha25, Tab.kt:Tab and TabBaselineLayout.
    private static readonly FontFeatureCollection s_defaultLabelFontFeatures =
        FontFeatureCollection.Parse("liga=0, clig=0");

    /// <inheritdoc cref="Icon" />
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<MaterialTab, object?>(nameof(Icon));

    public static readonly DirectProperty<MaterialTab, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialTab, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialTab, IImmutableBrush> IconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialTab, IImmutableBrush>(nameof(IconBrush), o => o.IconBrush);

    public static readonly DirectProperty<MaterialTab, IBrush?> EffectiveForegroundProperty =
        AvaloniaProperty.RegisterDirect<MaterialTab, IBrush?>(
            nameof(EffectiveForeground),
            o => o.EffectiveForeground);

    private readonly MaterialInteractionController _interaction;
    // Avalonia's inherited default Foreground is a resolved value and therefore makes IsSet
    // true even when no author supplied a colour. Track the binding priority of the last
    // effective value instead so the MaterialValue profile remains authoritative until an
    // application/style/local value explicitly overrides it.
    private bool _hasForegroundOverride;

    private static readonly TabItemGeometryProfile DefaultGeometryProfile = new(
        48,
        64,
        24,
        TypographyRole.TitleSmall);

    private static readonly TabItemVisualColors DefaultVisualColors = new(
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush());

    internal static readonly StyledProperty<TabItemGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialTab, TabItemGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    internal static readonly StyledProperty<TabItemVisualColors> VisualColorsProperty =
        AvaloniaProperty.Register<MaterialTab, TabItemVisualColors>(
            nameof(VisualColors),
            DefaultVisualColors);

    public static readonly StyledProperty<TabVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialTab, TabVariant>(nameof(Variant), TabVariant.Primary);

    public MaterialTab()
    {
        _interaction = MaterialElementContext.Ensure(this).Interaction;

        ApplyProfile();
    }

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        InvalidateVisual();

    /// <summary>An optional icon above the label.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public double IconSize => GeometryProfile.IconSize;

    public IImmutableBrush IconBrush =>
        IsChecked == true ? VisualColors.ActiveIconColor : VisualColors.InactiveIconColor;

    /// <summary>
    /// Gets the public foreground when an application/local value supplied one, otherwise the
    /// checked-state colour from the selected Material Value profile.
    /// </summary>
    public IBrush? EffectiveForeground =>
        // ToggleButton/TextElement supplies a framework default Foreground even when the
        // application has not authored one.  Treat only an explicitly set value as an override;
        // otherwise the typed profile must remain the source of the tab label colour.
        _hasForegroundOverride
            ? Foreground
            : IsChecked == true
                ? VisualColors.ActiveLabelTextColor
                : VisualColors.InactiveLabelTextColor;

    /// <summary>Which tab style this belongs to; set by the parent.</summary>
    public TabVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Gets or sets the hidden variant-selected item geometry profile.</summary>
    internal TabItemGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    /// <summary>Gets or sets the hidden variant-selected item brush profile.</summary>
    internal TabItemVisualColors VisualColors
    {
        get => GetValue(VisualColorsProperty);
        set => SetValue(VisualColorsProperty, value);
    }

    /// <summary>Compatibility alias used by the owning row while the old API is retired.</summary>
    [Obsolete(
        "Legacy internal tab style transport. Set Variant instead.",
        DiagnosticId = "M3E1005")]
    internal TabVariant Style
    {
        get => Variant;
        set => Variant = value;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _interaction.DrawLayers(context, VisualColors.StateLayerColor, default);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            var previousChecked = change.GetOldValue<bool?>();
            var previousIconBrush = previousChecked == true
                ? VisualColors.ActiveIconColor
                : VisualColors.InactiveIconColor;
            ApplyColors(previousIconBrush);
        }
        else if (change.Property == ForegroundProperty)
        {
            _hasForegroundOverride = change.Priority <= BindingPriority.Style;
            RaisePropertyChanged(
                EffectiveForegroundProperty,
                change.GetOldValue<IBrush?>(),
                EffectiveForeground);
        }
        else if (change.Property == GeometryProfileProperty)
            ApplyProfile();
        else if (change.Property == VisualColorsProperty)
        {
            var previousColors = change.GetOldValue<TabItemVisualColors>();
            var previousIconBrush = IsChecked == true
                ? previousColors.ActiveIconColor
                : previousColors.InactiveIconColor;
            ApplyProfile(previousIconBrush);
        }
        else if (change.Property == IconProperty)
            ApplyProfile();
    }

    private void ApplyProfile(IImmutableBrush? previousIconBrush = null)
    {
        var previousIconSize = IconSize;
        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);

        // Tab.kt wraps the text slot in ProvideTextStyle(TitleSmall).  Preserve the full
        // inherited type scale for a supplied TextBlock, not only the three TextElement values
        // exposed by the control itself.
        TypographyScheme.SetTextStyle(this, GeometryProfile.LabelTextStyle);

        // Tab.kt copies TitleSmall with TextAlign.Center before it invokes the supplied text
        // lambda. Existing TextBlock content needs the equivalent local paragraph setting; the
        // ContentPresenter's text properties only affect string content.
        if (Content is TextBlock textBlock)
        {
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.FontFeatures ??= s_defaultLabelFontFeatures;
        }

        MinHeight = Icon is null ? GeometryProfile.ContainerHeight : GeometryProfile.IconAndLabelHeight;

        ApplyColors(previousIconBrush);
        InvalidateMeasure();
    }

    private void ApplyColors(IImmutableBrush? previousIconBrush = null)
    {
        previousIconBrush ??= IconBrush;

        var previousForeground = EffectiveForeground;

        RaisePropertyChanged(IconBrushProperty, previousIconBrush, IconBrush);
        RaisePropertyChanged(EffectiveForegroundProperty, previousForeground, EffectiveForeground);

        InvalidateVisual();
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}

/// <summary>
/// Lays out fixed tabs using the integral-width rule from Compose's TabRow implementation.
/// </summary>
/// <remarks>
/// TabRow.kt calculates <c>tabWidth</c> with integral division of the physical row width, then
/// places child <c>index</c> at <c>index * tabWidth</c>. A generic UniformGrid keeps fractional
/// logical widths instead, which makes intrinsic content placement renderer-dependent at high DPI.
/// </remarks>
public sealed class MaterialTabItemsPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return default;

        var scale = GetScale();
        var hasFiniteWidth = !double.IsInfinity(availableSize.Width);
        var tabWidth = hasFiniteWidth
            ? ToLogicalPixels(ToComposePixels(availableSize.Width, scale) / visibleChildren.Length, scale)
            : double.PositiveInfinity;
        var desiredWidth = 0d;
        var desiredHeight = 0d;

        foreach (var child in visibleChildren)
        {
            child.Measure(new Size(tabWidth, availableSize.Height));
            desiredWidth += child.DesiredSize.Width;
            desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);
        }

        return new Size(hasFiniteWidth ? availableSize.Width : desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return finalSize;

        var scale = GetScale();
        var tabWidth = ToComposePixels(finalSize.Width, scale) / visibleChildren.Length;

        for (var index = 0; index < visibleChildren.Length; index++)
        {
            visibleChildren[index].Arrange(new Rect(
                ToLogicalPixels(index * tabWidth, scale),
                0,
                ToLogicalPixels(tabWidth, scale),
                finalSize.Height));
        }

        return finalSize;
    }

    private double GetScale()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        return scale > 0 && !double.IsNaN(scale) && !double.IsInfinity(scale) ? scale : 1;
    }

    private static int ToComposePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));

    private static double ToLogicalPixels(int value, double scale) => value / scale;
}

/// <summary>
/// Lays out a scrollable Material tab row with the fixed leading/trailing affordance prescribed
/// by Compose's <c>ScrollableTabRowImpl</c>.
/// </summary>
/// <remarks>
/// Unlike <see cref="MaterialTabItemsPanel"/>, tabs retain their intrinsic width (subject to the
/// upstream 90dp minimum) and the panel's extent contains 52dp of space at either end. The owning
/// <see cref="MaterialTabs"/> centres a newly-selected tab in its <see cref="ScrollViewer"/>
/// viewport when possible.
/// </remarks>
public sealed class MaterialScrollableTabItemsPanel : Panel
{
    public static readonly StyledProperty<double> EdgePaddingProperty =
        AvaloniaProperty.Register<MaterialScrollableTabItemsPanel, double>(nameof(EdgePadding), 52);

    public static readonly StyledProperty<double> MinimumTabWidthProperty =
        AvaloniaProperty.Register<MaterialScrollableTabItemsPanel, double>(nameof(MinimumTabWidth), 90);

    static MaterialScrollableTabItemsPanel()
    {
        AffectsMeasure<MaterialScrollableTabItemsPanel>(EdgePaddingProperty, MinimumTabWidthProperty);
    }

    /// <summary>Space at both ends of the scrollable extent.</summary>
    public double EdgePadding
    {
        get => GetValue(EdgePaddingProperty);
        set => SetValue(EdgePaddingProperty, value);
    }

    /// <summary>The Compose Material3 minimum tab width.</summary>
    public double MinimumTabWidth
    {
        get => GetValue(MinimumTabWidthProperty);
        set => SetValue(MinimumTabWidthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        var edgePadding = Math.Max(0, EdgePadding);
        var minimumWidth = Math.Max(0, MinimumTabWidth);
        var desiredWidth = edgePadding * 2;
        var desiredHeight = 0d;

        foreach (var child in visibleChildren)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            desiredWidth += Math.Max(minimumWidth, child.DesiredSize.Width);
            desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);
        }

        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var edgePadding = Math.Max(0, EdgePadding);
        var minimumWidth = Math.Max(0, MinimumTabWidth);
        var left = edgePadding;

        foreach (var child in Children.Where(child => child.IsVisible))
        {
            var width = Math.Max(minimumWidth, child.DesiredSize.Width);
            child.Arrange(new Rect(left, 0, width, finalSize.Height));
            left += width;
        }

        return finalSize;
    }
}

/// <summary>
/// Centres a tab's intrinsic content with Compose's physical-pixel rounding semantics.
/// </summary>
/// <remarks>
/// The text-only path follows Tab.kt's <c>TabBaselineLayout</c> inside the enclosing tab Column:
/// the intrinsic text-and-padding width is centred, then the resulting offset is rounded to an
/// integral physical pixel. This is intentionally different from the navigation item layout,
/// whose upstream code uses integer division after measuring its label placeable.
/// </remarks>
public sealed class MaterialTabContentPanel : Panel
{
    /// <summary>The vertical gap between the icon and text slots.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MaterialTabContentPanel, double>(nameof(Spacing));

    static MaterialTabContentPanel()
    {
        AffectsMeasure<MaterialTabContentPanel>(SpacingProperty);
    }

    /// <inheritdoc cref="SpacingProperty" />
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return default;

        var desiredWidth = 0d;
        var desiredHeight = 0d;

        foreach (var child in visibleChildren)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
            desiredHeight += child.DesiredSize.Height;
        }

        desiredHeight += Spacing * (visibleChildren.Length - 1);
        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return finalSize;

        var scale = GetScale();
        var finalWidth = ToComposePixels(finalSize.Width, scale);
        var finalHeight = ToComposePixels(finalSize.Height, scale);
        var spacing = ToComposePixels(Spacing, scale);
        var childWidths = new int[visibleChildren.Length];
        var childHeights = new int[visibleChildren.Length];
        var contentHeight = spacing * (visibleChildren.Length - 1);

        for (var index = 0; index < visibleChildren.Length; index++)
        {
            childWidths[index] = ToPlaceablePixels(visibleChildren[index].DesiredSize.Width, scale);
            childHeights[index] = ToPlaceablePixels(visibleChildren[index].DesiredSize.Height, scale);
            contentHeight += childHeights[index];
        }

        var top = ToComposePixels((finalHeight - contentHeight) / 2d, 1);

        for (var index = 0; index < visibleChildren.Length; index++)
        {
            // Column's centre alignment rounds the raw intrinsic width, rather than first
            // truncating the width to the integral placeable bounds used by navigation items.
            var desiredWidth = visibleChildren[index].DesiredSize.Width * scale;
            var left = ToComposePixels((finalWidth - desiredWidth) / 2d, 1);

            visibleChildren[index].Arrange(new Rect(
                ToLogicalPixels(left, scale),
                ToLogicalPixels(top, scale),
                ToLogicalPixels(childWidths[index], scale),
                ToLogicalPixels(childHeights[index], scale)));

            top += childHeights[index] + spacing;
        }

        return finalSize;
    }

    private double GetScale()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        return scale > 0 && !double.IsNaN(scale) && !double.IsInfinity(scale) ? scale : 1;
    }

    private static int ToComposePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));

    private static int ToPlaceablePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Ceiling(value * scale - 0.000001));

    private static double ToLogicalPixels(int value, double scale) => value / scale;
}

/// <summary>
/// A row of tabs with an indicator that slides between them.
/// </summary>
/// <remarks>
/// The indicator is drawn by the row rather than by the selected tab, because it travels: it
/// slides from where it was to where it is going, and a tab can only draw inside itself. The slide
/// is a spring, so switching tabs quickly redirects it from wherever it had got to instead of
/// restarting.
/// <para>
/// The primary form's indicator hugs the label; the secondary form's spans the whole tab. That is
/// a token, not a special case.
/// </para>
/// </remarks>
public class MaterialTabs : TemplatedControl, IMaterialMotionConsumer, IMaterialInteractionConsumer, IVisualBoundsProbe
{
    private MaterialElementContext? _material;

    private MaterialMotionController Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        InvalidateVisual();

    private void OnMotionTick() => InvalidateVisual();

    public static readonly StyledProperty<TabVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialTabs, TabVariant>(nameof(Variant));

    /// <inheritdoc cref="SelectedIndex" />
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MaterialTabs, int>(
            nameof(SelectedIndex), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <inheritdoc cref="IsScrollable" />
    public static readonly StyledProperty<bool> IsScrollableProperty =
        AvaloniaProperty.Register<MaterialTabs, bool>(nameof(IsScrollable));

    /// <inheritdoc cref="ScrollableEdgePadding" />
    public static readonly StyledProperty<double> ScrollableEdgePaddingProperty =
        AvaloniaProperty.Register<MaterialTabs, double>(nameof(ScrollableEdgePadding), 52);

    /// <inheritdoc cref="ScrollableMinimumTabWidth" />
    public static readonly StyledProperty<double> ScrollableMinimumTabWidthProperty =
        AvaloniaProperty.Register<MaterialTabs, double>(nameof(ScrollableMinimumTabWidth), 90);

    private static readonly TabRowGeometryProfile DefaultGeometryProfile = new(
        3,
        3,
        1,
        true);

    private static readonly TabRowVisualColors DefaultVisualColors = new(
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush());

    internal static readonly StyledProperty<TabRowGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialTabs, TabRowGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    internal static readonly StyledProperty<TabRowVisualColors> VisualColorsProperty =
        AvaloniaProperty.Register<MaterialTabs, TabRowVisualColors>(
            nameof(VisualColors),
            DefaultVisualColors);

    private SpringAnimation? _centre;
    private SpringAnimation? _width;
    private bool _updatingSelection;
    private ScrollViewer? _scrollViewer;
    private bool _scrollSelectedTab;

    public MaterialTabs()
    {
        _material = MaterialElementContext.Ensure(this);
        Items.CollectionChanged += (_, _) => AttachItems();

        ApplyProfile();
    }

    static MaterialTabs()
    {
        AffectsRender<MaterialTabs>(
            VariantProperty,
            SelectedIndexProperty,
            IsScrollableProperty,
            GeometryProfileProperty,
            VisualColorsProperty);
        AffectsMeasure<MaterialTabs>(IsScrollableProperty, ScrollableEdgePaddingProperty, ScrollableMinimumTabWidthProperty);
    }

    public TabVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Gets or sets the hidden variant-selected row geometry profile.</summary>
    internal TabRowGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    /// <summary>Gets or sets the hidden variant-selected row brush profile.</summary>
    internal TabRowVisualColors VisualColors
    {
        get => GetValue(VisualColorsProperty);
        set => SetValue(VisualColorsProperty, value);
    }

    /// <summary>Which tab is selected. -1 for none.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Uses the scrollable TabRow layout: intrinsic tabs, a 52dp end affordance, and selected-tab
    /// centring. The default remains the fixed, evenly divided TabRow.
    /// </summary>
    public bool IsScrollable
    {
        get => GetValue(IsScrollableProperty);
        set => SetValue(IsScrollableProperty, value);
    }

    /// <summary>Leading and trailing space in a scrollable row. Defaults to Compose's 52dp.</summary>
    public double ScrollableEdgePadding
    {
        get => GetValue(ScrollableEdgePaddingProperty);
        set => SetValue(ScrollableEdgePaddingProperty, value);
    }

    /// <summary>Intrinsic tab-width floor in a scrollable row. Defaults to Compose's 90dp.</summary>
    public double ScrollableMinimumTabWidth
    {
        get => GetValue(ScrollableMinimumTabWidthProperty);
        set => SetValue(ScrollableMinimumTabWidthProperty, value);
    }

    [Content]
    public AvaloniaList<MaterialTab> Items { get; } = new();

    public override void Render(DrawingContext context)
    {
        MaterialContainer.Draw(context, Bounds.Size, default, VisualColors.ContainerColor);

        // The divider runs under every tab, selected or not; the indicator sits on top of it.
        var dividerY = Bounds.Height - GeometryProfile.DividerHeight / 2;
        context.DrawLine(
            new ImmutablePen(VisualColors.DividerColor, GeometryProfile.DividerHeight),
            new Point(0, dividerY),
            new Point(Bounds.Width, dividerY));

        if (TryGetVisualBounds("indicator", out var rect))
        {
            var radius = GeometryProfile.ActiveIndicatorCornerRadius;

            // PrimaryIndicator applies RoundedCornerShape(3.dp) to all four corners. The
            // bottom edge touches the row boundary, but Compose still rasterizes both bottom
            // corner arcs before the surface clip is applied.
            context.DrawRectangle(
                VisualColors.ActiveIndicatorColor,
                null,
                new RoundedRect(rect, new CornerRadius(radius, radius, radius, radius)));
        }

        base.Render(context);
    }

    /// <inheritdoc />
    public bool TryGetVisualBounds(string id, out Rect bounds)
    {
        if (id == "indicator" && IndicatorWidth.Value > 0.5)
        {
            var width = IndicatorWidth.Value;
            var height = GeometryProfile.ActiveIndicatorHeight;
            bounds = new Rect(IndicatorCentre.Value - width / 2, Bounds.Height - height, width, height);
            return true;
        }

        bounds = default;
        return false;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);

        ScrollSelectedTabIntoView();
        UpdateIndicator(snap: IndicatorWidth.Value <= 0);
        return size;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_scrollViewer is { })
            _scrollViewer.ScrollChanged -= OnScrollChanged;

        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        if (_scrollViewer is { })
            _scrollViewer.ScrollChanged += OnScrollChanged;

        if (IsScrollable)
            _scrollSelectedTab = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty)
            ApplyProfile();
        else if (change.Property == GeometryProfileProperty || change.Property == VisualColorsProperty)
            ApplyProfile();
        else if (change.Property == SelectedIndexProperty)
            SyncSelection();
        else if (change.Property == IsScrollableProperty)
        {
            PseudoClasses.Set(":scrollable", IsScrollable);
            _scrollSelectedTab = IsScrollable;
        }
    }

    private SpringAnimation IndicatorCentre =>
        _centre ??= Motion.AddSpring(0, Spring(MotionRole.FastSpatial));

    private SpringAnimation IndicatorWidth =>
        _width ??= Motion.AddSpring(0, Spring(MotionRole.FastSpatial));

    private void AttachItems()
    {
        foreach (var item in Items)
        {
            item.Variant = Variant;

            item.IsCheckedChanged -= OnItemCheckedChanged;
            item.IsCheckedChanged += OnItemCheckedChanged;
        }

        SyncSelection();
        InvalidateMeasure();
    }

    private void OnItemCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_updatingSelection || sender is not MaterialTab tab)
            return;

        if (tab.IsChecked != true)
        {
            SyncSelection();
            return;
        }

        SelectedIndex = Items.IndexOf(tab);
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

        _scrollSelectedTab |= IsScrollable;
        UpdateIndicator();
    }

    /// <summary>
    /// Points the indicator at the selected tab.
    /// </summary>
    /// <remarks>
    /// Centre and width are separate springs, so a jump between tabs of different widths stretches
    /// as it travels rather than resizing on arrival — which is what upstream's indicator does and
    /// what makes the movement read as one object rather than two changes.
    /// </remarks>
    private void UpdateIndicator(bool snap = false)
    {
        if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
        {
            IndicatorWidth.AnimateTo(0, Spring(MotionRole.FastSpatial));
            Motion.RequestFrame();
            return;
        }

        var tab = Items[SelectedIndex];
        var bounds = TabBoundsInThis(tab);
        if (bounds.Width <= 0)
            return;

        var width = bounds.Width;
        var centre = bounds.Center.X;
        if (GeometryProfile.IndicatorSpansLabel)
        {
            var scale = GetLayoutScale();
            var tabLeft = ToComposePixels(bounds.X, scale);
            var tabWidth = ToComposePixels(bounds.Width, scale);
            // TabRow derives contentWidth from maxIntrinsicWidth, which is an integral physical
            // pixel value. The indicator is then centred with integer division. TextLayout.Width
            // is a fractional advance, so preserve the upstream contract here instead of
            // feeding that fraction directly to Skia's rounded-rectangle rasterizer.
            var minimumWidth = ToComposePixels(24, scale);
            var contentWidth = Math.Max(minimumWidth, ToPlaceablePixels(MeasuredLabelWidth(tab), scale));
            contentWidth = Math.Min(tabWidth, contentWidth);
            var left = tabLeft + Math.Max(0, (tabWidth - contentWidth) / 2);

            width = ToLogicalPixels(contentWidth, scale);
            centre = ToLogicalPixels(left + contentWidth / 2d, scale);
        }

        if (snap)
        {
            IndicatorCentre.SnapTo(centre);
            IndicatorWidth.SnapTo(width);
            InvalidateVisual();
            return;
        }

        var spring = Spring(MotionRole.FastSpatial);
        IndicatorCentre.AnimateTo(centre, spring);
        IndicatorWidth.AnimateTo(width, spring);
        Motion.RequestFrame();
    }

    private void ScrollSelectedTabIntoView()
    {
        if (!_scrollSelectedTab || !IsScrollable || _scrollViewer is null
            || SelectedIndex < 0 || SelectedIndex >= Items.Count)
            return;

        var tab = Items[SelectedIndex];
        var transform = tab.TransformToVisual(_scrollViewer);
        if (transform is null || _scrollViewer.Viewport.Width <= 0)
            return;

        // Compose does not centre in fractional Dp. ScrollableTabData.calculateTabOffset first
        // rounds the total extent, tab left and width to physical px, then uses integer division
        // for both the viewport and the tab half-width. Preserve that sequence exactly: keeping
        // the calculation in Avalonia DIPs moves an off-screen preceding label by 3 DIP at 2x.
        var scale = GetLayoutScale();
        var totalWidth = ToComposePixels(_scrollViewer.Extent.Width, scale);
        var availableSpace = Math.Max(0, totalWidth - ToComposePixels(_scrollViewer.Viewport.Width, scale));
        var visibleWidth = totalWidth - availableSpace;
        var tabLeft = ToComposePixels(
            _scrollViewer.Offset.X + transform.Value.Transform(default).X,
            scale);
        var tabWidth = ToComposePixels(tab.Bounds.Width, scale);
        var centredTabOffset = tabLeft - (visibleWidth / 2 - tabWidth / 2);
        var target = Math.Clamp(centredTabOffset, 0, availableSpace);
        _scrollViewer.Offset = new Vector(ToLogicalPixels(target, scale), _scrollViewer.Offset.Y);
        _scrollSelectedTab = false;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.OffsetDelta.X == 0)
            return;

        // The indicator lives in the row's drawing layer, not the ScrollViewer content, so update
        // its coordinates immediately rather than springing behind a user drag.
        UpdateIndicator(snap: true);
    }

    private Rect TabBoundsInThis(MaterialTab tab)
    {
        var transform = tab.TransformToVisual(this);
        return transform is { } matrix
            ? new Rect(matrix.Transform(default), tab.Bounds.Size)
            : tab.Bounds;
    }

    /// <summary>
    /// How wide the primary indicator should be: the label's width, not the tab's.
    /// </summary>
    private double MeasuredLabelWidth(MaterialTab tab)
    {
        // A consumer may supply a TextBlock instead of a string. Its TextLayout is the same
        // already-resolved type scale that the tab rendered, so it is the closest Avalonia
        // equivalent to TabRow.kt's maxIntrinsicWidth minus horizontal text padding.
        if (tab.Content is TextBlock textBlock)
            return textBlock.TextLayout.Width;

        var text = tab.Content as string;
        if (string.IsNullOrEmpty(text))
            return tab.Bounds.Width;

        var formatted = new FormattedText(
            text!,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(tab.FontFamily),
            tab.FontSize,
            Brushes.Black);

        return formatted.Width;
    }

    private double GetLayoutScale()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        return scale > 0 && !double.IsNaN(scale) && !double.IsInfinity(scale) ? scale : 1;
    }

    private static int ToComposePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));

    private static int ToPlaceablePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Ceiling(value * scale - 0.000001));

    private static double ToLogicalPixels(double value, double scale) => value / scale;

    private void ApplyProfile()
    {
        AttachItems();
        InvalidateVisual();
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}
