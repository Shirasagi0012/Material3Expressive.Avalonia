using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Whether picker metrics are for the date or time picker.</summary>
public enum PickerVariant { Date, Time }

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
public class MaterialTimePicker : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.Theme.Current.IsDark ?? false;
    private MaterialMotionController Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    private void OnMotionTick() => InvalidateVisual();

    /// <inheritdoc cref="State" />
    public static readonly DirectProperty<MaterialTimePicker, MaterialTimePickerState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialTimePicker, MaterialTimePickerState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    /// <inheritdoc cref="Time" />
    public static readonly StyledProperty<TimeSpan> TimeProperty =
        AvaloniaProperty.Register<MaterialTimePicker, TimeSpan>(
            nameof(Time),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay,
            validate: static value => value >= TimeSpan.Zero && value < TimeSpan.FromDays(1));

    /// <inheritdoc cref="Field" />
    public static readonly StyledProperty<TimePickerField> FieldProperty =
        AvaloniaProperty.Register<MaterialTimePicker, TimePickerField>(nameof(Field));

    /// <summary>Twelve o'clock is up, and angles are measured from there.</summary>
    private const double TopAngle = -90;

    private MaterialTimePickerState _state = new();
    private bool _updatingFromState;

    private SpringAnimation? _hand;
    private PickerMetrics _tokens = PickerDefaults.Metrics(PickerVariant.Time);
    private PickerVisualColors _colors = PickerDefaults.Colors(ResolvedTheme.Default, PickerVariant.Time, false);
    private bool _dragging;

    public MaterialTimePicker()
    {
        _material = MaterialElementContext.Ensure(this);
        _state.PropertyChanged += OnStateChanged;
        Focusable = true;
        ApplyTokens();
    }

    /// <summary>
    /// The non-null state shared with a <see cref="MaterialTimeInput"/> when both edit one value.
    /// </summary>
    public MaterialTimePickerState State
    {
        get => _state;
        set => SetState(value);
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

    /// <summary>Replaces external state with a fresh, control-owned default.</summary>
    public void ResetState() => SetState(null);

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
        ResolvedTheme.Typography.GetTypeScale(_tokens.HeadlineTextStyle).LineHeight + 32;

    private SpringAnimation Hand =>
        _hand ??= Motion.AddSpring(TargetAngle(), Spring(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var corners = ResolvedTheme.Shapes.GetCornerRadius(_tokens.ContainerShape, Bounds.Size);
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

        if (!_updatingFromState)
        {
            if (change.Property == TimeProperty)
                _state.Time = Time;
            else if (change.Property == FieldProperty)
                _state.Field = Field;
        }

        if (change.Property == TimeProperty || change.Property == FieldProperty)
        {
            Hand.AnimateTo(TargetAngle(), Spring(MotionRole.FastSpatial));
            Motion.RequestFrame();
        }
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        _updatingFromState = true;
        try
        {
            if (e.PropertyName == nameof(MaterialTimePickerState.Time))
                SetCurrentValue(TimeProperty, _state.Time);
            if (e.PropertyName == nameof(MaterialTimePickerState.Field))
                SetCurrentValue(FieldProperty, _state.Field);
        }
        finally
        {
            _updatingFromState = false;
        }
    }

    private void SetState(MaterialTimePickerState? value)
    {
        var next = value ?? new MaterialTimePickerState();
        if (ReferenceEquals(_state, next))
            return;

        _state.PropertyChanged -= OnStateChanged;
        SetAndRaise(StateProperty, ref _state, next);
        _state.PropertyChanged += OnStateChanged;

        _updatingFromState = true;
        try
        {
            SetCurrentValue(TimeProperty, _state.Time);
            SetCurrentValue(FieldProperty, _state.Field);
        }
        finally
        {
            _updatingFromState = false;
        }
    }

    private void DrawHeadline(DrawingContext context)
    {
        var size = ResolvedTheme.Typography.GetTypeScale(_tokens.HeadlineTextStyle).FontSize;
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

        var fontSize = ResolvedTheme.Typography.GetTypeScale(_tokens.DayTextStyle).FontSize;
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
        var typography = ResolvedTheme.Typography;

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
        _tokens = PickerDefaults.Metrics(PickerVariant.Time);
        _colors = PickerDefaults.Colors(ResolvedTheme, PickerVariant.Time, IsDarkVariant);

        InvalidateMeasure();
        InvalidateVisual();
    }
}
