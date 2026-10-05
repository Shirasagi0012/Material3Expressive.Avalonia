using Avalonia;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

// ToggleButton.kt animateBorderStrokeAsState: width FastSpatial, solid ink DefaultEffects.
internal sealed class AnimatedButtonBorder : IDisposable
{
    private readonly MaterialSurface _surface;
    private readonly SurfaceOutline _outline;
    private readonly VisualAnimationScope _motion;
    private readonly Animatable<float> _width;
    private readonly Animatable<Color> _color;
    private bool _pending;

    internal AnimatedButtonBorder(MaterialSurface surface, SurfaceOutline outline, VisualAnimationScope parent)
    {
        _surface = surface; _outline = outline; _motion = parent.CreateChild("Toggle border");
        _width = _motion.CreateAnimatable((float)surface.BorderThickness.Left);
        _color = _motion.CreateAnimatable(surface.BorderBrush is ISolidColorBrush brush ? brush.Color : default);
        Paint();
    }

    internal void Update()
    {
        if (_pending || _motion.IsEnding) return;
        _pending = true;
        _ = _motion.Launch(async ct =>
        {
            await _motion.Clock.NextFrameAsync(ct);
            _pending = false;
            if (ButtonSurfaceState.GetBorderWidthAnimation(_surface) is not { } widthSpec
                || ButtonSurfaceState.GetBorderColorAnimation(_surface) is not { } colorSpec
                || !(widthSpec.Stiffness > 0) || !(colorSpec.Stiffness > 0)) return;
            var width = (float)_surface.BorderThickness.Left;
            var color = _surface.BorderBrush is ISolidColorBrush brush ? brush.Color : default;
            if (_width.TargetValue != width)
                // Spring<Dp> without an explicit threshold uses Dp.VisibilityThreshold (0.4).
                _ = _motion.Launch(c => _width.AnimateToAsync(width, new ComposeAnimation.Runtime.SpringSpec<float>(widthSpec.DampingRatio, widthSpec.Stiffness, .4f), block: _ => Paint(), cancellationToken: c), "Border width");
            if (_color.TargetValue != color)
                _ = _motion.Launch(c => _color.AnimateToAsync(color, new ComposeAnimation.Runtime.SpringSpec<Color>(colorSpec.DampingRatio, colorSpec.Stiffness), block: _ => Paint(), cancellationToken: c), "Border ink");
            Paint();
        }, "Border composition");
    }

    private void Paint()
    {
        // Keep the animated Dp value continuous; SurfaceOutline performs Border.kt's
        // physical ceil, hairline and half-min-dimension cap against its actual bounds.
        var width = Math.Max(0, _width.Value);
        _outline.UseHairline = true;
        _outline.BorderThickness = new Thickness(width);
        _surface.UpdateOutlineCornerRadius();
        _outline.BorderBrush = _surface.BorderBrush is ISolidColorBrush or null ? new SolidColorBrush(_color.Value) : _surface.BorderBrush;
    }
    public void Dispose() => _motion.Dispose();
}
