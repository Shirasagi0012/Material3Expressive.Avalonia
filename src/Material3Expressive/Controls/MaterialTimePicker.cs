using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Which half of the time the dial is currently editing.</summary>
public enum TimePickerField
{
    Hour,
    Minute
}

/// <summary>
/// A Material 3 time picker: a clock dial with a hand you drag.
/// </summary>
/// <remarks>
/// The dial edits one field at a time and switches to minutes once an hour is chosen, because a
/// single dial cannot show both scales at once — that switch is the interaction, not a shortcut.
/// <para>
/// The hand's angle is a spring, so dragging it round and letting go settles rather than stopping
/// dead, and tapping a number on the far side sweeps to it instead of jumping.
/// </para>
/// </remarks>
public class MaterialTimePicker : TemplatedControl
{
    /// <inheritdoc cref="Time" />
    public static readonly StyledProperty<TimeSpan> TimeProperty =
        AvaloniaProperty.Register<MaterialTimePicker, TimeSpan>(
            nameof(Time), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <inheritdoc cref="Field" />
    public static readonly StyledProperty<TimePickerField> FieldProperty =
        AvaloniaProperty.Register<MaterialTimePicker, TimePickerField>(nameof(Field));

    /// <summary>Twelve o'clock is up, and angles are measured from there.</summary>
    private const double TopAngle = -90;

    private readonly MaterialThemeWatcher _theme;
    private readonly MotionGroup _motion;

    private SpringAnimation? _hand;
    private PickerTokens _tokens = PickerTokens.Time;
    private PickerColors _colors = PickerTokens.Time.Resolve(null, false);
    private bool _dragging;

    public MaterialTimePicker()
    {
        _theme = new MaterialThemeWatcher(this);
        _motion = new MotionGroup(this);
        _motion.Ticked += (_, _) => InvalidateVisual();

        _theme.Changed += (_, _) => ApplyTokens();
        Focusable = true;
        ApplyTokens();
    }

    static MaterialTimePicker()
    {
        AffectsRender<MaterialTimePicker>(TimeProperty, FieldProperty);
    }

    /// <summary>The chosen time.</summary>
    public TimeSpan Time
    {
        get => GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    /// <summary>Which half the dial is editing.</summary>
    public TimePickerField Field
    {
        get => GetValue(FieldProperty);
        set => SetValue(FieldProperty, value);
    }

    /// <summary>The dial's box, below the headline.</summary>
    protected Rect DialArea
    {
        get
        {
            var size = Math.Min(Bounds.Width, Bounds.Height - HeadlineHeight);
            return new Rect((Bounds.Width - size) / 2, HeadlineHeight, size, size);
        }
    }

    private double HeadlineHeight =>
        _theme.Theme.Typography.GetTypeScale(_tokens.HeadlineTextStyle).LineHeight + 32;

    private SpringAnimation Hand =>
        _hand ??= _motion.AddSpring(TargetAngle(), _theme.Spring(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, Bounds.Size);
        MaterialContainer.Draw(context, Bounds.Size, corners, _colors.ContainerColor);

        DrawHeadline(context);
        DrawDial(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = _tokens.ContainerWidth;
        return new Size(width, HeadlineHeight + width);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!DialArea.Contains(e.GetPosition(this)))
            return;

        _dragging = true;
        e.Pointer.Capture(this);
        SetFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_dragging)
            SetFromPoint(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_dragging)
            return;

        _dragging = false;

        // Choosing an hour moves you on to the minutes; that switch is the interaction.
        if (Field == TimePickerField.Hour)
            Field = TimePickerField.Minute;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TimeProperty || change.Property == FieldProperty)
        {
            Hand.AnimateTo(TargetAngle(), _theme.Spring(MotionRole.FastSpatial));
            _motion.Start();
        }
    }

    private void DrawHeadline(DrawingContext context)
    {
        var size = _theme.Theme.Typography.GetTypeScale(_tokens.HeadlineTextStyle).FontSize;
        var text = Text(
            $"{Time.Hours % 24:00}:{Time.Minutes:00}",
            size,
            _colors.HeadlineColor);

        context.DrawText(text, new Point((Bounds.Width - text.Width) / 2, 16));
    }

    private void DrawDial(DrawingContext context)
    {
        var dial = DialArea;
        if (dial.Width <= 0)
            return;

        var centre = dial.Center;
        var radius = dial.Width / 2;
        var numberRadius = radius - _tokens.DaySize / 2;

        // The face.
        context.DrawEllipse(_colors.SelectedDayContainerColor, null, centre, radius, radius);

        var angle = Hand.Value;
        var handEnd = PointOn(centre, numberRadius, angle);

        context.DrawLine(new ImmutablePen(_colors.DayColor, 2), centre, handEnd);
        context.DrawEllipse(_colors.DayColor, null, centre, 4, 4);
        context.DrawEllipse(_colors.DayColor, null, handEnd, _tokens.DaySize / 2, _tokens.DaySize / 2);

        var fontSize = _theme.Theme.Typography.GetTypeScale(_tokens.DayTextStyle).FontSize;
        var count = Field == TimePickerField.Hour ? 12 : 12;
        var step = Field == TimePickerField.Hour ? 1 : 5;

        for (var i = 0; i < count; i++)
        {
            var value = Field == TimePickerField.Hour
                ? i == 0 ? 12 : i
                : i * step;

            var at = PointOn(centre, numberRadius, TopAngle + i * (360.0 / count));
            var selected = Math.Abs(NormalisedDelta(TopAngle + i * (360.0 / count), angle)) < 5;

            var text = Text(
                value.ToString("0", CultureInfo.CurrentCulture),
                fontSize,
                selected ? _colors.SelectedDayColor : _colors.DayColor);

            context.DrawText(text, new Point(at.X - text.Width / 2, at.Y - text.Height / 2));
        }
    }

    /// <summary>Where the hand should point for the current field and time.</summary>
    private double TargetAngle()
    {
        return Field == TimePickerField.Hour
            ? TopAngle + Time.Hours % 12 * 30
            : TopAngle + Time.Minutes * 6;
    }

    private void SetFromPoint(Point point)
    {
        var centre = DialArea.Center;
        var degrees = Math.Atan2(point.Y - centre.Y, point.X - centre.X) * 180 / Math.PI;
        var fromTop = (degrees - TopAngle + 360) % 360;

        if (Field == TimePickerField.Hour)
        {
            var hour = (int)Math.Round(fromTop / 30) % 12;
            Time = new TimeSpan(Time.Hours / 12 * 12 + hour, Time.Minutes, 0);
        }
        else
        {
            var minute = (int)Math.Round(fromTop / 6) % 60;
            Time = new TimeSpan(Time.Hours, minute, 0);
        }
    }

    /// <summary>The signed difference between two angles, in [-180, 180).</summary>
    private static double NormalisedDelta(double a, double b)
    {
        return (a - b + 540) % 360 - 180;
    }

    private static Point PointOn(Point centre, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180;
        return new Point(centre.X + Math.Cos(radians) * radius, centre.Y + Math.Sin(radians) * radius);
    }

    private FormattedText Text(string value, double size, IImmutableBrush brush)
    {
        var typography = _theme.Theme.Typography;

        return new FormattedText(
            value,
            CultureInfo.CurrentCulture,
            FlowDirection,
            new Typeface(typography.GetFontFamily(_tokens.DayTextStyle)),
            size,
            brush);
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.GetPickerTokens(PickerVariant.Time);
        _colors = _theme.Theme.GetPickerColors(PickerVariant.Time, _theme.IsDark);

        InvalidateMeasure();
        InvalidateVisual();
    }
}
