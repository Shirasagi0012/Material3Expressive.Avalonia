using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Whether a bottom sheet is modal or embedded in its surrounding surface.</summary>
public enum BottomSheetVariant
{
    Modal,
    Standard
}

/// <summary>
/// A bottom sheet: a surface that rises from the bottom edge.
/// </summary>
/// <remarks>
/// Like the drawer, this lives in the tree rather than in a popup, so it shares a layout with the
/// content it covers and stays inside the theme scope it was declared in.
/// <para>
/// It can be dragged: the sheet follows the pointer while held and, on release, either settles
/// open or continues closed depending on how far it has come — the spring picks up whatever
/// velocity the drag had, so a flick keeps going instead of stopping dead at the release point.
/// </para>
/// </remarks>
public class MaterialBottomSheet : ContentControl, IMaterialMotionConsumer
{
    public static readonly StyledProperty<BottomSheetVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialBottomSheet, BottomSheetVariant>(nameof(Variant));

    /// <inheritdoc cref="IsOpen" />
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MaterialBottomSheet, bool>(
            nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    internal static readonly StyledProperty<BottomSheetGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialBottomSheet, BottomSheetGeometryProfile>(
            nameof(GeometryProfile),
            new BottomSheetGeometryProfile(
                0,
                ShapeRole.CornerExtraLargeTop,
                32,
                4,
                22,
                new Thickness(0, 22, 0, 0),
                640,
                0.32,
                true));

    internal static readonly StyledProperty<BottomSheetVisualProfile> VisualProfileProperty =
        AvaloniaProperty.Register<MaterialBottomSheet, BottomSheetVisualProfile>(
            nameof(VisualProfile),
            new BottomSheetVisualProfile(
                TransparentBrush(),
                TransparentBrush(),
                TransparentBrush(),
                Colors.Black));

    /// <summary>Past this fraction of the sheet's height, a release settles it closed.</summary>
    private const double DismissThreshold = 0.4;

    private readonly MaterialThemeController _theme;
    private readonly MaterialMotionController _motion;

    private SpringAnimation? _open;
    private Point? _dragStart;
    private double _dragStartFraction;

    public MaterialBottomSheet()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _motion = MaterialElementContext.Ensure(this).Motion;
        IsHitTestVisible = IsOpen;
    }

    void IMaterialMotionConsumer.OnMaterialMotionFrame()
    {
        InvalidateArrange();
        InvalidateVisual();
    }

    static MaterialBottomSheet()
    {
        AffectsMeasure<MaterialBottomSheet>(GeometryProfileProperty);
        AffectsRender<MaterialBottomSheet>(
            VariantProperty,
            IsOpenProperty,
            GeometryProfileProperty,
            VisualProfileProperty);
    }

    public BottomSheetVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Whether the sheet is up.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>How far up the sheet is, 0 down and 1 up.</summary>
    protected double OpenFraction => _open?.Value ?? (IsOpen ? 1 : 0);

    private SpringAnimation Open =>
        _open ??= _motion.AddSpring(IsOpen ? 1 : 0, _theme.Current.Theme.Motion.Get(MotionRole.DefaultSpatial));

    private Rect SheetArea => GetSheetArea(Bounds.Size);

    private Rect GetSheetArea(Size available)
    {
        var width = Math.Min(available.Width, GeometryProfile.MaxWidth);
        var height = Presenter?.DesiredSize.Height ?? 0;
        var hidden = height * (1 - OpenFraction);

        return new Rect(
            (available.Width - width) / 2,
            available.Height - height + hidden,
            width,
            height);
    }

    public override void Render(DrawingContext context)
    {
        var fraction = OpenFraction;
        if (fraction <= 0.001)
            return;

        if (GeometryProfile.IsModal && GeometryProfile.ScrimOpacity > 0)
        {
            using (context.PushOpacity(GeometryProfile.ScrimOpacity * fraction))
                context.FillRectangle(VisualProfile.ScrimColor, new Rect(Bounds.Size));
        }

        var sheet = SheetArea;
        if (sheet.Height <= 0)
            return;

        var corners = _theme.Current.Theme.Shapes.GetCornerRadius(
            GeometryProfile.ContainerShape,
            sheet.Size);

        using (context.PushTransform(Matrix.CreateTranslation(sheet.X, sheet.Y)))
        {
            MaterialContainer.Draw(
                context,
                sheet.Size,
                corners,
                VisualProfile.ContainerColor);

            // The drag handle sits in the space the sheet reserves above its content.
            var handle = new Rect(
                (sheet.Width - GeometryProfile.DragHandleWidth) / 2,
                (GeometryProfile.DragHandleSpace - GeometryProfile.DragHandleHeight) / 2,
                GeometryProfile.DragHandleWidth,
                GeometryProfile.DragHandleHeight);

            context.DrawRectangle(
                VisualProfile.DragHandleColor,
                null,
                new RoundedRect(handle, new CornerRadius(GeometryProfile.DragHandleHeight / 2)));
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Presenter is { } presenter)
            presenter.Measure(
                new Size(Math.Min(availableSize.Width, GeometryProfile.MaxWidth), availableSize.Height));

        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Bounds still contains the previous layout slot while ArrangeOverride runs. Computing
        // from finalSize prevents the first arrangement from giving the presenter a zero width.
        var sheet = GetSheetArea(finalSize);

        if (Presenter is { } presenter)
            presenter.Arrange(sheet);

        return finalSize;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsOpen)
            return;

        var position = e.GetPosition(this);
        var sheet = SheetArea;

        if (!sheet.Contains(position))
        {
            // Outside the sheet is the scrim, and tapping it dismisses a modal sheet.
            if (GeometryProfile.IsModal)
            {
                IsOpen = false;
                e.Handled = true;
            }

            return;
        }

        // Dragging starts anywhere in the handle strip along the top.
        if (position.Y - sheet.Y > GeometryProfile.DragHandleSpace)
            return;

        _dragStart = position;
        _dragStartFraction = OpenFraction;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_dragStart is not { } start || SheetArea.Height <= 0)
            return;

        var delta = e.GetPosition(this).Y - start.Y;
        var fraction = Math.Clamp(_dragStartFraction - delta / SheetArea.Height, 0, 1);

        Open.SnapTo(fraction);
        InvalidateArrange();
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndDrag();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndDrag();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            Open.AnimateTo(IsOpen ? 1 : 0, _theme.Current.Theme.Motion.Get(MotionRole.DefaultSpatial));
            _motion.RequestFrame();
            IsHitTestVisible = IsOpen;
        }
    }

    /// <summary>
    /// Settles the sheet after a drag: far enough down and it goes, otherwise it springs back.
    /// </summary>
    private void EndDrag()
    {
        if (_dragStart is null)
            return;

        _dragStart = null;

        var shouldClose = OpenFraction < 1 - DismissThreshold;

        if (shouldClose && IsOpen)
        {
            IsOpen = false;
        }
        else
        {
            Open.AnimateTo(IsOpen ? 1 : 0, _theme.Current.Theme.Motion.Get(MotionRole.DefaultSpatial));
            _motion.RequestFrame();
        }
    }

    internal BottomSheetGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    internal BottomSheetVisualProfile VisualProfile
    {
        get => GetValue(VisualProfileProperty);
        set => SetValue(VisualProfileProperty, value);
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}
