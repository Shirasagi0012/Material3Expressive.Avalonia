using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A Material 3 radio button.
/// </summary>
/// <remarks>
/// Derives from Avalonia's <see cref="RadioButton" />, so grouping and mutual exclusion are the
/// stock behaviour. The dot scales in with a spring rather than appearing: Material animates the
/// selection, and at this size a snap reads as a glitch.
/// </remarks>
public class MaterialRadioButton : RadioButton, IMaterialThemeConsumer, IMaterialInteractionConsumer
{
    private readonly MaterialThemeController _theme;
    private readonly MaterialInteractionController _interaction;
    private MaterialInteractionPresentationLease? _presentation;

    private MaterialInteractionPresentationLease InteractionPresentation =>
        _presentation is { IsDisposed: false } existing
            ? existing
            : _presentation = _interaction.AttachPresentation(this);

    private SpringAnimation? _dot;
    private RadioButtonMetrics _tokens = RadioButtonDefaults.Metrics();
    private RadioButtonColors _colors = RadioButtonDefaults.Colors(ResolvedTheme.Default, isDark: false);

    public MaterialRadioButton()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _interaction = MaterialElementContext.Ensure(this).Interaction;
        _interaction.IsRippleBounded = false;

        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialInteractionConsumer.OnMaterialInteractionChanged(InteractionState state) =>
        InvalidateVisual();

    /// <summary>The state-layer circle, which is also the ring's alignment box.</summary>
    protected Rect GlyphArea
    {
        get
        {
            var size = _tokens.StateLayerSize;
            return new Rect(0, (Bounds.Height - size) / 2, size, size);
        }
    }

    private SpringAnimation Dot =>
        _dot ??= _interaction.Motion.AddSpring(
            IsChecked == true ? 1 : 0,
            _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));

    public override void Render(DrawingContext context)
    {
        var area = GlyphArea;
        InteractionPresentation.UpdateGeometry(
            new MaterialInteractionGeometry(area, null, null));

        var enabled = IsEffectivelyEnabled;
        var selected = IsChecked == true;

        var color = enabled
            ? selected ? _colors.SelectedColor : _colors.UnselectedColor
            : selected ? _colors.DisabledSelectedColor : _colors.DisabledUnselectedColor;

        var centre = area.Center;
        var ringRadius = (_tokens.IconSize - _tokens.RingWidth) / 2;
        context.DrawEllipse(null, new ImmutablePen(color, _tokens.RingWidth), centre, ringRadius, ringRadius);

        var scale = Dot.Value;
        if (scale > 0)
        {
            var dotRadius = _tokens.DotSize / 2 * scale;
            context.DrawEllipse(color, null, centre, dotRadius, dotRadius);
        }

        base.Render(context);
        _interaction.DrawLayers(context, color, new CornerRadius(_tokens.StateLayerSize / 2));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = base.MeasureOverride(availableSize);
        return new Size(content.Width, Math.Max(content.Height, _tokens.StateLayerSize));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsEffectivelyEnabledProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == IsCheckedProperty)
        {
            Dot.AnimateTo(IsChecked == true ? 1 : 0, _theme.Current.Theme.Motion.Get(MotionRole.FastSpatial));
            _interaction.Motion.RequestFrame();
            InvalidateVisual();
        }
    }

    private void ApplyTokens()
    {
        _tokens = RadioButtonDefaults.Metrics();
        _colors = RadioButtonDefaults.Colors(_theme.Current.Theme, _theme.Current.IsDark);
        _interaction.RippleRadius = _tokens.StateLayerSize / 2;

        Padding = new Thickness(_tokens.StateLayerSize, 0, 0, 0);
        InvalidateMeasure();
        InvalidateVisual();
    }
}
