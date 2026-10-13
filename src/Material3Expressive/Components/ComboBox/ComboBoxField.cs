// Closed ComboBox decoration follows TextFieldImpl / OutlinedTextField for the single-line field.
// The native ComboBox keeps selection, editable text and the popup. Baseline: docs/upstream-baseline.json.
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Typography;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;
using TextStyle = Material3Expressive.Foundation.Typography.TextStyle;

namespace Material3Expressive.Components;

internal sealed class ComboBoxFieldDecoration : Control
{
    public override void Render(DrawingContext context) => (Parent as ComboBoxField)?.Draw(context);
}

internal sealed class ComboBoxInputHost : Panel
{
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<ComboBoxInputHost, Thickness>(nameof(Padding));
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
    static ComboBoxInputHost() => AffectsMeasure<ComboBoxInputHost>(PaddingProperty);

    protected override Size MeasureOverride(Size available)
    {
        var pad = Padding;
        var inner = new Size(Math.Max(0, available.Width - pad.Left - pad.Right), Math.Max(0, available.Height - pad.Top - pad.Bottom));
        var size = new Size();
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Measure(inner);
            size = new Size(Math.Max(size.Width, child.DesiredSize.Width), Math.Max(size.Height, child.DesiredSize.Height));
        }
        return new Size(size.Width + pad.Left + pad.Right, size.Height + pad.Top + pad.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var pad = Padding;
        var rect = new Rect(pad.Left, pad.Top, Math.Max(0, finalSize.Width - pad.Left - pad.Right), Math.Max(0, finalSize.Height - pad.Top - pad.Bottom));
        foreach (var child in Children)
            if (child.IsVisible) child.Arrange(rect);
        return finalSize;
    }
}

internal sealed class ComboBoxField : Panel
{
    public static readonly StyledProperty<double> DefaultMinimumWidthProperty = AvaloniaProperty.Register<ComboBoxField, double>(nameof(DefaultMinimumWidth));
    public double DefaultMinimumWidth { get => GetValue(DefaultMinimumWidthProperty); set => SetValue(DefaultMinimumWidthProperty, value); }
    public static readonly StyledProperty<double> DefaultMinimumHeightProperty = AvaloniaProperty.Register<ComboBoxField, double>(nameof(DefaultMinimumHeight));
    public double DefaultMinimumHeight { get => GetValue(DefaultMinimumHeightProperty); set => SetValue(DefaultMinimumHeightProperty, value); }
    public static readonly StyledProperty<double?> DefaultMinimizedLabelLineHeightProperty = AvaloniaProperty.Register<ComboBoxField, double?>(nameof(DefaultMinimizedLabelLineHeight));
    public double? DefaultMinimizedLabelLineHeight { get => GetValue(DefaultMinimizedLabelLineHeightProperty); set => SetValue(DefaultMinimizedLabelLineHeightProperty, value); }
    public static readonly StyledProperty<MotionSpring?> SpatialAnimationProperty = AvaloniaProperty.Register<ComboBoxField, MotionSpring?>(nameof(SpatialAnimation));
    public MotionSpring? SpatialAnimation { get => GetValue(SpatialAnimationProperty); set => SetValue(SpatialAnimationProperty, value); }
    public static readonly StyledProperty<MotionSpring?> FastEffectsAnimationProperty = AvaloniaProperty.Register<ComboBoxField, MotionSpring?>(nameof(FastEffectsAnimation));
    public MotionSpring? FastEffectsAnimation { get => GetValue(FastEffectsAnimationProperty); set => SetValue(FastEffectsAnimationProperty, value); }
    public static readonly StyledProperty<MotionSpring?> SlowEffectsAnimationProperty = AvaloniaProperty.Register<ComboBoxField, MotionSpring?>(nameof(SlowEffectsAnimation));
    public MotionSpring? SlowEffectsAnimation { get => GetValue(SlowEffectsAnimationProperty); set => SetValue(SlowEffectsAnimationProperty, value); }
    public static readonly StyledProperty<IBrush?> ContainerBrushProperty = AvaloniaProperty.Register<ComboBoxField, IBrush?>(nameof(ContainerBrush));
    public IBrush? ContainerBrush { get => GetValue(ContainerBrushProperty); set => SetValue(ContainerBrushProperty, value); }
    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty = AvaloniaProperty.Register<ComboBoxField, IBrush?>(nameof(IndicatorBrush));
    public IBrush? IndicatorBrush { get => GetValue(IndicatorBrushProperty); set => SetValue(IndicatorBrushProperty, value); }
    public static readonly StyledProperty<IBrush?> LabelBrushProperty = AvaloniaProperty.Register<ComboBoxField, IBrush?>(nameof(LabelBrush));
    public IBrush? LabelBrush { get => GetValue(LabelBrushProperty); set => SetValue(LabelBrushProperty, value); }
    public static readonly StyledProperty<TextStyle?> LargeTextStyleProperty = AvaloniaProperty.Register<ComboBoxField, TextStyle?>(nameof(LargeTextStyle));
    public TextStyle? LargeTextStyle { get => GetValue(LargeTextStyleProperty); set => SetValue(LargeTextStyleProperty, value); }
    public static readonly StyledProperty<TextStyle?> SmallTextStyleProperty = AvaloniaProperty.Register<ComboBoxField, TextStyle?>(nameof(SmallTextStyle));
    public TextStyle? SmallTextStyle { get => GetValue(SmallTextStyleProperty); set => SetValue(SmallTextStyleProperty, value); }

    private ComboBox? _owner;
    private ContentPresenter _label = null!, _leading = null!, _placeholder = null!, _supporting = null!;
    private ContentPresenter _expandedLabelStyle = null!, _floatingLabelStyle = null!;
    private ComboBoxFieldDecoration? _decoration;
    private ComboBoxInputHost _editor = null!;
    private TextBox? _input;
    private TextPresenter? _preedit;
    private Panel _trailing = null!;
    private Visual _arrow = null!;
    private AnimationChildScope? _attachScope;
    private Animatable<float>? _labelProgress, _placeholderAlpha, _thickness;
    private Animatable<Color>? _labelColor, _containerColor, _indicatorColor;
    private bool _pending;
    private int _phase;
    private double _cutoutTop, _aboveHeight, _supportingHeight, _containerHeight;
    private Thickness _padding;
    private Rect _containerBounds, _cutout;
    private IDisposable? _nameBinding, _helpBinding;

    private bool HasLabel => _owner is { } o && MaterialComboBox.GetLabel(o) != null;
    private TextFieldLabelPosition Position => _owner is { } o ? MaterialComboBox.GetLabelPosition(o) : TextFieldLabelPosition.Inside;
    private bool Cutout => Position == TextFieldLabelPosition.Cutout && HasLabel;
    private bool Above => Position == TextFieldLabelPosition.Above && HasLabel;
    private bool Outlined => _owner is { } o && MaterialComboBox.GetOutlined(o);
    private bool HasValue => _owner is { IsEditable: true }
        ? !string.IsNullOrEmpty(_owner.Text) || !string.IsNullOrEmpty(_preedit?.PreeditText)
        : _owner?.SelectionBoxItem != null;
    // An open menu keeps the focused label phase. Item press moves focus before selection is
    // committed; dropping to the empty phase there sends the label to the center and back.
    private bool InputFocused => _owner is { IsFocused: true } || _owner?.IsDropDownOpen == true
        || _owner?.IsKeyboardFocusWithin == true && _owner.IsEditable;
    private int Phase => _owner is { IsEffectivelyEnabled: true } && InputFocused ? 1 : HasValue ? 2 : 0;
    private bool Expanded => HasLabel && !Above;
    private float LabelTarget => Phase == 0 && Expanded ? 0 : 1;
    internal float LabelProgress => _labelProgress?.Value ?? LabelTarget;
    private float ThicknessTarget => _owner is { IsEffectivelyEnabled: true } && InputFocused ? 2 : 1;
    private double Scale => LayoutHelper.GetLayoutScale(this);
    private double Round(double value) => MathUtils.RoundToInt(value * Scale) / Scale;
    private double Lerp(double a, double b, double progress) => Round(a + (b - a) * progress);
    private Color PaintedLabelColor => _labelColor?.Value ?? ColorOf(LabelBrush);

    private double MinimizedLabelLineHeight
    {
        get
        {
            var height = _floatingLabelStyle.GetValue(TextBlock.LineHeightProperty);
            if (double.IsFinite(height) && height >= 0) return height;
            if (double.IsNaN(height) && DefaultMinimizedLabelLineHeight is { } fallback && double.IsFinite(fallback) && fallback >= 0)
                return fallback;
            return 0;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = (ComboBox)TemplatedParent!;
        _decoration = (ComboBoxFieldDecoration)Children.Single(c => c.Name == "Decoration");
        _label = (ContentPresenter)Children.Single(c => c.Name == "Label");
        _expandedLabelStyle = (ContentPresenter)Children.Single(c => c.Name == "ExpandedLabelStyle");
        _floatingLabelStyle = (ContentPresenter)Children.Single(c => c.Name == "FloatingLabelStyle");
        _leading = (ContentPresenter)Children.Single(c => c.Name == "Leading");
        _trailing = (Panel)Children.Single(c => c.Name == "Trailing");
        _arrow = (Visual)_trailing.Children.Single(c => c.Name == "DropDownArrow");
        _placeholder = (ContentPresenter)Children.Single(c => c.Name == "Placeholder");
        _supporting = (ContentPresenter)Children.Single(c => c.Name == "Supporting");
        _editor = (ComboBoxInputHost)Children.Single(c => c.Name == "Editor");
        _input = _editor.Children.OfType<TextBox>().Single(c => c.Name == "PART_EditableTextBox");
        _input.TemplateApplied += InputTemplateApplied;
        HookPreedit();
        _owner.PropertyChanged += OwnerChanged;
        _expandedLabelStyle.PropertyChanged += LabelEndpointChanged;
        _floatingLabelStyle.PropertyChanged += LabelEndpointChanged;
        foreach (var slot in new[] { _leading, _supporting, _placeholder })
            slot.PropertyChanged += SlotChanged;
        UpdateAccessibility();
        var motion = _attachScope = AnimationScope.Get(this).CreateChild("ComboBox");
        _phase = Phase;
        _labelProgress = new Animatable<float>(new FloatConverter(), LabelTarget, VisibilityThresholds.Float);
        _placeholderAlpha = new Animatable<float>(new FloatConverter(), Phase == 1 || Phase == 0 && !Expanded ? 1f : 0f, VisibilityThresholds.Float);
        _thickness = new Animatable<float>(new FloatConverter(), ThicknessTarget, VisibilityThresholds.Float);
        _labelColor = new Animatable<Color>(new ColorConverter(), ColorOf(LabelBrush), default);
        _containerColor = new Animatable<Color>(new ColorConverter(), ColorOf(ContainerBrush), default);
        _indicatorColor = new Animatable<Color>(new ColorConverter(), ColorOf(IndicatorBrush), default);
        motion.SubscribeValue(_labelProgress, _ => UpdateSlots());
        motion.SubscribeValue(_placeholderAlpha, _ => UpdateSlots());
        motion.SubscribeValue(_labelColor, _ => UpdateSlots());
        motion.SubscribeValue(_containerColor, _ => InvalidateDecoration());
        motion.SubscribeValue(_indicatorColor, _ => InvalidateDecoration());
        motion.SubscribeValue(_thickness, _ => InvalidateDecoration());
        UpdateArrow();
        UpdateSlots();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _pending = false;
        if (_owner != null) _owner.PropertyChanged -= OwnerChanged;
        if (_expandedLabelStyle != null) _expandedLabelStyle.PropertyChanged -= LabelEndpointChanged;
        if (_floatingLabelStyle != null) _floatingLabelStyle.PropertyChanged -= LabelEndpointChanged;
        if (_leading != null)
            foreach (var slot in new[] { _leading, _supporting, _placeholder })
                slot.PropertyChanged -= SlotChanged;
        if (_input != null) _input.TemplateApplied -= InputTemplateApplied;
        if (_preedit != null) _preedit.PropertyChanged -= PreeditChanged;
        _input = null; _preedit = null;
        _nameBinding?.Dispose(); _helpBinding?.Dispose();
        _nameBinding = _helpBinding = null;
        _attachScope?.Dispose(); _attachScope = null;
        _owner = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void SlotChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ContentProperty || e.Property == ContentPresenter.ContentTemplateProperty) UpdateSlots();
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialComboBox.IsErrorProperty || e.Property == MaterialComboBox.ErrorMessageProperty ||
            e.Property == MaterialComboBox.SupportingTextProperty || e.Property == MaterialComboBox.LabelProperty ||
            e.Property == DataValidationErrors.HasErrorsProperty)
            UpdateAccessibility();
        if (e.Property == ComboBox.IsDropDownOpenProperty) UpdateArrow();
        UpdateSlots();
        Retarget();
    }

    private void UpdateArrow()
    {
        if (_arrow == null) return;
        _arrow.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        _arrow.RenderTransform = new RotateTransform(_owner?.IsDropDownOpen == true ? 180 : 0);
    }

    private void UpdateAccessibility()
    {
        if (_owner == null) return;
        _nameBinding?.Dispose();
        _nameBinding = _owner.SetValue(AutomationProperties.NameProperty, MaterialComboBox.GetLabel(_owner) as string, BindingPriority.Style);
        _helpBinding?.Dispose();
        var help = MaterialComboBox.GetIsError(_owner) || DataValidationErrors.GetHasErrors(_owner)
            ? MaterialComboBox.GetErrorMessage(_owner) : MaterialComboBox.GetSupportingText(_owner) as string;
        _helpBinding = _owner.SetValue(AutomationProperties.HelpTextProperty, help, BindingPriority.Style);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_owner == null) return;
        UpdateSlots();
        Retarget();
        InvalidateMeasure();
        InvalidateDecoration();
    }

    private void LabelEndpointChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextElement.FontFamilyProperty || e.Property == TextElement.FontSizeProperty ||
            e.Property == TextElement.FontWeightProperty || e.Property == TextElement.FontStyleProperty ||
            e.Property == TextElement.FontStretchProperty || e.Property == TextElement.LetterSpacingProperty ||
            e.Property == TextBlock.LineHeightProperty || e.Property == TextBlock.TextAlignmentProperty)
            UpdateSlots();
    }

    private void UpdateSlots()
    {
        if (_owner == null || _label == null) return;
        _label.IsVisible = HasLabel;
        var leadingIcon = MaterialComboBox.GetLeadingIcon(_owner);
        var leadingTemplate = MaterialComboBox.GetLeadingIconTemplate(_owner);
        _leading.Content = leadingIcon;
        _leading.ContentTemplate = leadingTemplate;
        _leading.IsVisible = leadingIcon is not null || leadingTemplate is not null;
        var labelColor = new SolidColorBrush(PaintedLabelColor);
        var labelStyle = TextFieldLayout.InterpolateLabelStyle(_expandedLabelStyle, _floatingLabelStyle, _labelProgress?.Value ?? LabelTarget)
            ?? MaterialText.GetStyle(_label) ?? TextStyle.Empty;
        MaterialText.SetStyle(_label, labelStyle with { Foreground = labelColor });
        _label.Foreground = labelColor;
        _supporting.IsVisible = _supporting.Content != null;
        var placeholder = Math.Clamp(_placeholderAlpha?.Value ?? 0, 0, 1);
        var empty = _owner.IsEditable
            ? string.IsNullOrEmpty(_owner.Text) && string.IsNullOrEmpty(_preedit?.PreeditText)
            : _owner.SelectionBoxItem == null;
        _placeholder.IsVisible = _placeholder.Content != null && empty && placeholder > 0;
        _placeholder.Opacity = placeholder;
        InvalidateMeasure();
        InvalidateDecoration();
    }

    private void InputTemplateApplied(object? sender, TemplateAppliedEventArgs e) => HookPreedit();

    private void HookPreedit()
    {
        if (_preedit != null) _preedit.PropertyChanged -= PreeditChanged;
        _preedit = _input?.GetTemplateDescendants().OfType<TextPresenter>().FirstOrDefault(p => p.Name == "PART_TextPresenter");
        if (_preedit != null) _preedit.PropertyChanged += PreeditChanged;
    }

    private void PreeditChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextPresenter.PreeditTextProperty) { UpdateSlots(); Retarget(); }
    }

    private void InvalidateDecoration() => _decoration?.InvalidateVisual();
    private static Color ColorOf(IBrush? brush) => (brush as ISolidColorBrush)?.Color ?? Colors.Transparent;

    private void Retarget()
    {
        if (!this.IsAttachedToVisualTree() || _pending || _attachScope is not { } motion
            || motion.Lifetime.IsCancellationRequested) return;
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct); _pending = false;
            if (_owner is null) return;
            if (SpatialAnimation is not { } spatial || FastEffectsAnimation is not { } fast || SlowEffectsAnimation is not { } slow) return;
            var previous = _phase; _phase = Phase;
            var idleColorChange = previous == _phase && !_labelProgress!.IsRunning && !_labelColor!.IsRunning;
            Animate(_labelProgress!, LabelTarget, spatial);
            Animate(_placeholderAlpha!, Phase == 1 || Phase == 0 && !Expanded ? 1 : 0,
                previous == 0 && Phase == 1 || previous == 2 && Phase == 0 ? slow : fast);
            Animate(_thickness!, ThicknessTarget, spatial, !_owner.IsEffectivelyEnabled);
            AnimateColor(_labelColor!, ColorOf(LabelBrush), false);
            AnimateColor(_containerColor!, ColorOf(ContainerBrush), false);
            AnimateColor(_indicatorColor!, ColorOf(IndicatorBrush), !_owner.IsEffectivelyEnabled);

            void Animate(Animatable<float> value, float target, MotionSpring spec, bool snap = false)
            {
                if (value.TargetValue == target && (!snap || !value.IsRunning)) return;
                _ = motion.Launch(async token =>
                {
                    // updateTransition preserves velocity; thickness uses animateAsState.
                    if (ReferenceEquals(value, _thickness)) await value.StopAsync();
                    token.ThrowIfCancellationRequested();
                    if (snap) await value.SnapToAsync(target);
                    else await value.AnimateToAsync(motion, target, new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.01f), cancellationToken: token);
                }, "ComboBox scalar");
            }
            void AnimateColor(Animatable<Color> value, Color target, bool snap)
            {
                if (value.TargetValue == target && (!snap || !value.IsRunning)) return;
                _ = motion.Launch(async token =>
                {
                    if (!ReferenceEquals(value, _labelColor)) await value.StopAsync();
                    // An idle Transition starts its new child color on the next transition frame;
                    // animateColorAsState (container/indicator) has its own earlier registration.
                    else if (idleColorChange) await motion.NextFrameAsync(token);
                    token.ThrowIfCancellationRequested();
                    if (snap) await value.SnapToAsync(target);
                    else await value.AnimateToAsync(motion, target, new SpringSpec<Color>(fast.DampingRatio, fast.Stiffness), cancellationToken: token);
                }, "ComboBox color");
            }
        }, "ComboBox targets");
    }

    protected override Size MeasureOverride(Size available)
    {
        if (_owner == null) return base.MeasureOverride(available);
        var p = Math.Clamp(_labelProgress?.Value ?? LabelTarget, 0f, 1f);
        _cutoutTop = Cutout ? Round(MinimizedLabelLineHeight / 2) : 0;
        var maxHeight = Math.Max(0, available.Height - _cutoutTop);
        _padding = _owner.Padding;
        var top = Round(_padding.Top); var bottom = Round(_padding.Bottom);
        var start = Round(_padding.Left); var end = Round(_padding.Right);
        var startPad = _leading.IsVisible ? Math.Max(0, start - 12) : start;
        var endPad = _trailing.IsVisible ? Math.Max(0, end - 12) : end;
        _editor.Padding = _placeholder.Padding = new Thickness(startPad, 0, endPad, 0);
        _label.Padding = Above ? new Thickness(4, 0, 4, 4) : Cutout ? default : new Thickness(startPad, 0, endPad, 0);
        _supporting.Padding = new Thickness(16, 4, 16, 0);
        _label.MinHeight = Lerp(24, 16, p);
        var occupied = 0d; var occupiedY = 0d;
        void Measure(Control child, double width, double height) => child.Measure(new Size(Math.Max(0, width), Math.Max(0, height)));
        foreach (var child in new Control[] { _leading, _trailing })
        {
            Measure(child, available.Width - occupied, maxHeight);
            occupied += child.DesiredSize.Width; occupiedY = Math.Max(occupiedY, child.DesiredSize.Height);
        }
        var minWidth = double.IsNaN(_owner.Width) ? Math.Min(_owner.MinWidth > 0 ? _owner.MinWidth : DefaultMinimumWidth, available.Width) : available.Width;
        Measure(_label, Above ? minWidth : available.Width - (Cutout ? Lerp(occupied + start + end, start + end, p) : occupied), maxHeight - bottom);
        var labelIntrinsic = Above ? _label.DesiredSize.Height : 0;
        Measure(_supporting, minWidth, maxHeight);
        var supportingIntrinsic = _supporting.DesiredSize.Height;
        var labelHeight = _label.DesiredSize.Height;
        var effectiveTop = Cutout ? Math.Max(top, labelHeight / 2) : top + (HasLabel ? labelHeight : 0);
        Measure(_editor, available.Width - occupied, maxHeight - effectiveTop - bottom - supportingIntrinsic);
        Measure(_placeholder, available.Width - occupied, maxHeight - effectiveTop - bottom - supportingIntrinsic);
        occupiedY = Math.Max(occupiedY, Math.Max(_editor.DesiredSize.Height, _placeholder.DesiredSize.Height) + effectiveTop + bottom);
        var middleWidth = Math.Max(Math.Max(_editor.DesiredSize.Width, _placeholder.DesiredSize.Width),
            Cutout ? Lerp(_label.DesiredSize.Width, 0, p) : _label.DesiredSize.Width);
        var width = _leading.DesiredSize.Width + middleWidth + _trailing.DesiredSize.Width;
        if (Cutout) width = Math.Max(width, Round((_label.DesiredSize.Width + start + end) * p));
        width = Math.Min(available.Width, Math.Max(minWidth, width));
        if (Above) Measure(_label, width, labelIntrinsic);
        Measure(_supporting, width, maxHeight - occupiedY);
        _aboveHeight = Above ? _label.DesiredSize.Height : 0;
        _supportingHeight = _supporting.DesiredSize.Height;
        var inputHeight = new[] { _editor.DesiredSize.Height, _placeholder.DesiredSize.Height, Above ? 0 : Lerp(labelHeight, 0, p) }.Max();
        var labelContribution = HasLabel && !Above && !Cutout
            ? Math.Max(Round(MinimizedLabelLineHeight), Lerp(0, labelHeight, new CubicBezierEasing(0.3, 0, 0.8, 0.15).Ease(p))) : 0;
        var actualTop = Cutout ? Lerp(top, Math.Max(top, labelHeight / 2), p) : top + labelContribution;
        _containerHeight = Math.Max(Math.Max(_leading.DesiredSize.Height, _trailing.DesiredSize.Height), Round(actualTop + inputHeight + bottom));
        var minHeight = _owner.MinHeight > 0 ? _owner.MinHeight : DefaultMinimumHeight;
        var total = Math.Max(minHeight, _aboveHeight + _containerHeight + _supportingHeight);
        total = Math.Min(maxHeight, total);
        return new Size(width, Math.Max(0, total + _cutoutTop));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_owner == null) return base.ArrangeOverride(finalSize);
        _decoration?.Arrange(new Rect(finalSize));
        var width = finalSize.Width; var p = _labelProgress?.Value ?? LabelTarget;
        var yOffset = _cutoutTop + _aboveHeight;
        var height = Math.Max(0, finalSize.Height - yOffset - _supportingHeight);
        _containerBounds = new Rect(0, yOffset, width, height);
        var leading = _leading.DesiredSize.Width; var trailing = _trailing.DesiredSize.Width;
        var top = Round(_padding.Top);
        double Center(double item) => Round((height - item) / 2);
        void Place(Control child, double x, double y, double? w = null, double? h = null)
        {
            var size = child.DesiredSize;
            child.Arrange(new Rect(x, y, Math.Max(0, w ?? size.Width), Math.Max(0, h ?? size.Height)));
        }
        double Align(TextFieldLabelAlignment alignment, double item, double space) => alignment switch
        {
            TextFieldLabelAlignment.Center => Round((space - item) / 2),
            TextFieldLabelAlignment.End => space - item,
            _ => 0,
        };
        Place(_leading, 0, yOffset + Center(_leading.DesiredSize.Height));
        Place(_trailing, width - trailing, yOffset + Center(_trailing.DesiredSize.Height));
        var labelWidth = _label.DesiredSize.Width; var labelHeight = _label.DesiredSize.Height;
        var startPad = Round(_padding.Left); var endPad = Round(_padding.Right);
        var startX = Align(TextFieldLabelAlignment.Start, labelWidth, width - leading - trailing) + leading;
        var endX = startX;
        if (Above) startX = endX = Align(TextFieldLabelAlignment.Start, labelWidth, width);
        if (Cutout)
        {
            var lp = _leading.IsVisible ? leading + Math.Max(0, startPad - 12) : startPad;
            var tp = _trailing.IsVisible ? trailing + Math.Max(0, endPad - 12) : endPad;
            startX = Align(TextFieldLabelAlignment.Start, labelWidth, width - lp - tp) + lp;
            endX = Align(TextFieldLabelAlignment.Start, labelWidth, width - startPad - endPad) + startPad;
        }
        var startY = Above ? 0 : Center(labelHeight);
        var endY = Above ? 0 : Cutout ? -Math.Truncate(labelHeight * Scale / 2) / Scale : top;
        Place(_label, Lerp(startX, endX, p), _cutoutTop + Lerp(startY, endY, p));
        double TextY(Control child) => yOffset + (HasLabel && !Cutout ? top + (Above ? 0 : labelHeight) :
            Math.Max(Center(child.DesiredSize.Height), Cutout ? labelHeight / 2 : 0));
        var inputX = leading;
        var inputWidth = width - inputX - trailing;
        Place(_editor, inputX, TextY(_editor), inputWidth);
        Place(_placeholder, inputX, TextY(_placeholder), inputWidth);
        Place(_supporting, 0, yOffset + height, width);
        var cutoutProgress = Math.Clamp(p, 0f, 1f);
        var cutWidth = labelWidth * cutoutProgress;
        var cutLeft = Align(TextFieldLabelAlignment.Start, Round(cutWidth), width - startPad - endPad) + startPad - 4;
        _cutout = Cutout && cutWidth > 0 ? new Rect(Math.Max(0, cutLeft), yOffset - labelHeight * cutoutProgress / 2,
            Math.Max(0, Math.Min(width, cutLeft + cutWidth + 8) - Math.Max(0, cutLeft)), labelHeight * cutoutProgress) : default;
        InvalidateDecoration();
        return finalSize;
    }

    internal void Draw(DrawingContext context)
    {
        if (_owner == null) return;
        var radius = _owner.CornerRadius;
        var container = new RoundedRect(_containerBounds, radius);
        var background = ContainerBrush is ISolidColorBrush ? new SolidColorBrush(_containerColor?.Value ?? ColorOf(ContainerBrush)) : ContainerBrush;
        var indicator = IndicatorBrush is ISolidColorBrush ? new SolidColorBrush(_indicatorColor?.Value ?? ColorOf(IndicatorBrush)) : IndicatorBrush;
        var width = Math.Max(0, _thickness?.Value ?? ThicknessTarget);
        var clip = new GeometryGroup { FillRule = FillRule.EvenOdd };
        clip.Children.Add(new RectangleGeometry(new Rect(Bounds.Size)));
        if (_cutout.Width > 0) clip.Children.Add(new RectangleGeometry(_cutout));
        using (context.PushGeometryClip(clip))
        {
            context.DrawRectangle(background, null, container);
            if (width == 0) return;
            using var containerClip = context.PushClip(container);
            if (Outlined)
            {
                var inset = width / 2;
                var r = new CornerRadius(Math.Max(0, radius.TopLeft - inset), Math.Max(0, radius.TopRight - inset),
                    Math.Max(0, radius.BottomRight - inset), Math.Max(0, radius.BottomLeft - inset));
                context.DrawRectangle(null, new Pen(indicator, width), new RoundedRect(_containerBounds.Deflate(inset), r));
            }
            else context.DrawLine(new Pen(indicator, width), new Point(0, _containerBounds.Bottom - width / 2), new Point(_containerBounds.Width, _containerBounds.Bottom - width / 2));
        }
    }
}
