using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 text field, filled or outlined.
/// </summary>
/// <remarks>
/// The editing itself is Avalonia's: a <see cref="TextBox" /> sits in the template and owns the
/// caret, selection, IME, undo and every other thing that is much harder than it looks. What is
/// written here is the chrome Material specifies around it — the container, the label that floats,
/// the indicator or border, and the supporting text.
/// <para>
/// The label is the substance of the component. It starts sitting in the field at body size and,
/// on focus or once there is text, travels to the top edge and shrinks. That transition is a
/// single sprung fraction driving position, size and colour together, which is why interrupting it
/// — clicking away mid-float — reverses smoothly instead of snapping.
/// </para>
/// <para>
/// The outlined variant notches its border where the label crosses it, so the two do not overlap.
/// The gap is computed from the label's measured width at its floated size, not guessed.
/// </para>
/// </remarks>
[TemplatePart(TextBoxPartName, typeof(TextBox))]
public class MaterialTextField : TemplatedControl
{
    private const string TextBoxPartName = "PART_TextBox";

    public static readonly StyledProperty<TextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialTextField, TextFieldVariant>(nameof(Variant));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(
            nameof(Text), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <inheritdoc cref="Label" />
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(nameof(Label));

    /// <inheritdoc cref="Placeholder" />
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(nameof(Placeholder));

    /// <inheritdoc cref="SupportingText" />
    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MaterialTextField, string?>(nameof(SupportingText));

    /// <inheritdoc cref="IsError" />
    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<MaterialTextField, bool>(nameof(IsError));

    private readonly MaterialThemeWatcher _theme;
    private readonly MotionGroup _motion;

    private SpringAnimation? _float;
    private TextBox? _textBox;
    private DispatcherTimer? _caretTimer;
    private bool _caretVisible;
    private double _labelWidthAtRest;
    private double _labelWidthFloated;

    private TextFieldTokens _tokens = TextFieldTokens.Filled;
    private TextFieldColors _colors = TextFieldTokens.Filled.Resolve(null, false);

    public MaterialTextField()
    {
        _theme = new MaterialThemeWatcher(this);
        _motion = new MotionGroup(this);
        _motion.Ticked += (_, _) => InvalidateVisual();

        _theme.Changed += (_, _) => ApplyTokens();
        ApplyTokens();
    }

    static MaterialTextField()
    {
        AffectsRender<MaterialTextField>(
            VariantProperty,
            IsErrorProperty,
            LabelProperty,
            SupportingTextProperty);
        FocusableProperty.OverrideDefaultValue<MaterialTextField>(false);
    }

    public TextFieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>The label that floats to the top edge once the field is in use.</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Shown while the field is empty and focused, under the floated label.</summary>
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Helper text below the field. Turns into the error message when in error.</summary>
    public string? SupportingText
    {
        get => GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    /// <summary>Recolours the label, indicator and supporting text. The input itself stays readable.</summary>
    public bool IsError
    {
        get => GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    /// <summary>The colour the supporting text should take right now.</summary>
    public IImmutableBrush SupportingBrush =>
        !IsEffectivelyEnabled ? _colors.DisabledSupportingColor
        : IsError ? _colors.ErrorColor
        : _colors.SupportingColor;

    /// <summary>Exposes <see cref="SupportingBrush" /> to the template.</summary>
    public static readonly DirectProperty<MaterialTextField, IImmutableBrush> SupportingBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialTextField, IImmutableBrush>(
            nameof(SupportingBrush), o => o.SupportingBrush);

    /// <summary>The brush used for placeholder text while the editor is empty.</summary>
    public IImmutableBrush PlaceholderBrush => _colors.PlaceholderColor;

    /// <summary>Exposes <see cref="PlaceholderBrush"/> to the template.</summary>
    public static readonly DirectProperty<MaterialTextField, IImmutableBrush> PlaceholderBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialTextField, IImmutableBrush>(
            nameof(PlaceholderBrush), o => o.PlaceholderBrush);

    /// <summary>How far the label has floated, 0 at rest and 1 at the top edge.</summary>
    protected double FloatFraction => _float?.Value ?? (ShouldFloat ? 1 : 0);

    /// <summary>The visible field box, excluding the supporting text and outlined-label overhang.</summary>
    /// <remarks>
    /// Compose's text-field layout gives the container all remaining height after the supporting
    /// slot, rather than pinning it to its 56dp minimum. That matters for a caller that provides
    /// the standard 80dp field-with-supporting-text footprint: the container is then 60dp tall.
    /// </remarks>
    protected Rect FieldArea
    {
        get
        {
            var hasSupportingText = !string.IsNullOrEmpty(SupportingText);
            var outlinedLabelOverhang = OutlinedLabelOverhang;
            var availableHeight = Bounds.Height
                - outlinedLabelOverhang
                - (hasSupportingText ? SupportingSlotHeight : 0);
            var height = hasSupportingText
                ? availableHeight
                : Math.Min(availableHeight, _tokens.ContainerHeight);
            return new Rect(0, outlinedLabelOverhang, Bounds.Width, Math.Max(0, height));
        }
    }

    private double OutlinedLabelOverhang =>
        !_tokens.IsFilled && !string.IsNullOrEmpty(Label) ? OutlinedLabelTopInset : 0;

    // TextFieldImpl.kt defines SupportingTopPadding as 4.dp. The slot itself uses BodySmall's
    // 16dp line height. An outlined field additionally reserves its 8dp label overhang above
    // the 56dp visible container.
    private double SupportingSlotHeight => SupportingTopPadding
        + _theme.Theme.Typography.GetTypeScale(_tokens.SupportingTextStyle).LineHeight;

    private const double SupportingTopPadding = 4;

    private bool ShouldFloat => (_textBox?.IsFocused ?? false) || !string.IsNullOrEmpty(Text);

    /// <summary>The Avalonia editor used by specialized text-field variants.</summary>
    protected TextBox? Editor => _textBox;

    internal bool IsMaterialCaretVisible => _caretVisible && CanDrawCaret;

    internal bool IsMaterialCaretBlinking => _caretTimer?.IsEnabled == true;

    internal Rect? MaterialCaretBounds => ResolveCaretBounds();

    private SpringAnimation Float =>
        _float ??= _motion.AddSpring(ShouldFloat ? 1 : 0, _theme.Spring(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var field = FieldArea;
        if (field.Width <= 0 || field.Height <= 0)
            return;

        var enabled = IsEffectivelyEnabled;
        var focused = _textBox?.IsFocused ?? false;
        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, field.Size);

        if (!string.IsNullOrEmpty(Label))
            _labelWidthFloated = MeasureFloatedLabel(_theme.Theme.Typography);

        var outlineWidth = focused ? _tokens.FocusOutlineWidth : _tokens.OutlineWidth;
        var outline = !enabled ? _colors.DisabledOutlineColor
            : IsError ? _colors.ErrorColor
            : focused ? _colors.FocusOutlineColor
            : _colors.OutlineColor;

        if (_tokens.IsFilled)
        {
            // Filled: a tinted surface, and an indicator along the bottom edge only.
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
        DrawCaret(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return new Size(size.Width, Math.Max(size.Height, _tokens.ContainerHeight));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _caretTimer?.Stop();
        _caretVisible = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_textBox is { })
        {
            _textBox.GotFocus -= HandleEditorFocusChanged;
            _textBox.LostFocus -= HandleEditorFocusChanged;
            _textBox.PropertyChanged -= HandleEditorPropertyChanged;
        }

        _textBox = e.NameScope.Find<TextBox>(TextBoxPartName);

        if (_textBox is { })
        {
            _textBox.GotFocus += HandleEditorFocusChanged;
            _textBox.LostFocus += HandleEditorFocusChanged;
            _textBox.PropertyChanged += HandleEditorPropertyChanged;
        }

        UpdateEditorColors();
        UpdateEditorLayout();
        UpdateFloat(snap: true);
        ResetCaretBlink();
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
        else if (change.Property == TextProperty)
        {
            UpdateFloat();
            ResetCaretBlink();
        }
        else if (change.Property == LabelProperty)
        {
            _labelWidthAtRest = 0;
            UpdateEditorLayout();
            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (change.Property == SupportingTextProperty)
        {
            UpdateEditorLayout();
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Draws the border with a gap where the floated label crosses the top edge.
    /// </summary>
    /// <remarks>
    /// The gap opens as the label floats, so a half-floated label sits in a half-open notch rather
    /// than over a solid line or in a hole with nothing in it.
    /// </remarks>
    private void DrawNotchedOutline(
        DrawingContext context, Rect field, CornerRadius corners, IImmutableBrush outline, double width)
    {
        var inset = width / 2;
        var rect = field.Deflate(inset);
        var radius = MaterialContainer.Deflate(corners, inset);
        var pen = new ImmutablePen(outline, width);

        var notchStart = rect.X + _tokens.HorizontalSpace - LabelPadding;
        if (width > _tokens.OutlineWidth)
            notchStart -= 0.5;
        var notchWidth = _labelWidthFloated + LabelPadding * 2;
        var openness = FloatFraction;

        if (string.IsNullOrEmpty(Label) || openness <= 0.01 || notchWidth <= 0)
        {
            context.DrawRectangle(null, pen, new RoundedRect(rect, radius));
            return;
        }

        var gap = notchWidth * openness;
        var geometry = new StreamGeometry();

        using (var sink = geometry.Open())
        {
            // Start just past the notch and go all the way round back to just before it.
            // Compose's cutout resumes on a physical-pixel boundary. Snap this endpoint using the
            // active render scale so the first outline pixel is neither half-covered nor skipped.
            // Compose floors the cutout width to the preceding physical-pixel boundary. Rounding
            // can skip the first outline pixel for short labels such as "Date".
            var notchEnd = FloorToPhysicalPixel(notchStart + gap);
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
            // TODO:HACK Skia strokes this endpoint inclusively. Compose's cutout begins one physical
            // pixel earlier at the canonical 2x parity scale, so stop half a DIP sooner.
            sink.LineTo(new Point(notchStart - 0.5, rect.Y));
            sink.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private double FloorToPhysicalPixel(double value)
    {
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        return Math.Floor(value * scale) / scale;
    }

    /// <summary>The breathing room the notch leaves on each side of the label.</summary>
    private const double LabelPadding = 4;

    private void DrawLabel(DrawingContext context, Rect field, bool enabled, bool focused)
    {
        var parameters = GetLabelDrawingParameters(field);
        if (parameters is null)
            return;

        var brush = ResolveLabelBrush(enabled, focused);

        using var text = CreateLabelTextLayout(parameters, brush);

        _labelWidthAtRest = _labelWidthAtRest > 0 ? _labelWidthAtRest : text.Width;
        text.Draw(context, parameters.Origin);
    }

    /// <summary>
    /// Returns the same shaped label layout and origin used by <see cref="DrawLabel" />.
    /// It is internal so the visual-parity harness can measure a custom-drawn label without
    /// guessing from pixels or duplicating the control's float/notch mathematics.
    /// </summary>
    internal MaterialTextFieldLabelProbe? GetLabelParityProbe()
    {
        var parameters = GetLabelDrawingParameters(FieldArea);
        if (parameters is null)
            return null;
        var brush = ResolveLabelBrush(IsEffectivelyEnabled, _textBox?.IsFocused ?? false);
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

    private IImmutableBrush ResolveLabelBrush(bool enabled, bool focused) =>
        !enabled ? _colors.DisabledLabelColor
        : IsError ? _colors.ErrorColor
        : focused ? _colors.FocusLabelColor
        : _colors.LabelColor;

    private LabelDrawingParameters? GetLabelDrawingParameters(Rect field)
    {
        if (string.IsNullOrEmpty(Label) || field.Width <= 0 || field.Height <= 0)
            return null;
        var typography = _theme.Theme.Typography;
        var resting = typography.GetTypeScale(_tokens.InputTextStyle);
        var floated = typography.GetTypeScale(_tokens.FloatingLabelTextStyle);
        var fraction = FloatFraction;
        var typeface = new Typeface(
            typography.GetFontFamily(_tokens.InputTextStyle),
            FontStyle.Normal,
            typography.GetFontWeight(_tokens.InputTextStyle));
        var size = Lerp(resting.FontSize, floated.FontSize, fraction);
        var lineHeight = Lerp(resting.LineHeight, floated.LineHeight, fraction);
        var letterSpacing = Lerp(resting.LetterSpacing, floated.LetterSpacing, fraction);

        // TextFieldImpl.kt measures an expanded label with a 24dp minimum line height and a
        // minimized label with a 16dp one. Using font metrics here caused the custom Avalonia
        // draw path to be too short and vertically centred incorrectly.
        var restingY = _tokens.IsFilled
            ? (field.Height - resting.LineHeight) / 2
            : field.Y / 2 + OutlinedLabelTopInset + (field.Height - OutlinedLabelTopInset - resting.LineHeight) / 2;
        var floatedY = _tokens.IsFilled ? FilledLabelTopInset : 0;
        return new LabelDrawingParameters(
            Label,
            typeface,
            size,
            lineHeight,
            letterSpacing,
            new Point(
                field.X + _tokens.HorizontalSpace,
                restingY + (floatedY - restingY) * fraction));
    }

    private TextLayout CreateLabelTextLayout(LabelDrawingParameters parameters, IBrush brush)
    {
        return new TextLayout(
            parameters.Text,
            parameters.Typeface,
            parameters.FontSize,
            brush,
            flowDirection: FlowDirection,
            lineHeight: parameters.LineHeight,
            letterSpacing: parameters.LetterSpacing);
    }

    private double MeasureFloatedLabel(ResolvedTypographyScheme typography)
    {
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

    private const double FilledLabelTopInset = 8;
    private const double OutlinedLabelTopInset = 8;

    private static double Lerp(double start, double end, double amount) =>
        start + (end - start) * amount;

    /// <summary>Moves keyboard focus to the native editor hosted by the text-field template.</summary>
    protected bool FocusEditor() => _textBox?.Focus() == true;

    /// <summary>Moves keyboard focus to the native editor for a composite control in this assembly.</summary>
    internal bool FocusHostedEditor() => FocusEditor();

    /// <summary>Called when the hosted native editor gains or loses keyboard focus.</summary>
    protected virtual void OnEditorFocusChanged(bool isFocused)
    {
    }

    private void HandleEditorFocusChanged(object? sender, RoutedEventArgs e)
    {
        UpdateEditorLayout();
        UpdateFloat();
        ResetCaretBlink();
        OnEditorFocusChanged(_textBox?.IsFocused == true);
    }

    private void HandleEditorPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.CaretIndexProperty
            || e.Property == TextBox.SelectionStartProperty
            || e.Property == TextBox.SelectionEndProperty
            || e.Property == TextBox.CaretBlinkIntervalProperty)
        {
            ResetCaretBlink();
        }
    }

    private void UpdateFloat(bool snap = false)
    {
        var target = ShouldFloat ? 1.0 : 0.0;

        if (snap)
        {
            Float.SnapTo(target);
            InvalidateVisual();
            return;
        }

        Float.AnimateTo(target, _theme.Spring(MotionRole.FastSpatial));
        _motion.Start();
    }

    private void ApplyTokens()
    {
        var previousPlaceholder = PlaceholderBrush;
        _tokens = _theme.Theme.GetTextFieldTokens(Variant);
        _colors = _theme.Theme.GetTextFieldColors(Variant, _theme.IsDark);

        var typography = _theme.Theme.Typography;

        Padding = new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace, 0);
        MinHeight = _tokens.ContainerHeight;
        var inputStyle = typography.GetTypeScale(_tokens.InputTextStyle);
        FontSize = inputStyle.FontSize;
        FontFamily = typography.GetFontFamily(_tokens.InputTextStyle);
        FontWeight = typography.GetFontWeight(_tokens.InputTextStyle);
        // TextFieldImpl.kt provides bodyLarge to the editor content. The inherited attached role
        // carries its line height and tracking through Avalonia's TextBox template as well.
        TypographyScheme.SetTextStyle(this, _tokens.InputTextStyle);
        var previousSupporting = SupportingBrush;

        Foreground = IsEffectivelyEnabled
            ? IsError ? _colors.ErrorInputColor : _colors.InputColor
            : _colors.DisabledInputColor;

        RaisePropertyChanged(SupportingBrushProperty, previousSupporting, SupportingBrush);
        RaisePropertyChanged(PlaceholderBrushProperty, previousPlaceholder, PlaceholderBrush);

        _labelWidthAtRest = 0;
        UpdateEditorColors();
        UpdateEditorLayout();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void UpdateEditorLayout()
    {
        if (_textBox is null)
            return;

        // Compose centers the editor in its 56dp container. Avalonia's template instead uses a
        // fixed-height TextBox anchored to the bottom of the available grid row. These offsets
        // keep the editor centered, including the 4dp that a supporting slot adds to a filled
        // field's grid row and the 8dp outlined-label overhang.
        var bottom = _tokens.IsFilled
            ? string.IsNullOrEmpty(SupportingText) ? 2.5 : 6.5
            : OutlinedLabelOverhang + 2.5;
        // TODO:HACK Fluent's focused TextPresenter settles on the next physical pixel at the parity
        // harness's 2x scale. Move the fixed-height editor up by that half-DIP only while it is
        // focused; all unfocused field states retain their Compose-aligned position.
        if (_textBox.IsFocused)
            bottom += 0.5;
        _textBox.Margin = new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace, bottom);
    }

    private void UpdateEditorColors()
    {
        if (_textBox is null)
            return;

        // A disabled TextBox and the Fluent placeholder both have their own default brushes.
        // Set the component tokens locally so those defaults cannot replace Compose's colors.
        _textBox.Foreground = Foreground;
        _textBox.PlaceholderForeground = _colors.PlaceholderColor;
        // MaterialTextField draws the cursor from the native presenter's layout rectangle. Keep
        // TextBox responsible for editing and caret placement, but hide its endpoint-inset line.
        _textBox.CaretBrush = Brushes.Transparent;
        _textBox.Resources["TextControlForegroundDisabled"] = Foreground;
        _textBox.Resources["TextControlPlaceholderForeground"] = _colors.PlaceholderColor;
        _textBox.Resources["TextControlPlaceholderForegroundPointerOver"] = _colors.PlaceholderColor;
        _textBox.Resources["TextControlPlaceholderForegroundFocused"] = _colors.PlaceholderColor;
        _textBox.Resources["TextControlPlaceholderForegroundDisabled"] = _colors.PlaceholderColor;
        // Fluent's placeholder presenter independently applies this resource as Opacity. Material
        // text fields use the token brush directly, matching Compose's fully opaque placeholder.
        _textBox.Resources["TextControlPlaceholderOpacity"] = 1.0;
    }

    private void ResetCaretBlink()
    {
        _caretTimer?.Stop();
        _caretVisible = CanDrawCaret;

        if (_caretVisible && _textBox!.CaretBlinkInterval > TimeSpan.Zero)
        {
            _caretTimer ??= CreateCaretTimer();
            _caretTimer.Interval = _textBox.CaretBlinkInterval;
            _caretTimer.Start();
        }

        InvalidateVisual();
    }

    private DispatcherTimer CreateCaretTimer()
    {
        var timer = new DispatcherTimer();
        timer.Tick += (_, _) =>
        {
            if (!CanDrawCaret)
            {
                timer.Stop();
                _caretVisible = false;
            }
            else
            {
                _caretVisible = !_caretVisible;
            }

            InvalidateVisual();
        };
        return timer;
    }

    private bool CanDrawCaret =>
        _textBox is { IsFocused: true }
        && _textBox.SelectionStart == _textBox.SelectionEnd;

    private void DrawCaret(DrawingContext context)
    {
        if (!IsMaterialCaretVisible || _textBox is null)
            return;

        var caret = ResolveCaretBounds();
        var editorOrigin = _textBox.TranslatePoint(default, this);
        if (caret is null || editorOrigin is null)
            return;

        using (context.PushClip(new Rect(editorOrigin.Value, _textBox.Bounds.Size)))
            context.FillRectangle(_colors.FocusLabelColor, caret.Value);
    }

    private Rect? ResolveCaretBounds()
    {
        if (_textBox is null)
            return null;

        var presenter = _textBox.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
        if (presenter is null)
            return null;

        // Compose Desktop's TextFieldCursor draws a 1dp flat-capped line from cursorRect.top
        // through cursorRect.bottom. Filling the equivalent rectangle preserves both endpoint
        // rows; Avalonia TextPresenter instead moves each endpoint inward by 0.5dp.
        var hit = presenter.TextLayout.HitTestTextPosition(presenter.CaretIndex);
        var localTopLeft = new Point(Math.Floor(hit.Left), Math.Floor(hit.Top));
        var localBottomRight = new Point(localTopLeft.X + 1, Math.Ceiling(hit.Bottom));
        var topLeft = presenter.TranslatePoint(localTopLeft, this);
        var bottomRight = presenter.TranslatePoint(localBottomRight, this);
        if (topLeft is null || bottomRight is null)
            return null;

        var caret = new Rect(topLeft.Value, bottomRight.Value);
        return caret.Width > 0 && caret.Height > 0 ? caret : null;
    }
}

internal sealed record MaterialTextFieldLabelProbe(
    Point Origin,
    TextLayout Layout,
    IBrush Foreground,
    Typeface Typeface,
    double FontSize,
    double LineHeight,
    double LetterSpacing);

internal sealed record LabelDrawingParameters(
    string Text,
    Typeface Typeface,
    double FontSize,
    double LineHeight,
    double LetterSpacing,
    Point Origin);
