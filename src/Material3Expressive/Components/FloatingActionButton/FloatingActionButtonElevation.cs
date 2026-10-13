using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

public sealed record FloatingActionButtonElevation(double Default, double Pressed, double Focused, double Hovered)
{
    public static IMultiValueConverter FromValues { get; } = new ElevationConverter();
    private sealed class ElevationConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [double rest, double pressed, double focused, double hovered]
                ? new FloatingActionButtonElevation(rest, pressed, focused, hovered) : AvaloniaProperty.UnsetValue;
    }
}

// FloatingActionButtonElevationAnimatable retains the last actual interaction even when
// its elevation equals another state's height. Button's target-height inference is different.
internal sealed class FloatingActionButtonElevationController : IDisposable
{
    private readonly AnimationChildScope _scope;
    private readonly Animatable<double> _value;
    private FloatingActionButtonElevation _values;
    private InteractionState _last, _target;
    private bool _pending;
    private (FloatingActionButtonElevation Values, InteractionState Interaction) _next;

    internal FloatingActionButtonElevationController(MaterialSurface surface, AnimationScope parent, FloatingActionButtonElevation values, InteractionState interaction)
    {
        _values = values;
        _scope = parent.CreateChild("FAB elevation");
        _value = new Animatable<double>(new DoubleConverter(), values.Default, VisibilityThresholds.Double);
        _scope.BindAnimatedValue(surface, MaterialSurface.ShadowElevationProperty, _value);
        Update(values, interaction);
    }
    private static double Target(FloatingActionButtonElevation values, InteractionState state) => (state switch
    {
        InteractionState.Pressed => values.Pressed, InteractionState.Hovered => values.Hovered,
        InteractionState.Focused => values.Focused, _ => values.Default
    });
    internal void Update(FloatingActionButtonElevation values, InteractionState interaction)
    {
        _next = (values, interaction);
        if (_pending || _scope.Lifetime.IsCancellationRequested) return;
        _pending = true;
        _ = _scope.Launch(async ct =>
        {
            await _scope.NextFrameAsync(ct);
            _pending = false;
            var (configuration, state) = _next;
            if (_values != configuration)
            {
                _values = configuration;
                // LaunchedEffect(elevation) updates and snaps the current target first.
                await _value.SnapToAsync(Target(_values, _target), ct);
                _last = _target;
            }
            var target = Target(_values, state);
            var from = _last;
            _target = state;
            try
            {
                if (_value.TargetValue != target)
                {
                    if (from == InteractionState.None && state == InteractionState.None)
                        await _value.SnapToAsync(target, ct);
                    else
                        await _value.AnimateToAsync(_scope, target,
                            new TweenSpec<double>(TimeSpan.FromMilliseconds(state != InteractionState.None || from == InteractionState.Hovered ? 120 : 150),
                                easing: state != InteractionState.None ? ComposeEasings.FastOutSlowIn : new CubicBezierEasing(0.4, 0, 0.6, 1)), cancellationToken: ct);
                }
            }
            finally { _last = state; }
        }, "FAB elevation effect");
    }
    public void Dispose() => _scope.Dispose();
}
