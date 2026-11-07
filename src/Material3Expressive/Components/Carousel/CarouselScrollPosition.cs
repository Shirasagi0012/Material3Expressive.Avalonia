using CommunityToolkit.Mvvm.ComponentModel;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

// PagerState.performScroll: physical integer positions and a remembered fractional accumulator.
// This is the carousel's scroll consumption state, not an additional animation engine.
internal sealed class CarouselScrollPosition : ObservableObject, IAnimationValue<double>
{
    private float _position, _accumulator, _density = 1;
    public double Value => (double)(_position / _density);
    internal float Position => _position;
    internal bool LastScrolledForward { get; private set; }
    internal void Rebase(double offset, float density)
    {
        if (_density != density) _accumulator = 0;
        _density = density;
        _position = CarouselStrategy.Round((float)offset * density);
        OnPropertyChanged(nameof(Value));
    }
    internal float Consume(float delta, float maximum, CarouselPanel? panel = null)
    {
        var accumulation = delta + _accumulator;
        var integer = CarouselStrategy.Round(accumulation);
        _accumulator = accumulation - integer;
        if (MathF.Abs(delta) < 1e-4f) return delta;
        var updated = _position + integer;
        var coerced = Math.Clamp(updated, 0, maximum);
        var consumed = coerced - _position;
        var changed = updated != coerced;
        if (panel is not null) (coerced, consumed, changed) = panel.ApplyScroll(_position, integer);
        // PagerState remembers direction only when physical integer scrolling occurs.
        // Subpixel spring corrections must not reverse a bring-into-view settling target.
        if (consumed != 0) LastScrolledForward = consumed > 0;
        _position = coerced;
        OnPropertyChanged(nameof(Value));
        return changed ? consumed : delta;
    }
}
