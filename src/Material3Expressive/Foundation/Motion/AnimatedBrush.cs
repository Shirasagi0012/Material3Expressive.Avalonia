// The ListItem consumer follows AndroidX ff0a0a99 / Material3 1.5.0-alpha25
// ListItem.kt:1492-1555, where one DefaultEffects color spring drives every resolved list-item
// color. Compose animates Color's RGBA vector; this Avalonia helper keeps one critically damped
// spring per effective ARGB channel. Non-solid brushes deliberately snap because Compose token
// colors are solid and Avalonia has no equivalent, general gradient-vector interpolation contract.

using System;
using Avalonia.Media;

namespace Material3Expressive.Foundation;

/// <summary>
/// A retargetable solid brush animated by the component-owned <c>MotionGroup</c> clock.
/// </summary>
/// <remarks>
/// The animation interface is scalar because a <c>MotionGroup</c> only needs scheduling
/// and settling. <see cref="Value" /> exposes alpha for that interface; rendering always reads the
/// live <see cref="Brush" /> instead. An unsupported non-solid source is intentionally assigned
/// without motion instead of guessing a gradient interpolation policy.
/// </remarks>
internal sealed class AnimatedBrush : IAnimation
{
    private SpringAnimation? _alpha;
    private SpringAnimation? _red;
    private SpringAnimation? _green;
    private SpringAnimation? _blue;
    private SolidColorBrush? _solidBrush;
    private IBrush _brush;

    public AnimatedBrush(IBrush initial)
    {
        _brush = initial ?? throw new ArgumentNullException(nameof(initial));
    }

    /// <summary>The brush at the current motion checkpoint.</summary>
    public IBrush Brush => _brush;

    /// <inheritdoc />
    public double Value => _alpha?.Value ?? 0;

    /// <inheritdoc />
    public bool IsRunning =>
        (_alpha?.IsRunning ?? false) ||
        (_red?.IsRunning ?? false) ||
        (_green?.IsRunning ?? false) ||
        (_blue?.IsRunning ?? false);

    /// <summary>Retargets the brush using the supplied effects spring.</summary>
    public void AnimateTo(IBrush target, SpringSpec spring)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (!TryGetEffectiveColor(target, out var targetColor))
        {
            _brush = target;
            SnapChannelsToNothing();
            return;
        }

        EnsureChannels(targetColor, spring);
        _alpha!.AnimateTo(ToUnit(targetColor.A), spring);
        _red!.AnimateTo(ToUnit(targetColor.R), spring);
        _green!.AnimateTo(ToUnit(targetColor.G), spring);
        _blue!.AnimateTo(ToUnit(targetColor.B), spring);
        UpdateSolidBrush();
    }

    /// <summary>Sets a new brush without a transition.</summary>
    public void SnapTo(IBrush target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (!TryGetEffectiveColor(target, out var targetColor))
        {
            _brush = target;
            SnapChannelsToNothing();
            return;
        }

        EnsureChannels(targetColor, new SpringSpec(1.0, 1600));
        _alpha!.SnapTo(ToUnit(targetColor.A));
        _red!.SnapTo(ToUnit(targetColor.R));
        _green!.SnapTo(ToUnit(targetColor.G));
        _blue!.SnapTo(ToUnit(targetColor.B));
        UpdateSolidBrush();
    }

    /// <inheritdoc />
    public bool Tick(TimeSpan delta)
    {
        var running = false;
        if (_alpha is null)
            return false;

        running |= _alpha.Tick(delta);
        running |= _red!.Tick(delta);
        running |= _green!.Tick(delta);
        running |= _blue!.Tick(delta);
        UpdateSolidBrush();
        return running;
    }

    /// <inheritdoc />
    public void Settle()
    {
        _alpha?.Settle();
        _red?.Settle();
        _green?.Settle();
        _blue?.Settle();
        UpdateSolidBrush();
    }

    private void EnsureChannels(Color initial, SpringSpec spring)
    {
        if (_alpha is not null)
            return;

        _alpha = new SpringAnimation(ToUnit(initial.A), spring);
        _red = new SpringAnimation(ToUnit(initial.R), spring);
        _green = new SpringAnimation(ToUnit(initial.G), spring);
        _blue = new SpringAnimation(ToUnit(initial.B), spring);
        _solidBrush = new SolidColorBrush(initial);
        _brush = _solidBrush;
    }

    private void SnapChannelsToNothing()
    {
        _alpha = null;
        _red = null;
        _green = null;
        _blue = null;
        _solidBrush = null;
    }

    private void UpdateSolidBrush()
    {
        if (_solidBrush is null || _alpha is null)
            return;

        _solidBrush.Color = Color.FromArgb(
            ToByte(_alpha.Value * byte.MaxValue),
            ToByte(_red!.Value * byte.MaxValue),
            ToByte(_green!.Value * byte.MaxValue),
            ToByte(_blue!.Value * byte.MaxValue));
    }

    private static bool TryGetEffectiveColor(IBrush brush, out Color color)
    {
        if (brush is not ISolidColorBrush { Color: var source, Opacity: var opacity })
        {
            color = default;
            return false;
        }

        color = Color.FromArgb(
            ToByte(source.A * Math.Clamp(opacity, 0, 1)),
            source.R,
            source.G,
            source.B);
        return true;
    }

    private static byte ToByte(double value) =>
        (byte)Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), byte.MinValue, byte.MaxValue);

    private static double ToUnit(byte value) => value / (double)byte.MaxValue;
}
