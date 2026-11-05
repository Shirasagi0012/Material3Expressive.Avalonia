using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class FrameCommittedBindingTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task BatchCapturesBeforeEffectsRegardlessOfRegistrationOrder(bool effectFirst) => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        var source = Animatable.Create(0f);
        var target = new Border { Opacity = .25 };
        using var binding = scope.BindFrameCommittedValue(target, Visual.OpacityProperty, source, value => (double)value);
        void Effect() => _ = source.SnapToAsync(.75f);
        if (effectFirst) scope.ScheduleFrameCommit(Effect);
        await source.SnapToAsync(.5f);
        if (!effectFirst) scope.ScheduleFrameCommit(Effect);
        Assert.Equal(0, target.Opacity);
        Send(clock);
        Assert.Equal(.75f, source.Value);
        Assert.Equal(.5, target.Opacity);
        Send(clock);
        Assert.Equal(.75, target.Opacity);
        Assert.Equal(0, clock.PendingFrameCount);
        await scope.Diagnostics.EndAsync();
        Assert.Equal(.25, target.Opacity);
        window.Close();
    });

    [Fact]
    public Task SamplingJoinsCurrentFrameAndReentrantEffectWaitsForNextBatch() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        var source = Animatable.Create(0d);
        var target = new Border();
        scope.BindFrameCommittedValue(target, Visual.OpacityProperty, source);
        var calls = 0;
        var run = scope.Launch(async ct =>
        {
            await scope.NextFrameAsync(ct);
            await source.SnapToAsync(.5);
            scope.ScheduleFrameCommit(() =>
            {
                calls++;
                scope.ScheduleFrameCommit(() => calls++);
            });
        });
        Send(clock); await run;
        Assert.Equal(.5, target.Opacity);
        Assert.Equal(1, calls);
        Send(clock);
        Assert.Equal(2, calls);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task CancelAndScopeEndRestoreLiveBindingWithoutPendingWrites() => OnUi(async () =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        var source = Animatable.Create(.5);
        var underlying = new Border { Opacity = .25 };
        var target = new Border();
        using var original = target.Bind(Visual.OpacityProperty, underlying.GetObservable(Visual.OpacityProperty));
        var binding = scope.BindFrameCommittedValue(target, Visual.OpacityProperty, source);
        var calls = 0;
        var canceled = scope.ScheduleFrameCommit(() => calls++);
        canceled.Dispose();
        await source.SnapToAsync(.75);
        underlying.Opacity = .125;
        binding.Dispose();
        Assert.Equal(.125, target.Opacity);
        Assert.Equal(0, clock.PendingFrameCount);
        scope.BindFrameCommittedValue(target, Visual.OpacityProperty, source);
        scope.ScheduleFrameCommit(() => calls++);
        await source.SnapToAsync(1);
        await scope.Diagnostics.EndAsync();
        Send(clock);
        Assert.Equal(0, calls);
        Assert.Equal(.125, target.Opacity);
        Assert.Equal(0, scope.Diagnostics.BindingCount);
        Assert.Equal(0, scope.Diagnostics.SubscriptionCount);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    });

    [Fact]
    public Task ThrowingEffectIsReportedAndDoesNotLoseOtherCapturedValues() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        var source = new Sample();
        var target = new Border();
        var error = new InvalidOperationException("commit");
        var failures = new List<AnimationFailureEventArgs>();
        window.AnimationRoot.AnimationFailed += (_, e) => failures.Add(e);
        scope.ScheduleFrameCommit(() => throw error);
        scope.BindFrameCommittedValue(target, Visual.OpacityProperty, source);
        source.Set(.5);
        Send(clock);
        Assert.Equal(.5, target.Opacity);
        Assert.Same(error, Assert.Single(failures).Exception);
        source.Set(.75); Send(clock);
        Assert.Equal(.75, target.Opacity);
        window.Close();
    });

    private static void Send(ManualFrameSource clock)
    {
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public Task FailedProjectionRestoresItsPropertyWithoutRejectingOtherConsumers() => OnUi(() =>
    {
        using var clock = new ManualFrameSource();
        var window = new AnimationTestWindow(clock);
        var scope = window.AnimationRoot.CreateChild();
        var source = new Sample();
        var target = new Border { Opacity = .25 };
        var other = new Border();
        var error = new InvalidOperationException("projection");
        var failures = new List<AnimationFailureEventArgs>();
        window.AnimationRoot.AnimationFailed += (_, e) => failures.Add(e);
        scope.BindFrameCommittedValue(target, Visual.OpacityProperty, source,
            value => value > 0 ? throw error : value);
        scope.BindFrameCommittedValue(other, Visual.OpacityProperty, source);
        var calls = 0;
        scope.ScheduleFrameCommit(() => calls++);
        source.Set(.5); Send(clock);
        Assert.Equal(.25, target.Opacity);
        Assert.Equal(.5, other.Opacity);
        Assert.Equal(1, calls);
        Assert.Same(error, Assert.Single(failures).Exception);
        source.Set(.75); Send(clock);
        Assert.Equal(.75, other.Opacity);
        Assert.Single(failures);
        Assert.Equal(1, scope.Diagnostics.BindingCount);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    });

    private sealed class Sample : IAnimationValue<double>
    {
        public double Value { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public void Set(double value)
        {
            Value = value;
            PropertyChanged?.Invoke(this, new(nameof(Value)));
        }
    }
}
