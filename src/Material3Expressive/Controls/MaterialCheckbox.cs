using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

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
        _interaction = new MaterialInteractionHost(this);

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

    /// <summary>The state-layer circle, which is also the box's alignment box.</summary>
    protected Rect GlyphArea
    {
        get
        {
            var size = _tokens.StateLayerSize;
            return new Rect(0, (Bounds.Height - size) / 2, size, size);
        }
    }

    public override void Render(DrawingContext context)
    {
        var area = GlyphArea;
        _interaction.InteractionArea = area;

        var enabled = IsEffectivelyEnabled;
        var checkedState = IsChecked;

        _interaction.DrawLayers(
            context,
            checkedState == true ? _colors.SelectedStateLayerColor : _colors.UnselectedStateLayerColor,
            new CornerRadius(_tokens.StateLayerSize / 2));

        var boxSize = _tokens.ContainerSize;
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
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = base.MeasureOverride(availableSize);
        return new Size(content.Width, System.Math.Max(content.Height, _tokens.StateLayerSize));
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

        Padding = new Thickness(_tokens.StateLayerSize, 0, 0, 0);
        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>
    /// The checkmark, as a two-segment stroke in fractions of the box.
    /// </summary>
    /// <remarks>
    /// Upstream draws this from a path in <c>Checkbox.kt</c> rather than from a token. The
    /// proportions here are that path's, expressed as fractions so the mark scales with a retuned
    /// container size.
    /// </remarks>
    private void DrawTick(DrawingContext context, Rect box, IImmutableBrush brush)
    {
        var pen = new ImmutablePen(brush, box.Width * 0.13, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        var start = new Point(box.X + box.Width * 0.22, box.Y + box.Height * 0.52);
        var corner = new Point(box.X + box.Width * 0.42, box.Y + box.Height * 0.72);
        var end = new Point(box.X + box.Width * 0.78, box.Y + box.Height * 0.30);

        context.DrawLine(pen, start, corner);
        context.DrawLine(pen, corner, end);
    }

    /// <summary>The indeterminate state is a single horizontal bar.</summary>
    private void DrawIndeterminate(DrawingContext context, Rect box, IImmutableBrush brush)
    {
        var pen = new ImmutablePen(brush, box.Width * 0.13, lineCap: PenLineCap.Round);

        context.DrawLine(
            pen,
            new Point(box.X + box.Width * 0.22, box.Center.Y),
            new Point(box.X + box.Width * 0.78, box.Center.Y));
    }
}
