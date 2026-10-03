using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// Paint is selected directly, without color animation, as in fixed SwitchImpl.
internal sealed class SwitchTrack : Decorator
{
    private readonly Outline _outline;

    public SwitchTrack()
    {
        // Compose border is an outer draw modifier: background, children/ripple, then outline.
        _outline = new Outline(this) { ZIndex = 1, IsHitTestVisible = false };
        VisualChildren.Add(_outline);
    }
    public static readonly StyledProperty<bool?> IsCheckedProperty = AvaloniaProperty.Register<SwitchTrack, bool?>(nameof(IsChecked), false);
    public bool? IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public static readonly StyledProperty<SwitchColors?> ColorsProperty = AvaloniaProperty.Register<SwitchTrack, SwitchColors?>(nameof(Colors));
    public SwitchColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<Color> CheckedThumbColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(CheckedThumbColor));
    public Color CheckedThumbColor { get => GetValue(CheckedThumbColorProperty); set => SetValue(CheckedThumbColorProperty, value); }
    public static readonly StyledProperty<Color> CheckedTrackColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(CheckedTrackColor));
    public Color CheckedTrackColor { get => GetValue(CheckedTrackColorProperty); set => SetValue(CheckedTrackColorProperty, value); }
    public static readonly StyledProperty<Color> CheckedBorderColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(CheckedBorderColor));
    public Color CheckedBorderColor { get => GetValue(CheckedBorderColorProperty); set => SetValue(CheckedBorderColorProperty, value); }
    public static readonly StyledProperty<Color> CheckedIconColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(CheckedIconColor));
    public Color CheckedIconColor { get => GetValue(CheckedIconColorProperty); set => SetValue(CheckedIconColorProperty, value); }
    public static readonly StyledProperty<Color> UncheckedThumbColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(UncheckedThumbColor));
    public Color UncheckedThumbColor { get => GetValue(UncheckedThumbColorProperty); set => SetValue(UncheckedThumbColorProperty, value); }
    public static readonly StyledProperty<Color> UncheckedTrackColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(UncheckedTrackColor));
    public Color UncheckedTrackColor { get => GetValue(UncheckedTrackColorProperty); set => SetValue(UncheckedTrackColorProperty, value); }
    public static readonly StyledProperty<Color> UncheckedBorderColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(UncheckedBorderColor));
    public Color UncheckedBorderColor { get => GetValue(UncheckedBorderColorProperty); set => SetValue(UncheckedBorderColorProperty, value); }
    public static readonly StyledProperty<Color> UncheckedIconColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(UncheckedIconColor));
    public Color UncheckedIconColor { get => GetValue(UncheckedIconColorProperty); set => SetValue(UncheckedIconColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledCheckedThumbColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(DisabledCheckedThumbColor));
    public Color DisabledCheckedThumbColor { get => GetValue(DisabledCheckedThumbColorProperty); set => SetValue(DisabledCheckedThumbColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledCheckedTrackColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(DisabledCheckedTrackColor));
    public Color DisabledCheckedTrackColor { get => GetValue(DisabledCheckedTrackColorProperty); set => SetValue(DisabledCheckedTrackColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledCheckedBorderColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(DisabledCheckedBorderColor));
    public Color DisabledCheckedBorderColor { get => GetValue(DisabledCheckedBorderColorProperty); set => SetValue(DisabledCheckedBorderColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledCheckedIconColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(DisabledCheckedIconColor));
    public Color DisabledCheckedIconColor { get => GetValue(DisabledCheckedIconColorProperty); set => SetValue(DisabledCheckedIconColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledUncheckedThumbColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(DisabledUncheckedThumbColor));
    public Color DisabledUncheckedThumbColor { get => GetValue(DisabledUncheckedThumbColorProperty); set => SetValue(DisabledUncheckedThumbColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledUncheckedTrackColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(DisabledUncheckedTrackColor));
    public Color DisabledUncheckedTrackColor { get => GetValue(DisabledUncheckedTrackColorProperty); set => SetValue(DisabledUncheckedTrackColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledUncheckedBorderColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(DisabledUncheckedBorderColor));
    public Color DisabledUncheckedBorderColor { get => GetValue(DisabledUncheckedBorderColorProperty); set => SetValue(DisabledUncheckedBorderColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledUncheckedIconColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(DisabledUncheckedIconColor));
    public Color DisabledUncheckedIconColor { get => GetValue(DisabledUncheckedIconColorProperty); set => SetValue(DisabledUncheckedIconColorProperty, value); }
    public static readonly StyledProperty<Color> SurfaceColorProperty = AvaloniaProperty.Register<SwitchTrack, Color>(nameof(SurfaceColor));
    public Color SurfaceColor { get => GetValue(SurfaceColorProperty); set => SetValue(SurfaceColorProperty, value); }

    internal Color PaintedThumb { get; private set; }
    internal Color PaintedTrack { get; private set; }
    internal Color PaintedBorder { get; private set; }
    internal Color PaintedIcon { get; private set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        UpdatePaint();
        Child?.Measure(new Size(20, 32));
        _outline.Measure(new Size(52, 32));
        return new Size(52, 32);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            // Native ToggleSwitch uses this panel's width as its drag range. The pressed
            // Material handle moves from x=2 to x=22, hence a 20 DIP native drag range.
            child.Arrange(new Rect(0, 0, 20, 32));
        }
        _outline.Arrange(new Rect(0, 0, 52, 32));
        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty || change.Property == IsEffectivelyEnabledProperty ||
            change.Property == ColorsProperty || change.Property.PropertyType == typeof(Color))
            UpdatePaint();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdatePaint();
    }

    private void UpdatePaint()
    {
        var palette = Colors ?? new SwitchColors(CheckedThumbColor, CheckedTrackColor, CheckedBorderColor, CheckedIconColor,
            UncheckedThumbColor, UncheckedTrackColor, UncheckedBorderColor, UncheckedIconColor,
            Composite(DisabledCheckedThumbColor), Composite(DisabledCheckedTrackColor),
            DisabledCheckedBorderColor, Composite(DisabledCheckedIconColor), Composite(DisabledUncheckedThumbColor),
            Composite(DisabledUncheckedTrackColor), Composite(DisabledUncheckedBorderColor), Composite(DisabledUncheckedIconColor));
        Color Composite(Color color) => ColorComposite.Over(color, 1, SurfaceColor);
        (PaintedThumb, PaintedTrack, PaintedBorder, PaintedIcon) = (IsEffectivelyEnabled, IsChecked == true) switch
        {
            (true, true) => (palette.CheckedThumbColor, palette.CheckedTrackColor, palette.CheckedBorderColor, palette.CheckedIconColor),
            (true, false) => (palette.UncheckedThumbColor, palette.UncheckedTrackColor, palette.UncheckedBorderColor, palette.UncheckedIconColor),
            (false, true) => (palette.DisabledCheckedThumbColor, palette.DisabledCheckedTrackColor, palette.DisabledCheckedBorderColor, palette.DisabledCheckedIconColor),
            (false, false) => (palette.DisabledUncheckedThumbColor, palette.DisabledUncheckedTrackColor, palette.DisabledUncheckedBorderColor, palette.DisabledUncheckedIconColor),
        };
        InvalidateVisual();
        _outline?.InvalidateVisual();
        var thumb = this.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Ellipse>().FirstOrDefault();
        if (thumb is not null) thumb.Fill = new SolidColorBrush(PaintedThumb);
        var content = this.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault();
        if (content is not null) content.Foreground = new SolidColorBrush(PaintedIcon);
    }

    public override void Render(DrawingContext context)
    {
        // A Border's inset background is not equivalent when the outline is transparent.
        context.DrawRectangle(new SolidColorBrush(PaintedTrack), null, new Rect(0, 0, 52, 32), 16, 16);
    }

    private sealed class Outline(SwitchTrack owner) : Control
    {
        public override void Render(DrawingContext context) =>
            context.DrawRectangle(null, new Pen(new SolidColorBrush(owner.PaintedBorder), 2),
                new Rect(1, 1, 50, 30), 15, 15);
    }
}
