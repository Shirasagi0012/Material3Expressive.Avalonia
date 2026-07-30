using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

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
public class MaterialCheckbox : CheckBox
{
    // The pinned Compose reference is Material3 1.5.0-alpha22. In that release Checkbox.kt's
    // compatibility path uses CheckboxSize (20dp) and CheckboxDefaultPadding (2dp), rather than
    // the newer CheckboxTokens.ContainerSize (18dp) path. Keep this separate from the generated
    // current token table so moving the reference lineage is an explicit compatibility decision.
    private const double ReferenceContainerSize = 20;

    // androidx.compose.foundation.layout.minimumInteractiveComponentSize() supplies the standard
    // 48dp touch target around Checkbox's 20dp canvas. The 40dp state layer remains centred inside
    // this larger layout slot.
    private const double MinimumInteractiveComponentSize = 48;

    /// <summary>Puts the checkbox into its error appearance.</summary>
    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<MaterialCheckbox, bool>(nameof(IsError));

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;

    private CheckboxTokens _tokens = CheckboxTokens.Default;
    private CheckboxColors _colors = CheckboxTokens.Default.Resolve(null, false);

    public MaterialCheckbox()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this) { IsRippleBounded = false };

        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => InvalidateVisual();

        ApplyTokens();
    }

    static MaterialCheckbox()
    {
        AffectsRender<MaterialCheckbox>(IsErrorProperty, IsCheckedProperty);
    }

    /// <inheritdoc cref="IsErrorProperty" />
    public bool IsError
    {
        get => GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    /// <summary>The 40dp state layer centred inside the leading 48dp interactive layout slot.</summary>
    protected Rect GlyphArea
    {
        get
        {
            var size = _tokens.StateLayerSize;
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
        _interaction.InteractionArea = area;

        var enabled = IsEffectivelyEnabled;
        var checkedState = IsChecked;

        var boxSize = ReferenceContainerSize;
        var box = new Rect(
            area.X + (area.Width - boxSize) / 2,
            area.Y + (area.Height - boxSize) / 2,
            boxSize,
            boxSize);
        var corners = new CornerRadius(_tokens.ContainerCornerRadius);

        if (checkedState == false)
        {
            var outline = !enabled ? _colors.DisabledOutlineColor
                : IsError ? _colors.ErrorOutlineColor
                : _colors.UnselectedOutlineColor;

            // The border is inside the box, so inset by half its width.
            var inset = _tokens.OutlineWidth / 2;
            context.DrawRectangle(
                null,
                new ImmutablePen(outline, _tokens.OutlineWidth),
                new RoundedRect(box.Deflate(inset), MaterialContainer.Deflate(corners, inset)));
        }
        else
        {
            var fill = !enabled ? _colors.DisabledSelectedContainerColor
                : IsError ? _colors.ErrorContainerColor
                : _colors.SelectedContainerColor;

            var mark = !enabled ? _colors.DisabledSelectedIconColor
                : IsError ? _colors.ErrorIconColor
                : _colors.SelectedIconColor;

            context.DrawRectangle(fill, null, new RoundedRect(box, corners));

            if (checkedState == true)
                DrawTick(context, box, mark);
            else
                DrawIndeterminate(context, box, mark);
        }

        base.Render(context);
        _interaction.DrawLayers(
            context,
            checkedState == true ? _colors.SelectedStateLayerColor : _colors.UnselectedStateLayerColor,
            new CornerRadius(_tokens.StateLayerSize / 2));
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

        if (change.Property == IsEffectivelyEnabledProperty)
            ApplyTokens();
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.CheckboxTokens;
        _colors = _theme.Theme.GetCheckboxColors(_theme.IsDark);
        _interaction.RippleRadius = _tokens.StateLayerSize / 2;

        // Compose keeps a fixed 48dp interactive checkbox slot ahead of an optional label.
        // Centre the 40dp state layer inside that slot, then start the content after the slot.
        Padding = new Thickness(MinimumInteractiveComponentSize, 0, 0, 0);
        Foreground = new SolidColorBrush(
            _theme.Theme.ColorScheme?.GetColor(SysColorToken.OnSurface, _theme.IsDark) ?? Colors.Black);
        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>
    /// The alpha22 checkmark, as a two-segment 2dp square-capped stroke in fractions of the box.
    /// </summary>
    /// <remarks>
    /// Upstream's compatibility path in <c>Checkbox.kt</c> uses (0.2, 0.5), (0.4, 0.7), and
    /// (0.8, 0.3) with <c>CheckboxDefaults.StrokeWidth</c> (2dp). Those values differ from the
    /// newer checkbox-styling-fix path, so retain the pinned reference geometry exactly.
    /// </remarks>
    private void DrawTick(DrawingContext context, Rect box, IImmutableBrush brush)
    {
        var pen = new ImmutablePen(
            brush,
            2,
            lineCap: PenLineCap.Square,
            lineJoin: PenLineJoin.Miter);

        var start = new Point(box.X + box.Width * 0.20, box.Y + box.Height * 0.50);
        var corner = new Point(box.X + box.Width * 0.40, box.Y + box.Height * 0.70);
        var end = new Point(box.X + box.Width * 0.80, box.Y + box.Height * 0.30);

        // Compose strokes one continuous Path. Drawing the two segments independently gives the
        // shared corner two square caps instead of the path's miter join.
        var geometry = new StreamGeometry();
        using (var sink = geometry.Open())
        {
            sink.BeginFigure(start, false);
            sink.LineTo(corner);
            sink.LineTo(end);
            sink.EndFigure(false);
        }
        context.DrawGeometry(null, pen, geometry);
    }

    /// <summary>
    /// The indeterminate state is a single horizontal bar. Compose first creates its 20%–80%
    /// path, then its 2dp stroke expands the visible endpoints by 1dp; capture therefore observes
    /// a 15%–85% filled segment at the settled state.
    /// </summary>
    private void DrawIndeterminate(DrawingContext context, Rect box, IImmutableBrush brush)
    {
        context.FillRectangle(
            brush,
            new Rect(box.X + box.Width * 0.15, box.Center.Y - 1, box.Width * 0.70, 2));
    }
}
