using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
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
        AffectsRender<MaterialTextField>(VariantProperty, IsErrorProperty, LabelProperty);
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

    /// <summary>How far the label has floated, 0 at rest and 1 at the top edge.</summary>
    protected double FloatFraction => _float?.Value ?? (ShouldFloat ? 1 : 0);

    /// <summary>The field's own box, excluding the supporting text below it.</summary>
    protected Rect FieldArea => new(0, 0, Bounds.Width, Math.Min(Bounds.Height, _tokens.ContainerHeight));

    private bool ShouldFloat => (_textBox?.IsFocused ?? false) || !string.IsNullOrEmpty(Text);

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
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return new Size(size.Width, Math.Max(size.Height, _tokens.ContainerHeight));
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_textBox is { })
        {
            _textBox.GotFocus -= OnEditorFocusChanged;
            _textBox.LostFocus -= OnEditorFocusChanged;
        }

        _textBox = e.NameScope.Find<TextBox>(TextBoxPartName);

        if (_textBox is { })
        {
            _textBox.GotFocus += OnEditorFocusChanged;
            _textBox.LostFocus += OnEditorFocusChanged;
        }

        UpdateFloat(snap: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty || change.Property == IsEffectivelyEnabledProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == TextProperty)
        {
            UpdateFloat();
        }
        else if (change.Property == LabelProperty || change.Property == IsErrorProperty)
        {
            _labelWidthAtRest = 0;
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
            sink.BeginFigure(new Point(notchStart + gap, rect.Y), false);
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
            sink.LineTo(new Point(notchStart, rect.Y));
            sink.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    /// <summary>The breathing room the notch leaves on each side of the label.</summary>
    private const double LabelPadding = 4;

    private void DrawLabel(DrawingContext context, Rect field, bool enabled, bool focused)
    {
        if (string.IsNullOrEmpty(Label))
            return;

        var brush = !enabled ? _colors.DisabledLabelColor
            : IsError ? _colors.ErrorColor
            : focused ? _colors.FocusLabelColor
            : _colors.LabelColor;

        var fraction = FloatFraction;
        var typography = _theme.Theme.Typography;
        var restingSize = typography.GetTypeScale(_tokens.InputTextStyle).FontSize;
        var floatedSize = typography.GetTypeScale(_tokens.FloatingLabelTextStyle).FontSize;
        var size = restingSize + (floatedSize - restingSize) * fraction;

        var text = new FormattedText(
            Label!,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(typography.GetFontFamily(_tokens.InputTextStyle)),
            size,
            brush);

        _labelWidthAtRest = _labelWidthAtRest > 0 ? _labelWidthAtRest : text.Width;
        _labelWidthFloated = MeasureFloatedLabel(typography, floatedSize);

        // At rest the label sits on the input's baseline; floated it rides the top edge — inside
        // the field for the filled variant, on the border for the outlined one.
        var restingY = (field.Height - text.Height) / 2;
        var floatedY = _tokens.IsFilled ? 8 : -text.Height / 2;

        context.DrawText(text, new Point(
            field.X + _tokens.HorizontalSpace,
            restingY + (floatedY - restingY) * fraction));
    }

    private double MeasureFloatedLabel(ResolvedTypographyScheme typography, double floatedSize)
    {
        var text = new FormattedText(
            Label ?? string.Empty,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(typography.GetFontFamily(_tokens.FloatingLabelTextStyle)),
            floatedSize,
            Brushes.Black);

        return text.Width;
    }

    private void OnEditorFocusChanged(object? sender, RoutedEventArgs e)
    {
        UpdateFloat();
        InvalidateVisual();
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
        _tokens = _theme.Theme.GetTextFieldTokens(Variant);
        _colors = _theme.Theme.GetTextFieldColors(Variant, _theme.IsDark);

        var typography = _theme.Theme.Typography;

        Padding = new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace, 0);
        MinHeight = _tokens.ContainerHeight;
        FontSize = typography.GetTypeScale(_tokens.InputTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.InputTextStyle);
        var previousSupporting = SupportingBrush;

        Foreground = IsEffectivelyEnabled
            ? IsError ? _colors.ErrorInputColor : _colors.InputColor
            : _colors.DisabledInputColor;

        RaisePropertyChanged(SupportingBrushProperty, previousSupporting, SupportingBrush);

        _labelWidthAtRest = 0;
        InvalidateMeasure();
        InvalidateVisual();
    }
}
