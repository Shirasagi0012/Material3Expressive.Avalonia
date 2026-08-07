using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// TabRow.kt (ScrollableTabData.calculateTabOffset and ScrollableTabRowWithSubcomposeImpl).
// The executable Compose parity reference is alpha22, whose matching tab-row behavior is used
// only as a visual conformance observation; alpha25 remains the implementation source baseline.

/// <summary>One tab in a <see cref="MaterialTabs" />.</summary>
public class MaterialTab : ToggleButton, IMaterialThemeConsumer, IMaterialInteractionConsumer
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

    private readonly MaterialElementContext _theme;
    private readonly MaterialInteractionController _interaction;

    private TabTokens _tokens = TabTokens.Primary;
    private TabColors _colors = TabTokens.Primary.Resolve(null, false);

    public MaterialTab()
    {
        _theme = MaterialElementContext.GetOrCreate(this);
        _interaction = MaterialElementContext.GetOrCreate(this).Interaction;

        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        InvalidateVisual();

    /// <summary>An optional icon above the label.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public double IconSize => _tokens.IconSize;

    public IImmutableBrush IconBrush =>
        IsChecked == true ? _colors.ActiveIconColor : _colors.InactiveIconColor;

    /// <summary>Which tab style this belongs to; set by the parent.</summary>
    internal TabVariant Style { get; set; } = TabVariant.Primary;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _interaction.DrawLayers(context, _colors.StateLayerColor, default);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
            ApplyColors();
    }

    internal void ApplyTokens()
    {
        var previousIconSize = IconSize;

        _tokens = _theme.Theme.GetTabTokens(Style);
        _colors = _theme.Theme.GetTabColors(Style, _theme.IsDark);
        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);

        var typography = _theme.Theme.Typography;
        FontSize = typography.GetTypeScale(_tokens.LabelTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.LabelTextStyle);
        FontWeight = typography.GetFontWeight(_tokens.LabelTextStyle);
        // Tab.kt wraps the text slot in ProvideTextStyle(TitleSmall).  Preserve the full
        // inherited type scale for a supplied TextBlock, not only the three TextElement values
        // exposed by the control itself.
        TypographyScheme.SetTextStyle(this, _tokens.LabelTextStyle);

        // Tab.kt copies TitleSmall with TextAlign.Center before it invokes the supplied text
        // lambda. Existing TextBlock content needs the equivalent local paragraph setting; the
        // ContentPresenter's text properties only affect string content.
        if (Content is TextBlock textBlock)
        {
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.FontFeatures ??= s_defaultLabelFontFeatures;
        }

        MinHeight = Icon is null ? _tokens.ContainerHeight : _tokens.IconAndLabelHeight;

        ApplyColors();
        InvalidateMeasure();
    }

    private void ApplyColors()
    {
        var previousIconBrush = IconBrush;

        Foreground = IsChecked == true ? _colors.ActiveLabelTextColor : _colors.InactiveLabelTextColor;
        RaisePropertyChanged(IconBrushProperty, previousIconBrush, IconBrush);

        InvalidateVisual();
    }
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
public class MaterialTabs : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer, IMaterialInteractionConsumer, IVisualBoundsProbe
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.IsDark ?? false;
    private MaterialMotionScope Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

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

    private SpringAnimation? _centre;
    private SpringAnimation? _width;
    private TabTokens _tokens = TabTokens.Primary;
    private TabColors _colors = TabTokens.Primary.Resolve(null, false);
    private bool _updatingSelection;
    private ScrollViewer? _scrollViewer;
    private bool _scrollSelectedTab;

    public MaterialTabs()
    {
        _material = MaterialElementContext.GetOrCreate(this);
        Items.CollectionChanged += (_, _) => AttachItems();

        ApplyTokens();
    }

    static MaterialTabs()
    {
        AffectsRender<MaterialTabs>(VariantProperty, SelectedIndexProperty, IsScrollableProperty);
        AffectsMeasure<MaterialTabs>(IsScrollableProperty, ScrollableEdgePaddingProperty, ScrollableMinimumTabWidthProperty);
    }

    public TabVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
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
        MaterialContainer.Draw(context, Bounds.Size, default, _colors.ContainerColor);

        // The divider runs under every tab, selected or not; the indicator sits on top of it.
        var dividerY = Bounds.Height - _tokens.DividerHeight / 2;
        context.DrawLine(
            new ImmutablePen(_colors.DividerColor, _tokens.DividerHeight),
            new Point(0, dividerY),
            new Point(Bounds.Width, dividerY));

        if (TryGetVisualBounds("indicator", out var rect))
        {
            var radius = _tokens.ActiveIndicatorCornerRadius;

            // PrimaryIndicator applies RoundedCornerShape(3.dp) to all four corners. The
            // bottom edge touches the row boundary, but Compose still rasterizes both bottom
            // corner arcs before the surface clip is applied.
            context.DrawRectangle(
                _colors.ActiveIndicatorColor,
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
            var height = _tokens.ActiveIndicatorHeight;
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
            ApplyTokens();
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
            item.Style = Variant;
            item.ApplyTokens();

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
            Motion.Start();
            return;
        }

        var tab = Items[SelectedIndex];
        var bounds = TabBoundsInThis(tab);
        if (bounds.Width <= 0)
            return;

        var width = bounds.Width;
        var centre = bounds.Center.X;
        if (_tokens.IndicatorSpansLabel)
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
        Motion.Start();
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

        var typography = ResolvedTheme.Typography;
        var formatted = new FormattedText(
            text!,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(typography.GetFontFamily(_tokens.LabelTextStyle)),
            typography.GetTypeScale(_tokens.LabelTextStyle).FontSize,
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

    private void ApplyTokens()
    {
        _tokens = ResolvedTheme.GetTabTokens(Variant);
        _colors = ResolvedTheme.GetTabColors(Variant, IsDarkVariant);

        AttachItems();
        InvalidateVisual();
    }
}
