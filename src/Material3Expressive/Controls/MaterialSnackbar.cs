// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Snackbar.kt:101-177,
// 291-462 and tokens/SnackbarDefaultsTokens.kt. The Avalonia template adapts the one-row slot layout;
// generated host actions retain the upstream TextButton size, padding, and LabelLarge typography.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
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
public class MaterialSnackbar : TemplatedControl, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private MaterialMotionController Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

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

    internal static readonly StyledProperty<SnackbarGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialSnackbar, SnackbarGeometryProfile>(
            nameof(GeometryProfile),
            new SnackbarGeometryProfile(
                0,
                ShapeTokens.CornerExtraSmall,
                48,
                68,
                344,
                600,
                new Thickness(16, 0, 8, 0),
                24,
                TypographyRole.BodyMedium,
                TypographyRole.LabelLarge));

    internal static readonly StyledProperty<SnackbarVisualProfile> VisualProfileProperty =
        AvaloniaProperty.Register<MaterialSnackbar, SnackbarVisualProfile>(
            nameof(VisualProfile),
            new SnackbarVisualProfile(
                TransparentBrush(),
                TransparentBrush(),
                TransparentBrush(),
                TransparentBrush(),
                Colors.Black));

    internal static readonly StyledProperty<SpringSpec> MotionSpecProperty =
        AvaloniaProperty.Register<MaterialSnackbar, SpringSpec>(
            nameof(MotionSpec),
            MotionTokens.Standard.DefaultSpatial);

    private SpringAnimation? _open;
    private TimeSpan _shownFor;
    public MaterialSnackbar()
    {
        _material = MaterialElementContext.Ensure(this);
        IsHitTestVisible = IsOpen;
    }

    static MaterialSnackbar()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialSnackbar>(false);
        AffectsMeasure<MaterialSnackbar>(GeometryProfileProperty);
        AffectsRender<MaterialSnackbar>(
            IsOpenProperty,
            GeometryProfileProperty,
            VisualProfileProperty);
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
    public IImmutableBrush MessageBrush => VisualProfile.SupportingTextColor;

    /// <summary>The action brush.</summary>
    public IImmutableBrush ActionBrush => VisualProfile.ActionLabelTextColor;

    public static readonly DirectProperty<MaterialSnackbar, IImmutableBrush> MessageBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSnackbar, IImmutableBrush>(nameof(MessageBrush), o => o.MessageBrush);

    public static readonly DirectProperty<MaterialSnackbar, IImmutableBrush> ActionBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialSnackbar, IImmutableBrush>(nameof(ActionBrush), o => o.ActionBrush);

    /// <summary>How far open the snackbar is, 0 hidden and 1 shown.</summary>
    protected double OpenFraction => _open?.Value ?? (IsOpen ? 1 : 0);

    internal ComposeShadowShape ParityShadowShape =>
        ComposeShadowShape.RoundedRectangle(new Rect(Bounds.Size), ContainerCornerRadius);

    internal ComposeShadowParameters ParityShadowParameters =>
        ComposeShadowParameters.Material(GeometryProfile.ContainerElevation, ShadowColor);

    internal bool ParityShadowOccluderDrawn => true;

    private SpringAnimation Open =>
        _open ??= Motion.AddSpring(IsOpen ? 1 : 0, MotionSpec);

    internal void SnapOpenState(bool isOpen)
    {
        IsOpen = isOpen;
        Open.SnapTo(isOpen ? 1 : 0);
        _shownFor = TimeSpan.Zero;
        IsHitTestVisible = isOpen;
        InvalidateVisual();
    }

    private CornerRadius ContainerCornerRadius =>
        GeometryProfile.ContainerShape.ToCornerRadius(
            Bounds.Size,
            FlowDirection == FlowDirection.RightToLeft);

    private Color ShadowColor => VisualProfile.ShadowColor;

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
                VisualProfile.ContainerColor,
                null,
                0,
                GeometryProfile.ContainerElevation,
                ShadowColor);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);

        return new Size(
            Math.Clamp(size.Width, GeometryProfile.MinWidth, GeometryProfile.MaxWidth),
            Math.Max(size.Height, GeometryProfile.SingleLineHeight));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            _shownFor = TimeSpan.Zero;
            Open.AnimateTo(IsOpen ? 1 : 0, MotionSpec);
            Motion.RequestFrame();
            IsHitTestVisible = IsOpen;
        }
        else if (change.Property == VisualProfileProperty)
        {
            var oldProfile = change.GetOldValue<SnackbarVisualProfile>();
            RaisePropertyChanged(
                MessageBrushProperty,
                oldProfile.SupportingTextColor,
                MessageBrush);
            RaisePropertyChanged(
                ActionBrushProperty,
                oldProfile.ActionLabelTextColor,
                ActionBrush);
        }
        else if (change.Property == MotionSpecProperty && _open is not null)
        {
            _open.AnimateTo(IsOpen ? 1 : 0, MotionSpec);
            Motion.RequestFrame();
        }
    }

    private void OnMotionTick()
    {
        OnFrame();
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

    internal SnackbarGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    internal SnackbarVisualProfile VisualProfile
    {
        get => GetValue(VisualProfileProperty);
        set => SetValue(VisualProfileProperty, value);
    }

    internal SpringSpec MotionSpec
    {
        get => GetValue(MotionSpecProperty);
        set => SetValue(MotionSpecProperty, value);
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}
