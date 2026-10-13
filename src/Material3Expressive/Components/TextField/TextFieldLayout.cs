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
    private bool _pending;
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
    private int Phase => _owner is { IsFocused: true, IsEffectivelyEnabled: true } ? 1 : string.IsNullOrEmpty(_owner?.Text) ? 0 : 2;
    private bool Expanded => HasLabel && !Above && !MaterialTextField.GetAlwaysMinimizeLabel(_owner!);
    private float LabelTarget => Phase == 0 && Expanded ? 0 : 1;
    private float ThicknessTarget => _owner is { IsFocused: true, IsEffectivelyEnabled: true } ? 2 : 1;
    private double Scale => LayoutHelper.GetLayoutScale(this);
    private double Round(double value) => MathUtils.RoundToInt(value * Scale) / Scale;
    private double Lerp(double a, double b, double progress) => Round(a + (b - a) * progress);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = (TextBox)TemplatedParent!;
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
        _text.PropertyChanged += PresenterChanged;
        foreach (var slot in new[] { _leading, _trailing, _prefix, _suffix, _supporting, _placeholder })
            slot.PropertyChanged += SlotChanged;
        _expandedLabelStyle.PropertyChanged += LabelEndpointChanged;
        _floatingLabelStyle.PropertyChanged += LabelEndpointChanged;
        // Style priority preserves application-provided accessibility metadata.

        UpdateAccessibility();
        var motion = AnimationScope.Get(this);
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
        UpdateSlots();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Release icon controls so a replacement template (e.g. a Theme change) can host them;
        // UpdateSlots assigns them again on attach.
        if (_leading != null) { _leading.Content = null; _leading.ContentTemplate = null; }
        if (_trailing != null) { _trailing.Content = null; _trailing.ContentTemplate = null; }
        _pending = false;
        if (_owner != null) _owner.PropertyChanged -= OwnerChanged;
        if (_text != null) _text.PropertyChanged -= PresenterChanged;
        foreach (var slot in new[] { _leading, _trailing, _prefix, _suffix, _supporting, _placeholder })
            slot.PropertyChanged -= SlotChanged;
        _expandedLabelStyle.PropertyChanged -= LabelEndpointChanged;
        _floatingLabelStyle.PropertyChanged -= LabelEndpointChanged;
        _nameBinding?.Dispose(); _helpBinding?.Dispose();
        _nameBinding = _helpBinding = null;
        _owner = null;
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
            e.Property != SpatialAnimationProperty && e.Property != FastEffectsAnimationProperty &&
            e.Property != SlowEffectsAnimationProperty && e.Property != FlowDirectionProperty)) return;
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

    private void InvalidateDecoration() => _decoration?.InvalidateVisual();
    private static Color ColorOf(IBrush? brush) => (brush as ISolidColorBrush)?.Color ?? Colors.Transparent;

    private void Retarget()
    {
        if (!this.IsAttachedToVisualTree() || _pending) return;
        var motion = AnimationScope.Get(this);
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
            Animate(_affixAlpha!, Phase != 0 || !Expanded ? 1 : 0, fast);
            Animate(_thickness!, ThicknessTarget, spatial, !_owner!.IsEffectivelyEnabled);
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
                    else if (idleColorChange) await motion.NextFrameAsync(token);
                    token.ThrowIfCancellationRequested();
                    if (snap) await value.SnapToAsync(target);
                    else await value.AnimateToAsync(motion, target, new SpringSpec<Color>(fast.DampingRatio, fast.Stiffness), cancellationToken: token);
                }, "TextField color");
            }
        }, "TextField targets");
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
        var startPad = _leading.IsVisible ? Math.Max(0, start - 12) : start;
        var endPad = _trailing.IsVisible ? Math.Max(0, end - 12) : end;
        // Avalonia mirrors the visual subtree for RTL; all coordinates here remain logical.
        Thickness Insets(double s, double t, double e, double b) => new(s, t, e, b);
        _prefix.Padding = Insets(startPad, 0, 2, 0); _suffix.Padding = Insets(2, 0, endPad, 0);
        _editor.Padding = _placeholder.Padding = Insets(_prefix.IsVisible ? 0 : startPad, 0, _suffix.IsVisible ? 0 : endPad, 0);
        _label.Padding = Above ? new Thickness(4, 0, 4, 4) : Cutout ? default : Insets(startPad, 0, endPad, 0);
        _supporting.Padding = new Thickness(16, 4, 16, 0);
        // TextFieldImpl.textFieldLabelMinHeight: fixed MinTextLineHeight / MinFocusedLabelLineHeight,
        // distinct from the effective text line height and minimized-label geometry reservation.
        _label.MinHeight = Lerp(24, 16, p);
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
        _decoration?.Arrange(new Rect(finalSize));
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
            var lp = _leading.IsVisible ? leading + Math.Max(0, startPad - 12) : startPad;
            var tp = _trailing.IsVisible ? trailing + Math.Max(0, endPad - 12) : endPad;
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
        var width = Math.Max(0, IndicatorThickness);
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
                var r = new CornerRadius(Math.Max(0, radius.TopLeft - inset), Math.Max(0, radius.TopRight - inset), Math.Max(0, radius.BottomRight - inset), Math.Max(0, radius.BottomLeft - inset));
                context.DrawRectangle(null, new Pen(indicator, width), new RoundedRect(_containerBounds.Deflate(inset), r));
            }
            else context.DrawLine(new Pen(indicator, width), new Point(0, _containerBounds.Bottom - width / 2), new Point(_containerBounds.Width, _containerBounds.Bottom - width / 2));
        }
    }
}
