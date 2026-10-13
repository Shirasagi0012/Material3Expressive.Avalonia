using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ComposeAnimation.Runtime;

public partial class Transition<TState>
{
    public sealed class TransitionAnimationState<TValue> : ObservableObject, ITransitionAnimationNode, IAnimationValue<TValue>, IDisposable
    {
        private readonly Transition<TState> _owner;
        private readonly AnimationVector _velocityVector;
        private readonly SpringSpec<TValue> _interruptionSpec;
        private Func<TState, TValue> _targetValueByState;
        private Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>> _transitionSpec;
        private bool _detached;

        internal TransitionAnimationState(Transition<TState> owner, ITwoWayConverter<TValue> converter,
            Func<TState, TValue> targetValueByState,
            Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>> transitionSpec, string label)
        {
            _owner = owner;
            TypeConverter = converter;
            Label = label;
            _targetValueByState = targetValueByState;
            _transitionSpec = transitionSpec;
            Value = targetValueByState(owner.CurrentState);
            TargetValue = Value;
            _velocityVector = new AnimationVector(converter.Dimension);
            _interruptionSpec = TransitionDefaults.InterruptionSpring(converter);
            AnimationSpec = new SpringSpec<TValue>();
            Animation = new TargetBasedAnimation<TValue>(AnimationSpec, converter, Value, Value,
                initialVelocityVector: _velocityVector);
        }

        public ITwoWayConverter<TValue> TypeConverter { get; }
        internal Transition<TState> Owner => _owner;
        public string Label { get; }
        public TValue Value { get; private set; }
        public TValue TargetValue { get; private set; }
        public TValue Velocity => TypeConverter.From(_velocityVector.AsReadOnlySpan());
        public ReadOnlySpan<double> VelocityVector => _velocityVector.AsReadOnlySpan();
        public IFiniteAnimationSpec<TValue> AnimationSpec { get; private set; }
        public TargetBasedAnimation<TValue> Animation { get; private set; }
        public TimeSpan Duration => Animation.Duration;
        public bool IsFinished { get; private set; } = true;
        internal bool IsDetached => _detached;

        void ITransitionAnimationNode.Reconfigure(bool reset, bool seeking, bool preserveCurrentValue)
            => Reconfigure(reset, seeking, false, default!, null, preserveCurrentValue);

        private void Reconfigure(bool reset, bool seeking, bool hasForcedValue, TValue forcedValue,
            AnimationVector? forcedVelocity, bool preserveCurrentValue = false)
        {
            if (_detached) return;
            var target = _targetValueByState(_owner.TargetState);
            if (!reset && !seeking && !hasForcedValue && EqualityComparer<TValue>.Default.Equals(TargetValue, target))
                return;
            var spec = _transitionSpec(_owner.Segment)
                ?? throw new InvalidOperationException("The transition spec factory returned null.");
            var initial = hasForcedValue
                ? forcedValue
                : seeking && !preserveCurrentValue
                    ? _targetValueByState(_owner.CurrentState)
                    : Value;
            var velocity = seeking ? new AnimationVector(TypeConverter.Dimension) : _velocityVector.Clone();
            if (hasForcedValue && forcedVelocity is not null) velocity.CopyFrom(forcedVelocity.AsReadOnlySpan());
            var interrupted = !IsFinished && !seeking && !hasForcedValue;
            IFiniteAnimationSpec<TValue> effectiveSpec = interrupted && spec is not SpringSpec<TValue>
                ? _interruptionSpec : spec;
            if (!seeking && _owner.PlayTime > TimeSpan.Zero)
                effectiveSpec = new StartDelayAnimationSpec<TValue>(effectiveSpec, _owner.PlayTime);

            var animation = new TargetBasedAnimation<TValue>(effectiveSpec, TypeConverter, initial, target,
                initialVelocityVector: velocity);
            if (animation.IsInfinite)
                throw new ArgumentException("Transition children must use finite animations.");
            AnimationSpec = spec;
            Animation = animation;
            TargetValue = target;
            _velocityVector.CopyFrom(velocity.AsReadOnlySpan());
            if (hasForcedValue || seeking) Value = initial;
            IsFinished = false;
            _owner.AnimationChanged();
            Notify(nameof(AnimationSpec));
            Notify(nameof(Animation));
            Notify(nameof(TargetValue));
            Notify(nameof(Duration));
            Notify(nameof(IsFinished));
            if (hasForcedValue) Notify(nameof(Value));
        }

        void ITransitionAnimationNode.Pulse(TimeSpan playTime, bool scaleToEnd)
            => Sample(scaleToEnd ? Duration : playTime);

        void ITransitionAnimationNode.Seek(TimeSpan playTime) => Sample(playTime);

        private void Sample(TimeSpan playTime)
        {
            if (_detached) return;
            var old = Value;
            var wasFinished = IsFinished;
            Value = Animation.GetValue(playTime);
            Animation.GetVelocityVector(playTime, _velocityVector.AsSpan());
            IsFinished = Animation.IsFinished(playTime);
            if (!EqualityComparer<TValue>.Default.Equals(old, Value)) Notify(ValueChangedArgs);
            Notify(VelocityChangedArgs);
            if (wasFinished != IsFinished) Notify(nameof(IsFinished));
        }

        // Closures are not snapshot-observed in C#; callers explicitly refresh or update mappings.
        public void Update(Func<TState, TValue> targetValueByState,
            Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>>? transitionSpec = null)
        {
            ArgumentNullException.ThrowIfNull(targetValueByState);
            VerifyAttached();
            _owner.Mutate(() =>
            {
                _targetValueByState = targetValueByState;
                if (transitionSpec is not null) _transitionSpec = transitionSpec;
                Reconfigure(false, _owner.IsSeeking, false, default!, null);
                if (_owner.IsSeeking) Sample(_owner.PlayTime);
            });
        }

        internal void UpdateForced(Func<TState, TValue> targetValueByState,
            Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>> transitionSpec,
            TValue initialValue, AnimationVector? initialVelocity)
        {
            ArgumentNullException.ThrowIfNull(targetValueByState);
            ArgumentNullException.ThrowIfNull(transitionSpec);
            VerifyAttached();
            var velocityCopy = initialVelocity?.Clone();
            _owner.Mutate(() =>
            {
                _targetValueByState = targetValueByState;
                _transitionSpec = transitionSpec;
                Reconfigure(false, _owner.IsSeeking, true, initialValue, velocityCopy);
                if (_owner.IsSeeking) Sample(_owner.PlayTime);
            });
        }

        private static readonly PropertyChangedEventArgs ValueChangedArgs = new(nameof(Value));
        private static readonly PropertyChangedEventArgs VelocityChangedArgs = new(nameof(Velocity));

        private void Notify(string name) => Notify(new PropertyChangedEventArgs(name));

        // Static callback with explicit state: steady frames publish without allocating.
        private void Notify(PropertyChangedEventArgs args) => _owner.Publish(this, args, static (s, a) =>
        {
            var state = (TransitionAnimationState<TValue>)s!;
            if (!state._detached) state.OnPropertyChanged((PropertyChangedEventArgs)a!);
        });

        private void VerifyAttached()
        {
            _owner.VerifyAccess();
            ObjectDisposedException.ThrowIf(_detached, this);
        }

        public void Dispose()
        {
            _owner.VerifyThreadAccess();
            if (_detached) return;
            _owner.Mutate(() =>
            {
                _detached = true;
                _owner.RemoveAnimation(this);
            });
        }

        void ITransitionAnimationNode.Detach() => _detached = true;
    }

    public sealed class DeferredAnimation<TValue> : IDisposable
    {
        private readonly Transition<TState> _owner;
        private TransitionAnimationState<TValue>? _animation;
        private bool _disposed;

        internal DeferredAnimation(Transition<TState> owner, ITwoWayConverter<TValue> converter, string label)
        {
            _owner = owner;
            TypeConverter = converter;
            Label = label;
        }

        public ITwoWayConverter<TValue> TypeConverter { get; }
        public string Label { get; }

        public TransitionAnimationState<TValue> Animate(
            Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>> transitionSpec,
            Func<TState, TValue> targetValueByState)
        {
            VerifyAccess();
            ArgumentNullException.ThrowIfNull(transitionSpec);
            if (_animation is null)
                _animation = _owner.AnimateValue(TypeConverter, targetValueByState, transitionSpec, Label);
            else
                _animation.Update(targetValueByState, transitionSpec);
            return _animation;
        }

        // An overload distinguishes an absent initial value from default(TValue).
        public TransitionAnimationState<TValue> Animate(
            Func<TransitionSegment<TState>, IFiniteAnimationSpec<TValue>> transitionSpec,
            TValue forcedInitialValue, AnimationVector? forcedInitialVelocity,
            Func<TState, TValue> targetValueByState)
        {
            var animation = Animate(transitionSpec, targetValueByState);
            animation.UpdateForced(targetValueByState, transitionSpec, forcedInitialValue, forcedInitialVelocity);
            return animation;
        }

        private void VerifyAccess()
        {
            _owner.VerifyAccess();
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public void Dispose()
        {
            _owner.VerifyThreadAccess();
            if (_disposed) return;
            _disposed = true;
            _animation?.Dispose();
        }
    }
}

internal static class TransitionDefaults
{
    public static SpringSpec<T> InterruptionSpring<T>(ITwoWayConverter<T> converter)
        => new(visibilityThreshold: VisibilityThresholds.For(converter));
}
