using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 checkbox.
/// </summary>
/// <remarks>
/// Derives from Avalonia's <see cref="CheckBox" />, so the three-state behaviour, the content slot
/// and keyboard activation are the stock ones. What is drawn is entirely Material's: an outlined
/// box that fills when checked, inside a 40dp state-layer circle that is deliberately larger than
/// the 18dp box — the touch target and the feedback area are the same thing, and both are bigger
/// than the mark.
/// </remarks>
public class MaterialCheckbox : CheckBox, IMaterialThemeConsumer, IMaterialInteractionConsumer
{
    private const double SnapAnimationDelayMilliseconds = 100;
    // androidx.compose.foundation.layout.minimumInteractiveComponentSize() supplies the standard
    // 48dp touch target around Checkbox's 20dp canvas. The 40dp state layer remains centred inside
    // this larger layout slot.
    private const double MinimumInteractiveComponentSize = 48;

    /// <summary>Puts the checkbox into its error appearance.</summary>
    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<MaterialCheckbox, bool>(nameof(IsError));

    private static readonly CheckboxGeometryProfile DefaultGeometryProfile = new(
        ContainerSize: 20,
        ContainerCornerRadius: 2,
        StateLayerSize: 40,
        OutlineWidth: 2);

    private static readonly CheckboxVisualColors DefaultVisualColors = new(
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush(),
        TransparentBrush());

    /// <summary>
    /// The renderer geometry profile supplied by the Material control theme.
    /// </summary>
    /// <remarks>This is an implementation property, not a public authoring surface.</remarks>
    internal static readonly StyledProperty<CheckboxGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialCheckbox, CheckboxGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    /// <summary>
    /// The renderer brush profile supplied by the Material control theme.
    /// </summary>
    /// <remarks>This is an implementation property, not a public authoring surface.</remarks>
    internal static readonly StyledProperty<CheckboxVisualColors> VisualColorsProperty =
        AvaloniaProperty.Register<MaterialCheckbox, CheckboxVisualColors>(
            nameof(VisualColors),
            DefaultVisualColors);

    private readonly MaterialThemeController _theme;
    private readonly MaterialInteractionController _interaction;
    private MaterialInteractionPresentationLease? _presentation;

    private MaterialInteractionPresentationLease InteractionPresentation =>
        _presentation is { IsDisposed: false } existing
            ? existing
            : _presentation = _interaction.AttachPresentation(this);

    private SpringAnimation? _checkDrawFraction;
    private SpringAnimation? _checkCenterGravitation;
    private SpringAnimation? _checkOpacity;
    private AnimatedBrush? _boxBrush;
    private AnimatedBrush? _borderBrush;
    private IBrush _checkColor = Brushes.Transparent;
    private DelayedSnapAnimation? _delayedCheckClear;
    private bool? _visualState;
    private bool _hasVisualState;

    public MaterialCheckbox()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _interaction = MaterialElementContext.Ensure(this).Interaction;
        _interaction.IsRippleBounded = false;

        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) =>
        ApplyTokens(snap: !this.IsAttachedToVisualTree());

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        InvalidateVisual();

    static MaterialCheckbox()
    {
        AffectsRender<MaterialCheckbox>(
            IsErrorProperty,
            IsCheckedProperty,
            GeometryProfileProperty,
            VisualColorsProperty);
        AffectsMeasure<MaterialCheckbox>(GeometryProfileProperty);
    }

    /// <inheritdoc cref="IsErrorProperty" />
    public bool IsError
    {
        get => GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    /// <summary>Gets or sets the hidden renderer geometry profile.</summary>
    internal CheckboxGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    /// <summary>Gets or sets the hidden renderer brush profile.</summary>
    internal CheckboxVisualColors VisualColors
    {
        get => GetValue(VisualColorsProperty);
        set => SetValue(VisualColorsProperty, value);
    }

    /// <summary>The 40dp state layer centred inside the leading 48dp interactive layout slot.</summary>
    protected Rect GlyphArea
    {
        get
        {
            var size = GeometryProfile.StateLayerSize;
            return new Rect(
                (MinimumInteractiveComponentSize - size) / 2,
                (Bounds.Height - size) / 2,
                size,
                size);
        }
    }

    public override void Render(DrawingContext context)
    {
        var area = GlyphArea;
        InteractionPresentation.UpdateGeometry(
            new MaterialInteractionGeometry(area, null, null));

        EnsureVisualState(snap: true);

        var boxSize = GeometryProfile.ContainerSize;
        var box = new Rect(
            area.X + (area.Width - boxSize) / 2,
            area.Y + (area.Height - boxSize) / 2,
            boxSize,
            boxSize);
        var corners = new CornerRadius(GeometryProfile.ContainerCornerRadius);

        DrawBox(context, box, corners, _boxBrush!.Brush, _borderBrush!.Brush);
        DrawCheck(
            context,
            box,
            CheckBrush,
            _checkDrawFraction!.Value,
            _checkCenterGravitation!.Value);

        base.Render(context);
        _interaction.DrawLayers(
            context,
            IsChecked == true ? VisualColors.SelectedStateLayerColor : VisualColors.UnselectedStateLayerColor,
            new CornerRadius(GeometryProfile.StateLayerSize / 2));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = base.MeasureOverride(availableSize);
        return new Size(
            System.Math.Max(content.Width, MinimumInteractiveComponentSize),
            System.Math.Max(content.Height, MinimumInteractiveComponentSize));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GeometryProfileProperty || change.Property == VisualColorsProperty)
            ApplyTokens(snap: !this.IsAttachedToVisualTree());
        else if (change.Property == IsEffectivelyEnabledProperty || change.Property == IsErrorProperty)
            ApplyTokens(snap: !this.IsAttachedToVisualTree());
        else if (change.Property == IsCheckedProperty)
            UpdateVisualState(
                snap: !this.IsAttachedToVisualTree(),
                previousState: change.GetOldValue<bool?>(),
                hasPreviousState: true);
    }

    private void ApplyTokens(bool snap = true)
    {
        _interaction.RippleRadius = GeometryProfile.StateLayerSize / 2;
        UpdateVisualState(snap: snap || !_hasVisualState);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void EnsureVisualState(bool snap)
    {
        if (_checkDrawFraction is not null)
            return;

        var targets = VisualTargets(IsChecked);
        _checkDrawFraction = _interaction.Motion.AddSpring(targets.DrawFraction, _theme.Current.Theme.Motion.Get(MotionRole.DefaultSpatial));
        _checkCenterGravitation = _interaction.Motion.AddSpring(targets.Gravitation, _theme.Current.Theme.Motion.Get(MotionRole.DefaultSpatial));
        _checkOpacity = _interaction.Motion.AddSpring(targets.DrawFraction, _theme.Current.Theme.Motion.Get(MotionRole.DefaultEffects));
        _boxBrush = _interaction.Motion.Add(new AnimatedBrush(targets.Box));
        _borderBrush = _interaction.Motion.Add(new AnimatedBrush(targets.Border));
        _delayedCheckClear = _interaction.Motion.Add(new DelayedSnapAnimation());

        if (snap)
            SnapVisualState(targets);
    }

    private void UpdateVisualState(bool snap) => UpdateVisualState(snap, _visualState, _hasVisualState);

    private void UpdateVisualState(bool snap, bool? previousState, bool hasPreviousState)
    {
        // Checkbox.kt selects the colour spring from the *initial* ToggleableState: leaving
        // Off uses FastEffects, while every other origin uses DefaultEffects. The target alone
        // cannot encode that distinction (notably Indeterminate -> Off fades more slowly).
        var startedOff = hasPreviousState && previousState is false;
        EnsureVisualState(snap);
        var targets = VisualTargets(IsChecked);
        if (snap)
        {
            SnapVisualState(targets);
            _visualState = IsChecked;
            _hasVisualState = true;
            return;
        }

        // Checkbox.kt uses the standard Material3 DefaultSpatial token, independently of this
        // library's expressive component defaults.
        var spatial = MotionTokens.Standard.DefaultSpatial;
        var effects = _theme.Current.Theme.Motion.Get(startedOff ? MotionRole.FastEffects : MotionRole.DefaultEffects);
        _boxBrush!.AnimateTo(targets.Box, effects);
        _borderBrush!.AnimateTo(targets.Border, effects);
        if (IsChecked is false)
        {
            _checkColor = VisualTargets(true).Check;
            _checkOpacity!.SnapTo(1);
        }
        if (IsChecked is not false)
            _checkColor = targets.Check;
        _checkOpacity!.AnimateTo(targets.DrawFraction, effects);

        if (IsChecked is false)
        {
            _delayedCheckClear!.Schedule(
                TimeSpan.FromMilliseconds(SnapAnimationDelayMilliseconds),
                () =>
                {
                    _checkDrawFraction!.SnapTo(0);
                    _checkCenterGravitation!.SnapTo(0);
                });
        }
        else
        {
            _delayedCheckClear!.Cancel();
            _checkDrawFraction!.AnimateTo(targets.DrawFraction, spatial);
            _checkCenterGravitation!.AnimateTo(targets.Gravitation, spatial);
        }

        _interaction.Motion.RequestFrame();
        _visualState = IsChecked;
        _hasVisualState = true;
        InvalidateVisual();
    }

    private void SnapVisualState(CheckboxVisualTargets targets)
    {
        _delayedCheckClear?.Cancel();
        _checkDrawFraction?.SnapTo(targets.DrawFraction);
        _checkCenterGravitation?.SnapTo(targets.Gravitation);
        _checkOpacity?.SnapTo(targets.DrawFraction);
        _boxBrush?.SnapTo(targets.Box);
        _borderBrush?.SnapTo(targets.Border);
        _checkColor = targets.Check;
    }

    private CheckboxVisualTargets VisualTargets(bool? state)
    {
        var enabled = IsEffectivelyEnabled;
        var selected = state is not false;
        var box = !enabled ? selected ? VisualColors.DisabledSelectedContainerColor : Brushes.Transparent
            : IsError ? selected ? VisualColors.ErrorContainerColor : Brushes.Transparent
            : selected ? VisualColors.SelectedContainerColor : Brushes.Transparent;
        var border = !enabled ? selected ? VisualColors.DisabledSelectedContainerColor : VisualColors.DisabledOutlineColor
            : IsError ? selected ? VisualColors.ErrorContainerColor : VisualColors.ErrorOutlineColor
            : selected ? VisualColors.SelectedContainerColor : VisualColors.UnselectedOutlineColor;
        var check = !enabled ? selected ? VisualColors.DisabledSelectedIconColor : Brushes.Transparent
            : IsError ? selected ? VisualColors.ErrorIconColor : Brushes.Transparent
            : selected ? VisualColors.SelectedIconColor : Brushes.Transparent;
        return new CheckboxVisualTargets(
            box,
            border,
            check,
            selected ? 1 : 0,
            state is null ? 1 : 0);
    }

    private void DrawBox(
        DrawingContext context,
        Rect box,
        CornerRadius corners,
        IBrush fill,
        IBrush border)
    {
        var fillColor = EffectiveColor(fill);
        var borderColor = EffectiveColor(border);
        if (fillColor == borderColor)
        {
            context.DrawRectangle(fill, null, new RoundedRect(box, corners));
            return;
        }

        if (fillColor.A > 0)
        {
            context.DrawRectangle(
                fill,
                null,
                new RoundedRect(
                    box.Deflate(GeometryProfile.OutlineWidth),
                    MaterialContainer.Deflate(corners, GeometryProfile.OutlineWidth)));
        }

        if (borderColor.A == 0)
            return;

        var inset = GeometryProfile.OutlineWidth / 2;
        context.DrawRectangle(
            null,
            new Pen(border, GeometryProfile.OutlineWidth),
            new RoundedRect(box.Deflate(inset), MaterialContainer.Deflate(corners, inset)));
    }

    private static void DrawCheck(
        DrawingContext context,
        Rect box,
        IBrush brush,
        double fraction,
        double gravitation)
    {
        if (fraction <= 0 || EffectiveColor(brush).A == 0)
            return;

        var start = new Point(box.X + box.Width * .20, box.Y + box.Height * .50);
        var corner = new Point(
            box.X + box.Width * Lerp(.40, .50, gravitation),
            box.Y + box.Height * Lerp(.70, .50, gravitation));
        var end = new Point(box.X + box.Width * .80, box.Y + box.Height * Lerp(.30, .50, gravitation));
        var firstLength = Distance(start, corner);
        var secondLength = Distance(corner, end);
        var length = (firstLength + secondLength) * Math.Clamp(fraction, 0, 1);
        var geometry = new StreamGeometry();
        using (var sink = geometry.Open())
        {
            sink.BeginFigure(start, false);
            if (length <= firstLength)
                sink.LineTo(Interpolate(start, corner, length / firstLength));
            else
            {
                sink.LineTo(corner);
                sink.LineTo(Interpolate(corner, end, (length - firstLength) / secondLength));
            }
            sink.EndFigure(false);
        }
        context.DrawGeometry(
            null,
            new Pen(brush, 2, lineCap: PenLineCap.Square, lineJoin: PenLineJoin.Miter),
            geometry);
    }

    private static Color EffectiveColor(IBrush brush) => brush is ISolidColorBrush solid
        ? Color.FromArgb(
            (byte)Math.Round(solid.Color.A * Math.Clamp(solid.Opacity, 0, 1), MidpointRounding.AwayFromZero),
            solid.Color.R,
            solid.Color.G,
            solid.Color.B)
        : Colors.Transparent;

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);

    private IBrush CheckBrush
    {
        get
        {
            var color = EffectiveColor(_checkColor);
            if (color.A == 0 || _checkOpacity is not { Value: > 0 } opacity)
                return Brushes.Transparent;

            return new SolidColorBrush(color, Math.Clamp(opacity.Value, 0, 1));
        }
    }

    private static Point Interpolate(Point from, Point to, double fraction) => new(
        Lerp(from.X, to.X, fraction),
        Lerp(from.Y, to.Y, fraction));

    private static double Distance(Point from, Point to) => Math.Sqrt(
        Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));

    private static double Lerp(double from, double to, double fraction) => from + (to - from) * fraction;

    private sealed record CheckboxVisualTargets(
        IBrush Box,
        IBrush Border,
        IBrush Check,
        double DrawFraction,
        double Gravitation);

    private sealed class DelayedSnapAnimation : IAnimation
    {
        private TimeSpan _delay;
        private TimeSpan _elapsed;
        private Action? _completion;

        public double Value => _elapsed.TotalMilliseconds;

        public bool IsRunning => _completion is not null;

        public void Schedule(TimeSpan delay, Action completion)
        {
            _delay = delay;
            _elapsed = TimeSpan.Zero;
            _completion = completion;
        }

        public void Cancel() => _completion = null;

        public bool Tick(TimeSpan delta)
        {
            if (_completion is null)
                return false;

            _elapsed += delta;
            if (_elapsed < _delay)
                return true;

            Complete();
            return false;
        }

        public void Settle() => Complete();

        private void Complete()
        {
            var completion = _completion;
            _completion = null;
            completion?.Invoke();
        }
    }
}
