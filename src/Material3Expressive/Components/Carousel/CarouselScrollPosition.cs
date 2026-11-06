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
    internal void Rebase(double offset, float density)
    {
        if (_density != density) _accumulator = 0;
        _density = density;
        _position = CarouselStrategy.Round((float)offset * density);
        OnPropertyChanged(nameof(Value));
    }
    internal float Consume(float delta, float maximum)
    {
        var accumulation = delta + _accumulator;
        var integer = CarouselStrategy.Round(accumulation);
        _accumulator = accumulation - integer;
        if (MathF.Abs(delta) < 1e-4f) return delta;
        var updated = _position + integer;
        var coerced = Math.Clamp(updated, 0, maximum);
        var consumed = coerced - _position;
        _position = coerced;
        OnPropertyChanged(nameof(Value));
        return updated != coerced ? consumed : delta;
    }
}
