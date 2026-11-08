// Portions translated from AndroidX TextFieldImpl.kt / OutlinedTextField.kt.
// Copyright The Android Open Source Project. Apache-2.0.
// Baseline: docs/upstream-baseline.json. Native TextBox owns all editing state.
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
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

// One template-local owner of decoration layout, transition state and rendering.
// The order of measurement and placement follows Inside/CutoutTextFieldLayout.
internal sealed partial class TextFieldLayout : Panel
{
    private TextBox? _owner;
    private ContentPresenter _label = null!, _leading = null!, _trailing = null!, _prefix = null!, _suffix = null!, _supporting = null!, _placeholder = null!;
    private ContentPresenter _expandedLabelStyle = null!, _floatingLabelStyle = null!;
    private TextFieldDecoration? _decoration;
    private Border _editor = null!;
    private TextPresenter _text = null!;
    private Animatable<float>? _labelProgress, _placeholderAlpha, _affixAlpha, _thickness;
    private Animatable<Color>? _labelColor, _containerColor, _indicatorColor;
    private AnimationChildScope? _attachScope;
    private bool _pending, _pendingIndicatorUpdate;
    private bool _outlinedIndicatorEnabled;
    private int _phase;
    private double _cutoutTop, _aboveHeight, _supportingHeight, _containerHeight;
    private Thickness _padding;
    private Rect _containerBounds, _cutout;
    private IDisposable? _nameBinding, _helpBinding;

    internal float LabelProgress => _labelProgress?.Value ?? LabelTarget;
    internal float IndicatorThickness => _thickness?.Value ?? ThicknessTarget;
    internal Color PaintedLabelColor => _labelColor?.Value ?? ColorOf(LabelBrush);
    internal Rect ContainerBounds => _containerBounds;
    internal Rect CutoutBounds => _cutout;
    private bool HasLabel => _owner is { } o && MaterialTextField.GetLabel(o) != null;
    private TextFieldLabelPosition Position => _owner is { } o ? MaterialTextField.GetLabelPosition(o) : TextFieldLabelPosition.Inside;
    private bool Cutout => Position == TextFieldLabelPosition.Cutout && HasLabel;
    private bool Above => Position == TextFieldLabelPosition.Above && HasLabel;
    private bool SingleLine => _owner?.AcceptsReturn != true;
    private bool Outlined => _owner is { } o && MaterialTextField.GetOutlined(o);
    private int Phase => _sourceFocused ? 1 : string.IsNullOrEmpty(_owner?.Text) ? 0 : 2;
    private bool Expanded => HasLabel && !Above && !MaterialTextField.GetAlwaysMinimizeLabel(_owner!);
    private float LabelTarget => Phase == 0 && Expanded ? 0 : 1;
    private float ThicknessTarget => (Outlined ? _sourceFocused : _indicatorFocused) && _owner is { IsEffectivelyEnabled: true } ? 2 : 1;
    private double Scale => LayoutHelper.GetLayoutScale(this);
    private double Round(double value) => MathUtils.RoundToInt(value * Scale) / Scale;
    private double Lerp(double a, double b, double progress) => Round(a + (b - a) * progress);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = (TextBox)TemplatedParent!;
        _outlinedIndicatorEnabled = _owner.IsEffectivelyEnabled;
        _decoration = (TextFieldDecoration)Children.Single(c => c.Name == "Decoration");
        _label = (ContentPresenter)Children.Single(c => c.Name == "Label");
        _expandedLabelStyle = (ContentPresenter)Children.Single(c => c.Name == "ExpandedLabelStyle");
        _floatingLabelStyle = (ContentPresenter)Children.Single(c => c.Name == "FloatingLabelStyle");
        _leading = (ContentPresenter)Children.Single(c => c.Name == "Leading");
        _trailing = (ContentPresenter)Children.Single(c => c.Name == "Trailing");
        _prefix = (ContentPresenter)Children.Single(c => c.Name == "Prefix");
        _suffix = (ContentPresenter)Children.Single(c => c.Name == "Suffix");
        _supporting = (ContentPresenter)Children.Single(c => c.Name == "Supporting");
        _placeholder = (ContentPresenter)Children.Single(c => c.Name == "Placeholder");
        _editor = (Border)Children.Single(c => c.Name == "Editor");
        _text = (TextPresenter)((ScrollViewer)_editor.Child!).Content!;
        _owner.PropertyChanged += OwnerChanged;
        ObserveShape();
        _text.PropertyChanged += PresenterChanged;
        foreach (var slot in new[] { _leading, _trailing, _prefix, _suffix, _supporting, _placeholder })
            slot.PropertyChanged += SlotChanged;
        _expandedLabelStyle.PropertyChanged += LabelEndpointChanged;
        _floatingLabelStyle.PropertyChanged += LabelEndpointChanged;
        // Style priority preserves application-provided accessibility metadata.

        UpdateAccessibility();
        var motion = _attachScope = AnimationScope.Get(this).CreateChild("TextField");
        _phase = Phase;
        _labelProgress = new Animatable<float>(new FloatConverter(), LabelTarget, VisibilityThresholds.Float);
        _placeholderAlpha = new Animatable<float>(new FloatConverter(), Phase == 1 || Phase == 0 && !Expanded ? 1f : 0f, VisibilityThresholds.Float);
        _affixAlpha = new Animatable<float>(new FloatConverter(), Phase != 0 || !Expanded ? 1f : 0f, VisibilityThresholds.Float);
        _thickness = new Animatable<float>(new FloatConverter(), ThicknessTarget, VisibilityThresholds.Float);
        _labelColor = new Animatable<Color>(new ColorConverter(), ColorOf(LabelBrush), default);
        _containerColor = new Animatable<Color>(new ColorConverter(), ColorOf(ContainerBrush), default);
        _indicatorColor = new Animatable<Color>(new ColorConverter(), ColorOf(IndicatorBrush), default);
        motion.SubscribeValue(_labelProgress, _ => UpdateSlots());
        motion.SubscribeValue(_placeholderAlpha, _ => UpdateSlots());
        motion.SubscribeValue(_affixAlpha, _ => UpdateSlots());
        motion.SubscribeValue(_labelColor, _ => UpdateSlots());
        motion.SubscribeValue(_containerColor, _ => InvalidateDecoration());
        motion.SubscribeValue(_indicatorColor, _ => InvalidateDecoration());
        motion.SubscribeValue(_thickness, _ => InvalidateDecoration());
        AttachInteractions();
        UpdateSlots();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Release icon controls so a replacement template (e.g. a Theme change) can host them;
        // UpdateSlots assigns them again on attach.
        if (_leading != null) { _leading.Content = null; _leading.ContentTemplate = null; }
        if (_trailing != null) { _trailing.Content = null; _trailing.ContentTemplate = null; }
        _pending = false;
        _pendingIndicatorUpdate = false;
        _attachScope?.Dispose(); _attachScope = null;
        if (_owner != null) _owner.PropertyChanged -= OwnerChanged;
        DetachInteractions();
        if (_text != null) _text.PropertyChanged -= PresenterChanged;
        foreach (var slot in new[] { _leading, _trailing, _prefix, _suffix, _supporting, _placeholder })
            if (slot is not null) slot.PropertyChanged -= SlotChanged;
        _expandedLabelStyle.PropertyChanged -= LabelEndpointChanged;
        _floatingLabelStyle.PropertyChanged -= LabelEndpointChanged;
        _nameBinding?.Dispose(); _helpBinding?.Dispose();
        _nameBinding = _helpBinding = null;
        _owner = null;
        ObserveShape();
        base.OnDetachedFromVisualTree(e);
    }

    private void PresenterChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextPresenter.PreeditTextProperty) UpdateSlots();
    }

    private void SlotChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ContentProperty || e.Property == ContentPresenter.ContentTemplateProperty) UpdateSlots();
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialTextField.ShapeProperty) ObserveShape();
        if (e.Property == MaterialTextField.InteractionSourceProperty) ReplaceInteractionSource();
        if (e.Property == InputElement.IsEffectivelyEnabledProperty && _owner?.IsEffectivelyEnabled == false)
        {
            EndNativePress(cancelled: true);
            EndNativeFocus();
        }
        if (e.Property == InputElement.IsEffectivelyEnabledProperty || e.Property == MaterialTextField.IsErrorProperty)
        {
            if (_owner is { } owner && !Outlined) TextFieldThemeDefaults.SetIndicatorFocused(owner, _indicatorFocused);
            _pendingIndicatorUpdate = true;
        }
        // Enabled changes keep the common AndroidX transition alive. The fixed Core desktop
        // context-menu branch instead rebuilds decoration; README records the maintainer's choice.
        if (e.Property == MaterialTextField.IsErrorProperty || e.Property == MaterialTextField.ErrorMessageProperty ||
            e.Property == MaterialTextField.SupportingTextProperty || e.Property == MaterialTextField.LabelProperty || e.Property == DataValidationErrors.HasErrorsProperty) UpdateAccessibility();
        UpdateSlots();
        Retarget();
    }

    private void UpdateAccessibility()
    {
        if (_owner == null) return;
        _nameBinding?.Dispose();
        _nameBinding = _owner.SetValue(AutomationProperties.NameProperty, MaterialTextField.GetLabel(_owner) as string, BindingPriority.Style);
        _helpBinding?.Dispose();
        var help = MaterialTextField.GetIsError(_owner) || DataValidationErrors.GetHasErrors(_owner)
            ? MaterialTextField.GetErrorMessage(_owner) : MaterialTextField.GetSupportingText(_owner) as string;
        _helpBinding = _owner.SetValue(AutomationProperties.HelpTextProperty, help, BindingPriority.Style);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_owner == null || (e.Property != ContainerBrushProperty && e.Property != IndicatorBrushProperty &&
            e.Property != LabelBrushProperty && e.Property != LargeTextStyleProperty && e.Property != SmallTextStyleProperty &&
            e.Property != DefaultMinimumWidthProperty && e.Property != DefaultMinimumHeightProperty &&
            e.Property != DefaultMinimizedLabelLineHeightProperty &&
            e.Property != SupportingPaddingProperty && e.Property != IconAdjacentPaddingReductionProperty &&
            e.Property != AffixSpacingProperty && e.Property != AboveLabelPaddingProperty &&
            e.Property != ExpandedLabelMinHeightProperty && e.Property != CollapsedLabelMinHeightProperty &&
            e.Property != SpatialAnimationProperty && e.Property != FastEffectsAnimationProperty &&
            e.Property != SlowEffectsAnimationProperty && e.Property != FlowDirectionProperty)) return;
        if (e.Property == IndicatorBrushProperty || e.Property == SpatialAnimationProperty || e.Property == FastEffectsAnimationProperty)
            _pendingIndicatorUpdate = true;
        UpdateSlots(); Retarget(); InvalidateMeasure(); InvalidateDecoration();
    }

    private void UpdateSlots()
    {
        if (_owner == null || _label == null) return;
        _label.IsVisible = HasLabel;
        var leadingIcon = MaterialTextField.GetLeadingIcon(_owner);
        var leadingIconTemplate = MaterialTextField.GetLeadingIconTemplate(_owner);
        var useLeadingIcon = leadingIcon is not null || leadingIconTemplate is not null;
        _leading.Content = useLeadingIcon ? leadingIcon : _owner.InnerLeftContent;
        _leading.ContentTemplate = useLeadingIcon ? leadingIconTemplate : null;
        var trailingIcon = MaterialTextField.GetTrailingIcon(_owner);
        var trailingIconTemplate = MaterialTextField.GetTrailingIconTemplate(_owner);
        var useTrailingIcon = trailingIcon is not null || trailingIconTemplate is not null;
        _trailing.Content = useTrailingIcon ? trailingIcon : _owner.InnerRightContent;
        _trailing.ContentTemplate = useTrailingIcon ? trailingIconTemplate : null;
        _placeholder.SetCurrentValue(ContentPresenter.ContentProperty, MaterialTextField.GetPlaceholder(_owner) ?? _owner.PlaceholderText);
        var labelColor = new SolidColorBrush(PaintedLabelColor);
        var labelStyle = InterpolateLabelStyle(_expandedLabelStyle, _floatingLabelStyle, LabelProgress)
            ?? MaterialText.GetStyle(_label) ?? TextStyle.Empty;
        // Even while invalid metrics hold the previous typography, update the content scope's
        // color: child TextBlocks consume its Foreground projection above native inheritance.
        MaterialText.SetStyle(_label, labelStyle with { Foreground = labelColor });
        _label.Foreground = labelColor;
        _leading.IsVisible = _leading.Content != null || _leading.ContentTemplate != null;
        _trailing.IsVisible = _trailing.Content != null || _trailing.ContentTemplate != null;
        _supporting.IsVisible = _supporting.Content != null;
        var affix = Math.Clamp(_affixAlpha?.Value ?? (Expanded && Phase == 0 ? 0 : 1), 0, 1);
        _prefix.IsVisible = _prefix.Content != null && affix > 0;
        _suffix.IsVisible = _suffix.Content != null && affix > 0;
        _prefix.Opacity = _suffix.Opacity = affix;
        var placeholder = Math.Clamp(_placeholderAlpha?.Value ?? 0, 0, 1);
        _placeholder.IsVisible = (_placeholder.Content != null) && string.IsNullOrEmpty(_owner.Text) && string.IsNullOrEmpty(_text?.PreeditText) && placeholder > 0;
        _placeholder.Opacity = placeholder;
        InvalidateMeasure(); InvalidateDecoration();
    }

    private void LabelEndpointChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextElement.FontFamilyProperty || e.Property == TextElement.FontSizeProperty ||
            e.Property == TextElement.FontWeightProperty || e.Property == TextElement.FontStyleProperty ||
            e.Property == TextElement.FontStretchProperty || e.Property == TextElement.LetterSpacingProperty ||
            e.Property == TextBlock.LineHeightProperty || e.Property == TextBlock.TextAlignmentProperty)
            UpdateSlots();
    }

    // Each endpoint is projected independently by MaterialText, with native Avalonia inheritance
    // and precedence. Read only these unanimated inputs; native overrides on Label remain above
    // its interpolated MaterialText.Style and clearing them exposes the current progress immediately.
    internal static TextStyle? InterpolateLabelStyle(ContentPresenter expanded, ContentPresenter floating, float progress)
    {
        var p = Math.Clamp(progress, 0f, 1f);
        var expandedLineHeight = expanded.GetValue(TextBlock.LineHeightProperty);
        var floatingLineHeight = floating.GetValue(TextBlock.LineHeightProperty);
        // NaN is Avalonia's explicit automatic line height; it cannot be numerically interpolated.
        // Preserve endpoints with the same discrete boundary as Compose lerpTextUnitInheritable.
        // Infinity is invalid configuration, not automatic line height: keep the previous output
        // until the inputs recover rather than projecting a non-finite length into text layout.
        if (double.IsInfinity(expandedLineHeight) || double.IsInfinity(floatingLineHeight))
            return null;
        var lineHeight = double.IsNaN(expandedLineHeight) || double.IsNaN(floatingLineHeight)
            ? p < .5f ? expandedLineHeight : floatingLineHeight
            : MathUtils.Lerp(expandedLineHeight, floatingLineHeight, p);
        return new TextStyle
        {
            FontSize = MathUtils.Lerp(expanded.FontSize, floating.FontSize, p),
            LineHeight = lineHeight,
            LetterSpacing = MathUtils.Lerp(expanded.GetValue(TextElement.LetterSpacingProperty), floating.GetValue(TextElement.LetterSpacingProperty), p),
            FontWeight = (FontWeight)MathUtils.RoundToInt(MathUtils.Lerp((int)expanded.FontWeight, (int)floating.FontWeight, p)),
            FontFamily = p < 0.5 ? expanded.FontFamily : floating.FontFamily,
            FontStyle = p < 0.5 ? expanded.FontStyle : floating.FontStyle,
            // Avalonia's additional font-stretch field uses the same discrete endpoint boundary.
            FontStretch = p < 0.5 ? expanded.FontStretch : floating.FontStretch,
            TextAlignment = (p < 0.5 ? expanded : floating).GetValue(TextBlock.TextAlignmentProperty),
        };
    }

    // TextFieldImpl.minimizedLabelHalfHeight uses the small role's line height, with its
    // dedicated type-scale fallback for an unspecified/automatic height. This geometry value
    // must not flow back into the typography endpoints. Missing/invalid fallback reserves no
    // extra space until configuration recovers; it is not a substitute font metric.
    internal double MinimizedLabelLineHeight
    {
        get
        {
            var height = _floatingLabelStyle.GetValue(TextBlock.LineHeightProperty);
            if (double.IsFinite(height) && height >= 0)
                return height;
            if (double.IsNaN(height) && DefaultMinimizedLabelLineHeight is { } fallback &&
                double.IsFinite(fallback) && fallback >= 0)
                return fallback;
            return 0;
        }
    }

    private IBrush? PaintedIndicator => IndicatorBrush is ISolidColorBrush
        ? new SolidColorBrush(_indicatorColor?.Value ?? ColorOf(IndicatorBrush)) : IndicatorBrush;

    private void InvalidateDecoration(Size? size = null)
    {
        UpdateShape();
        _decoration?.Update(_containerBounds, _cutout, size ?? Bounds.Size,
            Outlined, PaintedIndicator, Math.Max(0, IndicatorThickness), ShapeCorners,
            _observedShape is Foundation.Rendering.RoundedCornerShape or Foundation.Rendering.CutCornerShape { IsRectangle: true }
                ? null : _shapePath, logicalShape: _observedShape is not null);
    }
    private static Color ColorOf(IBrush? brush) => (brush as ISolidColorBrush)?.Color ?? Colors.Transparent;

    private void Retarget()
    {
        if (!this.IsAttachedToVisualTree() || _pending || _attachScope is not { } motion
            || motion.Lifetime.IsCancellationRequested) return;
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            if (!await motion.WaitForNextFrameAsync(ct)) return; _pending = false;
            if (_owner is null) return;
            if (SpatialAnimation is not { } spatial || FastEffectsAnimation is not { } fast || SlowEffectsAnimation is not { } slow) return;
            // Node parameters arrive during recomposition; interaction collection below is
            // immediate. Keep this update separate from IndicatorLineNode's focus collector.
            if (_pendingIndicatorUpdate) { _pendingIndicatorUpdate = false; RetargetFilledIndicator(); }
            var previous = _phase; _phase = Phase;
            // animateBorderStrokeAsState removes its animateAsState branches while disabled.
            // Re-entering creates them at the current targets, independently of label motion.
            var initializeOutline = Outlined && _owner.IsEffectivelyEnabled && !_outlinedIndicatorEnabled;
            _outlinedIndicatorEnabled = _owner.IsEffectivelyEnabled;
            var idleColorChange = previous == _phase && !_labelProgress!.IsRunning && !_labelColor!.IsRunning;
            Animate(_labelProgress!, LabelTarget, spatial);
            Animate(_placeholderAlpha!, Phase == 1 || Phase == 0 && !Expanded ? 1 : 0,
                previous == 0 && Phase == 1 || previous == 2 && Phase == 0 ? slow : fast);
            Animate(_affixAlpha!, Phase != 0 || !Expanded ? 1 : 0, fast);
            if (Outlined) Animate(_thickness!, ThicknessTarget, spatial, !_owner!.IsEffectivelyEnabled || initializeOutline);
            AnimateColor(_labelColor!, ColorOf(LabelBrush), false);
            AnimateColor(_containerColor!, ColorOf(ContainerBrush), false);
            if (Outlined) AnimateColor(_indicatorColor!, ColorOf(IndicatorBrush), !_owner.IsEffectivelyEnabled || initializeOutline);

            void Animate(Animatable<float> value, float target, MotionSpring spec, bool snap = false)
            {
                if (value.TargetValue == target && (!snap || !value.IsRunning)) return;
                _ = motion.Launch(async token =>
                {
                    // updateTransition preserves velocity; thickness uses animateAsState.
                    if (ReferenceEquals(value, _thickness)) await value.StopAsync();
                    if (token.IsCancellationRequested) return;
                    if (snap) await value.SnapToAsync(target);
                    else await value.AnimateToAsync(motion, target, new SpringSpec<float>(spec.DampingRatio, spec.Stiffness, 0.01f), cancellationToken: token);
                }, "TextField scalar");
            }
            void AnimateColor(Animatable<Color> value, Color target, bool snap)
            {
                if (value.TargetValue == target && (!snap || !value.IsRunning)) return;
                _ = motion.Launch(async token =>
                {
                    if (!ReferenceEquals(value, _labelColor)) await value.StopAsync();
                    // An idle Transition starts its new child color on the next transition frame;
                    // animateColorAsState (container/indicator) has its own earlier registration.
                    else if (idleColorChange && !await motion.WaitForNextFrameAsync(token)) return;
                    if (token.IsCancellationRequested) return;
                    if (snap) await value.SnapToAsync(target);
                    else await value.AnimateToAsync(motion, target, new SpringSpec<Color>(fast.DampingRatio, fast.Stiffness), cancellationToken: token);
                }, "TextField color");
            }
        }, "TextField targets");
    }

    // Filled TextField.kt::IndicatorLineNode invalidates directly from its interaction collector;
    // it is not a child of the label's Transition or Outlined's animateBorderStrokeAsState.
    private void RetargetFilledIndicator()
    {
        if (Outlined || _owner is not { } owner || _attachScope is not { } scope ||
            scope.Lifetime.IsCancellationRequested || _indicatorColor is not { } color || _thickness is not { } width ||
            SpatialAnimation is not { } spatial || FastEffectsAnimation is not { } effects) return;
        if (TextFieldThemeDefaults.GetIndicatorFocused(owner) != _indicatorFocused)
            TextFieldThemeDefaults.SetIndicatorFocused(owner, _indicatorFocused);
        var targetColor = ColorOf(IndicatorBrush);
        var targetWidth = ThicknessTarget;
        var enabled = owner.IsEffectivelyEnabled;
        if (color.TargetValue != targetColor || !enabled && color.IsRunning)
            _ = scope.Launch(async ct =>
            {
                if (enabled) await color.AnimateToAsync(scope, targetColor,
                    new SpringSpec<Color>(effects.DampingRatio, effects.Stiffness), cancellationToken: ct);
                else await color.AnimateToAsync(scope, targetColor, new SnapSpec<Color>(), cancellationToken: ct);
            }, "TextField indicator color");
        if (width.TargetValue != targetWidth || !enabled && width.IsRunning)
            _ = scope.Launch(async ct =>
            {
                if (enabled) await width.AnimateToAsync(scope, targetWidth,
                    new SpringSpec<float>(spatial.DampingRatio, spatial.Stiffness, 0.01f), cancellationToken: ct);
                else await width.AnimateToAsync(scope, targetWidth, new SnapSpec<float>(), cancellationToken: ct);
            }, "TextField indicator width");
    }

    protected override Size MeasureOverride(Size available)
    {
        if (_owner == null) return base.MeasureOverride(available);
        // Keep spatial overshoot for placement, but never extrapolate typography or measured size.
        var p = Math.Clamp(LabelProgress, 0f, 1f);
        _cutoutTop = Cutout ? Round(MinimizedLabelLineHeight / 2) : 0;
        var maxHeight = Math.Max(0, available.Height - _cutoutTop);
        _padding = _owner.Padding;
        var top = Round(_padding.Top); var bottom = Round(_padding.Bottom);
        var start = Round(_padding.Left); var end = Round(_padding.Right);
        // Logical start/end padding; icons include their 48-DIP minimum interactive slot.
        var startPad = _leading.IsVisible ? Math.Max(0, start - IconAdjacentPaddingReduction) : start;
        var endPad = _trailing.IsVisible ? Math.Max(0, end - IconAdjacentPaddingReduction) : end;
        // Avalonia mirrors the visual subtree for RTL; all coordinates here remain logical.
        Thickness Insets(double s, double t, double e, double b) => new(s, t, e, b);
        _prefix.Padding = Insets(startPad, 0, AffixSpacing, 0); _suffix.Padding = Insets(AffixSpacing, 0, endPad, 0);
        _editor.Padding = _placeholder.Padding = Insets(_prefix.IsVisible ? 0 : startPad, 0, _suffix.IsVisible ? 0 : endPad, 0);
        _label.Padding = Above ? AboveLabelPadding : Cutout ? default : Insets(startPad, 0, endPad, 0);
        _supporting.Padding = SupportingPadding;
        // TextFieldImpl.textFieldLabelMinHeight: fixed MinTextLineHeight / MinFocusedLabelLineHeight,
        // distinct from the effective text line height and minimized-label geometry reservation.
        _label.MinHeight = Lerp(ExpandedLabelMinHeight, CollapsedLabelMinHeight, p);
        var occupied = 0d; var occupiedY = 0d;
        void Measure(Control child, double width, double height)
        {
            child.Measure(new Size(Math.Max(0, width), Math.Max(0, height)));
        }
        foreach (var child in new Control[] { _leading, _trailing, _prefix, _suffix })
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
        var affixWidth = _prefix.DesiredSize.Width + _suffix.DesiredSize.Width;
        var middleWidth = Math.Max(Math.Max(_editor.DesiredSize.Width, _placeholder.DesiredSize.Width) + affixWidth,
            Cutout ? Lerp(_label.DesiredSize.Width, 0, p) : _label.DesiredSize.Width);
        var width = _leading.DesiredSize.Width + middleWidth + _trailing.DesiredSize.Width;
        if (Cutout) width = Math.Max(width, Round((_label.DesiredSize.Width + start + end) * p));
        width = Math.Min(available.Width, Math.Max(minWidth, width));
        if (Above) Measure(_label, width, labelIntrinsic);
        Measure(_supporting, width, maxHeight - occupiedY);
        _aboveHeight = Above ? _label.DesiredSize.Height : 0;
        _supportingHeight = _supporting.DesiredSize.Height;
        var inputHeight = new[] { _editor.DesiredSize.Height, _placeholder.DesiredSize.Height, _prefix.DesiredSize.Height, _suffix.DesiredSize.Height,
            Above ? 0 : Lerp(labelHeight, 0, p) }.Max();
        // Inside's non-overlapped label contribution uses EmphasizedAccelerate, not label progress directly.
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
        var width = finalSize.Width; var p = LabelProgress;
        var yOffset = _cutoutTop + _aboveHeight;
        var height = Math.Max(0, finalSize.Height - yOffset - _supportingHeight);
        _containerBounds = new Rect(0, yOffset, width, height);
        var leading = _leading.DesiredSize.Width; var trailing = _trailing.DesiredSize.Width;
        var top = Round(_padding.Top);
        double Center(double item) => Round((height - item) / 2);
        void Place(Control child, double x, double y, double? w = null, double? h = null)
        {
            var size = child.DesiredSize;
            var cw = Math.Max(0, w ?? size.Width); var ch = Math.Max(0, h ?? size.Height);
            child.Arrange(new Rect(x, y, cw, ch));
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
        var startX = Align(MaterialTextField.GetExpandedLabelAlignment(_owner), labelWidth, width - leading - trailing) + leading;
        var endX = Align(MaterialTextField.GetMinimizedLabelAlignment(_owner), labelWidth, width - leading - trailing) + leading;
        if (Above) startX = endX = Align(MaterialTextField.GetMinimizedLabelAlignment(_owner), labelWidth, width);
        if (Cutout)
        {
            var lp = _leading.IsVisible ? leading + Math.Max(0, startPad - IconAdjacentPaddingReduction) : startPad;
            var tp = _trailing.IsVisible ? trailing + Math.Max(0, endPad - IconAdjacentPaddingReduction) : endPad;
            startX = Align(MaterialTextField.GetExpandedLabelAlignment(_owner), labelWidth, width - lp - tp) + lp;
            endX = Align(MaterialTextField.GetMinimizedLabelAlignment(_owner), labelWidth, width - startPad - endPad) + startPad;
        }
        var startY = Above ? 0 : SingleLine ? Center(labelHeight) : top + (Cutout ? 0 : Round(MinimizedLabelLineHeight / 2));
        var endY = Above ? 0 : Cutout ? -Math.Truncate(labelHeight * Scale / 2) / Scale : top;
        Place(_label, Lerp(startX, endX, p), _cutoutTop + Lerp(startY, endY, p));
        double TextY(Control child) => yOffset + (HasLabel && !Cutout ? top + (Above ? 0 : labelHeight) :
            Math.Max(SingleLine ? Center(child.DesiredSize.Height) : top, Cutout ? labelHeight / 2 : 0));
        Place(_prefix, leading, TextY(_prefix));
        var inputX = leading + _prefix.DesiredSize.Width;
        var inputWidth = width - inputX - trailing - _suffix.DesiredSize.Width;
        Place(_editor, inputX, TextY(_editor), inputWidth);
        Place(_placeholder, inputX, TextY(_placeholder), inputWidth);
        Place(_suffix, width - trailing - _suffix.DesiredSize.Width, TextY(_suffix));
        Place(_supporting, 0, yOffset + height, width);
        var cutoutProgress = Math.Clamp(p, 0f, 1f);
        var cutWidth = labelWidth * cutoutProgress;
        var cutLeft = Align(MaterialTextField.GetMinimizedLabelAlignment(_owner), Round(cutWidth), width - startPad - endPad) + startPad - 4;
        _cutout = Cutout && cutWidth > 0 ? new Rect(Math.Max(0, cutLeft), yOffset - labelHeight * cutoutProgress / 2,
            Math.Max(0, Math.Min(width, cutLeft + cutWidth + 8) - Math.Max(0, cutLeft)), labelHeight * cutoutProgress) : default;
        InvalidateDecoration(finalSize);
        _decoration?.Arrange(new Rect(finalSize));
        return finalSize;
    }

    internal void Draw(DrawingContext context)
    {
        if (_owner == null) return;
        var radius = _owner.CornerRadius;
        var container = new RoundedRect(_containerBounds, radius);
        var background = ContainerBrush is ISolidColorBrush ? new SolidColorBrush(_containerColor?.Value ?? ColorOf(ContainerBrush)) : ContainerBrush;
        var width = Math.Max(0, IndicatorThickness);
        if (_shapePath is { } shape)
        {
            using var translation = context.PushTransform(Matrix.CreateTranslation(_containerBounds.X, _containerBounds.Y));
            context.DrawGeometry(background, null, shape);
            if (!Outlined && width > 0)
            {
                // IndicatorLineNode constructs a NonZero shape path and intersects the bottom
                // rectangle. This is decoration only; it must not clip the editor or content slots.
                var line = new RectangleGeometry(new Rect(0, _containerBounds.Height - width, _containerBounds.Width, width));
                context.DrawGeometry(PaintedIndicator, null, new CombinedGeometry(GeometryCombineMode.Intersect, line, _indicatorShapePath!));
            }
            return;
        }
        context.DrawRectangle(background, null, container);
        if (Outlined || width == 0) return;
        using var containerClip = context.PushClip(container);
        context.DrawLine(new Pen(PaintedIndicator, width), new Point(0, _containerBounds.Bottom - width / 2), new Point(_containerBounds.Width, _containerBounds.Bottom - width / 2));
    }
}
