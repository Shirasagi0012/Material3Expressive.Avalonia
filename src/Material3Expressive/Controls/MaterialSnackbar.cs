using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialSkiaShadow;

namespace Material3Expressive.Controls;

/// <summary>
/// A snackbar: a brief message on an inverted surface.
/// </summary>
/// <remarks>
/// The inversion is the point. A snackbar reports something that has already happened rather than
/// asking for anything, so it has to read as separate from the page without stealing focus — which
/// is why it takes the inverse surface and its action takes the inverse primary.
/// <para>
/// It slides up as it fades in, and <see cref="Duration" /> dismisses it on its own. The timer is
/// a looping animation on the same frame loop as the slide rather than a dispatcher timer, so a
/// snackbar on a detached control stops counting down along with everything else.
/// </para>
/// </remarks>
public class MaterialSnackbar : TemplatedControl
{
    /// <inheritdoc cref="IsOpen" />
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MaterialSnackbar, bool>(
            nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <inheritdoc cref="Message" />
    public static readonly StyledProperty<object?> MessageProperty =
        AvaloniaProperty.Register<MaterialSnackbar, object?>(nameof(Message));

    /// <inheritdoc cref="Action" />
    public static readonly StyledProperty<object?> ActionProperty =
        AvaloniaProperty.Register<MaterialSnackbar, object?>(nameof(Action));

    /// <inheritdoc cref="Duration" />
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<MaterialSnackbar, TimeSpan>(nameof(Duration), TimeSpan.FromSeconds(4));

    private readonly MaterialThemeWatcher _theme;
    private readonly MotionGroup _motion;

    private SpringAnimation? _open;
    private TimeSpan _shownFor;
    private SnackbarTokens _tokens = SnackbarTokens.Default;
    private SnackbarColors _colors = SnackbarTokens.Default.Resolve(null, false);

    public MaterialSnackbar()
    {
        _theme = new MaterialThemeWatcher(this);
        _motion = new MotionGroup(this);
        _motion.Ticked += (_, _) => OnFrame();

        _theme.Changed += (_, _) => ApplyTokens();
        ApplyTokens();
    }

    static MaterialSnackbar()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialSnackbar>(false);
        AffectsRender<MaterialSnackbar>(IsOpenProperty);
    }

    /// <summary>Whether the snackbar is showing.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>What happened.</summary>
    public object? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>One thing the user can do about it — usually undo.</summary>
    public object? Action
    {
        get => GetValue(ActionProperty);
        set => SetValue(ActionProperty, value);
    }

    /// <summary>How long it stays. <see cref="TimeSpan.Zero" /> keeps it until dismissed.</summary>
    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>The message brush, exposed for the template.</summary>
    public IImmutableBrush MessageBrush => _colors.SupportingTextColor;

    /// <summary>The action brush.</summary>
    public IImmutableBrush ActionBrush => _colors.ActionLabelTextColor;

    public static readonly DirectProperty<MaterialSnackbar, IImmutableBrush> MessageBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSnackbar, IImmutableBrush>(nameof(MessageBrush), o => o.MessageBrush);

    public static readonly DirectProperty<MaterialSnackbar, IImmutableBrush> ActionBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSnackbar, IImmutableBrush>(nameof(ActionBrush), o => o.ActionBrush);

    /// <summary>How far open the snackbar is, 0 hidden and 1 shown.</summary>
    protected double OpenFraction => _open?.Value ?? (IsOpen ? 1 : 0);

    internal ComposeShadowShape ParityShadowShape =>
        ComposeShadowShape.RoundedRectangle(new Rect(Bounds.Size), ContainerCornerRadius);

    internal ComposeShadowParameters ParityShadowParameters =>
        ComposeShadowParameters.Material(_tokens.ContainerElevation, ShadowColor);

    internal bool ParityShadowOccluderDrawn => true;

    private SpringAnimation Open =>
        _open ??= _motion.AddSpring(IsOpen ? 1 : 0, _theme.Spring(MotionRole.DefaultSpatial));

    private CornerRadius ContainerCornerRadius =>
        _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, Bounds.Size);

    private Color ShadowColor =>
        _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;

    public override void Render(DrawingContext context)
    {
        var fraction = OpenFraction;
        if (fraction <= 0.001)
            return;

        // Rises as it fades in: eight pixels is enough to read as arriving from below.
        using (context.PushTransform(Matrix.CreateTranslation(0, (1 - fraction) * 8)))
        using (context.PushOpacity(fraction))
        {
            MaterialContainer.Draw(
                context,
                this,
                Bounds.Size,
                ContainerCornerRadius,
                _colors.ContainerColor,
                null,
                0,
                _tokens.ContainerElevation,
                ShadowColor);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);

        return new Size(
            Math.Clamp(size.Width, _tokens.MinWidth, _tokens.MaxWidth),
            Math.Max(size.Height, _tokens.SingleLineHeight));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            _shownFor = TimeSpan.Zero;
            Open.AnimateTo(IsOpen ? 1 : 0, _theme.Spring(MotionRole.DefaultSpatial));
            _motion.Start();
            IsHitTestVisible = IsOpen;
        }
    }

    /// <summary>
    /// Counts down the display time on the frame loop.
    /// </summary>
    /// <remarks>
    /// A dispatcher timer would keep running after the control left the tree; this stops when
    /// everything else does, which is the behaviour a detached snackbar should have.
    /// </remarks>
    private void OnFrame()
    {
        InvalidateVisual();

        if (!IsOpen || Duration <= TimeSpan.Zero)
            return;

        _shownFor += TimeSpan.FromSeconds(1.0 / 60);

        if (_shownFor >= Duration)
            IsOpen = false;
    }

    private void ApplyTokens()
    {
        var previousMessage = MessageBrush;
        var previousAction = ActionBrush;

        _tokens = _theme.Theme.SnackbarTokens;
        _colors = _theme.Theme.GetSnackbarColors(_theme.IsDark);

        RaisePropertyChanged(MessageBrushProperty, previousMessage, MessageBrush);
        RaisePropertyChanged(ActionBrushProperty, previousAction, ActionBrush);

        var typography = _theme.Theme.Typography;

        Padding = new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace, 0);
        MinHeight = _tokens.SingleLineHeight;
        FontSize = typography.GetTypeScale(_tokens.SupportingTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.SupportingTextStyle);
        IsHitTestVisible = IsOpen;

        InvalidateMeasure();
        InvalidateVisual();
    }
}
