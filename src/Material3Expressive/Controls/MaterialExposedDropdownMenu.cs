using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material exposed dropdown: a text-field anchor and a menu whose choices remain visible in
/// context.
/// </summary>
/// <remarks>
/// Avalonia's <see cref="ComboBox"/> remains the behavioral owner. It supplies selection,
/// text-search, editable input, keyboard navigation, popup dismissal, and the standard
/// ExpandCollapse, Selection, and Value automation patterns. This type adds the Material
/// text-field anchor, anchor-width menu policy, constrained popup geometry, focus restoration,
/// and coordinated menu/indicator motion.
/// <para>
/// Expansion and selection deliberately use the inherited bindable
/// <see cref="ComboBox.IsDropDownOpen"/>, <see cref="SelectingItemsControl.SelectedItem"/>, and
/// <see cref="ComboBox.Text"/> properties. There is no parallel state object to synchronize.
/// </para>
/// </remarks>
[TemplatePart(PopupPartName, typeof(Popup), IsRequired = true)]
[TemplatePart(EditableTextBoxPartName, typeof(TextBox), IsRequired = false)]
[TemplatePart(AnchorPartName, typeof(Control), IsRequired = true)]
public class MaterialExposedDropdownMenu : ComboBox, IVisualBoundsProbe, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private const string PopupPartName = "PART_Popup";
    private const string EditableTextBoxPartName = "PART_EditableTextBox";
    private const string AnchorPartName = "PART_Anchor";
    private const double SupportingTopPadding = 4;
    private const double OutlinedLabelTopInset = 8;
    private const double FilledLabelTopInset = 8;
    private const double LabelPadding = 4;

    /// <inheritdoc cref="Variant" />
    public static readonly StyledProperty<TextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialExposedDropdownMenu, TextFieldVariant>(nameof(Variant));

    /// <inheritdoc cref="Label" />
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MaterialExposedDropdownMenu, string?>(nameof(Label));

    /// <inheritdoc cref="SupportingText" />
    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MaterialExposedDropdownMenu, string?>(nameof(SupportingText));

    /// <inheritdoc cref="IsError" />
    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<MaterialExposedDropdownMenu, bool>(nameof(IsError));

    /// <inheritdoc cref="MatchAnchorWidth" />
    public static readonly StyledProperty<bool> MatchAnchorWidthProperty =
        AvaloniaProperty.Register<MaterialExposedDropdownMenu, bool>(
            nameof(MatchAnchorWidth), true);

    /// <inheritdoc cref="MenuVerticalMargin" />
    public static readonly StyledProperty<double> MenuVerticalMarginProperty =
        AvaloniaProperty.Register<MaterialExposedDropdownMenu, double>(
            nameof(MenuVerticalMargin), 48, validate: value => value >= 0);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, IImmutableBrush>
        SupportingBrushProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, IImmutableBrush>(
                nameof(SupportingBrush), owner => owner.SupportingBrush);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, IImmutableBrush>
        PlaceholderBrushProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, IImmutableBrush>(
                nameof(PlaceholderBrush), owner => owner.PlaceholderBrush);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, Thickness>
        AnchorContentMarginProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, Thickness>(
                nameof(AnchorContentMargin), owner => owner.AnchorContentMargin);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        AnchorLayoutHeightProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(AnchorLayoutHeight), owner => owner.AnchorLayoutHeight);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, Thickness>
        AnchorMarginProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, Thickness>(
                nameof(AnchorMargin), owner => owner.AnchorMargin);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, Thickness>
        TrailingIconMarginProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, Thickness>(
                nameof(TrailingIconMargin), owner => owner.TrailingIconMargin);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        PopupWidthProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(PopupWidth), owner => owner.PopupWidth);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        PopupMinWidthProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(PopupMinWidth), owner => owner.PopupMinWidth);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        PopupMaxWidthProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(PopupMaxWidth), owner => owner.PopupMaxWidth);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        EffectiveMenuMaxHeightProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(EffectiveMenuMaxHeight), owner => owner.EffectiveMenuMaxHeight);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        MenuOpenFractionProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(MenuOpenFraction), owner => owner.MenuOpenFraction);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        ArrowAngleProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(ArrowAngle), owner => owner.ArrowAngle);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        MenuScaleProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(MenuScale), owner => owner.MenuScale);

    public static readonly DirectProperty<MaterialExposedDropdownMenu, double>
        MenuOpacityProperty =
            AvaloniaProperty.RegisterDirect<MaterialExposedDropdownMenu, double>(
                nameof(MenuOpacity), owner => owner.MenuOpacity);

    private readonly MaterialThemeController _theme;
    private readonly MaterialMotionController _motion;
    private SpringAnimation? _float;
    private SpringAnimation? _menuScale;
    private SpringAnimation? _menuAlpha;
    private TextBox? _editableTextBox;
    private Popup? _popup;
    private MaterialSurface? _menuSurface;
    private Canvas? _trailingIcon;
    private Control? _anchor;
    private TopLevel? _topLevel;
    private double _labelWidthFloated;
    private double _lastMenuOpenFraction;
    private double _lastMenuOpacity;
    private Thickness _anchorContentMargin;
    private double _anchorLayoutHeight = 56;
    private Thickness _anchorMargin;
    private Thickness _trailingIconMargin = new(0, 0, 12, 0);
    private double _popupWidth = double.NaN;
    private double _popupMinWidth = 112;
    private double _popupMaxWidth = 280;
    private double _effectiveMenuMaxHeight = double.PositiveInfinity;

    private TextFieldMetrics _tokens = TextFieldDefaults.Metrics(TextFieldVariant.Filled);
    private TextFieldVisualColors _colors = TextFieldDefaults.Colors(Foundation.ResolvedTheme.Default, TextFieldVariant.Filled, false);

    public MaterialExposedDropdownMenu()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _motion = MaterialElementContext.Ensure(this).Motion;
        AttachedToVisualTree += HandleAttachedToVisualTree;
        DetachedFromVisualTree += HandleDetachedFromVisualTree;
        DropDownOpened += HandleDropDownOpened;
        DropDownClosed += HandleDropDownClosed;
        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() => HandleMotionFrame();

    static MaterialExposedDropdownMenu()
    {
        MaxDropDownHeightProperty.OverrideDefaultValue<MaterialExposedDropdownMenu>(
            double.PositiveInfinity);
        AffectsRender<MaterialExposedDropdownMenu>(
            VariantProperty,
            LabelProperty,
            SupportingTextProperty,
            IsErrorProperty,
            IsDropDownOpenProperty,
            SelectedItemProperty,
            TextProperty,
            IsKeyboardFocusWithinProperty);
    }

    /// <summary>The filled or outlined text-field treatment used by the anchor.</summary>
    public TextFieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>The anchor label. It floats while focused, expanded, or carrying a value.</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Helper or error text below the anchor.</summary>
    public string? SupportingText
    {
        get => GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    /// <summary>Uses error colors and exposes an Invalid automation item status.</summary>
    public bool IsError
    {
        get => GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    /// <summary>
    /// Whether the popup matches the anchor width. When false, Material's 112–280dp menu width
    /// range is used.
    /// </summary>
    public bool MatchAnchorWidth
    {
        get => GetValue(MatchAnchorWidthProperty);
        set => SetValue(MatchAnchorWidthProperty, value);
    }

    /// <summary>
    /// Space reserved above and below the visible window when constraining the menu. AndroidX uses
    /// 48dp so the popup never consumes the whole window or overlaps the software keyboard edge.
    /// </summary>
    public double MenuVerticalMargin
    {
        get => GetValue(MenuVerticalMarginProperty);
        set => SetValue(MenuVerticalMarginProperty, value);
    }

    /// <summary>The current helper/error brush used by the template.</summary>
    public IImmutableBrush SupportingBrush =>
        !IsEffectivelyEnabled ? _colors.DisabledSupportingColor
        : IsError ? _colors.ErrorColor
        : _colors.SupportingColor;

    /// <summary>The Material placeholder brush used by the editable and non-editable anchors.</summary>
    public IImmutableBrush PlaceholderBrush => _colors.PlaceholderColor;

    /// <summary>Insets the selected value/editor while leaving room for the trailing icon.</summary>
    public Thickness AnchorContentMargin => _anchorContentMargin;

    /// <summary>Height of the template row containing the visible anchor.</summary>
    public double AnchorLayoutHeight => _anchorLayoutHeight;

    /// <summary>Positions the outlined anchor below its floating-label overhang.</summary>
    public Thickness AnchorMargin => _anchorMargin;

    /// <summary>Centers the trailing indicator within the visible field, not its label overhang.</summary>
    public Thickness TrailingIconMargin => _trailingIconMargin;

    /// <summary>Exact popup width, or NaN when the menu uses its intrinsic width.</summary>
    public double PopupWidth => _popupWidth;

    /// <summary>Minimum popup width under the current sizing policy.</summary>
    public double PopupMinWidth => _popupMinWidth;

    /// <summary>Maximum popup width under the current sizing policy.</summary>
    public double PopupMaxWidth => _popupMaxWidth;

    /// <summary>Popup height after applying both the caller limit and visible-window margin.</summary>
    public double EffectiveMenuMaxHeight => _effectiveMenuMaxHeight;

    /// <summary>0 while collapsed and 1 while expanded, including deterministic intermediate motion.</summary>
    public double MenuOpenFraction => MenuOpen.Value;

    /// <summary>
    /// Trailing indicator rotation. AndroidX rotates this icon immediately when expansion changes;
    /// it is not part of the popup's entrance transition.
    /// </summary>
    public double ArrowAngle => IsDropDownOpen ? 180 : 0;

    /// <summary>Popup entrance scale derived from <see cref="MenuOpenFraction"/>.</summary>
    public double MenuScale => 0.8 + MenuOpenFraction * 0.2;

    /// <summary>Popup entrance alpha driven by the separate FastEffects motion role.</summary>
    public double MenuOpacity => MenuAlpha.Value;

    private double OutlinedLabelOverhang =>
        !_tokens.IsFilled && !string.IsNullOrEmpty(Label) ? OutlinedLabelTopInset : 0;

    private double SupportingSlotHeight => SupportingTopPadding
        + _theme.Current.Theme.Typography.GetTypeScale(_tokens.SupportingTextStyle).LineHeight;

    private Rect FieldArea
    {
        get
        {
            var availableHeight = Bounds.Height
                - OutlinedLabelOverhang
                - (!string.IsNullOrEmpty(SupportingText) ? SupportingSlotHeight : 0);
            return new Rect(
                0,
                OutlinedLabelOverhang,
                Bounds.Width,
                Math.Max(0, Math.Min(availableHeight, _tokens.ContainerHeight)));
        }
    }

    private bool ShouldFloat =>
        IsKeyboardFocusWithin
        || IsDropDownOpen
        || SelectedItem is not null
        || !string.IsNullOrEmpty(Text);

    private SpringAnimation Float =>
        _float ??= _motion.AddSpring(ShouldFloat ? 1 : 0, _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));

    private SpringAnimation MenuOpen =>
        _menuScale ??= _motion.AddSpring(
            IsDropDownOpen ? 1 : 0,
            _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));

    private SpringAnimation MenuAlpha =>
        _menuAlpha ??= _motion.AddSpring(
            IsDropDownOpen ? 1 : 0,
            _theme.Current.Theme.Motion.Get(MotionRole.FastEffects));

    public override void Render(DrawingContext context)
    {
        var field = FieldArea;
        if (field.Width <= 0 || field.Height <= 0)
            return;

        var enabled = IsEffectivelyEnabled;
        // Compose applies the anchor's expanded focus treatment on the first delivered motion
        // frame, not synchronously with the state write at tick zero.
        var focused = IsKeyboardFocusWithin || MenuOpenFraction > 0.000001;
        var corners = _theme.Current.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, field.Size);
        var outlineWidth = focused ? _tokens.FocusOutlineWidth : _tokens.OutlineWidth;
        var outline = !enabled ? _colors.DisabledOutlineColor
            : IsError ? _colors.ErrorColor
            : focused ? _colors.FocusOutlineColor
            : _colors.OutlineColor;

        if (!string.IsNullOrEmpty(Label))
            _labelWidthFloated = MeasureFloatedLabel();

        if (_tokens.IsFilled)
        {
            MaterialContainer.Draw(
                context,
                field.Size,
                corners,
                enabled ? _colors.ContainerColor : _colors.DisabledContainerColor);
            var y = field.Bottom - outlineWidth / 2;
            context.DrawLine(
                new ImmutablePen(outline, outlineWidth),
                new Point(field.X, y),
                new Point(field.Right, y));
        }
        else
        {
            DrawNotchedOutline(context, field, corners, outline, outlineWidth);
        }

        DrawLabel(context, field, enabled, focused);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        var supporting = string.IsNullOrEmpty(SupportingText) ? 0 : SupportingSlotHeight;
        return new Size(
            Math.Max(size.Width, 112),
            Math.Max(size.Height, _tokens.ContainerHeight + OutlinedLabelOverhang + supporting));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_editableTextBox is { })
        {
            _editableTextBox.GotFocus -= HandleEditorFocusChanged;
            _editableTextBox.LostFocus -= HandleEditorFocusChanged;
            _editableTextBox.PointerReleased -= HandleEditablePointerReleased;
        }

        base.OnApplyTemplate(e);

        _popup = e.NameScope.Find<Popup>(PopupPartName);
        _menuSurface = e.NameScope.Find<MaterialSurface>("PART_MenuSurface");
        _editableTextBox = e.NameScope.Find<TextBox>(EditableTextBoxPartName);
        _trailingIcon = e.NameScope.Find<Canvas>("PART_TrailingIcon");
        _anchor = e.NameScope.Find<Control>(AnchorPartName);

        if (_editableTextBox is { })
        {
            _editableTextBox.GotFocus += HandleEditorFocusChanged;
            _editableTextBox.LostFocus += HandleEditorFocusChanged;
            _editableTextBox.PointerReleased += HandleEditablePointerReleased;
        }

        UpdateEditorColors();
        UpdateFloat(snap: true);
        UpdateMenuMotion(snap: true);
        UpdateTrailingIcon();
        RaiseLayoutProperties();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty
            || change.Property == IsEffectivelyEnabledProperty
            || change.Property == IsErrorProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == IsDropDownOpenProperty)
        {
            var wasOpen = change.GetOldValue<bool>();
            var isOpen = change.GetNewValue<bool>();
            RaisePropertyChanged(
                ArrowAngleProperty,
                wasOpen ? 180 : 0,
                isOpen ? 180 : 0);
            UpdateTrailingIcon();
            UpdateFloat();
            UpdateMenuMotion();
        }
        else if (change.Property == SelectedItemProperty
            || change.Property == TextProperty
            || change.Property == IsKeyboardFocusWithinProperty)
        {
            UpdateFloat();
            RaiseLayoutProperties();
        }
        else if (change.Property == LabelProperty
            || change.Property == SupportingTextProperty)
        {
            RaiseLayoutProperties();
            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (change.Property == MatchAnchorWidthProperty
            || change.Property == BoundsProperty)
        {
            RaisePopupGeometry();
        }
        else if (change.Property == MenuVerticalMarginProperty
            || change.Property == MaxDropDownHeightProperty)
        {
            RaiseEffectiveMenuMaxHeight();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && IsEditable && !IsDropDownOpen && e.Key == Key.Enter)
        {
            SetCurrentValue(IsDropDownOpenProperty, true);
            e.Handled = true;
            return;
        }

        if (!e.Handled
            && IsEditable
            && IsDropDownOpen
            && (e.Key == Key.Down || e.Key == Key.Up))
        {
            if (FocusMenuItem(e.Key == Key.Down))
            {
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialExposedDropdownMenuAutomationPeer(this);

    /// <inheritdoc />
    public bool TryGetVisualBounds(string nodeId, out Rect bounds)
    {
        if (nodeId is "menu" or "menu-shadow"
            && _anchor is { } anchor
            && _popup?.Child is Control popupChild
            && popupChild.GetVisualDescendants().OfType<MaterialSurface>().SingleOrDefault() is { } menu
            && anchor.TransformToVisual(this) is { } anchorTransform
            && menu.TransformToVisual(this) is { } menuTransform)
        {
            var anchorBounds = TransformRect(new Rect(anchor.Bounds.Size), anchorTransform);
            var renderedMenuBounds = TransformRect(new Rect(menu.Bounds.Size), menuTransform);
            var opensBelow = renderedMenuBounds.Center.Y >= anchorBounds.Center.Y;
            var unscaled = new Rect(
                anchorBounds.Left,
                opensBelow ? anchorBounds.Bottom : anchorBounds.Top - menu.Bounds.Height,
                menu.Bounds.Width,
                menu.Bounds.Height);
            if (nodeId == "menu-shadow")
            {
                bounds = new Rect(
                    unscaled.Left + (1 - MenuScale) * unscaled.Width / 2,
                    unscaled.Top,
                    unscaled.Width * MenuScale,
                    unscaled.Height * MenuScale);
                return true;
            }

            // Compose's onGloballyPositioned probe observes graphicsLayer's translated origin
            // under center-top scaling while retaining the layout node's unscaled size.
            bounds = unscaled.Translate(new Vector(
                (1 - MenuScale) * unscaled.Width / 2,
                0));
            return true;
        }

        bounds = default;
        return false;
    }

    private void UpdateTrailingIcon()
    {
        if (_trailingIcon?.RenderTransform is RotateTransform rotation)
            rotation.Angle = ArrowAngle;
    }

    private void UpdateMenuSurfaceMotion()
    {
        if (_menuSurface is null)
            return;

        // Compose quantizes graphics-layer alpha to the nearest 8-bit value before Skia
        // compositing; Avalonia otherwise truncates the multiplier one step earlier.
        _menuSurface.Opacity = MaterialInteractionRendering.ComposeOpacity(MenuOpacity);
        if (_menuSurface.RenderTransform is ScaleTransform scale)
        {
            scale.ScaleX = MenuScale;
            scale.ScaleY = MenuScale;
        }
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new MaterialExposedDropdownMenuItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is MaterialExposedDropdownMenuItem)
        {
            recycleKey = null;
            return false;
        }

        recycleKey = null;
        return true;
    }

    private bool FocusMenuItem(bool forward)
    {
        if (ItemCount == 0)
            return false;

        var index = SelectedIndex >= 0
            ? SelectedIndex
            : forward ? 0 : ItemCount - 1;
        ScrollIntoView(index);
        return ContainerFromIndex(index)?.Focus(NavigationMethod.Directional) == true;
    }

    private void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is { })
            _topLevel.PropertyChanged += HandleTopLevelPropertyChanged;
        RaiseEffectiveMenuMaxHeight();
        _motion.RequestFrame();
    }

    private void HandleDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_topLevel is { })
            _topLevel.PropertyChanged -= HandleTopLevelPropertyChanged;
        _topLevel = null;
    }

    private void HandleTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
            RaiseEffectiveMenuMaxHeight();
    }

    private void HandleDropDownOpened(object? sender, EventArgs e)
    {
        RaiseEffectiveMenuMaxHeight();

        // AndroidX keeps the primary editable anchor focused so typing can continue while choices
        // are visible. Avalonia focuses the selected item by default, so restore the editor here.
        if (IsEditable)
            _editableTextBox?.Focus();
    }

    private void HandleDropDownClosed(object? sender, EventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var focused = topLevel?.FocusManager.GetFocusedElement();

        // An outside click already moved focus somewhere meaningful and must not be stolen. A
        // selection or Escape leaves focus null or in the now-detached popup; return that focus to
        // the anchor.
        if (focused is null
            || (focused is Visual visual && TopLevel.GetTopLevel(visual) != topLevel))
        {
            Focus(NavigationMethod.Unspecified);
        }
    }

    private void HandleEditablePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!e.Handled && e.InitialPressMouseButton == MouseButton.Left)
        {
            SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
            e.Handled = true;
        }
    }

    private void HandleEditorFocusChanged(object? sender, RoutedEventArgs e)
    {
        UpdateFloat();
        RaiseLayoutProperties();
        InvalidateVisual();
    }

    private void HandleMotionFrame()
    {
        var previous = _lastMenuOpenFraction;
        var current = MenuOpenFraction;
        var previousOpacity = _lastMenuOpacity;
        var currentOpacity = MenuOpacity;
        _lastMenuOpenFraction = current;
        _lastMenuOpacity = currentOpacity;
        RaisePropertyChanged(MenuOpenFractionProperty, previous, current);
        RaisePropertyChanged(MenuScaleProperty, 0.8 + previous * 0.2, 0.8 + current * 0.2);
        RaisePropertyChanged(MenuOpacityProperty, previousOpacity, currentOpacity);
        UpdateMenuSurfaceMotion();
        InvalidateVisual();
    }

    private void UpdateFloat(bool snap = false)
    {
        var target = ShouldFloat ? 1.0 : 0.0;
        if (snap)
            Float.SnapTo(target);
        else
            Float.AnimateTo(target, _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));
        _motion.RequestFrame();
        InvalidateVisual();
    }

    private void UpdateMenuMotion(bool snap = false)
    {
        var target = IsDropDownOpen ? 1.0 : 0.0;
        var previous = MenuOpenFraction;
        var previousOpacity = MenuOpacity;
        if (snap)
        {
            MenuOpen.SnapTo(target);
            MenuAlpha.SnapTo(target);
        }
        else
        {
            MenuOpen.AnimateTo(target, _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));
            MenuAlpha.AnimateTo(target, _theme.Current.Theme.Motion.Get(MotionRole.FastEffects));
        }
        _lastMenuOpenFraction = previous;
        _lastMenuOpacity = previousOpacity;
        _motion.RequestFrame();
        HandleMotionFrame();
    }

    private void ApplyTokens()
    {
        var previousSupporting = SupportingBrush;
        var previousPlaceholder = PlaceholderBrush;
        _tokens = TextFieldDefaults.Metrics(Variant);
        _colors = TextFieldDefaults.Colors(_theme.Current.Theme, Variant, _theme.Current.IsDark);
        var typography = _theme.Current.Theme.Typography;
        var inputStyle = typography.GetTypeScale(_tokens.InputTextStyle);

        MinHeight = _tokens.ContainerHeight;
        FontSize = inputStyle.FontSize;
        FontFamily = typography.GetFontFamily(_tokens.InputTextStyle);
        FontWeight = typography.GetFontWeight(_tokens.InputTextStyle);
        TypographyScheme.SetTextStyle(this, _tokens.InputTextStyle);
        Foreground = IsEffectivelyEnabled
            ? IsError ? _colors.ErrorInputColor : _colors.InputColor
            : _colors.DisabledInputColor;
        PlaceholderForeground = _colors.PlaceholderColor;

        RaisePropertyChanged(SupportingBrushProperty, previousSupporting, SupportingBrush);
        RaisePropertyChanged(PlaceholderBrushProperty, previousPlaceholder, PlaceholderBrush);
        UpdateEditorColors();
        RaiseLayoutProperties();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void UpdateEditorColors()
    {
        if (_editableTextBox is null)
            return;

        _editableTextBox.Foreground = Foreground;
        _editableTextBox.PlaceholderForeground = PlaceholderBrush;
        _editableTextBox.CaretBrush = _colors.FocusLabelColor;
        _editableTextBox.Resources["TextControlForegroundDisabled"] = Foreground;
        _editableTextBox.Resources["TextControlPlaceholderOpacity"] = 1.0;
    }

    private void RaiseLayoutProperties()
    {
        var overhang = OutlinedLabelOverhang;
        var bottom = _tokens.IsFilled ? 2.5 : overhang + 2.5;
        if (IsKeyboardFocusWithin)
            bottom += 0.5;

        SetAndRaise(
            AnchorContentMarginProperty,
            ref _anchorContentMargin,
            new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace + 32, bottom));
        SetAndRaise(
            AnchorLayoutHeightProperty,
            ref _anchorLayoutHeight,
            _tokens.ContainerHeight + overhang);
        SetAndRaise(
            AnchorMarginProperty,
            ref _anchorMargin,
            new Thickness(0, overhang, 0, 0));
        SetAndRaise(
            TrailingIconMarginProperty,
            ref _trailingIconMargin,
            new Thickness(0, overhang, 12, 0));
        RaisePopupGeometry();
    }

    private void RaisePopupGeometry()
    {
        var matches = MatchAnchorWidth && Bounds.Width > 0;
        SetAndRaise(
            PopupWidthProperty,
            ref _popupWidth,
            matches ? Bounds.Width : double.NaN);
        SetAndRaise(
            PopupMinWidthProperty,
            ref _popupMinWidth,
            matches ? Bounds.Width : 112);
        SetAndRaise(
            PopupMaxWidthProperty,
            ref _popupMaxWidth,
            matches ? Bounds.Width : 280);
        RaiseEffectiveMenuMaxHeight();
    }

    private void RaiseEffectiveMenuMaxHeight()
    {
        var windowHeight = TopLevel.GetTopLevel(this)?.ClientSize.Height
            ?? double.PositiveInfinity;
        var available = Math.Max(0, windowHeight - MenuVerticalMargin * 2);
        SetAndRaise(
            EffectiveMenuMaxHeightProperty,
            ref _effectiveMenuMaxHeight,
            Math.Min(MaxDropDownHeight, available));
    }

    private void DrawNotchedOutline(
        DrawingContext context,
        Rect field,
        CornerRadius corners,
        IImmutableBrush outline,
        double width)
    {
        var inset = width / 2;
        var rect = field.Deflate(inset);
        var radius = MaterialContainer.Deflate(corners, inset);
        var pen = new ImmutablePen(outline, width);
        var notchStart = rect.X + _tokens.HorizontalSpace - LabelPadding;
        var gap = (_labelWidthFloated + LabelPadding * 2) * Float.Value;

        if (string.IsNullOrEmpty(Label) || gap <= 0.01)
        {
            context.DrawRectangle(null, pen, new RoundedRect(rect, radius));
            return;
        }

        var geometry = new StreamGeometry();
        using (var sink = geometry.Open())
        {
            var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
            var notchEnd = Math.Floor((notchStart + gap) * scale) / scale;
            sink.BeginFigure(new Point(notchEnd, rect.Y), false);
            sink.LineTo(new Point(rect.Right - radius.TopRight, rect.Y));
            sink.ArcTo(new Point(rect.Right, rect.Y + radius.TopRight),
                new Size(radius.TopRight, radius.TopRight), 0, false, SweepDirection.Clockwise);
            sink.LineTo(new Point(rect.Right, rect.Bottom - radius.BottomRight));
            sink.ArcTo(new Point(rect.Right - radius.BottomRight, rect.Bottom),
                new Size(radius.BottomRight, radius.BottomRight), 0, false, SweepDirection.Clockwise);
            sink.LineTo(new Point(rect.X + radius.BottomLeft, rect.Bottom));
            sink.ArcTo(new Point(rect.X, rect.Bottom - radius.BottomLeft),
                new Size(radius.BottomLeft, radius.BottomLeft), 0, false, SweepDirection.Clockwise);
            sink.LineTo(new Point(rect.X, rect.Y + radius.TopLeft));
            sink.ArcTo(new Point(rect.X + radius.TopLeft, rect.Y),
                new Size(radius.TopLeft, radius.TopLeft), 0, false, SweepDirection.Clockwise);
            sink.LineTo(new Point(notchStart - 0.5, rect.Y));
            sink.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private void DrawLabel(DrawingContext context, Rect field, bool enabled, bool focused)
    {
        var parameters = GetLabelDrawingParameters(field);
        if (parameters is null)
            return;

        var brush = ResolveLabelBrush(enabled, focused);
        using var text = CreateLabelTextLayout(parameters, brush);
        text.Draw(context, parameters.Origin);
    }

    internal MaterialTextFieldLabelProbe? GetLabelParityProbe()
    {
        var parameters = GetLabelDrawingParameters(FieldArea);
        if (parameters is null)
            return null;
        var brush = ResolveLabelBrush(
            IsEffectivelyEnabled,
            IsKeyboardFocusWithin || IsDropDownOpen);
        var layout = CreateLabelTextLayout(parameters, brush);
        return new MaterialTextFieldLabelProbe(
            parameters.Origin,
            layout,
            brush,
            parameters.Typeface,
            parameters.FontSize,
            parameters.LineHeight,
            parameters.LetterSpacing);
    }

    private LabelDrawingParameters? GetLabelDrawingParameters(Rect field)
    {
        if (string.IsNullOrEmpty(Label) || field.Width <= 0 || field.Height <= 0)
            return null;

        var typography = _theme.Current.Theme.Typography;
        var resting = typography.GetTypeScale(_tokens.InputTextStyle);
        var floated = typography.GetTypeScale(_tokens.FloatingLabelTextStyle);
        var fraction = Float.Value;
        var size = Lerp(resting.FontSize, floated.FontSize, fraction);
        var lineHeight = Lerp(resting.LineHeight, floated.LineHeight, fraction);
        var letterSpacing = Lerp(resting.LetterSpacing, floated.LetterSpacing, fraction);
        var restingY = _tokens.IsFilled
            ? (field.Height - resting.LineHeight) / 2
            : field.Y / 2 + OutlinedLabelTopInset
                + (field.Height - OutlinedLabelTopInset - resting.LineHeight) / 2;
        var floatedY = _tokens.IsFilled ? FilledLabelTopInset : 0;
        return new LabelDrawingParameters(
            Label,
            new Typeface(
                typography.GetFontFamily(_tokens.InputTextStyle),
                FontStyle.Normal,
                typography.GetFontWeight(_tokens.InputTextStyle)),
            size,
            lineHeight,
            letterSpacing,
            new Point(
                field.X + _tokens.HorizontalSpace,
                restingY + (floatedY - restingY) * fraction));
    }

    private IImmutableBrush ResolveLabelBrush(bool enabled, bool focused)
    {
        if (!enabled)
            return _colors.DisabledLabelColor;
        if (IsError)
            return _colors.ErrorColor;
        if (!focused)
            return _colors.LabelColor;

        return Blend(_colors.LabelColor, _colors.FocusLabelColor, Float.Value);
    }

    private TextLayout CreateLabelTextLayout(LabelDrawingParameters parameters, IBrush brush) =>
        new(
            parameters.Text,
            parameters.Typeface,
            parameters.FontSize,
            brush,
            flowDirection: FlowDirection,
            lineHeight: parameters.LineHeight,
            letterSpacing: parameters.LetterSpacing);

    private double MeasureFloatedLabel()
    {
        var typography = _theme.Current.Theme.Typography;
        var style = typography.GetTypeScale(_tokens.FloatingLabelTextStyle);
        using var text = new TextLayout(
            Label ?? string.Empty,
            new Typeface(
                typography.GetFontFamily(_tokens.FloatingLabelTextStyle),
                FontStyle.Normal,
                typography.GetFontWeight(_tokens.FloatingLabelTextStyle)),
            style.FontSize,
            Brushes.Black,
            flowDirection: FlowDirection,
            lineHeight: style.LineHeight,
            letterSpacing: style.LetterSpacing);
        return text.Width;
    }

    private static double Lerp(double start, double end, double amount) =>
        start + (end - start) * amount;

    private static Rect TransformRect(Rect rect, Matrix transform)
    {
        var topLeft = transform.Transform(rect.TopLeft);
        var topRight = transform.Transform(rect.TopRight);
        var bottomLeft = transform.Transform(rect.BottomLeft);
        var bottomRight = transform.Transform(rect.BottomRight);
        var left = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        var top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        var right = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        var bottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
        return new Rect(left, top, right - left, bottom - top);
    }

    private static IImmutableBrush Blend(
        IImmutableBrush from,
        IImmutableBrush to,
        double fraction)
    {
        if (from is not ISolidColorBrush fromSolid || to is not ISolidColorBrush toSolid)
            return fraction >= 0.5 ? to : from;

        static byte LerpByte(byte start, byte end, double amount) =>
            (byte)Math.Clamp(Math.Round(start + (end - start) * amount), 0, 255);

        var start = fromSolid.Color;
        var end = toSolid.Color;
        return new ImmutableSolidColorBrush(Color.FromArgb(
            LerpByte(start.A, end.A, fraction),
            LerpByte(start.R, end.R, fraction),
            LerpByte(start.G, end.G, fraction),
            LerpByte(start.B, end.B, fraction)));
    }
}

/// <summary>The generated item container used inside a Material exposed dropdown.</summary>
public class MaterialExposedDropdownMenuItem : ComboBoxItem;
