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
    private readonly Animatable<float> _value;
    private FloatingActionButtonElevation _values;
    private InteractionState _last, _target;
    private bool _pending;
    private FloatingActionButtonElevation _nextValues;

    internal FloatingActionButtonElevationController(MaterialSurface surface, AnimationScope parent, FloatingActionButtonElevation values, InteractionState interaction)
    {
        _values = _nextValues = values;
        _scope = parent.CreateChild("FAB elevation");
        // Dp.VectorConverter retains Float samples and endpoints for interruption.
        // Widen only at the Avalonia sink; FloatConverter also selects Float tween arithmetic.
        _value = new Animatable<float>(new FloatConverter(), (float)values.Default, VisibilityThresholds.Float);
        _scope.BindFrameCommittedValue(surface, MaterialSurface.ShadowElevationProperty, _value, static value => (double)value);
        UpdateInteraction(interaction);
    }
    private static float Target(FloatingActionButtonElevation values, InteractionState state) => (float)(state switch
    {
        InteractionState.Pressed => values.Pressed, InteractionState.Hovered => values.Hovered,
        InteractionState.Focused => values.Focused, _ => values.Default
    });
    internal void UpdateConfiguration(FloatingActionButtonElevation values)
    {
        _nextValues = values;
        if (_pending || _values == values || _scope.Lifetime.IsCancellationRequested) return;
        _pending = true;
        _scope.ScheduleFrameCommit(() =>
        {
            // LaunchedEffect(elevation) updates configuration during composition. Changing
            // an unused state's height must not snap or cancel the current animation.
            _pending = false;
            _values = _nextValues;
            var target = Target(_values, _target);
            if (_value.TargetValue != target)
            {
                _ = _scope.Launch(async ct =>
                {
                    try { await _value.SnapToAsync(target, ct); }
                    finally { _last = _target; }
                }, "FAB elevation configuration");
            }
        });
    }
    internal void UpdateInteraction(InteractionState interaction)
    {
        if (_scope.Lifetime.IsCancellationRequested) return;
        // The interaction collector launches directly, independently of the configuration
        // effect. It also observes overlapping events whose target height has not changed.
        _ = _scope.Launch(async ct =>
        {
            var target = Target(_values, interaction);
            var from = _last;
            _target = interaction;
            try
            {
                if (_value.TargetValue != target)
                {
                    if (from == InteractionState.None && interaction == InteractionState.None)
                        await _value.SnapToAsync(target, ct);
                    else
                        await _value.AnimateToAsync(_scope, target,
                            new TweenSpec<float>(TimeSpan.FromMilliseconds(interaction != InteractionState.None || from == InteractionState.Hovered ? 120 : 150),
                                easing: interaction != InteractionState.None ? ComposeEasings.FastOutSlowIn : new CubicBezierEasing(0.4, 0, 0.6, 1)), cancellationToken: ct);
                }
            }
            finally { _last = interaction; }
        }, "FAB elevation effect");
    }
    public void Dispose() => _scope.Dispose();
}
