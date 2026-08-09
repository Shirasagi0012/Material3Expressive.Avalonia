using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Automation.Peers;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// One destination in a <see cref="MaterialNavigationBar" />.
/// </summary>
/// <remarks>
/// The item is a toggle, and the bar keeps exactly one of them checked. The visible selection is
/// the indicator: a pill that grows behind the icon rather than a colour change alone, which is
/// what Material 3 replaced the old highlighted-tab look with.
/// </remarks>
public class MaterialNavigationItem : ToggleButton, IMaterialThemeConsumer, IMaterialInteractionConsumer
{
    public static readonly StyledProperty<bool> FollowWideRailExpansionProperty =
        AvaloniaProperty.Register<MaterialNavigationItem, bool>(
            nameof(FollowWideRailExpansion),
            true);

    public static readonly StyledProperty<NavigationItemIconPosition> IconPositionProperty =
        AvaloniaProperty.Register<MaterialNavigationItem, NavigationItemIconPosition>(
            nameof(IconPosition),
            NavigationItemIconPosition.Top);

    /// <inheritdoc cref="Icon" />
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<MaterialNavigationItem, object?>(nameof(Icon));

    // Read-only outputs of token resolution; the template binds to them.
    public static readonly DirectProperty<MaterialNavigationItem, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(nameof(IconSize), o => o.IconSize);

    public static readonly DirectProperty<MaterialNavigationItem, double> IconLabelSpaceProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(
            nameof(IconLabelSpace), o => o.IconLabelSpace);

    public static readonly DirectProperty<MaterialNavigationItem, IImmutableBrush> IconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, IImmutableBrush>(nameof(IconBrush), o => o.IconBrush);

    public static readonly DirectProperty<MaterialNavigationItem, double> TopItemVerticalPaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(
            nameof(TopItemVerticalPadding), o => o.TopItemVerticalPadding);

    public static readonly DirectProperty<MaterialNavigationItem, double> StartIndicatorHorizontalPaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(
            nameof(StartIndicatorHorizontalPadding), o => o.StartIndicatorHorizontalPadding);

    public static readonly DirectProperty<MaterialNavigationItem, double> StartIndicatorVerticalPaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(
            nameof(StartIndicatorVerticalPadding), o => o.StartIndicatorVerticalPadding);

    public static readonly DirectProperty<MaterialNavigationItem, double> TopIndicatorVerticalPaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(
            nameof(TopIndicatorVerticalPadding), o => o.TopIndicatorVerticalPadding);

    public static readonly DirectProperty<MaterialNavigationItem, double> LabelLineHeightProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(
            nameof(LabelLineHeight), o => o.LabelLineHeight);

    public static readonly DirectProperty<MaterialNavigationItem, double> LabelLetterSpacingProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, double>(
            nameof(LabelLetterSpacing), o => o.LabelLetterSpacing);

    public static readonly DirectProperty<MaterialNavigationItem, bool> IsShortNavigationItemProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, bool>(
            nameof(IsShortNavigationItem), o => o.IsShortNavigationItem);

    public static readonly DirectProperty<MaterialNavigationItem, bool> IsWideNavigationItemProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, bool>(
            nameof(IsWideNavigationItem), o => o.IsWideNavigationItem);

    public static readonly DirectProperty<MaterialNavigationItem, bool> IsWideNavigationExpandedItemProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, bool>(
            nameof(IsWideNavigationExpandedItem), o => o.IsWideNavigationExpandedItem);

    public static readonly DirectProperty<MaterialNavigationItem, bool> AnchorTopIconProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationItem, bool>(
            nameof(AnchorTopIcon), o => o.AnchorTopIcon);

    private readonly MaterialElementContext _theme;
    private readonly MaterialInteractionController _interaction;

    private SpringAnimation? _indicator;
    private NavigationMetrics _tokens = NavigationDefaults.Metrics(NavigationVariant.Bar);
    private NavigationVisualColors _colors = NavigationDefaults.Colors(Foundation.ResolvedTheme.Default, NavigationVariant.Bar, false);
    private MaterialNavigationItemContentPanel? _contentPanel;
    private ContentPresenter? _labelPresenter;
    private readonly List<Action> _restoreLabelMetrics = new();

    public MaterialNavigationItem()
    {
        _theme = MaterialElementContext.GetOrCreate(this);
        _interaction = MaterialElementContext.GetOrCreate(this).Interaction;

        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        InvalidateVisual();

    /// <summary>The destination's icon.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Places the icon above the label or at its logical start.</summary>
    public NavigationItemIconPosition IconPosition
    {
        get => GetValue(IconPositionProperty);
        set => SetValue(IconPositionProperty, value);
    }

    /// <summary>
    /// Whether a wide navigation rail automatically maps collapsed to Top and expanded to Start.
    /// Set this to false to keep an explicitly supplied <see cref="IconPosition"/>.
    /// </summary>
    public bool FollowWideRailExpansion
    {
        get => GetValue(FollowWideRailExpansionProperty);
        set => SetValue(FollowWideRailExpansionProperty, value);
    }

    /// <summary>The size the icon should be drawn at.</summary>
    public double IconSize => _tokens.IconSize;

    /// <summary>The gap between the indicator and the label under it.</summary>
    public double IconLabelSpace => _tokens.IconLabelSpace;

    /// <summary>Vertical content padding for a top-icon destination.</summary>
    public double TopItemVerticalPadding => _tokens.TopItemVerticalPadding;

    /// <summary>Horizontal padding inside a leading-icon indicator.</summary>
    public double StartIndicatorHorizontalPadding => _tokens.StartIndicatorHorizontalPadding;

    /// <summary>Vertical padding inside a leading-icon indicator.</summary>
    public double StartIndicatorVerticalPadding =>
        IsWideNavigationExpandedItem ? 16 : _tokens.StartIndicatorVerticalPadding;

    /// <summary>Padding between a top indicator and its icon.</summary>
    public double TopIndicatorVerticalPadding =>
        (_tokens.ActiveIndicatorHeight - _tokens.IconSize) / 2;

    /// <summary>The resolved line height for label content.</summary>
    public double LabelLineHeight =>
        _theme.Theme.Typography.GetTypeScale(LabelTextStyle).LineHeight;

    /// <summary>The resolved letter spacing for label content.</summary>
    public double LabelLetterSpacing =>
        _theme.Theme.Typography.GetTypeScale(LabelTextStyle).LetterSpacing;

    /// <summary>Whether the item is currently owned by a short navigation bar.</summary>
    public bool IsShortNavigationItem => OwnerShortNavigationBar is not null;

    /// <summary>Whether the item is currently owned by a wide navigation rail.</summary>
    public bool IsWideNavigationItem => OwnerWideNavigationRail is not null;

    /// <summary>Whether the owning wide navigation rail is targeting its expanded state.</summary>
    public bool IsWideNavigationExpandedItem => OwnerWideNavigationRail?.IsExpanded == true;

    /// <summary>Whether the top-icon geometry uses its token-defined top anchor.</summary>
    public bool AnchorTopIcon => IsShortNavigationItem || (IsWideNavigationItem && !IsWideNavigationExpandedItem);

    /// <summary>The colour the icon should take right now.</summary>
    public IImmutableBrush IconBrush =>
        IsEffectivelyEnabled
            ? IsChecked == true
                ? _colors.ActiveIconColor
                : _colors.InactiveIconColor
            : WithOpacity(_colors.InactiveIconColor, 0.38);

    /// <summary>Which navigation surface this item belongs to; set by the parent.</summary>
    internal NavigationVariant Surface { get; set; } = NavigationVariant.Bar;

    internal MaterialShortNavigationBar? OwnerShortNavigationBar { get; set; }

    internal MaterialWideNavigationRail? OwnerWideNavigationRail { get; set; }

    internal void SetShortNavigationOwner(MaterialShortNavigationBar? owner)
    {
        var previous = IsShortNavigationItem;
        var previousAnchor = AnchorTopIcon;
        OwnerShortNavigationBar = owner;
        RaisePropertyChanged(IsShortNavigationItemProperty, previous, IsShortNavigationItem);
        RaisePropertyChanged(AnchorTopIconProperty, previousAnchor, AnchorTopIcon);
        InvalidateMeasure();
        InvalidateVisual();
    }

    internal void SetWideNavigationOwner(MaterialWideNavigationRail? owner)
    {
        var previousWide = IsWideNavigationItem;
        var previousExpanded = IsWideNavigationExpandedItem;
        var previousAnchor = AnchorTopIcon;
        var previousVerticalPadding = StartIndicatorVerticalPadding;
        OwnerWideNavigationRail = owner;
        RaisePropertyChanged(IsWideNavigationItemProperty, previousWide, IsWideNavigationItem);
        RaisePropertyChanged(
            IsWideNavigationExpandedItemProperty,
            previousExpanded,
            IsWideNavigationExpandedItem);
        RaisePropertyChanged(AnchorTopIconProperty, previousAnchor, AnchorTopIcon);
        RaisePropertyChanged(
            StartIndicatorVerticalPaddingProperty,
            previousVerticalPadding,
            StartIndicatorVerticalPadding);
        ApplyTokens();
    }

    internal void UpdateWideNavigationState()
    {
        var previous = !IsWideNavigationExpandedItem;
        var previousAnchor = IsShortNavigationItem || (IsWideNavigationItem && previous);
        var previousVerticalPadding = previous ? 16 : _tokens.StartIndicatorVerticalPadding;
        RaisePropertyChanged(
            IsWideNavigationExpandedItemProperty,
            previous,
            IsWideNavigationExpandedItem);
        RaisePropertyChanged(AnchorTopIconProperty, previousAnchor, AnchorTopIcon);
        RaisePropertyChanged(
            StartIndicatorVerticalPaddingProperty,
            previousVerticalPadding,
            StartIndicatorVerticalPadding);
        ApplyTokens();
    }

    /// <summary>The pill behind the icon, in control coordinates.</summary>
    protected Rect IndicatorArea
    {
        get
        {
            var startIcon = IconPosition == NavigationItemIconPosition.Start && Content is not null;
            _contentPanel ??= this.GetVisualDescendants()
                .OfType<MaterialNavigationItemContentPanel>()
                .FirstOrDefault();
            var width = startIcon
                ? Math.Min(Bounds.Width, _contentPanel?.StartIndicatorWidth ?? _tokens.ActiveIndicatorWidth)
                : _tokens.ActiveIndicatorWidth;
            var height = startIcon
                ? IsWideNavigationExpandedItem
                    ? 56
                    : _tokens.StartActiveIndicatorHeight
                : _tokens.ActiveIndicatorHeight;
            var scale = LayoutHelper.GetLayoutScale(this);
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
                scale = 1;

            // NavigationBarItemLayout places the indicator with integral physical-pixel
            // arithmetic: (containerWidthPx - indicatorWidthPx) / 2.  Keeping this in logical
            // coordinates leaves the middle item of an uneven weighted row at a half pixel and
            // changes its rounded-rectangle antialiasing, even though its text placement is
            // already correct.
            var containerWidth = ToComposePixels(Bounds.Width, scale);
            var indicatorWidth = ToComposePixels(width, scale);
            var left = IsWideNavigationExpandedItem && startIcon
                ? FlowDirection == FlowDirection.RightToLeft
                    ? Math.Max(0, containerWidth - ToComposePixels(20, scale) - indicatorWidth) / scale
                    : ToComposePixels(20, scale) / scale
                : Math.Max(0, (containerWidth - indicatorWidth) / 2) / scale;
            return new Rect(left, IndicatorTop(height, startIcon, scale), indicatorWidth / scale, height);
        }
    }

    private static int ToComposePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));

    private double IndicatorTop(double indicatorHeight, bool startIcon, double scale)
    {
        if (NavigationItemLayout.IsBaselineRail(Surface, IsWideNavigationItem))
        {
            var geometry = NavigationItemLayout.CalculateBaselineRail(
                ToComposePixels(Bounds.Height, scale),
                ToComposePixels(IconSize, scale),
                ToComposePixels(LabelHeight, scale),
                ToComposePixels(_tokens.IconLabelSpace, scale),
                ToComposePixels(4, scale));
            return geometry.IndicatorTop / scale;
        }

        return startIcon
            ? (Bounds.Height - indicatorHeight) / 2
            : AnchorTopIcon
                ? TopItemVerticalPadding
            // The label sits under the indicator, so the pair is centred rather than the pill alone.
                : (Bounds.Height - indicatorHeight - _tokens.IconLabelSpace - LabelHeight) / 2;
    }

    private TypographyRole LabelTextStyle =>
        IsWideNavigationExpandedItem ? TypographyRole.LabelLarge : _tokens.LabelTextStyle;

    private double LabelHeight =>
        Content is null ? 0 : _theme.Theme.Typography.GetTypeScale(LabelTextStyle).LineHeight;

    private SpringAnimation Indicator =>
        _indicator ??= _interaction.Motion.AddSpring(
            IsChecked == true ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var area = IndicatorArea;
        if (area.Width <= 0 || area.Height <= 0)
            return;

        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ActiveIndicatorShape, area.Size);

        var scale = Indicator.Value;
        if (scale > 0.001)
        {
            // The pill grows horizontally out of the icon rather than fading in.
            var width = area.Width * scale;
            var grown = new Rect(area.Center.X - width / 2, area.Y, width, area.Height);

            using (context.PushTransform(Matrix.CreateTranslation(grown.X, grown.Y)))
                MaterialContainer.Draw(context, grown.Size, corners, _colors.ActiveIndicatorColor);
        }

        _theme.InteractionPresentation.UpdateGeometry(
            new MaterialInteractionGeometry(area, null, null));
        base.Render(context);
        _interaction.DrawLayers(context, _colors.StateLayerColor, corners);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty)
        {
            Indicator.AnimateTo(IsChecked == true ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));
            _interaction.Motion.Start();
            ApplyColors();
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == IconPositionProperty || change.Property == ContentProperty)
        {
            if (change.Property == ContentProperty)
                Dispatcher.UIThread.Post(RefreshLabelTypography, DispatcherPriority.Loaded);
            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (change.Property == FollowWideRailExpansionProperty
                 && FollowWideRailExpansion
                 && OwnerWideNavigationRail is { } rail)
        {
            IconPosition = rail.IsExpanded
                ? NavigationItemIconPosition.Start
                : NavigationItemIconPosition.Top;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _contentPanel = e.NameScope.Find<MaterialNavigationItemContentPanel>("PART_ContentPanel");
        _labelPresenter = e.NameScope.Find<ContentPresenter>("PART_LabelPresenter");
        Dispatcher.UIThread.Post(RefreshLabelTypography, DispatcherPriority.Loaded);
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialNavigationItemAutomationPeer(this);

    internal void ApplyTokens()
    {
        var previousIconSize = IconSize;
        var previousSpace = IconLabelSpace;
        var previousTopPadding = TopItemVerticalPadding;
        var previousStartHorizontalPadding = StartIndicatorHorizontalPadding;
        var previousStartVerticalPadding = StartIndicatorVerticalPadding;
        var previousTopIndicatorVerticalPadding = TopIndicatorVerticalPadding;
        var previousLabelLineHeight = LabelLineHeight;
        var previousLabelLetterSpacing = LabelLetterSpacing;

        _tokens = NavigationDefaults.Metrics(Surface);
        _colors = NavigationDefaults.Colors(_theme.Theme, Surface, _theme.IsDark);

        RaisePropertyChanged(IconSizeProperty, previousIconSize, IconSize);
        RaisePropertyChanged(IconLabelSpaceProperty, previousSpace, IconLabelSpace);
        RaisePropertyChanged(TopItemVerticalPaddingProperty, previousTopPadding, TopItemVerticalPadding);
        RaisePropertyChanged(
            StartIndicatorHorizontalPaddingProperty,
            previousStartHorizontalPadding,
            StartIndicatorHorizontalPadding);
        RaisePropertyChanged(
            StartIndicatorVerticalPaddingProperty,
            previousStartVerticalPadding,
            StartIndicatorVerticalPadding);
        RaisePropertyChanged(
            TopIndicatorVerticalPaddingProperty,
            previousTopIndicatorVerticalPadding,
            TopIndicatorVerticalPadding);
        RaisePropertyChanged(
            LabelLineHeightProperty,
            previousLabelLineHeight,
            LabelLineHeight);
        RaisePropertyChanged(
            LabelLetterSpacingProperty,
            previousLabelLetterSpacing,
            LabelLetterSpacing);

        var typography = _theme.Theme.Typography;
        FontSize = typography.GetTypeScale(LabelTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(LabelTextStyle);
        FontWeight = typography.GetFontWeight(LabelTextStyle);

        // NavigationBarItem and NavigationRailItem both wrap their label in
        // ProvideContentColorTextStyle.  FontSize alone is not sufficient for a templated
        // Avalonia control: the supplied TextBlock otherwise retains the inherited BodyMedium
        // line height, tracking, and weight.  Carry the complete local text-style contract to
        // the content subtree, matching the Compose composition local.
        TypographyScheme.SetTextStyle(this, LabelTextStyle);
        RefreshLabelTypography();

        ApplyColors();
        InvalidateMeasure();
    }

    private void RefreshLabelTypography()
    {
        ClearLabelTypography();
        var textBlock = Content as TextBlock ?? _labelPresenter?.Child as TextBlock;
        if (textBlock is null)
            return;

        var hasExplicitRole =
            textBlock.GetBaseValue(TypographyScheme.TextStyleProperty).HasValue;
        var role = hasExplicitRole
            ? TypographyScheme.GetTextStyle(textBlock)
            : LabelTextStyle;
        var scale = _theme.Theme.Typography.GetTypeScale(role);
        OverrideLabelMetric(
            textBlock,
            TextBlock.LineHeightProperty,
            scale.LineHeight);
        OverrideLabelMetric(
            textBlock,
            TextBlock.LetterSpacingProperty,
            scale.LetterSpacing);
    }

    private void OverrideLabelMetric<T>(
        TextBlock textBlock,
        StyledProperty<T> property,
        T value)
    {
        var diagnostic = textBlock.GetDiagnostic(property);
        if (diagnostic.Priority <= BindingPriority.LocalValue)
            return;

        textBlock.SetCurrentValue(property, value);
        _restoreLabelMetrics.Add(() =>
        {
            var current = textBlock.GetDiagnostic(property);
            if (current.IsOverriddenCurrentValue
                && current.Priority > BindingPriority.LocalValue
                && Equals(current.Value, value))
            {
                textBlock.ClearValue(property);
            }
        });
    }

    private void ClearLabelTypography()
    {
        foreach (var restore in _restoreLabelMetrics)
            restore();
        _restoreLabelMetrics.Clear();
    }

    private void ApplyColors()
    {
        var previousIconBrush = IconBrush;

        Foreground = IsEffectivelyEnabled
            ? IsChecked == true
                ? _colors.ActiveLabelTextColor
                : _colors.InactiveLabelTextColor
            : WithOpacity(_colors.InactiveLabelTextColor, 0.38);
        RaisePropertyChanged(IconBrushProperty, previousIconBrush, IconBrush);

        InvalidateVisual();
    }

    private static IImmutableBrush WithOpacity(IImmutableBrush brush, double opacity) =>
        brush is ImmutableSolidColorBrush solid
            ? new ImmutableSolidColorBrush(solid.Color, solid.Opacity * opacity)
            : brush;
}

/// <summary>
/// Arranges a navigation item's icon and label using Compose's physical-pixel placement rules.
/// </summary>
/// <remarks>
/// <para>
/// NavigationBar.kt and NavigationRail.kt both calculate <c>labelX</c> as
/// <c>(containerWidth - labelPlaceable.width) / 2</c>, where both operands are integral physical
/// pixels. The integer division deliberately rounds a half pixel down. Avalonia's general-purpose
/// centre alignment preserves the fractional coordinate until rendering, which rounds the same
/// label up on a 2x surface.
/// </para>
/// <para>
/// The panel keeps the shared template but performs that Compose-specific calculation for each
/// child slot. Measured text extents are rounded up to the integral placeable size Compose uses;
/// this is a layout rule, not a label- or index-specific correction.
/// </para>
/// </remarks>
public sealed class MaterialNavigationItemContentPanel : Panel
{
    public static readonly StyledProperty<NavigationItemIconPosition> IconPositionProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, NavigationItemIconPosition>(
            nameof(IconPosition),
            NavigationItemIconPosition.Top);

    /// <summary>The fixed gap between the icon slot and the label slot.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, double>(nameof(Spacing));

    public static readonly StyledProperty<double> TopVerticalPaddingProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, double>(nameof(TopVerticalPadding));

    public static readonly StyledProperty<double> StartHorizontalPaddingProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, double>(nameof(StartHorizontalPadding));

    public static readonly StyledProperty<double> StartVerticalPaddingProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, double>(nameof(StartVerticalPadding));

    public static readonly StyledProperty<double> TopIndicatorVerticalPaddingProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, double>(nameof(TopIndicatorVerticalPadding));

    public static readonly StyledProperty<bool> AnchorTopProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, bool>(nameof(AnchorTop));

    public static readonly StyledProperty<bool> AlignStartProperty =
        AvaloniaProperty.Register<MaterialNavigationItemContentPanel, bool>(nameof(AlignStart));

    static MaterialNavigationItemContentPanel()
    {
        AffectsMeasure<MaterialNavigationItemContentPanel>(
            IconPositionProperty,
            SpacingProperty,
            TopVerticalPaddingProperty,
            StartHorizontalPaddingProperty,
            StartVerticalPaddingProperty,
            TopIndicatorVerticalPaddingProperty,
            AnchorTopProperty,
            AlignStartProperty);
    }

    /// <inheritdoc cref="SpacingProperty" />
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public NavigationItemIconPosition IconPosition
    {
        get => GetValue(IconPositionProperty);
        set => SetValue(IconPositionProperty, value);
    }

    public double TopVerticalPadding
    {
        get => GetValue(TopVerticalPaddingProperty);
        set => SetValue(TopVerticalPaddingProperty, value);
    }

    public double StartHorizontalPadding
    {
        get => GetValue(StartHorizontalPaddingProperty);
        set => SetValue(StartHorizontalPaddingProperty, value);
    }

    public double StartVerticalPadding
    {
        get => GetValue(StartVerticalPaddingProperty);
        set => SetValue(StartVerticalPaddingProperty, value);
    }

    public double TopIndicatorVerticalPadding
    {
        get => GetValue(TopIndicatorVerticalPaddingProperty);
        set => SetValue(TopIndicatorVerticalPaddingProperty, value);
    }

    public bool AnchorTop
    {
        get => GetValue(AnchorTopProperty);
        set => SetValue(AnchorTopProperty, value);
    }

    public bool AlignStart
    {
        get => GetValue(AlignStartProperty);
        set => SetValue(AlignStartProperty, value);
    }

    internal double StartIndicatorWidth { get; private set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return default;

        if (IconPosition == NavigationItemIconPosition.Start && visibleChildren.Length > 1)
        {
            visibleChildren[0].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var labelWidth = double.IsFinite(availableSize.Width)
                ? Math.Max(
                    0,
                    availableSize.Width
                    - StartHorizontalPadding * 2
                    - visibleChildren[0].DesiredSize.Width
                    - Spacing * (visibleChildren.Length - 1))
                : double.PositiveInfinity;
            for (var index = 1; index < visibleChildren.Length; index++)
                visibleChildren[index].Measure(new Size(labelWidth, double.PositiveInfinity));

            var contentWidth = visibleChildren.Sum(static child => child.DesiredSize.Width)
                + Spacing * (visibleChildren.Length - 1);
            // The expanded wide-rail item reserves its 4dp transition gutter outside the
            // leading icon/label stack; it belongs to the indicator's measured width.
            StartIndicatorWidth = StartHorizontalPadding * 2 + contentWidth + (AlignStart ? 4 : 0);
            var contentHeight = visibleChildren.Max(static child => child.DesiredSize.Height);
            return new Size(StartIndicatorWidth, StartVerticalPadding * 2 + contentHeight);
        }

        var availableWidth = availableSize.Width;
        var desiredWidth = 0d;
        var desiredHeight = 0d;

        foreach (var child in visibleChildren)
        {
            child.Measure(new Size(availableWidth, double.PositiveInfinity));
            desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
            desiredHeight += child.DesiredSize.Height;
        }

        desiredHeight += Spacing * (visibleChildren.Length - 1);
        return new Size(
            desiredWidth,
            desiredHeight + TopVerticalPadding * 2 + (AnchorTop ? TopIndicatorVerticalPadding : 0));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var visibleChildren = Children.Where(child => child.IsVisible).ToArray();
        if (visibleChildren.Length == 0)
            return finalSize;

        if (IconPosition == NavigationItemIconPosition.Start && visibleChildren.Length > 1)
        {
            var startScale = GetScale();
            var widths = visibleChildren.Select(child => ToPlaceablePixels(child.DesiredSize.Width, startScale)).ToArray();
            var heights = visibleChildren.Select(child => ToPlaceablePixels(child.DesiredSize.Height, startScale)).ToArray();
            var startSpacing = ToComposePixels(Spacing, startScale);
            var totalWidth = widths.Sum() + startSpacing * (visibleChildren.Length - 1);
            var startFinalWidth = ToComposePixels(finalSize.Width, startScale);
            var startFinalHeight = ToComposePixels(finalSize.Height, startScale);
            var x = AlignStart
                ? FlowDirection == FlowDirection.RightToLeft
                    ? Math.Max(
                        0,
                        startFinalWidth
                        - ToComposePixels(24 + StartHorizontalPadding, startScale)
                        - totalWidth)
                    : ToComposePixels(24 + StartHorizontalPadding, startScale)
                : Math.Max(0, (startFinalWidth - totalWidth) / 2);

            for (var index = 0; index < visibleChildren.Length; index++)
            {
                var childIndex = FlowDirection == FlowDirection.RightToLeft
                    ? visibleChildren.Length - 1 - index
                    : index;
                var width = widths[childIndex];
                var height = heights[childIndex];
                var y = Math.Max(0, (startFinalHeight - height) / 2);
                visibleChildren[childIndex].Arrange(new Rect(
                    ToLogicalPixels(x, startScale),
                    ToLogicalPixels(y, startScale),
                    ToLogicalPixels(width, startScale),
                    ToLogicalPixels(height, startScale)));
                x += width + startSpacing;
            }

            return finalSize;
        }

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

        var navigationItem = TemplatedParent as MaterialNavigationItem;
        var baselineRail = navigationItem is not null &&
            NavigationItemLayout.IsBaselineRail(
                navigationItem.Surface,
                navigationItem.IsWideNavigationItem);
        var baselineRailGeometry = baselineRail && visibleChildren.Length >= 2
            ? NavigationItemLayout.CalculateBaselineRail(
                finalHeight,
                childHeights[0],
                childHeights[1],
                spacing,
                ToComposePixels(4, scale))
            : default(BaselineRailGeometry?);

        // NavigationBar.kt / NavigationRail.kt use roundToInt only when emitting the vertical
        // placeable position. This is equivalent to rounding the shared pair offset once here.
        var top = navigationItem is null
            ? AnchorTop
                ? ToComposePixels(TopVerticalPadding + TopIndicatorVerticalPadding, scale)
                : ToComposePixels((finalHeight - contentHeight) / 2d, 1)
            : NavigationItemLayout.CalculateTopContentStack(
                navigationItem.Surface,
                navigationItem.IsWideNavigationItem,
                navigationItem.IsWideNavigationExpandedItem,
                AnchorTop,
                finalHeight,
                contentHeight,
                ToComposePixels(TopVerticalPadding, scale),
                ToComposePixels(TopIndicatorVerticalPadding, scale),
                ToComposePixels(4, scale));

        for (var index = 0; index < visibleChildren.Length; index++)
        {
            var width = childWidths[index];
            var height = childHeights[index];
            var left = Math.Max(0, (finalWidth - width) / 2);

            var childTop = baselineRailGeometry is { } geometry && index < 2
                ? index == 0 ? geometry.IconTop : geometry.LabelTop
                : top;
            visibleChildren[index].Arrange(new Rect(
                ToLogicalPixels(left, scale),
                ToLogicalPixels(childTop, scale),
                ToLogicalPixels(width, scale),
                ToLogicalPixels(height, scale)));

            if (baselineRailGeometry is null || index >= 2)
                top += height + spacing;
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

/// <summary>Position of a navigation destination icon relative to its label.</summary>
public enum NavigationItemIconPosition
{
    Top,
    Start
}

/// <summary>
/// Compose-equivalent geometry shared by a navigation item's renderer and content panel.
/// </summary>
internal static class NavigationItemLayout
{
    public static bool IsBaselineRail(NavigationVariant surface, bool isWideNavigationItem) =>
        surface == NavigationVariant.Rail && !isWideNavigationItem;

    /// <summary>
    /// Mirrors NavigationRail.kt's placeLabelAndIcon calculation in physical pixels.
    /// </summary>
    public static BaselineRailGeometry CalculateBaselineRail(
        int itemHeight,
        int iconHeight,
        int labelHeight,
        int indicatorVerticalPadding,
        int itemVerticalPadding)
    {
        var contentHeight =
            iconHeight + indicatorVerticalPadding + itemVerticalPadding + labelHeight;
        var contentVerticalPadding = Math.Max(
            (itemHeight - contentHeight) / 2,
            indicatorVerticalPadding);
        var iconTop = contentVerticalPadding;

        return new BaselineRailGeometry(
            iconTop - indicatorVerticalPadding,
            iconTop,
            iconTop + iconHeight + indicatorVerticalPadding + itemVerticalPadding);
    }

    /// <summary>
    /// Gets the top of a top-icon content stack using the rail's collapsed wide-layout branch.
    /// All parameters are physical pixels, matching Compose's placement inputs.
    /// </summary>
    public static int CalculateTopContentStack(
        NavigationVariant surface,
        bool isWideNavigationItem,
        bool isWideNavigationExpandedItem,
        bool anchorTop,
        int itemHeight,
        int contentHeight,
        int topVerticalPadding,
        int topIndicatorVerticalPadding,
        int wideRailCollapsedContentPadding)
    {
        if (!anchorTop)
            return Math.Max(0, (int)Math.Floor((itemHeight - contentHeight) / 2d + 0.5));

        var top = topVerticalPadding + topIndicatorVerticalPadding;
        return surface == NavigationVariant.Rail &&
            isWideNavigationItem &&
            !isWideNavigationExpandedItem
            ? top + wideRailCollapsedContentPadding
            : top;
    }
}

/// <summary>Physical-pixel positions emitted by the baseline navigation rail layout.</summary>
internal readonly record struct BaselineRailGeometry(int IndicatorTop, int IconTop, int LabelTop);
