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

/// <summary>One tab in a <see cref="MaterialTabs" />.</summary>
public class MaterialTab : ToggleButton
{
    /// <inheritdoc cref="Icon" />
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<MaterialTab, object?>(nameof(Icon));

    public static readonly DirectProperty<MaterialTab, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialTab, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialTab, IImmutableBrush> IconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialTab, IImmutableBrush>(nameof(IconBrush), o => o.IconBrush);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private TabTokens _tokens = TabTokens.Primary;
    private TabColors _colors = TabTokens.Primary.Resolve(null, false);

    public MaterialTab()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => InvalidateVisual();

        ApplyTokens();
    }

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
        _interaction.DrawLayers(context, _colors.StateLayerColor, default);
        base.Render(context);
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
            textBlock.TextAlignment = TextAlignment.Center;

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
public class MaterialTabs : TemplatedControl
{
    public static readonly StyledProperty<TabVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialTabs, TabVariant>(nameof(Variant));

    /// <inheritdoc cref="SelectedIndex" />
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MaterialTabs, int>(
            nameof(SelectedIndex), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private readonly MaterialThemeWatcher _theme;
    private readonly MotionGroup _motion;

    private SpringAnimation? _centre;
    private SpringAnimation? _width;
    private TabTokens _tokens = TabTokens.Primary;
    private TabColors _colors = TabTokens.Primary.Resolve(null, false);
    private bool _updatingSelection;

    public MaterialTabs()
    {
        _theme = new MaterialThemeWatcher(this);
        _motion = new MotionGroup(this);
        _motion.Ticked += (_, _) => InvalidateVisual();

        _theme.Changed += (_, _) => ApplyTokens();
        Items.CollectionChanged += (_, _) => AttachItems();

        ApplyTokens();
    }

    static MaterialTabs()
    {
        AffectsRender<MaterialTabs>(VariantProperty, SelectedIndexProperty);
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

        var width = IndicatorWidth.Value;
        if (width > 0.5)
        {
            var height = _tokens.ActiveIndicatorHeight;
            var rect = new Rect(IndicatorCentre.Value - width / 2, Bounds.Height - height, width, height);
            var radius = _tokens.ActiveIndicatorCornerRadius;

            // Only the top corners are rounded: the indicator sits on the bottom edge.
            context.DrawRectangle(
                _colors.ActiveIndicatorColor,
                null,
                new RoundedRect(rect, new CornerRadius(radius, radius, 0, 0)));
        }

        base.Render(context);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        UpdateIndicator(snap: IndicatorWidth.Value <= 0);
        return size;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty)
            ApplyTokens();
        else if (change.Property == SelectedIndexProperty)
            SyncSelection();
    }

    private SpringAnimation IndicatorCentre =>
        _centre ??= _motion.AddSpring(0, _theme.Spring(MotionRole.FastSpatial));

    private SpringAnimation IndicatorWidth =>
        _width ??= _motion.AddSpring(0, _theme.Spring(MotionRole.FastSpatial));

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
            IndicatorWidth.AnimateTo(0, _theme.Spring(MotionRole.FastSpatial));
            _motion.Start();
            return;
        }

        var tab = Items[SelectedIndex];
        var bounds = tab.Bounds;
        if (bounds.Width <= 0)
            return;

        var width = _tokens.IndicatorSpansLabel ? Math.Min(bounds.Width, MeasuredLabelWidth(tab)) : bounds.Width;
        var centre = bounds.Center.X;

        if (snap)
        {
            IndicatorCentre.SnapTo(centre);
            IndicatorWidth.SnapTo(width);
            InvalidateVisual();
            return;
        }

        var spring = _theme.Spring(MotionRole.FastSpatial);
        IndicatorCentre.AnimateTo(centre, spring);
        IndicatorWidth.AnimateTo(width, spring);
        _motion.Start();
    }

    /// <summary>
    /// How wide the primary indicator should be: the label's width, not the tab's.
    /// </summary>
    private double MeasuredLabelWidth(MaterialTab tab)
    {
        var text = tab.Content as string;
        if (string.IsNullOrEmpty(text))
            return tab.Bounds.Width;

        var typography = _theme.Theme.Typography;
        var formatted = new FormattedText(
            text!,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(typography.GetFontFamily(_tokens.LabelTextStyle)),
            typography.GetTypeScale(_tokens.LabelTextStyle).FontSize,
            Brushes.Black);

        return formatted.Width;
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.GetTabTokens(Variant);
        _colors = _theme.Theme.GetTabColors(Variant, _theme.IsDark);

        AttachItems();
        InvalidateVisual();
    }
}
