// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt:96-485,
// 700-859 and tokens/OutlinedSegmentedButtonTokens.kt (VERSION v0_162).
// The pinned Compose Multiplatform executable reference is 1.12.0-alpha03 / Jetpack alpha22;
// its SegmentedButton implementation has the same layout, tokens, and state contract.

using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// One segment in a <see cref="MaterialSegmentedButtonRow"/>.
/// </summary>
/// <remarks>
/// The control deliberately derives from <see cref="ToggleButton"/> so an application can bind
/// <see cref="ToggleButton.IsChecked"/>, content, commands, and the ordinary Avalonia click
/// event. Its parent supplies the segment shape and, in single-choice mode, the radio-like
/// mutual exclusion.
/// </remarks>
public class MaterialSegmentedButton : ToggleButton
{
    /// <summary>
    /// The 24x24 Filled.Check vector from AndroidX <c>internal/Icons.kt</c>.
    /// Avalonia's <see cref="Geometry"/> preserves the upstream path data while remaining
    /// bindable through <see cref="SelectionIcon"/>; the control template supplies the fixed
    /// 24×24 viewport that a geometry does not carry by itself.
    /// </summary>
    public static Geometry DefaultSelectionIcon { get; } =
        Geometry.Parse("M 9,16.17 L 4.83,12 L 3.41,13.41 L 9,19 L 21,7 L 19.59,5.59 Z");

    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<MaterialSegmentedButton, object?>(nameof(Icon));

    public static readonly StyledProperty<Geometry?> SelectionIconProperty =
        AvaloniaProperty.Register<MaterialSegmentedButton, Geometry?>(
            nameof(SelectionIcon),
            DefaultSelectionIcon);

    public static readonly StyledProperty<bool> ShowSelectionIconProperty =
        AvaloniaProperty.Register<MaterialSegmentedButton, bool>(
            nameof(ShowSelectionIcon),
            defaultValue: true);

    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<MaterialSegmentedButton, Thickness>(
            nameof(ContentPadding),
            new Thickness(12, 8));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<MaterialSegmentedButton, double>(
            nameof(IconSize),
            18);

    public static readonly StyledProperty<double> IconSpacingProperty =
        AvaloniaProperty.Register<MaterialSegmentedButton, double>(
            nameof(IconSpacing),
            8);

    public static readonly StyledProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        AvaloniaProperty.Register<MaterialSegmentedButton, RoundedCornerShape?>(
            nameof(ContainerShapeOverride));

    public static readonly DirectProperty<MaterialSegmentedButton, bool> HasVisibleIconProperty =
        AvaloniaProperty.RegisterDirect<MaterialSegmentedButton, bool>(
            nameof(HasVisibleIcon),
            owner => owner.HasVisibleIcon);

    public static readonly DirectProperty<MaterialSegmentedButton, bool> IsDefaultIconVisibleProperty =
        AvaloniaProperty.RegisterDirect<MaterialSegmentedButton, bool>(
            nameof(IsDefaultIconVisible),
            owner => owner.IsDefaultIconVisible);

    public static readonly DirectProperty<MaterialSegmentedButton, IImmutableBrush> IconBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSegmentedButton, IImmutableBrush>(
            nameof(IconBrush),
            owner => owner.IconBrush);

    public static readonly DirectProperty<MaterialSegmentedButton, double> IconScaleProperty =
        AvaloniaProperty.RegisterDirect<MaterialSegmentedButton, double>(
            nameof(IconScale),
            owner => owner.IconScale);

    public static readonly DirectProperty<MaterialSegmentedButton, double> IconOpacityProperty =
        AvaloniaProperty.RegisterDirect<MaterialSegmentedButton, double>(
            nameof(IconOpacity),
            owner => owner.IconOpacity);

    public static readonly DirectProperty<MaterialSegmentedButton, double> ContentOffsetProperty =
        AvaloniaProperty.RegisterDirect<MaterialSegmentedButton, double>(
            nameof(ContentOffset),
            owner => owner.ContentOffset);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;
    private SegmentedButtonColors _colors = SegmentedButtonTokens.Default.Resolve(null, false);
    private SegmentedButtonTokens _tokens = SegmentedButtonTokens.Default;
    private SpringAnimation? _contentOffsetAnimation;
    private SpringAnimation? _iconOpacityAnimation;
    private SpringAnimation? _iconScaleAnimation;
    private IDisposable? _contentPaddingLease;
    private IDisposable? _iconSizeLease;
    private IDisposable? _iconSpacingLease;
    private IDisposable? _minHeightLease;
    private IDisposable? _minWidthLease;
    private bool _hasVisibleIcon;
    private bool _isDefaultIconVisible;
    private RoundedCornerShape _segmentShape = RoundedCornerShape.Circle;
    private double _contentOffset;
    private double _iconOpacity;
    private double _iconScale;

    public MaterialSegmentedButton()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) =>
        {
            InteractionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        };
        _interaction.Motion.Ticked += (_, _) => SyncMotionOutputs();

        ApplyTokens();
    }

    static MaterialSegmentedButton()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialSegmentedButton>(false);
        FocusAdornerProperty.OverrideDefaultValue<MaterialSegmentedButton>(null);
        AffectsRender<MaterialSegmentedButton>(
            IsCheckedProperty,
            ShowSelectionIconProperty,
            SelectionIconProperty,
            ContainerShapeOverrideProperty);
        FocusableProperty.OverrideDefaultValue<MaterialSegmentedButton>(true);
    }

    /// <summary>Optional leading icon content. It remains visible in both states.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the vector geometry used for the default checked icon.
    /// Set to <see langword="null"/> together with <see cref="ShowSelectionIcon"/> to reserve no
    /// default icon; callers may provide an Avalonia <see cref="Geometry"/> equivalent to a
    /// Compose <c>ImageVector</c>.
    /// </summary>
    public Geometry? SelectionIcon
    {
        get => GetValue(SelectionIconProperty);
        set => SetValue(SelectionIconProperty, value);
    }

    /// <summary>
    /// Gets or sets whether a checked segment without a custom <see cref="Icon"/> shows the
    /// Material check icon. The default follows <c>SegmentedButtonDefaults.Icon</c>.
    /// </summary>
    public bool ShowSelectionIcon
    {
        get => GetValue(ShowSelectionIconProperty);
        set => SetValue(ShowSelectionIconProperty, value);
    }

    /// <summary>Padding inside the segment around its icon slot and label.</summary>
    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    /// <summary>The 18dp icon slot size from OutlinedSegmentedButtonTokens.</summary>
    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>The 8dp icon-to-label spacing from SegmentedButton.kt.</summary>
    public double IconSpacing
    {
        get => GetValue(IconSpacingProperty);
        set => SetValue(IconSpacingProperty, value);
    }

    /// <summary>The resolved icon/content brush for the current checked and enabled state.</summary>
    public IImmutableBrush IconBrush =>
        !IsEffectivelyEnabled
            ? IsChecked == true
                ? _colors.DisabledActiveContentColor
                : _colors.DisabledInactiveContentColor
            : IsChecked == true ? _colors.ActiveContentColor : _colors.InactiveContentColor;

    /// <summary>Whether the template should display an icon in the reserved leading slot.</summary>
    public bool HasVisibleIcon => _hasVisibleIcon;

    /// <summary>Whether the built-in check icon, rather than a caller icon, is visible.</summary>
    public bool IsDefaultIconVisible => _isDefaultIconVisible;

    /// <summary>The current FastSpatial scale applied to the built-in selection icon.</summary>
    public double IconScale => _iconScale;

    /// <summary>The current DefaultEffects opacity applied to the built-in selection icon.</summary>
    public double IconOpacity => _iconOpacity;

    /// <summary>The current animated label offset inside the reserved icon slot.</summary>
    public double ContentOffset => _contentOffset;

    /// <summary>
    /// Replaces the shape assigned by the parent row. This is intentionally public for templates
    /// and internal layout hosts, but normal applications should let the row infer it.
    /// </summary>
    public RoundedCornerShape? ContainerShapeOverride
    {
        get => GetValue(ContainerShapeOverrideProperty);
        set => SetValue(ContainerShapeOverrideProperty, value);
    }

    /// <summary>The current segment position, assigned by the containing row.</summary>
    internal SegmentedButtonSegmentPosition SegmentPosition { get; set; }

    internal MaterialSegmentedButtonRow? OwnerRow { get; set; }

    internal MaterialInteractionHost InteractionHost => _interaction;

    internal int InteractionZIndex =>
        (IsChecked == true ? 5 : 0)
        + (_interaction.Interaction.HasFlag(InteractionState.Pressed) ? 1 : 0)
        + (_interaction.Interaction.HasFlag(InteractionState.Focused) ? 1 : 0);

    internal event EventHandler? InteractionChanged;

    protected CornerRadius ContainerCornerRadius =>
        (ContainerShapeOverride ?? _segmentShape).ToCornerRadius(
            Bounds.Size,
            FlowDirection == FlowDirection.RightToLeft);

    public override void Render(DrawingContext context)
    {
        var enabled = IsEffectivelyEnabled;
        var active = IsChecked == true;
        var fill = enabled
            ? active ? _colors.ActiveContainerColor : _colors.InactiveContainerColor
            : active ? _colors.DisabledActiveContainerColor : _colors.DisabledInactiveContainerColor;
        var outline = enabled ? _colors.BorderColor : _colors.DisabledBorderColor;

        DrawContainer(
            context,
            Bounds.Size,
            ContainerCornerRadius,
            fill,
            outline,
            _tokens.OutlineWidth);

        _interaction.InteractionArea = new Rect(Bounds.Size);
        base.Render(context);
        _interaction.DrawLayers(
            context,
            enabled
                ? active ? _colors.ActiveContentColor : _colors.InactiveContentColor
                : active ? _colors.DisabledActiveContentColor : _colors.DisabledInactiveContentColor,
            ContainerCornerRadius);
    }

    private static void DrawContainer(
        DrawingContext context,
        Size size,
        CornerRadius corners,
        IBrush fill,
        IBrush outline,
        double outlineWidth)
    {
        if (size.Width <= 0 || size.Height <= 0)
            return;

        // Compose foundation Border.kt uses a stroked round-rect only for a simple radius. The
        // first and last segment are asymmetric, so it instead fills outer - inset rounded paths.
        // Matching that branch avoids a different Skia antialiasing path at the two pill ends.
        if (corners.TopLeft == corners.TopRight
            && corners.TopRight == corners.BottomRight
            && corners.BottomRight == corners.BottomLeft)
        {
            var simpleBounds = new Rect(size);
            context.DrawRectangle(fill, null, new RoundedRect(simpleBounds, corners));
            if (outlineWidth > 0)
            {
                var inset = outlineWidth / 2;
                context.DrawRectangle(
                    null,
                    new Pen(outline, outlineWidth),
                    new RoundedRect(
                        simpleBounds.Deflate(inset),
                        MaterialContainer.Deflate(corners, inset)));
            }
            return;
        }

        var bounds = new Rect(size);
        context.DrawRectangle(fill, null, new RoundedRect(bounds, corners));
        if (outlineWidth <= 0)
            return;

        var innerBounds = bounds.Deflate(outlineWidth);
        var outerGeometry = CreateRoundedRectangleGeometry(bounds, corners);
        var innerGeometry = CreateRoundedRectangleGeometry(
            innerBounds,
            MaterialContainer.Deflate(corners, outlineWidth));
        context.DrawGeometry(
            outline,
            null,
            new CombinedGeometry(
                GeometryCombineMode.Exclude,
                outerGeometry,
                innerGeometry));
    }

    private static StreamGeometry CreateRoundedRectangleGeometry(
        Rect rect,
        CornerRadius corners)
    {
        var geometry = new StreamGeometry();
        using var sink = geometry.Open();

        sink.BeginFigure(new Point(rect.X + corners.TopLeft, rect.Y), true);
        sink.LineTo(new Point(rect.Right - corners.TopRight, rect.Y));
        AddCorner(
            sink,
            new Point(rect.Right, rect.Y + corners.TopRight),
            corners.TopRight);
        sink.LineTo(new Point(rect.Right, rect.Bottom - corners.BottomRight));
        AddCorner(
            sink,
            new Point(rect.Right - corners.BottomRight, rect.Bottom),
            corners.BottomRight);
        sink.LineTo(new Point(rect.X + corners.BottomLeft, rect.Bottom));
        AddCorner(
            sink,
            new Point(rect.X, rect.Bottom - corners.BottomLeft),
            corners.BottomLeft);
        sink.LineTo(new Point(rect.X, rect.Y + corners.TopLeft));
        AddCorner(
            sink,
            new Point(rect.X + corners.TopLeft, rect.Y),
            corners.TopLeft);
        sink.EndFigure(true);

        return geometry;
    }

    private static void AddCorner(
        StreamGeometryContext sink,
        Point end,
        double radius)
    {
        if (radius <= 0)
            sink.LineTo(end);
        else
            sink.ArcTo(
                end,
                new Size(radius, radius),
                0,
                false,
                SweepDirection.Clockwise);
    }

    protected override void OnClick()
    {
        // ToggleButton.Toggle is overridden below for single-choice rows. Calling the base still
        // raises Click and executes a bound command, matching ordinary Avalonia button behavior.
        base.OnClick();
    }

    protected override void Toggle()
    {
        if (OwnerRow?.SelectionMode == SegmentedButtonSelectionMode.Single && IsChecked == true)
            return;

        base.Toggle();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialSegmentedButtonAutomationPeer(this);

    internal void InvokeFromAutomation()
    {
        if (!IsEffectivelyEnabled)
            throw new InvalidOperationException("The segmented button is disabled.");

        if (OwnerRow?.SelectionMode == SegmentedButtonSelectionMode.Single)
            IsChecked = true;
        else
            Toggle();
    }

    internal void SetSegmentShape(RoundedCornerShape shape)
    {
        if (_segmentShape == shape)
            return;

        _segmentShape = shape;
        InvalidateVisual();
    }

    private void ApplyTokens()
    {
        var previousIcon = IconBrush;
        var previousVisible = HasVisibleIcon;
        var previousDefault = IsDefaultIconVisible;

        _tokens = _theme.Theme.SegmentedButtonTokens;
        _colors = _theme.Theme.GetSegmentedButtonColors(_theme.IsDark);

        ReplaceLease(ref _minWidthLease, MinWidthProperty, 58d);
        ReplaceLease(ref _minHeightLease, MinHeightProperty, _tokens.ContainerHeight);
        ReplaceLease(ref _iconSizeLease, IconSizeProperty, _tokens.IconSize);
        ReplaceLease(ref _iconSpacingLease, IconSpacingProperty, _tokens.IconSpacing);
        ReplaceLease(
            ref _contentPaddingLease,
            ContentPaddingProperty,
            new Thickness(
                _tokens.HorizontalContentPadding,
                _tokens.VerticalContentPadding));

        var typeScale = _theme.Theme.Typography.GetTypeScale(_tokens.LabelTextStyle);
        FontSize = typeScale.FontSize;
        FontFamily = _theme.Theme.Typography.GetFontFamily(_tokens.LabelTextStyle);
        FontWeight = _theme.Theme.Typography.GetFontWeight(_tokens.LabelTextStyle);
        Foreground = IsEffectivelyEnabled
            ? IsChecked == true ? _colors.ActiveContentColor : _colors.InactiveContentColor
            : IsChecked == true ? _colors.DisabledActiveContentColor : _colors.DisabledInactiveContentColor;
        TypographyScheme.SetTextStyle(this, _tokens.LabelTextStyle);

        UpdateVisibleIcon();
        UpdateSelectionMotion(snap: true);
        RaisePropertyChanged(IconBrushProperty, previousIcon, IconBrush);
        RaisePropertyChanged(HasVisibleIconProperty, previousVisible, HasVisibleIcon);
        RaisePropertyChanged(IsDefaultIconVisibleProperty, previousDefault, IsDefaultIconVisible);
        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCheckedProperty
            || change.Property == IsEffectivelyEnabledProperty)
        {
            var previousIcon = IconBrush;
            var previousVisible = HasVisibleIcon;
            var previousDefault = IsDefaultIconVisible;
            Foreground = !IsEffectivelyEnabled
                ? IsChecked == true ? _colors.DisabledActiveContentColor : _colors.DisabledInactiveContentColor
                : IsChecked == true ? _colors.ActiveContentColor : _colors.InactiveContentColor;
            UpdateVisibleIcon();
            UpdateSelectionMotion(snap: false);
            RaisePropertyChanged(IconBrushProperty, previousIcon, IconBrush);
            RaisePropertyChanged(HasVisibleIconProperty, previousVisible, HasVisibleIcon);
            RaisePropertyChanged(IsDefaultIconVisibleProperty, previousDefault, IsDefaultIconVisible);
            InteractionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (change.Property == IconProperty
            || change.Property == ShowSelectionIconProperty
            || change.Property == SelectionIconProperty)
        {
            var previousVisible = HasVisibleIcon;
            var previousDefault = IsDefaultIconVisible;
            UpdateVisibleIcon();
            UpdateSelectionMotion(snap: false);
            RaisePropertyChanged(HasVisibleIconProperty, previousVisible, HasVisibleIcon);
            RaisePropertyChanged(IsDefaultIconVisibleProperty, previousDefault, IsDefaultIconVisible);
            InvalidateMeasure();
        }
        else if (change.Property == IconSizeProperty || change.Property == IconSpacingProperty)
        {
            UpdateSelectionMotion(snap: true);
            InvalidateMeasure();
        }
        else if (change.Property == ContainerShapeOverrideProperty)
        {
            InvalidateVisual();
        }
    }

    private bool HasVisibleIconValue =>
        Icon is not null
        || (ShowSelectionIcon && SelectionIcon is not null && IsChecked == true);

    private void UpdateVisibleIcon()
    {
        _hasVisibleIcon = HasVisibleIconValue;
        _isDefaultIconVisible = Icon is null
            && ShowSelectionIcon
            && SelectionIcon is not null
            && IsChecked == true;
    }

    private void UpdateSelectionMotion(bool snap)
    {
        var iconTarget = Icon is not null ? 1d : IsDefaultIconVisible ? 1d : 0d;
        var offsetTarget = HasVisibleIcon ? 0d : -(IconSize + IconSpacing) / 2d;
        var fastSpatial = _theme.Spring(MotionRole.FastSpatial);

        _contentOffsetAnimation ??= _interaction.Motion.AddSpring(offsetTarget, fastSpatial);
        _iconScaleAnimation ??= _interaction.Motion.AddSpring(iconTarget, fastSpatial);
        _iconOpacityAnimation ??= _interaction.Motion.AddSpring(
            iconTarget,
            _theme.Spring(MotionRole.DefaultEffects));

        if (snap)
        {
            _contentOffsetAnimation.SnapTo(offsetTarget);
            _iconScaleAnimation.SnapTo(iconTarget);
            _iconOpacityAnimation.SnapTo(iconTarget);
        }
        else
        {
            _contentOffsetAnimation.AnimateTo(offsetTarget, fastSpatial);
            if (iconTarget <= 0)
            {
                // SegmentedButtonDefaults.Icon uses ExitTransition.None.
                _iconScaleAnimation.SnapTo(0);
                _iconOpacityAnimation.SnapTo(0);
            }
            else
            {
                _iconScaleAnimation.AnimateTo(1, fastSpatial);
                _iconOpacityAnimation.AnimateTo(
                    1,
                    _theme.Spring(MotionRole.DefaultEffects));
            }
        }

        SyncMotionOutputs();
        _interaction.Motion.Start();
    }

    private void SyncMotionOutputs()
    {
        var previousOffset = _contentOffset;
        var previousOpacity = _iconOpacity;
        var previousScale = _iconScale;
        _contentOffset = _contentOffsetAnimation?.Value ?? 0;
        _iconScale = _iconScaleAnimation?.Value ?? 0;
        _iconOpacity = _iconOpacityAnimation?.Value ?? 0;

        if (!previousOffset.Equals(_contentOffset))
            RaisePropertyChanged(ContentOffsetProperty, previousOffset, _contentOffset);
        if (!previousScale.Equals(_iconScale))
            RaisePropertyChanged(IconScaleProperty, previousScale, _iconScale);
        if (!previousOpacity.Equals(_iconOpacity))
            RaisePropertyChanged(IconOpacityProperty, previousOpacity, _iconOpacity);

        if (!previousOffset.Equals(_contentOffset)
            || !previousScale.Equals(_iconScale)
            || !previousOpacity.Equals(_iconOpacity))
        {
            InvalidateArrange();
            InvalidateVisual();
        }
    }

    private void ReplaceLease<T>(
        ref IDisposable? lease,
        StyledProperty<T> property,
        T value)
    {
        lease?.Dispose();
        lease = SetValue(property, value, BindingPriority.Template);
    }
}

/// <summary>
/// Performs Compose's icon-slot/content measurement and the half-slot offset used when the
/// default checked icon is absent.
/// </summary>
public sealed class MaterialSegmentedButtonContentPanel : Panel
{
    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonContentPanel, double>(nameof(IconSize), 18);

    public static readonly StyledProperty<double> IconSpacingProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonContentPanel, double>(nameof(IconSpacing), 8);

    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonContentPanel, Thickness>(
            nameof(ContentPadding),
            new Thickness(12, 8));

    public static readonly StyledProperty<bool> HasVisibleIconProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonContentPanel, bool>(nameof(HasVisibleIcon));

    public static readonly StyledProperty<double> IconScaleProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonContentPanel, double>(
            nameof(IconScale),
            1);

    public static readonly StyledProperty<double> IconOpacityProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonContentPanel, double>(
            nameof(IconOpacity),
            1);

    public static readonly StyledProperty<double> ContentOffsetProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonContentPanel, double>(
            nameof(ContentOffset));

    static MaterialSegmentedButtonContentPanel()
    {
        AffectsMeasure<MaterialSegmentedButtonContentPanel>(
            IconSizeProperty,
            IconSpacingProperty,
            ContentPaddingProperty,
            HasVisibleIconProperty);
        AffectsArrange<MaterialSegmentedButtonContentPanel>(
            IconScaleProperty,
            IconOpacityProperty,
            ContentOffsetProperty);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public double IconSpacing
    {
        get => GetValue(IconSpacingProperty);
        set => SetValue(IconSpacingProperty, value);
    }

    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public bool HasVisibleIcon
    {
        get => GetValue(HasVisibleIconProperty);
        set => SetValue(HasVisibleIconProperty, value);
    }

    public double IconScale
    {
        get => GetValue(IconScaleProperty);
        set => SetValue(IconScaleProperty, value);
    }

    public double IconOpacity
    {
        get => GetValue(IconOpacityProperty);
        set => SetValue(IconOpacityProperty, value);
    }

    public double ContentOffset
    {
        get => GetValue(ContentOffsetProperty);
        set => SetValue(ContentOffsetProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 2)
            return default;

        var padding = ContentPadding;
        var iconWidth = Math.Max(0, IconSize);
        var spacing = Math.Max(0, IconSpacing);
        var contentAvailable = Math.Max(
            0,
            availableSize.Width - padding.Left - padding.Right - iconWidth - spacing);

        Children[0].Measure(new Size(iconWidth, Math.Max(0, availableSize.Height)));
        Children[1].Measure(new Size(contentAvailable, Math.Max(0, availableSize.Height)));

        var content = Children[1].DesiredSize;
        var height = Math.Max(iconWidth, content.Height);
        return new Size(
            padding.Left + iconWidth + spacing + content.Width + padding.Right,
            padding.Top + height + padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 2)
            return finalSize;

        var padding = ContentPadding;
        var iconWidth = Math.Max(0, IconSize);
        var spacing = Math.Max(0, IconSpacing);
        var content = Children[1].DesiredSize;
        var innerHeight = Math.Max(0, finalSize.Height - padding.Top - padding.Bottom);
        var iconTop = padding.Top + Math.Max(0, (innerHeight - iconWidth) / 2);
        var contentX = padding.Left + iconWidth + spacing + ContentOffset;
        var contentTop = padding.Top + Math.Max(0, (innerHeight - content.Height) / 2);
        var icon = Children[0];
        var scale = Math.Max(0, IconScale);
        icon.Opacity = Math.Clamp(IconOpacity, 0, 1);
        icon.RenderTransformOrigin = new RelativePoint(0, 1, RelativeUnit.Relative);
        if (icon.RenderTransform is not ScaleTransform scaleTransform)
        {
            scaleTransform = new ScaleTransform();
            icon.RenderTransform = scaleTransform;
        }
        scaleTransform.ScaleX = scale;
        scaleTransform.ScaleY = scale;

        icon.Arrange(new Rect(padding.Left, iconTop, iconWidth, iconWidth));
        Children[1].Arrange(new Rect(contentX, contentTop, content.Width, content.Height));
        return finalSize;
    }
}

internal enum SegmentedButtonSegmentPosition
{
    None,
    Single,
    First,
    Middle,
    Last
}
